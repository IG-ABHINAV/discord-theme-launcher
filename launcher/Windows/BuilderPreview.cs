using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text;

namespace CommunityThemes;

internal sealed class BuilderPreview : Panel
{
    private readonly WebView2 web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = LauncherStyle.Base };
    private readonly Label fallback = new() { Text = "Preparing local preview…", Dock = DockStyle.Fill, ForeColor = LauncherStyle.Muted, Padding = new Padding(24) };
    private string html = "";
    private readonly List<Stream> responses = [];
    private string currentUri = "https://theme-preview.invalid/";
    private ulong navigation;
    private bool requireFont;
    public bool Ready { get; private set; }
    public event EventHandler? PreviewReady;

    public BuilderPreview() { Controls.Add(web); Controls.Add(fallback); }
    public async Task InitializeAsync()
    {
        try
        {
            var folder = Path.Combine(Path.GetTempPath(), "PrimeBuild-ThemePreview-" + Guid.NewGuid().ToString("N"));
            var environment = await CoreWebView2Environment.CreateAsync(null, folder);
            await web.EnsureCoreWebView2Async(environment);
            web.ZoomFactor = .8;
            var core = web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsWebMessageEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false;
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            core.NavigationStarting += (_, e) => { if (e.Uri != currentUri) { e.Cancel = true; return; } navigation = e.NavigationId; Ready = false; };
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) =>
            {
                bool local = e.Request.Uri == currentUri;
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(local ? html : "")); responses.Add(stream);
                e.Response = environment.CreateWebResourceResponse(stream, local ? 200 : 403, local ? "OK" : "Blocked", "Content-Type: text/html; charset=utf-8\r\nCache-Control: no-store");
            };
            core.NavigationCompleted += async (_, e) =>
            {
                if (e.NavigationId != navigation) return;
                ulong completed = navigation;
                bool fontLoaded = true;
                try
                {
                    if (requireFont && e.IsSuccess) fontLoaded = await core.ExecuteScriptAsync("document.fonts.check('16px \"PrimeBuild Personal Display\"') && [...document.fonts].some(f => f.family.includes('PrimeBuild Personal Display') && f.status === 'loaded')") == "true";
                    if (e.IsSuccess) fontLoaded &= await core.ExecuteScriptAsync("getComputedStyle(document.querySelector('.base__5e434')).getPropertyValue('--pb-profile').trim() === '\"personal\"'") == "true";
                }
                catch (Exception error) when (error is InvalidOperationException or System.Runtime.InteropServices.COMException) { fontLoaded = false; }
                if (completed != navigation || IsDisposed) return;
                Ready = e.IsSuccess && fontLoaded;
                foreach (var response in responses) response.Dispose(); responses.Clear();
                fallback.Visible = !Ready;
                if (!Ready) fallback.Text = "Preview or display font could not load. Choose a supported font and rebuild the preview before applying.";
                PreviewReady?.Invoke(this, EventArgs.Empty);
            };
            if (html.Length > 0) core.Navigate(currentUri);
        }
        catch (Exception e) when (e is WebView2RuntimeNotFoundException or System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            web.Visible = false; fallback.Text = "The local preview needs Microsoft Edge WebView2 Runtime.\nInstall it from Microsoft's official WebView2 download page, then reopen the launcher.\nYour configuration can still be saved and shared.";
        }
    }

    public void Render(ThemeConfiguration configuration)
    {
        html = ThemePreview.Html(configuration); Ready = false; requireFont = configuration.Font != "native";
        currentUri = "https://theme-preview.invalid/?revision=" + Guid.NewGuid().ToString("N");
        web.CoreWebView2?.Navigate(currentUri);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { web.Dispose(); foreach (var response in responses) response.Dispose(); responses.Clear(); }
        base.Dispose(disposing);
    }
}
