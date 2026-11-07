using System.Text.Json;

namespace CommunityThemes;

internal sealed record StudioValues(string Name, string Guild, string Preset, string Palette, string Accent, string Surface, string Font, string Banner, string Background)
{
    public static StudioValues From(ThemeConfiguration c) => new(c.Name, c.Guild, c.Preset, c.Palette, c.Accent, c.Surface, c.Font, c.Banner, c.Background);
}

internal sealed class BuilderForm : Form
{
    private readonly bool dryRun;
    private readonly DiscordClient client = new();
    private readonly ThemeStore store;
    private readonly StudioSurface surface = new();
    private ThemeConfiguration configuration = new();
    private int revision, renderedRevision = -1;
    private bool busy, dirty, saved;
    private OperationConfirmation? confirmation;
    private CssImportReview? importReview;
    internal int SimulatedOperations { get; private set; }
    private string? documentPath;
    private string notice = "Design here. Preview here. Apply when you're ready.", validation = "";
    private string DraftPath => Path.Combine(client.StateRoot, "last-configuration.dtheme");

    public BuilderForm(bool safePreview)
    {
        dryRun = safePreview;
        store = new ThemeStore(client.VencordRoot, client.StateRoot, ThemeCompiler.BundledCatalog);
        BackColor = LauncherStyle.Surface; ForeColor = LauncherStyle.Text; Font = LauncherStyle.Font(); AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.Format("Theme Studio · Discord Theme Launcher — {0}", ThemeCompiler.BundledCatalog.Version);
        ClientSize = new Size(1420, 900); MinimumSize = new Size(1020, 700); StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(surface);
        surface.Loaded += Publish;
        surface.MessageReceived += async message => await HandleMessageAsync(message);
        surface.RuntimeUnavailable += ShowRuntimeFallback;
        if (dryRun) configuration = new ThemeConfiguration { Name = "Your community", Guild = "987654321012345678" };
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
        Shown += async (_, _) =>
        {
            LauncherStyle.DarkCaption(this);
            try { await surface.InitializeAsync(); }
            finally { if (!dryRun) await RepairUnbootableAsync(); }
        };
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
            var button = new DiscordButton { Text = UiText.Text(label), AutoSize = true, Padding = new Padding(12, 7, 12, 7) };
            button.Click += (_, _) => { try { action(); } catch { LauncherStyle.ShowMessage(this, UiText.Text("The configuration could not be processed. Check the format, size and permissions."), UiText.Text("Configuration action")); } };
            actions.Controls.Add(button);
        }
        surface.Controls.Add(actions); actions.BringToFront();
    }
    private void Changed() { confirmation = null; revision++; dirty = true; RenderPreview(); }
    private void RenderPreview()
    {
        try
        {
            // A synthetic target lets a new design render without choosing a real server implicitly.
            var value = configuration.Guild.Length == 0 ? configuration with { Guild = "987654321012345678" } : ReadConfiguration();
            validation = configuration.Guild.Length == 0 ? "Choose a target server before applying or saving." : "";
            renderedRevision = revision; surface.Render(value);
        }
        catch (InvalidDataException error) { validation = error.Message; renderedRevision = -1; }
        Publish();
    }
    private void Publish()
    {
        bool canDisable = false, canRestore = false;
        try { if (!dryRun) { canDisable = store.PersonalEnabled; canRestore = store.CanRestore; } }
        catch { notice = "The Vencord profile could not be read. Check the manual recovery guide."; }
        var patched = client.Patched;
        var readiness = ApplyReadiness.Evaluate(busy, validation, renderedRevision == revision && surface.Ready);
        surface.Publish(new { type = "state", values = StudioValues.From(configuration), assets = new { font = configuration.CustomFont?.Name, banner = configuration.CustomBanner?.Name, background = configuration.CustomBackground?.Name },
            busy, dirty, saved, version = ThemeCompiler.BundledCatalog.Version, notice = UiText.Text(notice),
            validation = UiText.Text(validation), previewId = surface.PreviewId, previewUri = renderedRevision == revision && (validation.Length == 0 || validation == "Choose a target server before applying or saving.") ? surface.PreviewUri : "",
            canApply = readiness.CanApply, applyReason = UiText.Text(readiness.Reason), applyAction = readiness.Action, applyActionLabel = readiness.ActionLabel.Length == 0 ? "" : UiText.Text(readiness.ActionLabel),
            canDisable = canDisable && !busy, canRestore = canRestore && !busy,
            designs = CommunityGallery.Designs,
            imported = configuration.Imported is { } imported ? ReviewState(CssThemeImporter.Inspect(imported)) : null,
            importReview = importReview is null ? null : ReviewState(importReview),
            confirmation = confirmation is { } pending ? new { pending.Id, pending.Operation, themeName = pending.Configuration?.Name,
                guild = pending.Configuration?.Guild, serverName = ThemeCompiler.BundledCatalog.Themes.FirstOrDefault(t => t.Guild == pending.Configuration?.Guild)?.Server ?? UiText.Text("Selected server"),
                pending.RequiresSetup, pending.Repair, problem = UiText.Text(pending.Problem) } : null,
            installation = UiText.Text(patched ? "Discord + Vencord ready. No installation needed." : client.Unbootable ? "Discord cannot start because Vencord's patcher.js is missing. The launcher repairs it with the official installer." : client.Found ? "Discord found. Apply will offer official Vencord setup if needed." : "Standard Discord desktop was not found for this Windows user."),
            language = UiText.Current });
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
                case "language":
                    var code = message.GetProperty("value").GetString();
                    if (!UiText.Supported(code)) return;
                    UiText.Use(code);
                    if (!dryRun) UiText.Save(client.StateRoot);
                    Text = UiText.Format("Theme Studio · Discord Theme Launcher — {0}", ThemeCompiler.BundledCatalog.Version);
                    Publish(); return;
                case "edit":
                    var edits = message.GetProperty("values").Deserialize<StudioValues>(Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid editor values.");
                    var strings = new[] { edits.Name, edits.Guild, edits.Preset, edits.Palette, edits.Accent, edits.Surface, edits.Font, edits.Banner, edits.Background };
                    if (strings.Any(s => s is null || s.Length > 256)) throw new InvalidDataException("Editor values exceed the supported limits.");
                    configuration = configuration with { Name = edits.Name, Guild = edits.Guild, Preset = edits.Preset, Palette = edits.Palette, Accent = edits.Accent, Surface = edits.Surface, Font = edits.Font, Banner = edits.Banner, Background = edits.Background };
                    Changed(); return;
                case "preset":
                    string id = message.GetProperty("value").GetString() ?? "";
                    if (!ThemeConfiguration.Presets.Contains(id)) return;
                    configuration = CommunityGallery.Create(id, configuration.Guild); Changed(); return;
                case "community":
                    if (!ConfirmDiscard()) return;
                    configuration = CommunityGallery.Create(message.GetProperty("value").GetString() ?? "", configuration.Guild);
                    documentPath = null; saved = false; Changed(); return;
                case "importCss": ReviewCss(); return;
                case "cancelImport": importReview = null; Publish(); return;
                case "acceptImport":
                    if (importReview is not { Supported: true, Theme: { } theme }) return;
                    configuration = new ThemeConfiguration { Name = theme.Name.Length > 80 ? theme.Name[..80] : theme.Name, Guild = configuration.Guild, Imported = theme };
                    importReview = null; documentPath = null; saved = false; Changed(); return;
                case "importVariable":
                    if (configuration.Imported is null) return;
                    string key = message.GetProperty("name").GetString() ?? "", edit = message.GetProperty("value").GetString() ?? "";
                    var importedTheme = configuration.Imported with { Edits = new(configuration.Imported.Edits) { [key] = edit } };
                    _ = CssThemeImporter.Inspect(importedTheme);
                    configuration = configuration with { Imported = importedTheme }; Changed(); return;
                case "browseThemes": OpenExternal("https://betterdiscord.app/themes"); return;
                case "submitTheme": OpenExternal(SubmissionDestination()); return;
                case "communitySource":
                    OpenExternal(CommunityGallery.Designs.Single(d => d.Id == message.GetProperty("value").GetString()).Source); return;
                case "communityAuthor":
                    OpenExternal(CommunityGallery.Designs.Single(d => d.Id == message.GetProperty("value").GetString()).AuthorUrl); return;
                case "previewReady":
                    if (renderedRevision == revision) validation = !surface.Ready ? "The design or display font did not load. Choose another font or reload the preview." : configuration.Guild.Length == 0 ? "Choose a target server before applying or saving." : "";
                    Publish(); return;
                case "refresh": RenderPreview(); return;
                case "cancelConfirmation":
                    if (confirmation?.Id == message.GetProperty("id").GetString()) confirmation = null;
                    Publish(); return;
                case "confirmOperation":
                    string confirmationId = message.GetProperty("id").GetString() ?? "";
                    bool allowSetup = message.TryGetProperty("allowSetup", out var setupConsent) && setupConsent.GetBoolean();
                    if (confirmation?.Id != confirmationId || confirmation.Problem.Length > 0 || confirmation.RequiresSetup && !allowSetup) return;
                    allowSetup = confirmation.RequiresSetup && allowSetup;
                    if (!OperationConfirmation.TryTake(ref confirmation, confirmationId, revision, out var confirmed)) { Publish(); return; }
                    await OperateAsync(confirmed!, allowSetup); return;
                case "new":
                    if (!ConfirmDiscard()) return;
                    confirmation = null; configuration = new ThemeConfiguration { Name = "My server theme", Guild = "" }; documentPath = null; dirty = saved = false; revision++; RenderPreview(); return;
                case "open": OpenDocument(); return;
                case "save": SaveDocument(false); return;
                case "share": SaveDocument(true); return;
                case "exportCss": ExportOriginalCss(); return;
                case "importFont":
                    var asset = AssetImport.Font(this); if (asset is null) return;
                    configuration = configuration with { Font = "custom", CustomFont = asset }; notice = "Font imported with its redistribution license: " + asset.Name; Changed(); return;
                case "importBanner": ImportImage(true); return;
                case "importBackground": ImportImage(false); return;
                case "serverHelp": ServerHelp(); return;
                case "help": Help(); return;
                case "presets": using (var form = new LauncherForm(dryRun)) form.ShowDialog(this); Publish(); return;
                case "apply": case "disable": case "restore": RequestConfirmation(action); return;
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException or InvalidOperationException or KeyNotFoundException)
        {
            notice = error is InvalidDataException ? error.Message : "The action could not finish. Check the file's format, size and permissions. Your saved configurations are safe.";
            LauncherStyle.ShowMessage(this, UiText.Text(notice), UiText.Text("Could not complete this action")); Publish();
        }
    }
    private void OpenDocument()
    {
        if (!ConfirmDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = UiText.Text("Theme configuration (*.dtheme)") + "|*.dtheme", Title = UiText.Text("Open a configuration and its assets") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var value = ThemeConfiguration.Load(dialog.FileName); AssetImport.ValidateImages(value);
        confirmation = null; configuration = value; documentPath = dialog.FileName; dirty = false; saved = true; revision++;
        notice = "Configuration opened. Review the target server before applying."; RenderPreview();
    }
    private void ReviewCss()
    {
        if (!ConfirmDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = "BetterDiscord (*.theme.css)|*.theme.css", Title = UiText.Text("Import a downloaded BetterDiscord theme") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        importReview = CssThemeImporter.ReviewFile(dialog.FileName);
        Publish();
    }
    private static object ReviewState(CssImportReview review) => new
    {
        review.Supported, name = review.Theme?.Name, author = review.Theme?.Author, source = review.Theme?.Source,
        sha256 = review.Theme?.Sha256, license = review.Theme?.License,
        notes = review.Notes.Select(UiText.Text), resources = review.Resources.Select(r => r.Replace("bytes · embedded", UiText.Text("bytes · embedded")).Replace("reviewed bundled font", UiText.Text("reviewed bundled font"))),
        variables = review.Variables.Select(v => new { v.Name, value = review.Theme?.Edits.GetValueOrDefault(v.Name) ?? v.Value, v.Kind })
    };
    private static void OpenExternal(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidDataException("Use an HTTPS destination without credentials.");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
    private static string SubmissionDestination()
    {
        var configured = Environment.GetEnvironmentVariable("THEME_STUDIO_SUBMISSION_DISCORD");
        if (Uri.TryCreate(configured, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.UserInfo.Length == 0 &&
            (uri.Host == "discord.gg" || uri.Host == "discord.com" && uri.AbsolutePath.StartsWith("/channels/", StringComparison.Ordinal))) return uri.AbsoluteUri;
        return "https://github.com/PrimeBuild-pc/discord-theme-launcher/issues/new?template=theme_submission.yml";
    }
    private void SaveDocument(bool share)
    {
        var value = ReadConfiguration(); string? path = share ? null : documentPath;
        if (path is null)
        {
            using var dialog = new SaveFileDialog { Filter = UiText.Text("Theme configuration (*.dtheme)") + "|*.dtheme", DefaultExt = "dtheme", AddExtension = true, FileName = "My-server-theme.dtheme", Title = UiText.Text(share ? "Share a copy with embedded assets" : "Save your configuration") };
            if (dialog.ShowDialog(this) != DialogResult.OK) return; path = dialog.FileName;
        }
        value.Save(path); if (!share) { documentPath = path; dirty = false; saved = true; }
        if (!dryRun) value.Save(DraftPath);
        notice = share ? "Portable copy saved with selected assets and font license. Send it to members yourself; each member applies it locally." : "Your configuration and selected assets are saved together.";
        Publish();
    }
    private void ExportOriginalCss()
    {
        if (configuration.Imported is not { } imported) return;
        _ = CssThemeImporter.Inspect(imported);
        using var dialog = new SaveFileDialog { Filter = "BetterDiscord (*.theme.css)|*.theme.css", DefaultExt = "theme.css", AddExtension = true, FileName = imported.FileName, Title = UiText.Text("Export original CSS") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ThemeStore.AtomicWrite(dialog.FileName, imported.Original);
    }
    private void ImportImage(bool banner)
    {
        using var dialog = new OpenFileDialog { Filter = UiText.Text("Images (*.png;*.jpg;*.jpeg)") + "|*.png;*.jpg;*.jpeg", Title = UiText.Text("Import an image you have permission to use and share") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var asset = AssetImport.Image(dialog.FileName);
        configuration = banner ? configuration with { Banner = "custom", CustomBanner = asset } : configuration with { Background = "custom", CustomBackground = asset };
        notice = "Imported " + asset.Name + ". The image is embedded; the original path is not shared."; Changed();
    }
    private bool ConfirmDiscard()
    {
        if (!dirty) return true;
        using var dialog = new Form { Text = UiText.Text("Unsaved configuration"), BackColor = LauncherStyle.Surface, ForeColor = LauncherStyle.Text, Font = LauncherStyle.Font(), ClientSize = new Size(560, 230), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog };
        var text = new Label { Text = UiText.Text("This configuration has unsaved edits. Save it before continuing if you want to keep them."), Dock = DockStyle.Top, Height = 130, Padding = new Padding(24) };
        var row = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(24, 0, 0, 0) };
        var keep = new DiscordButton { Text = UiText.Text("Keep editing"), Primary = true, AutoSize = true, Padding = new Padding(12, 7, 12, 7), DialogResult = DialogResult.Cancel };
        var discard = new DiscordButton { Text = UiText.Text("Discard edits"), AutoSize = true, Padding = new Padding(12, 7, 12, 7), DialogResult = DialogResult.OK };
        row.Controls.Add(keep); row.Controls.Add(discard); dialog.Controls.Add(text); dialog.Controls.Add(row); dialog.CancelButton = keep; dialog.AcceptButton = keep; dialog.Shown += (_, _) => LauncherStyle.DarkCaption(dialog);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }
    private void ServerHelp() => LauncherStyle.ShowMessage(this, UiText.Text(UiText.ServerHelpBody), UiText.Text(UiText.ServerHelpTitle), true);
    private void Help() => LauncherStyle.ShowMessage(this, UiText.Text(UiText.HelpBody), UiText.Text(UiText.HelpTitle), true);

    private void RequestConfirmation(string operation)
    {
        if (busy || confirmation is not null) return;
        ThemeConfiguration? value = operation == "apply" ? ReadConfiguration() : null;
        if (value is not null && !ApplyReadiness.Evaluate(busy, validation, renderedRevision == revision && surface.Ready).CanApply) return;
        string problem = "";
        bool setup = false, repair = false;
        if (!dryRun)
        {
            try
            {
                client.ValidateEnvironment();
                _ = store.InstalledSelection();
                setup = !client.Patched; repair = setup && client.NeedsRepair;
            }
            catch (Exception error)
            {
                problem = error is InvalidOperationException or InvalidDataException ? error.Message : "The client or Vencord profile could not be checked. Review the installation and manual recovery guide.";
            }
        }
        confirmation = OperationConfirmation.Create(operation, revision, value, setup, repair, problem);
        Publish();
    }

    private async Task RepairUnbootableAsync()
    {
        if (busy || !client.Found || !client.Unbootable) return;
        busy = true;
        notice = "Discord cannot start because Vencord's patcher.js is missing. Downloading the official installer…";
        Publish();
        try
        {
            client.ValidateEnvironment();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBuild-DiscordThemeLauncher/" + ThemeCompiler.BundledCatalog.Version);
            var installer = await ReleaseDownload.FetchAsync(http, ReleaseDownload.InstallerUri, ReleaseDownload.InstallerSha256, 32 * 1024 * 1024);
            notice = "Closing the broken Discord window and repairing official Vencord…"; Publish();
            await client.StopAsync();
            await client.InstallVencordAsync(installer, true);
            notice = "Opening Discord…"; Publish();
            await client.OpenAsync();
            notice = "Vencord repaired. Discord startup confirmed.";
        }
        catch (Exception error)
        {
            notice = error is InvalidOperationException or InvalidDataException ? error.Message : error is HttpRequestException ? "The download could not finish. Check your connection and the release page, then retry." : "Vencord repair could not finish. Check the connection and the manual recovery guide.";
            LauncherStyle.ShowMessage(this, UiText.Text(notice), UiText.Text("Vencord repair"));
        }
        finally { busy = false; Publish(); }
    }

    private async Task OperateAsync(OperationConfirmation confirmed, bool allowSetup)
    {
        if (busy) return;
        string operation = confirmed.Operation;
        var value = confirmed.Configuration;
        if (dryRun)
        {
            SimulatedOperations++;
            notice = "Simulation complete. No Discord or Vencord files or processes were changed.";
            Publish(); return;
        }
        bool stopped = false;
        try
        {
            if (value is not null && (!surface.Ready || renderedRevision != revision)) throw new InvalidDataException("Wait for a valid preview of your latest edits before applying.");
            client.ValidateEnvironment(); bool repair = client.NeedsRepair, setup = !client.Patched;
            if (setup && !allowSetup) throw new InvalidOperationException("Vencord now needs installation / repair. Click Apply again and review the setup option before continuing.");
            busy = true; Publish(); byte[]? installer = null;
            if (setup)
            {
                notice = "Downloading and verifying the official Vencord installer…"; Publish();
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBuild-DiscordThemeLauncher/" + ThemeCompiler.BundledCatalog.Version);
                installer = await ReleaseDownload.FetchAsync(http, ReleaseDownload.InstallerUri, ReleaseDownload.InstallerSha256, 32 * 1024 * 1024);
            }
            if (value is not null) await Task.Run(() => value.Save(DraftPath));
            notice = "Closing Discord after your confirmation…"; Publish(); await client.StopAsync(); stopped = true;
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
            LauncherStyle.ShowMessage(this, UiText.Text(notice), UiText.Text("Operation did not complete"));
        }
        finally { busy = false; Publish(); }
    }
}
