using System.Text.Json;

namespace CommunityThemes;

internal sealed record StudioValues(string Name, string Guild, string Preset, string Palette, string Accent, string Surface, string Font, string Banner, string Background)
{
    public static StudioValues From(ThemeConfiguration c) => new(c.Name, c.Guild, c.Preset, c.Palette, c.Accent, c.Surface, c.Font, c.Banner, c.Background);
}

internal sealed class BuilderForm : Form
{
    private readonly bool previewMode;
    private readonly DiscordClient client = new();
    private readonly ThemeStore store;
    private readonly StudioSurface surface = new();
    private ThemeConfiguration configuration = new();
    private int revision, renderedRevision = -1;
    private bool busy, dirty, acknowledged, install, saved;
    private string? documentPath;
    private string notice = "Design here. Preview here. Apply when you're ready.", validation = "";
    private string DraftPath => Path.Combine(client.StateRoot, "last-configuration.dtheme");

    public BuilderForm(bool preview)
    {
        previewMode = preview;
        store = new ThemeStore(client.VencordRoot, client.StateRoot, ThemeCompiler.BundledCatalog);
        BackColor = LauncherStyle.Surface; ForeColor = LauncherStyle.Text; Font = LauncherStyle.Font(); AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Theme Studio · Discord Theme Launcher — " + ThemeCompiler.BundledCatalog.Version + (preview ? " · Preview" : "");
        ClientSize = new Size(1420, 900); MinimumSize = new Size(1020, 700); StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(surface);
        surface.Loaded += Publish;
        surface.MessageReceived += async message => await HandleMessageAsync(message);
        surface.RuntimeUnavailable += ShowRuntimeFallback;
        if (preview) configuration = new ThemeConfiguration { Name = "Your community", Guild = "987654321012345678" };
        else if (File.Exists(DraftPath))
        {
            try { var draft = ThemeConfiguration.Load(DraftPath); AssetImport.ValidateImages(draft); configuration = draft; saved = true; }
            catch { notice = "The last configuration could not be reopened. Open another .dtheme file or start a new one."; }
        }
        FormClosing += (_, e) => { if (busy || !ConfirmDiscard()) e.Cancel = true; };
        Load += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width - 16), Math.Min(MinimumSize.Height, area.Height - 16));
            Size = new Size(Math.Min(Width, area.Width - 16), Math.Min(Height, area.Height - 16));
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        };
        Shown += async (_, _) => { LauncherStyle.DarkCaption(this); await surface.InitializeAsync(); };
        RenderPreview();
    }

    private ThemeConfiguration ReadConfiguration()
    {
        var value = configuration with { Name = configuration.Name.Trim(), Guild = ThemeConfiguration.ParseGuild(configuration.Guild),
            CustomFont = configuration.Font == "custom" ? configuration.CustomFont : null,
            CustomBanner = configuration.Banner == "custom" ? configuration.CustomBanner : null,
            CustomBackground = configuration.Background == "custom" ? configuration.CustomBackground : null };
        value.Validate(); return value;
    }
    private void ShowRuntimeFallback()
    {
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 70, Padding = new Padding(30, 8, 0, 0), BackColor = LauncherStyle.Surface };
        foreach (var (label, action) in new (string, Action)[] { ("Open configuration", OpenDocument), ("Save a copy", () => SaveDocument(true)), ("Help", Help) })
        {
            var button = new DiscordButton { Text = label, AutoSize = true, Padding = new Padding(12, 7, 12, 7) };
            button.Click += (_, _) => { try { action(); } catch { LauncherStyle.ShowMessage(this, "The configuration could not be processed. Check the format, size and permissions.", "Configuration action"); } };
            actions.Controls.Add(button);
        }
        surface.Controls.Add(actions); actions.BringToFront();
    }
    private void Changed() { revision++; dirty = true; RenderPreview(); }
    private void RenderPreview()
    {
        try { var value = ReadConfiguration(); validation = ""; renderedRevision = revision; surface.Render(value); }
        catch (InvalidDataException error) { validation = error.Message; renderedRevision = -1; }
        Publish();
    }
    private void Publish()
    {
        bool canDisable = false, canRestore = false, healthy = true;
        try { if (!previewMode && !DiscordClient.Elevated) { canDisable = store.PersonalEnabled; canRestore = store.CanRestore; } }
        catch { healthy = false; notice = "The Vencord profile could not be read. Check the manual recovery guide."; }
        var patched = client.Patched;
        surface.Publish(new { type = "state", values = StudioValues.From(configuration), assets = new { font = configuration.CustomFont?.Name, banner = configuration.CustomBanner?.Name, background = configuration.CustomBackground?.Name },
            busy, dirty, saved, acknowledged, install, previewMode, version = ThemeCompiler.BundledCatalog.Version,
            notice = previewMode ? "Preview mode · explore, save and share. Discord and Vencord are never changed." : DiscordClient.Elevated ? "Run normally, without Administrator privileges." : notice,
            validation, previewId = surface.PreviewId, previewUri = validation.Length == 0 ? surface.PreviewUri : "",
            canApply = healthy && !previewMode && !busy && !DiscordClient.Elevated && acknowledged && renderedRevision == revision && surface.Ready && client.Found && (patched || install),
            canDisable = canDisable && !busy, canRestore = canRestore && !busy, canInstall = !busy && !previewMode && !patched,
            installation = patched ? "Discord + Vencord ready. No installation needed." : client.Found ? "Discord found. Enable official Vencord setup to apply themes." : "Standard Discord desktop was not found for this Windows user." });
    }

    private async Task HandleMessageAsync(JsonElement message)
    {
        string? action = message.GetProperty("action").GetString();
        if (busy && action != "previewReady") return;
        try
        {
            switch (action)
            {
                case "loaded": Publish(); return;
                case "edit":
                    var edits = message.GetProperty("values").Deserialize<StudioValues>(Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid editor values.");
                    var strings = new[] { edits.Name, edits.Guild, edits.Preset, edits.Palette, edits.Accent, edits.Surface, edits.Font, edits.Banner, edits.Background };
                    if (strings.Any(s => s is null || s.Length > 256)) throw new InvalidDataException("Editor values exceed the supported limits.");
                    configuration = configuration with { Name = edits.Name, Guild = edits.Guild, Preset = edits.Preset, Palette = edits.Palette, Accent = edits.Accent, Surface = edits.Surface, Font = edits.Font, Banner = edits.Banner, Background = edits.Background };
                    Changed(); return;
                case "preset":
                    string id = message.GetProperty("value").GetString() ?? "";
                    if (!ThemeConfiguration.Presets.Contains(id)) return;
                    configuration = configuration with { Preset = id, Palette = id, Font = id, Banner = id, Background = id }; Changed(); return;
                case "previewReady":
                    if (renderedRevision == revision) validation = surface.Ready ? "" : "The design or display font did not load. Choose another font or reload the preview.";
                    Publish(); return;
                case "refresh": RenderPreview(); return;
                case "acknowledge": acknowledged = message.GetProperty("ready").GetBoolean(); Publish(); return;
                case "installation": install = message.GetProperty("install").GetBoolean(); Publish(); return;
                case "new":
                    if (!ConfirmDiscard()) return;
                    configuration = new ThemeConfiguration { Name = "My server theme", Guild = "" }; documentPath = null; dirty = saved = false; revision++; RenderPreview(); return;
                case "open": OpenDocument(); return;
                case "save": SaveDocument(false); return;
                case "share": SaveDocument(true); return;
                case "importFont":
                    var asset = AssetImport.Font(this); if (asset is null) return;
                    configuration = configuration with { Font = "custom", CustomFont = asset }; notice = "Font imported with its redistribution license: " + asset.Name; Changed(); return;
                case "importBanner": ImportImage(true); return;
                case "importBackground": ImportImage(false); return;
                case "serverHelp": ServerHelp(); return;
                case "help": Help(); return;
                case "presets": using (var form = new LauncherForm(previewMode)) form.ShowDialog(this); Publish(); return;
                case "apply": case "disable": case "restore": await OperateAsync(action); return;
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException or InvalidOperationException or KeyNotFoundException)
        {
            notice = error is InvalidDataException ? error.Message : "The action could not finish. Check the file's format, size and permissions. Your saved configurations are safe.";
            LauncherStyle.ShowMessage(this, notice, "Could not complete this action"); Publish();
        }
    }
    private void OpenDocument()
    {
        if (!ConfirmDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = "Theme configuration (*.dtheme)|*.dtheme", Title = "Open a configuration and its assets" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var value = ThemeConfiguration.Load(dialog.FileName); AssetImport.ValidateImages(value);
        configuration = value; documentPath = dialog.FileName; dirty = false; saved = true; revision++;
        notice = "Configuration opened. Review the target server before applying."; RenderPreview();
    }
    private void SaveDocument(bool share)
    {
        var value = ReadConfiguration(); string? path = share ? null : documentPath;
        if (path is null)
        {
            using var dialog = new SaveFileDialog { Filter = "Theme configuration (*.dtheme)|*.dtheme", DefaultExt = "dtheme", AddExtension = true, FileName = "My-server-theme.dtheme", Title = share ? "Share a copy with embedded assets" : "Save your configuration" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return; path = dialog.FileName;
        }
        value.Save(path); if (!share) { documentPath = path; dirty = false; saved = true; }
        if (!previewMode) value.Save(DraftPath);
        notice = share ? "Portable copy saved with selected assets and font license. Send it to members yourself; each member applies it locally." : "Your configuration and selected assets are saved together.";
        Publish();
    }
    private void ImportImage(bool banner)
    {
        using var dialog = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg", Title = "Import an image you have permission to use and share" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var asset = AssetImport.Image(dialog.FileName);
        configuration = banner ? configuration with { Banner = "custom", CustomBanner = asset } : configuration with { Background = "custom", CustomBackground = asset };
        notice = "Imported " + asset.Name + ". The image is embedded; the original path is not shared."; Changed();
    }
    private bool ConfirmDiscard()
    {
        if (!dirty) return true;
        using var dialog = new Form { Text = "Unsaved configuration", BackColor = LauncherStyle.Surface, ForeColor = LauncherStyle.Text, Font = LauncherStyle.Font(), ClientSize = new Size(520, 180), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog };
        var text = new Label { Text = "This configuration has unsaved edits. Save it before continuing if you want to keep them.", Dock = DockStyle.Top, Height = 90, Padding = new Padding(24) };
        var row = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(24, 0, 0, 0) };
        var keep = new DiscordButton { Text = "Keep editing", Primary = true, AutoSize = true, Padding = new Padding(12, 7, 12, 7), DialogResult = DialogResult.Cancel };
        var discard = new DiscordButton { Text = "Discard edits", AutoSize = true, Padding = new Padding(12, 7, 12, 7), DialogResult = DialogResult.OK };
        row.Controls.Add(keep); row.Controls.Add(discard); dialog.Controls.Add(text); dialog.Controls.Add(row); dialog.CancelButton = keep; dialog.AcceptButton = keep; dialog.Shown += (_, _) => LauncherStyle.DarkCaption(dialog);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }
    private void ServerHelp() => LauncherStyle.ShowMessage(this, "In Discord desktop:\n1. Open User Settings > Advanced > Developer Mode.\n2. Right-click the server icon and choose Copy Server ID.\n3. Paste it into Server ID in the editor.\n\nOr right-click a text channel, choose Copy Channel Link and paste it. Invite links cannot identify the target. You must already belong to the server. No token or credentials are needed.", "Choose your server", true);
    private void Help() => LauncherStyle.ShowMessage(this, "Choose a target in Server, then explore Style and Artwork. Click a starting preset to load its coordinated font, palette and images. The preview always starts on your target. Other server and DMs show stock Discord on purpose, demonstrating that your theme stays local to the selected server.\n\nSave or Share theme writes a .dtheme file with selected assets and the custom font license. Other members must choose to adopt it themselves. One personal theme can be active at a time. Recovery offers disable, restore and the original multi-server preset manager. Applying original presets replaces the personal slot too.\n\nWindows x64 / standard stable desktop Discord / default Vencord only. Applying, disabling and restoring restart Discord only after your acknowledgement. Vencord is unofficial and has Discord terms/account implications. This independent tool is not affiliated with Discord.\n\nBundled fonts are SIL OFL 1.1. Custom static TTF/OTF imports require editable embedding and redistribution rights plus the license. PNG/JPEG images must fit 8 MiB, 4096 px per side and 16 million pixels. Configurations are limited to 24 MiB. See the repository's Installation, Compatibility and Validation documents for details.", "Theme Studio · Help", true);

    private async Task OperateAsync(string operation)
    {
        if (busy || previewMode) return;
        if (!acknowledged) { LauncherStyle.ShowMessage(this, "Finish calls and save drafts, then acknowledge the restart before applying, disabling or restoring.", "Before restarting Discord"); return; }
        bool stopped = false;
        try
        {
            ThemeConfiguration? value = operation == "apply" ? ReadConfiguration() : null;
            if (value is not null && (!surface.Ready || renderedRevision != revision)) throw new InvalidDataException("Wait for a valid preview of your latest edits before applying.");
            client.ValidateEnvironment(); bool repair = client.NeedsRepair, setup = !client.Patched;
            if (setup && !install) throw new InvalidOperationException("Choose official Vencord installation / repair in Recovery, or install it manually first.");
            busy = true; Publish(); byte[]? installer = null;
            if (setup)
            {
                notice = "Downloading and verifying the official Vencord installer…"; Publish();
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBuild-DiscordThemeLauncher/" + ThemeCompiler.BundledCatalog.Version);
                installer = await ReleaseDownload.FetchAsync(http, ReleaseDownload.InstallerUri, ReleaseDownload.InstallerSha256, 32 * 1024 * 1024);
            }
            if (value is not null) await Task.Run(() => value.Save(DraftPath));
            notice = "Closing Discord after your acknowledgement…"; Publish(); await client.StopAsync(); stopped = true;
            if (installer is not null) { notice = "Installing / repairing official Vencord…"; Publish(); await client.InstallVencordAsync(installer, repair); }
            notice = "Saving a recovery snapshot and updating managed themes…"; Publish();
            await Task.Run(() => { if (operation == "restore") store.RestoreLast(); else store.ApplyPersonal(value); });
            notice = "Opening Discord and checking startup…"; Publish(); await client.OpenAsync(); stopped = false;
            notice = operation == "apply" ? "Theme applied to server " + value!.Guild + ". Discord startup confirmed. Changes are local to you." : operation == "disable" ? "Personal theme disabled. Discord startup confirmed. Restore can undo this." : "Previous managed setup restored. Discord startup confirmed.";
        }
        catch (Exception error)
        {
            notice = error is InvalidDataException or InvalidOperationException ? error.Message : "Setup could not finish. Check the profile, disk space and manual recovery guide.";
            if (stopped) notice += " No automatic retry was made.";
            LauncherStyle.ShowMessage(this, notice, "Operation did not complete");
        }
        finally { busy = false; acknowledged = false; Publish(); }
    }
}
