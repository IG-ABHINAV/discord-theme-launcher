using System.Diagnostics;
using System.Reflection;

namespace CommunityThemes;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        UiText.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeBuild", "DiscordThemeLauncher"));
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => LauncherStyle.ShowMessage(null, UiText.Text("The launcher encountered an unexpected error. Close it and reopen your saved configuration. Check the manual recovery guide if an operation was interrupted."), UiText.Text("Discord Theme Launcher"));
        using var typography = new LauncherStyle.Typography();
        using var instance = new Mutex(true, "Local\\PrimeBuild.DiscordThemeLauncher." + System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value + (args.Contains("--safe-preview") ? ".Preview" : ""), out bool first);
        if (!first) { LauncherStyle.ShowMessage(null, UiText.Text("The launcher is already open for this Windows user."), UiText.Text("Discord Theme Launcher")); return; }
        // Normal editing is always a preview until the in-app confirmation is accepted.
        // --preview remains a compatible way to open that workflow; --safe-preview is for non-mutating QA.
        Application.Run(new BuilderForm(args.Contains("--safe-preview")));
    }
}

internal sealed class LauncherForm : Form
{
    private readonly DiscordClient client = new();
    private readonly Catalog catalog;
    private readonly ThemeStore store;
    private readonly bool preview;
    private readonly List<CheckBox> choices = [];
    private readonly DiscordButton apply = new() { Text = UiText.Text("Apply & open Discord"), AutoSize = true, Padding = new Padding(16, 8, 16, 8) };
    private readonly DiscordButton restore = new() { Text = UiText.Text("Restore previous setup"), AutoSize = true, Padding = new Padding(12, 8, 12, 8) };
    private readonly DiscordCheckBox ready = new() { Text = UiText.Text("My calls are finished and drafts are saved. Discord may restart."), AutoSize = true };
    private readonly DiscordCheckBox install = new() { Text = UiText.Text("Install official Vencord if it is missing"), AutoSize = true };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(960, 0), ForeColor = LauncherStyle.Muted };
    private readonly ProgressBar progress = new() { Dock = DockStyle.Bottom, Height = 4, Visible = false, Style = ProgressBarStyle.Marquee };
    private bool busy;

    public LauncherForm(bool previewMode)
    {
        preview = previewMode;
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("catalog.json")!;
        using var reader = new StreamReader(stream);
        catalog = Catalog.Parse(reader.ReadToEnd());
        store = new ThemeStore(client.VencordRoot, client.StateRoot, catalog);
        Text = UiText.Format("Discord Theme Launcher — {0}", catalog.Version) + (preview ? UiText.Text(" · Preview (no changes)") : "");
        BackColor = LauncherStyle.Surface; ForeColor = LauncherStyle.Text;
        Font = LauncherStyle.Font(); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1080, 790); MinimumSize = new Size(850, 740); StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 76, 68, 35, 62, 44 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        Controls.Add(layout); Controls.Add(progress);
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        heading.Controls.Add(new Label { Text = UiText.Text("Community themes"), Font = LauncherStyle.Font(20, FontStyle.Bold), AutoSize = true });
        heading.Controls.Add(new Label { Text = UiText.Text("Choose original presets. Applying this selection replaces the personal theme slot too."), AutoSize = true, ForeColor = LauncherStyle.Muted });
        layout.Controls.Add(heading, 0, 0);
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        for (int i = 0; i < catalog.Themes.Length; i++) cards.Controls.Add(Card(catalog.Themes[i]), i, 0);
        layout.Controls.Add(cards, 0, 1);
        layout.Controls.Add(new Label { Text = UiText.Text("No login or server permissions required. Changes are local to this computer.\nIndependent community tool. Not affiliated with or endorsed by Discord.\nVencord is unofficial; Discord terms prohibit client mods. No account-safety guarantee."), Dock = DockStyle.Fill, ForeColor = LauncherStyle.Muted, AutoEllipsis = true }, 0, 2);
        var prerequisites = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        prerequisites.Controls.Add(install); prerequisites.Controls.Add(ready); layout.Controls.Add(prerequisites, 0, 3);
        var links = new FlowLayoutPanel { Dock = DockStyle.Fill };
        foreach (var (text, url) in new[] { ("Official Vencord", "https://vencord.dev/download/"), ("Source & manual setup", "https://github.com/PrimeBuild-pc/discord-theme-launcher"), ("Backups", "") })
        {
            var link = new LinkLabel { Text = UiText.Text(text), UseMnemonic = false, AutoSize = true, Margin = new Padding(0, 4, 22, 0), LinkColor = LauncherStyle.Link, ActiveLinkColor = LauncherStyle.Link, VisitedLinkColor = LauncherStyle.Link };
            link.LinkClicked += (_, _) => OpenLink(url.Length == 0 ? client.StateRoot : url); links.Controls.Add(link);
        }
        var licenses = new LinkLabel { Text = UiText.Text("Font license"), AutoSize = true, Margin = new Padding(0, 4, 22, 0), LinkColor = LauncherStyle.Link, ActiveLinkColor = LauncherStyle.Link, VisitedLinkColor = LauncherStyle.Link };
        licenses.LinkClicked += (_, _) => { using var licenseStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NotoSans-OFL.txt")!; using var licenseReader = new StreamReader(licenseStream); LauncherStyle.ShowMessage(this, licenseReader.ReadToEnd(), UiText.Text("Noto Sans - SIL Open Font License"), true); };
        links.Controls.Add(licenses);
        layout.Controls.Add(links, 0, 4);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        foreach (var button in new[] { apply, restore }) { button.UseMnemonic = false; button.FlatStyle = FlatStyle.Flat; button.ForeColor = Color.White; button.BackColor = LauncherStyle.Hover; actions.Controls.Add(button); }
        apply.Primary = true;
        apply.Click += async (_, _) => await RunAsync(false); restore.Click += async (_, _) => await RunAsync(true);
        layout.Controls.Add(actions, 0, 5); layout.Controls.Add(status, 0, 6);
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
        Shown += (_, _) => LauncherStyle.DarkCaption(this);
        RefreshState();
    }

    private Control Card(Theme theme)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = LauncherStyle.Card, Margin = new Padding(0, 0, 14, 12), Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        var check = new DiscordCheckBox { Text = theme.Server, Tag = theme.Id, Dock = DockStyle.Fill, Font = LauncherStyle.Font(12, FontStyle.Bold), ForeColor = LauncherStyle.Text, AccessibleName = UiText.Format("{0} theme", theme.Server) };
        choices.Add(check); card.Controls.Add(check, 0, 0);
        var image = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = LauncherStyle.Base, AccessibleName = UiText.Format("{0} sanitized theme preview", theme.Server) };
        var resource = theme.Id == "primebuild" ? "officina-preview.png" : theme.Id + "-preview.png";
        using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
        if (source != null) { using var original = Image.FromStream(source); image.Image = new Bitmap(original); }
        card.Controls.Add(image, 0, 1);
        card.Controls.Add(new Label { Text = theme.Name.Split('—').Last().Trim(), ForeColor = LauncherStyle.Text, Dock = DockStyle.Fill, Padding = new Padding(0, 14, 0, 0), Font = LauncherStyle.Font(11, FontStyle.Bold) }, 0, 2);
        card.Controls.Add(new Label { Text = theme.Tagline, Dock = DockStyle.Fill, ForeColor = LauncherStyle.Muted }, 0, 3);
        card.Controls.Add(new Label { Text = UiText.Text("Chat + channels + members"), Dock = DockStyle.Fill, ForeColor = LauncherStyle.Muted, Font = LauncherStyle.Font(9) }, 0, 4);
        return card;
    }

    private void RefreshState()
    {
        try
        {
            var selected = store.InstalledSelection();
            foreach (var choice in choices) choice.Checked = selected.Contains((string)choice.Tag!);
            install.Enabled = !client.Patched && !preview;
            install.Text = UiText.Text(client.Patched ? "Vencord is already installed" : client.NeedsRepair ? "Repair official Vencord (incomplete installation)" : "Install official Vencord if it is missing");
            install.Checked = client.Patched;
            restore.Enabled = store.CanRestore && !preview; apply.Enabled = !preview;
            status.Text = UiText.Text(preview ? "Preview mode — no network calls, files or Discord processes will be changed." : client.Found ? (client.Patched ? "Discord + Vencord detected. Choose themes and apply. Unchecked managed themes will be removed; other themes stay." : client.NeedsRepair ? "Vencord's loader is present but runtime files are missing or unreadable. Choose Repair above." : "Discord detected. Enable Vencord installation above for first setup.") : "Standard Discord desktop was not found for this Windows user.");
            if (DiscordClient.Elevated) { apply.Enabled = false; restore.Enabled = false; status.Text = UiText.Text("Run normally, without Administrator privileges."); }
        }
        catch { status.Text = UiText.Text("The profile could not be read. Check the manual setup guide before applying."); apply.Enabled = false; restore.Enabled = false; }
    }

    private async Task RunAsync(bool undo)
    {
        if (preview || busy) return;
        if (!ready.Checked) { LauncherStyle.ShowMessage(this, UiText.Text("Finish any call or stream and save your drafts. Then tick the restart checkbox."), UiText.Text("Before restarting Discord")); return; }
        bool stopped = false;
        try
        {
            client.ValidateEnvironment();
            bool repair = client.NeedsRepair;
            bool needsSetup = repair || (!undo && !client.Patched);
            if (needsSetup && !install.Checked) throw new InvalidOperationException(repair ? "Vencord is incomplete. Choose Repair official Vencord above, or repair it with the official installer first." : "Choose official Vencord installation above, or install it manually first.");
            busy = true; apply.Enabled = restore.Enabled = false; install.Enabled = ready.Enabled = false;
            foreach (var choice in choices) choice.Enabled = false;
            progress.Visible = true;
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBuild-DiscordThemeLauncher/" + catalog.Version);
            var ids = choices.Where(c => c.Checked).Select(c => (string)c.Tag!).ToArray();
            var downloads = new Dictionary<string, byte[]>(); byte[]? installer = null;
            if (!undo)
            {
                foreach (var theme in catalog.Themes.Where(t => ids.Contains(t.Id)))
                {
                    status.Text = UiText.Text("Downloading and verifying " + theme.Name + "…");
                    var local = Path.Combine(AppContext.BaseDirectory, theme.File);
                    ThemeStore.CheckPath(local);
                    if (File.Exists(local))
                    {
                        if (new FileInfo(local).Length != theme.Bytes) throw new InvalidDataException("A bundled theme does not match this launcher version. Download a fresh portable package.");
                        downloads[theme.File] = await File.ReadAllBytesAsync(local);
                        ReleaseDownload.Verify(downloads[theme.File], theme.Sha256, theme.Bytes);
                    }
                    else downloads[theme.File] = await ReleaseDownload.FetchAsync(http, ReleaseDownload.ThemeUri(catalog, theme), theme.Sha256, 358400, theme.Bytes);
                }
            }
            if (needsSetup)
            {
                status.Text = UiText.Text("Downloading the official Vencord installer…");
                installer = await ReleaseDownload.FetchAsync(http, ReleaseDownload.InstallerUri, ReleaseDownload.InstallerSha256, 32 * 1024 * 1024);
            }
            status.Text = UiText.Text("Closing Discord…"); await client.StopAsync(); stopped = true;
            if (installer != null) { status.Text = UiText.Text(repair ? "Repairing official Vencord…" : "Installing official Vencord…"); await client.InstallVencordAsync(installer, repair); }
            status.Text = UiText.Text(undo ? "Restoring previous setup…" : "Backing up and applying your selection…");
            await Task.Run(() => { if (undo) store.RestoreLast(); else store.Apply(ids, downloads); });
            status.Text = UiText.Text("Opening Discord and checking startup…");
            await client.OpenAsync(); stopped = false;
            RefreshState(); status.Text = UiText.Text(undo ? "Previous managed themes restored. Discord startup confirmed." : "Done. Discord startup confirmed. Each selected theme appears in its matching server.");
        }
        catch (Exception error)
        {
            var message = error is InvalidOperationException or InvalidDataException ? error.Message : error is HttpRequestException ? "The download could not finish. Check your connection and the release page, then retry." : "Setup could not finish. Check available disk space, the profile and the manual recovery guide.";
            if (stopped) message += " No automatic retry was made.";
            status.Text = UiText.Text(message); LauncherStyle.ShowMessage(this, UiText.Text(message), UiText.Text("Setup did not complete"));
        }
        finally
        {
            busy = false; progress.Visible = false; apply.Enabled = !preview; restore.Enabled = store.CanRestore && !preview; install.Enabled = !client.Patched && !preview; ready.Enabled = true; ready.Checked = false;
            foreach (var choice in choices) choice.Enabled = true;
        }
    }
    private void OpenLink(string target)
    {
        try { if (target == client.StateRoot && !Directory.Exists(target)) { status.Text = UiText.Text("No backup folder exists yet. It is created before the first change."); return; } Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose(); }
        catch { status.Text = UiText.Text("Could not open the link or folder. Use the repository URL in the release notes."); }
    }
}
