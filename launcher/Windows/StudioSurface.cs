using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CommunityThemes;

internal sealed class StudioSurface : Panel
{
    internal const string Origin = "https://theme-preview.invalid/";
    private readonly WebView2 web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = LauncherStyle.Surface };
    private readonly List<Stream> staticResponses = [], previewResponses = [];
    private readonly Label fallback = new() { Dock = DockStyle.Fill, ForeColor = LauncherStyle.Text, Padding = new Padding(36), Text = UiText.Text("Opening Theme Studio…") };
    private string previewHtml = "";
    private bool loaded;
    private ulong rootNavigation;
    public bool Ready { get; private set; }
    public string PreviewId { get; private set; } = "";
    public string PreviewUri => Origin + "preview?revision=" + PreviewId;
    public event Action<JsonElement>? MessageReceived;
    public event Action? Loaded;
    public event Action? RuntimeUnavailable;

    public StudioSurface() { Dock = DockStyle.Fill; Controls.Add(web); Controls.Add(fallback); }
    internal static byte[] Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidDataException("A bundled interface resource is missing.");
        using var result = new MemoryStream(); stream.CopyTo(result); return result.ToArray();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var folder = Path.Combine(Path.GetTempPath(), "PrimeBuild-ThemeStudio-" + Guid.NewGuid().ToString("N"));
            var environment = await CoreWebView2Environment.CreateAsync(null, folder);
            await web.EnsureCoreWebView2Async(environment);
            var core = web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false; core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false; core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false; core.Settings.IsGeneralAutofillEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.NavigationStarting += (_, e) => { if (e.Uri != Origin) { e.Cancel = true; return; } rootNavigation = e.NavigationId; };
            core.FrameCreated += (_, e) => e.Frame.NavigationStarting += (_, navigation) => { if (navigation.Uri != PreviewUri) navigation.Cancel = true; };
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) =>
            {
                byte[] bytes = []; string mime = "text/plain"; bool allowed = false;
                var uri = new Uri(e.Request.Uri);
                if (uri.GetLeftPart(UriPartial.Authority) == Origin.TrimEnd('/'))
                {
                    var path = uri.AbsolutePath;
                    if (path == "/preview" && e.Request.Uri == PreviewUri) { bytes = Encoding.UTF8.GetBytes(previewHtml); mime = "text/html"; allowed = true; }
                    else if (path is "/" or "/studio.css" or "/studio.js" or "/i18n.js")
                    { bytes = Resource(path == "/" ? "ui/studio.html" : "ui/" + path[1..]); mime = path == "/" ? "text/html" : path.EndsWith("css") ? "text/css" : "text/javascript"; allowed = true; }
                    else if (path is "/font/regular.ttf" or "/font/bold.ttf")
                    { bytes = Resource(path.Contains("regular") ? "NotoSans-Regular.ttf" : "NotoSans-Bold.ttf"); mime = "font/ttf"; allowed = true; }
                    else if (path.StartsWith("/art/", StringComparison.Ordinal))
                    {
                        var parts = path.Split('/');
                        if (parts.Length == 4 && CommunityGallery.Designs.Any(d => d.Id == parts[2]) && parts[3] is "header" or "chat")
                        { var data = ThemeCompiler.ArtworkDataUri(parts[2], parts[3]); bytes = Convert.FromBase64String(data[(data.IndexOf(',') + 1)..]); mime = data[5..data.IndexOf(';')]; allowed = true; }
                    }
                }
                var response = new MemoryStream(bytes);
                (uri.AbsolutePath == "/preview" ? previewResponses : staticResponses).Add(response);
                e.Response = environment.CreateWebResourceResponse(response, allowed ? 200 : 403, allowed ? "OK" : "Blocked", "Content-Type: " + mime + "; charset=utf-8\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff");
            };
            core.NavigationCompleted += (_, e) =>
            {
                if (e.NavigationId != rootNavigation) return;
                loaded = e.IsSuccess; fallback.Visible = !loaded;
                if (loaded) Loaded?.Invoke(); else fallback.Text = UiText.Text("Theme Studio could not load. Reopen the application; your saved configurations are safe.");
            };
            core.WebMessageReceived += (_, e) =>
            {
                if (e.Source != Origin || e.WebMessageAsJson.Length > 8192) return;
                try
                {
                    using var document = JsonDocument.Parse(e.WebMessageAsJson);
                    var message = document.RootElement;
                    if (message.ValueKind != JsonValueKind.Object || !message.TryGetProperty("action", out var action) || action.ValueKind != JsonValueKind.String) return;
                    if (action.GetString() == "previewReady")
                    {
                        if (message.GetProperty("id").GetString() != PreviewId) return;
                        Ready = message.GetProperty("ready").GetBoolean();
                        foreach (var stream in previewResponses) stream.Dispose(); previewResponses.Clear();
                    }
                    MessageReceived?.Invoke(message.Clone());
                }
                catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { }
            };
            core.Navigate(Origin);
        }
        catch (Exception error) when (error is WebView2RuntimeNotFoundException or System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            fallback.Visible = true; web.Visible = false;
            fallback.Text = UiText.Text(UiText.WebViewMissing);
            RuntimeUnavailable?.Invoke();
        }
    }

    public void Render(ThemeConfiguration configuration)
    { previewHtml = ThemePreview.Html(configuration, true); Ready = false; PreviewId = Guid.NewGuid().ToString("N"); }

    public void Publish(object state)
    {
        if (loaded && web.CoreWebView2.Source == Origin) web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(state, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { web.Dispose(); foreach (var response in staticResponses.Concat(previewResponses)) response.Dispose(); }
        base.Dispose(disposing);
    }
}
