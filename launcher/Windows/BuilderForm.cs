using System.Diagnostics;

namespace CommunityThemes;

internal sealed class BuilderForm : Form
{
    private readonly bool previewMode;
    private readonly DiscordClient client = new();
    private readonly ThemeStore store;
    private ThemeConfiguration configuration = new();
    private readonly TextBox name = Input("Theme name"), guild = Input("Server ID or channel link"), accent = Input("Accent color"), surface = Input("Dark surface color");
    private readonly ComboBox preset = Choice("Starting preset", "Prime Build", "Prime Build Dev", "FantaFort");
    private readonly ComboBox palette = Choice("Palette", "Prime Build", "Prime Build Dev", "FantaFort", "Discord blurple", "Custom colors");
    private readonly ComboBox font = Choice("Display font", "Rajdhani · bundled", "JetBrains Mono · bundled", "Bangers · bundled", "Native typography", "Imported font");
    private readonly ComboBox banner = Choice("Banner", "Build Your Dream", "Software House", "Arena", "No banner", "Imported banner");
    private readonly ComboBox background = Choice("Background", "Hardware", "Software House", "Arena", "No artwork", "Imported background");
    private readonly BuilderPreview themePreview = new() { Dock = DockStyle.Fill };
    private readonly Label status = new() { Dock = DockStyle.Fill, ForeColor = LauncherStyle.Muted, AutoEllipsis = true, AccessibleName = "Operation status" };
    private readonly Label validation = new() { Dock = DockStyle.Fill, ForeColor = LauncherStyle.Muted, AutoEllipsis = true, AccessibleName = "Configuration validation" };
    private readonly DiscordCheckBox ready = new() { Text = "Calls finished, drafts saved. Discord may restart.", AutoSize = true };
    private readonly DiscordCheckBox install = new() { Text = "Install / repair official Vencord if needed", AutoSize = true };
    private readonly DiscordButton apply = Button("Apply theme", true), disable = Button("Disable personal theme"), restore = Button("Restore previous setup");
    private readonly FlowLayoutPanel fileActions = new() { Dock = DockStyle.Fill, WrapContents = false };
    private readonly Panel editor = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly System.Windows.Forms.Timer debounce = new() { Interval = 500 };
    private int revision, renderedRevision = -1;
    private bool loading, busy, dirty;
    private string? documentPath;
    private string DraftPath => Path.Combine(client.StateRoot, "last-configuration.dtheme");
    private static readonly string[] Fonts = ["primebuild", "primebuild-dev", "fantafort", "native", "custom"];
    private static readonly string[] Art = ["primebuild", "primebuild-dev", "fantafort", "none", "custom"];

    public BuilderForm(bool preview)
    {
        previewMode = preview;
        store = new ThemeStore(client.VencordRoot, client.StateRoot, ThemeCompiler.BundledCatalog);
        BackColor = LauncherStyle.Surface; ForeColor = LauncherStyle.Text; Font = LauncherStyle.Font(); AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Discord Theme Launcher — " + ThemeCompiler.BundledCatalog.Version + (preview ? " · Preview" : "");
        ClientSize = new Size(1320, 900); MinimumSize = new Size(980, 680); StartPosition = FormStartPosition.CenterScreen;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 6 };
        foreach (var height in new[] { 66, 48 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 72, 52, 48 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        Controls.Add(root);
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        heading.Controls.Add(new Label { Text = "Make your server feel like yours.", Font = LauncherStyle.Font(21, FontStyle.Bold), AutoSize = true });
        heading.Controls.Add(new Label { Text = "Local client styling only. Other members must adopt the shared theme themselves. No login or token required.", ForeColor = LauncherStyle.Muted, AutoSize = true });
        root.Controls.Add(heading, 0, 0);
        AddAction(fileActions, "New", NewDocument); AddAction(fileActions, "Open configuration", OpenDocument); AddAction(fileActions, "Save", () => SaveDocument(false));
        AddAction(fileActions, "Share a copy…", () => SaveDocument(true));
        AddAction(fileActions, "Original presets…", () => { using var form = new LauncherForm(previewMode); form.ShowDialog(this); RefreshOperations(); });
        AddAction(fileActions, "Help", Help); root.Controls.Add(fileActions, 0, 1);
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360)); content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.Controls.Add(editor, 0, 0);
        var fields = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 0, 18, 12), Width = 335 };
        editor.Controls.Add(fields);
        Section(fields, "01  Choose your server"); Field(fields, "Theme name", name); Field(fields, "Server ID or channel link", guild);
        AddAction(fields, "How to copy a server ID", ServerHelp);
        Section(fields, "02  Make it your own"); Field(fields, "Start from a community preset", preset); Field(fields, "Display typography · chat stays native", font);
        AddAction(fields, "Import font…", ImportFont); Field(fields, "Color palette", palette);
        var colorRow = new FlowLayoutPanel { AutoSize = true, Width = 325, WrapContents = false };
        accent.Width = surface.Width = 108; colorRow.Controls.Add(accent); colorRow.Controls.Add(surface);
        AddAction(colorRow, "Pick…", PickColors); fields.Controls.Add(colorRow);
        fields.Controls.Add(Note("Accent / dark surface · #RRGGBB. Bright surfaces are rejected to protect readability."));
        Section(fields, "03  Add your artwork"); Field(fields, "Channel banner", banner); AddAction(fields, "Import banner…", () => ImportImage(true));
        Field(fields, "Chat background", background); AddAction(fields, "Import background…", () => ImportImage(false));
        fields.Controls.Add(Note("PNG / JPEG · ≤8 MiB · ≤4096 px per side. Use images you have permission to share. Imported backgrounds receive a readability overlay."));
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(18, 0, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        right.Controls.Add(new Label { Text = "LIVE PREVIEW   /   Generated CSS · synthetic content", AutoSize = true, ForeColor = LauncherStyle.Muted }, 0, 0);
        right.Controls.Add(themePreview, 0, 1); right.Controls.Add(validation, 0, 2); content.Controls.Add(right, 1, 0); root.Controls.Add(content, 0, 2);
        var checks = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        checks.Controls.Add(ready); checks.Controls.Add(install); root.Controls.Add(checks, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        actions.Controls.Add(apply); actions.Controls.Add(disable); actions.Controls.Add(restore); root.Controls.Add(actions, 0, 4); root.Controls.Add(status, 0, 5);
        apply.Click += async (_, _) => await OperateAsync("apply"); disable.Click += async (_, _) => await OperateAsync("disable"); restore.Click += async (_, _) => await OperateAsync("restore");
        foreach (var text in new[] { name, guild, accent, surface }) text.TextChanged += (_, _) => Changed();
        foreach (var choice in new[] { palette, font, banner, background }) choice.SelectedIndexChanged += (_, _) => Changed();
        preset.SelectedIndexChanged += (_, _) =>
        {
            if (loading) return;
            var current = configuration with { Name = name.Text, Guild = guild.Text, Preset = ThemeConfiguration.Presets[preset.SelectedIndex] };
            SetConfiguration(current with { Palette = current.Preset, Font = current.Preset, Banner = current.Preset, Background = current.Preset }); Changed();
        };
        ready.CheckedChanged += (_, _) => RefreshOperations();
        debounce.Tick += (_, _) => { debounce.Stop(); RenderPreview(); };
        themePreview.PreviewReady += (_, _) => RefreshOperations();
        FormClosing += (_, e) => { if (busy || !ConfirmDiscard()) e.Cancel = true; };
        Load += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width - 16), Math.Min(MinimumSize.Height, area.Height - 16));
            Size = new Size(Math.Min(Width, area.Width - 16), Math.Min(Height, area.Height - 16));
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        };
        Shown += async (_, _) => { LauncherStyle.DarkCaption(this); await themePreview.InitializeAsync(); RefreshOperations(); };
        SetConfiguration(preview ? new ThemeConfiguration { Name = "My community", Guild = "987654321012345678", Palette = "blurple", Font = "primebuild-dev", Banner = "primebuild-dev", Background = "primebuild-dev" } : configuration);
        if (!preview && File.Exists(DraftPath))
        {
            try { var draft = ThemeConfiguration.Load(DraftPath); AssetImport.ValidateImages(draft); SetConfiguration(draft); }
            catch { status.Text = "The last configuration could not be reopened. Open another .dtheme file or start a new one."; }
        }
        RenderPreview(); RefreshOperations();
    }

    private static TextBox Input(string label) => new() { BackColor = LauncherStyle.Base, ForeColor = LauncherStyle.Text, BorderStyle = BorderStyle.FixedSingle, Width = 320, AccessibleName = label };
    private static ComboBox Choice(string label, params string[] values)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = LauncherStyle.Base, ForeColor = LauncherStyle.Text, FlatStyle = FlatStyle.Flat, Width = 320, AccessibleName = label };
        box.Items.AddRange(values); box.SelectedIndex = 0; return box;
    }
    private static DiscordButton Button(string label, bool primary = false) => new() { Text = label, AutoSize = true, Padding = new Padding(12, 6, 12, 6), Primary = primary, Margin = new Padding(0, 0, 8, 6) };
    private static void AddAction(Control panel, string label, Action action) { var button = Button(label); button.Click += (_, _) => action(); panel.Controls.Add(button); }
    private static Label Note(string text) => new() { Text = text, AutoSize = true, MaximumSize = new Size(320, 0), ForeColor = LauncherStyle.Muted, Margin = new Padding(0, 4, 0, 12) };
    private static void Section(Control panel, string label) => panel.Controls.Add(new Label { Text = label, AutoSize = true, Font = LauncherStyle.Font(11, FontStyle.Bold), Margin = new Padding(0, 16, 0, 10) });
    private static void Field(Control panel, string label, Control input) { panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 4, 0, 4) }); input.Margin = new Padding(0, 0, 0, 8); panel.Controls.Add(input); }

    private void SetConfiguration(ThemeConfiguration value)
    {
        loading = true; configuration = value;
        name.Text = value.Name; guild.Text = value.Guild; accent.Text = value.Accent; surface.Text = value.Surface;
        preset.SelectedIndex = Array.IndexOf(ThemeConfiguration.Presets, value.Preset); palette.SelectedIndex = Array.IndexOf(ThemeConfiguration.Palettes, value.Palette);
        font.SelectedIndex = Array.IndexOf(Fonts, value.Font); banner.SelectedIndex = Array.IndexOf(Art, value.Banner); background.SelectedIndex = Array.IndexOf(Art, value.Background);
        font.Items[4] = value.CustomFont is null ? "Imported font" : "Imported · " + value.CustomFont.Name;
        banner.Items[4] = value.CustomBanner is null ? "Imported banner" : "Imported · " + value.CustomBanner.Name;
        background.Items[4] = value.CustomBackground is null ? "Imported background" : "Imported · " + value.CustomBackground.Name;
        loading = false;
    }
    private ThemeConfiguration ReadConfiguration()
    {
        var value = configuration with { Name = name.Text.Trim(), Guild = ThemeConfiguration.ParseGuild(guild.Text), Preset = ThemeConfiguration.Presets[preset.SelectedIndex],
            Palette = ThemeConfiguration.Palettes[palette.SelectedIndex], Accent = accent.Text.Trim(), Surface = surface.Text.Trim(), Font = Fonts[font.SelectedIndex], Banner = Art[banner.SelectedIndex], Background = Art[background.SelectedIndex],
            CustomFont = font.SelectedIndex == 4 ? configuration.CustomFont : null, CustomBanner = banner.SelectedIndex == 4 ? configuration.CustomBanner : null, CustomBackground = background.SelectedIndex == 4 ? configuration.CustomBackground : null };
        value.Validate(); return value;
    }
    private void Changed()
    {
        if (loading) return;
        revision++; dirty = true; renderedRevision = -1; debounce.Stop(); debounce.Start();
        accent.Enabled = surface.Enabled = palette.SelectedIndex == 4; RefreshOperations();
    }
    private void RenderPreview()
    {
        try
        {
            var value = ReadConfiguration(); renderedRevision = revision; themePreview.Render(value);
            validation.Text = "Target server: " + value.Guild + " · display font: " + font.Text + ".\nTry ALT or DM above to check isolation. This is an approximation of supported Discord layouts.";
            validation.ForeColor = LauncherStyle.Muted;
        }
        catch (InvalidDataException error) { renderedRevision = -1; validation.Text = error.Message; validation.ForeColor = Color.FromArgb(255, 155, 155); }
        RefreshOperations();
    }
    private void RefreshOperations()
    {
        apply.Enabled = !previewMode && !busy && ready.Checked && renderedRevision == revision && themePreview.Ready && !DiscordClient.Elevated;
        disable.Enabled = restore.Enabled = false;
        try { if (!previewMode && !busy && !DiscordClient.Elevated) { disable.Enabled = store.PersonalEnabled; restore.Enabled = store.CanRestore; } }
        catch { status.Text = "The Vencord profile could not be read. Check the manual recovery guide."; apply.Enabled = false; }
        if (previewMode) status.Text = "Preview mode · Discord and Vencord are never modified. Save / share remain available.";
        else if (DiscordClient.Elevated) status.Text = "Run normally, without Administrator privileges.";
        else if (!busy && status.Text.Length == 0) status.Text = "Preview your design, save a configuration, then acknowledge the restart to apply. One personal theme at a time.";
        install.Enabled = !busy && !previewMode && !client.Patched;
        accent.Enabled = surface.Enabled = palette.SelectedIndex == 4;
    }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
        { LauncherStyle.ShowMessage(this, e is InvalidDataException ? e.Message : "The file could not be processed. Check its format, size and permissions. Your current configuration is unchanged.", "Could not complete this action"); }
    }
    private void NewDocument()
    {
        if (!ConfirmDiscard()) return;
        SetConfiguration(new ThemeConfiguration { Name = "My server theme", Guild = "" }); documentPath = null; dirty = false; revision++; RenderPreview(); guild.Focus();
    }
    private void OpenDocument()
    {
        if (!ConfirmDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = "Theme configuration (*.dtheme)|*.dtheme", Title = "Open a configuration and its assets" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        Guard(() => { var value = ThemeConfiguration.Load(dialog.FileName); AssetImport.ValidateImages(value); SetConfiguration(value); documentPath = dialog.FileName; dirty = false; revision++; RenderPreview(); status.Text = "Configuration opened. Review the target server before applying."; });
    }
    private void SaveDocument(bool share) => Guard(() =>
    {
        var value = ReadConfiguration(); string? path = share ? null : documentPath;
        if (path is null)
        {
            using var dialog = new SaveFileDialog { Filter = "Theme configuration (*.dtheme)|*.dtheme", DefaultExt = "dtheme", AddExtension = true, FileName = "My-server-theme.dtheme", Title = share ? "Share a portable copy with embedded assets" : "Save your configuration" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return; path = dialog.FileName;
        }
        value.Save(path); if (!share) { documentPath = path; dirty = false; }
        if (!previewMode) value.Save(DraftPath);
        status.Text = share ? "Portable copy saved with its selected assets and font license. Send this file to members yourself; each member must apply it locally." : "Configuration and selected assets saved together. Reopen the .dtheme file to continue editing.";
    });
    private bool ConfirmDiscard()
    {
        if (!dirty) return true;
        using var dialog = new Form { Text = "Unsaved configuration", BackColor = LauncherStyle.Surface, ForeColor = LauncherStyle.Text, Font = LauncherStyle.Font(), ClientSize = new Size(510, 170), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog };
        var text = new Label { Text = "This configuration has unsaved edits. Save it before continuing if you want to keep them.", Dock = DockStyle.Top, Height = 85, Padding = new Padding(20) };
        var row = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(20, 0, 0, 0) };
        var keep = Button("Keep editing", true); keep.DialogResult = DialogResult.Cancel;
        var discard = Button("Discard edits"); discard.DialogResult = DialogResult.OK; row.Controls.Add(keep); row.Controls.Add(discard);
        dialog.Controls.Add(text); dialog.Controls.Add(row); dialog.CancelButton = keep; dialog.Shown += (_, _) => LauncherStyle.DarkCaption(dialog);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }
    private void ImportFont() => Guard(() => { var asset = AssetImport.Font(this); if (asset is null) return; configuration = configuration with { CustomFont = asset }; font.SelectedIndex = 4; Changed(); status.Text = "Imported font: " + asset.Name + " · license included when shared."; });
    private void ImportImage(bool isBanner) => Guard(() =>
    {
        using var dialog = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg", Title = "Import an image you have permission to use and share" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var asset = AssetImport.Image(dialog.FileName);
        if (isBanner) { configuration = configuration with { CustomBanner = asset }; banner.SelectedIndex = 4; }
        else { configuration = configuration with { CustomBackground = asset }; background.SelectedIndex = 4; }
        Changed(); status.Text = "Imported " + asset.Name + ". The image is embedded in your configuration; no original path is shared.";
    });
    private void PickColors() => Guard(() =>
    {
        var initial = System.Text.RegularExpressions.Regex.IsMatch(accent.Text, "^#[a-fA-F0-9]{6}$") ? accent.Text : "#8791ff";
        using var dialog = new ColorDialog { FullOpen = true, Color = ColorTranslator.FromHtml(initial) };
        if (dialog.ShowDialog(this) == DialogResult.OK) { accent.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"; palette.SelectedIndex = 4; }
        using var dark = new ColorDialog { FullOpen = true, Color = Color.FromArgb(16, 16, 19) };
        if (dark.ShowDialog(this) == DialogResult.OK) { surface.Text = $"#{dark.Color.R:X2}{dark.Color.G:X2}{dark.Color.B:X2}"; palette.SelectedIndex = 4; }
    });
    private void ServerHelp() => LauncherStyle.ShowMessage(this, "In Discord desktop:\n1. Open User Settings > Advanced > Developer Mode.\n2. Right-click the server icon and choose Copy Server ID.\n3. Paste it into Server ID above.\n\nOr right-click a text channel, choose Copy Channel Link and paste the link. Invite links cannot identify the target. You must already belong to the server. No token or credentials are needed.", "Choose a server", true);
    private void Help() => LauncherStyle.ShowMessage(this, "Create a configuration, choose its target, and inspect the live preview. Save or share a .dtheme file to include the selected images, font and license. Shared files are data: arbitrary CSS and remote assets are not imported.\n\nOne personal theme is active at a time. Apply replaces that slot and disables a bundled theme targeting the same server. Other bundled servers and unrelated themes remain enabled. Disable removes the personal slot. Restore recovers the previous managed files and enabled states.\n\nWindows x64 / stable desktop Discord / default Vencord profile only. Setup and recovery restart Discord only after your acknowledgement. Vencord is unofficial and has Discord terms implications. This independent project is not affiliated with Discord.\n\nPreview requires Microsoft Edge WebView2 Runtime. Bundled fonts use SIL OFL 1.1; imported fonts require editable embedding rights and their redistribution license. Check the repository's Installation and Compatibility documents for manual recovery and notices.", "Discord Theme Launcher · Help", true);

    private async Task OperateAsync(string operation)
    {
        if (busy || previewMode) return;
        if (!ready.Checked) { LauncherStyle.ShowMessage(this, "Finish calls and save drafts, then acknowledge the restart before applying, disabling or restoring.", "Before restarting Discord"); return; }
        bool stopped = false;
        try
        {
            ThemeConfiguration? value = operation == "apply" ? ReadConfiguration() : null;
            if (value is not null && (!themePreview.Ready || renderedRevision != revision)) throw new InvalidDataException("Wait for a valid preview of your latest edits before applying.");
            client.ValidateEnvironment(); bool repair = client.NeedsRepair; bool setup = !client.Patched;
            if (setup && !install.Checked) throw new InvalidOperationException("Choose official Vencord installation / repair, or install it manually before continuing.");
            busy = true; debounce.Stop(); editor.Enabled = fileActions.Enabled = ready.Enabled = install.Enabled = false; RefreshOperations();
            byte[]? installer = null;
            if (setup)
            {
                status.Text = "Downloading and verifying the official Vencord installer…";
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBuild-DiscordThemeLauncher/" + ThemeCompiler.BundledCatalog.Version);
                installer = await ReleaseDownload.FetchAsync(http, ReleaseDownload.InstallerUri, ReleaseDownload.InstallerSha256, 32 * 1024 * 1024);
            }
            if (value is not null) await Task.Run(() => value.Save(DraftPath));
            status.Text = "Closing Discord after your acknowledgement…"; await client.StopAsync(); stopped = true;
            if (installer is not null) { status.Text = "Installing / repairing official Vencord…"; await client.InstallVencordAsync(installer, repair); }
            status.Text = "Saving a recovery snapshot and updating managed themes…";
            await Task.Run(() => { if (operation == "restore") store.RestoreLast(); else store.ApplyPersonal(value); });
            status.Text = "Opening Discord and checking startup…"; await client.OpenAsync(); stopped = false;
            status.Text = operation == "apply" ? "Personal theme applied to server " + value!.Guild + ". Discord startup confirmed. Changes are local to you." : operation == "disable" ? "Personal theme disabled. Discord startup confirmed. Use Restore to undo." : "Previous managed setup restored. Discord startup confirmed.";
        }
        catch (Exception error)
        {
            status.Text = error is InvalidDataException or InvalidOperationException ? error.Message : "Setup could not finish. Check the profile, disk space and manual recovery guide.";
            if (stopped) status.Text += " No automatic retry was made.";
            LauncherStyle.ShowMessage(this, status.Text, "Operation did not complete");
        }
        finally { busy = false; editor.Enabled = fileActions.Enabled = ready.Enabled = true; ready.Checked = false; RefreshOperations(); }
    }
    protected override void Dispose(bool disposing) { if (disposing) debounce.Dispose(); base.Dispose(disposing); }
}
