using CommunityThemes;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WindowsTests;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using var typography = new LauncherStyle.Typography();
        using var form = new Form { Text = "Theme preview integration tests", ClientSize = new Size(1000, 760), ShowInTaskbar = false };
        var preview = new BuilderPreview { Dock = DockStyle.Fill }; form.Controls.Add(preview);
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; Console.WriteLine("PASS " + label); }
        void Reject(Action action, string label)
        {
            bool rejected = false;
            try { action(); } catch (Exception e) when (e is InvalidDataException or ArgumentException or ExternalException) { rejected = true; }
            Check(rejected, label);
        }
        Check(ApplyReadiness.Evaluate(false, "", true).CanApply, "a valid preview can open confirmation without a separate mode or restart checkbox");
        Check(!ApplyReadiness.Evaluate(true, "", true).CanApply, "busy operation prevents another confirmation");
        Check(!ApplyReadiness.Evaluate(false, "Invalid target", true).CanApply, "invalid configuration cannot be confirmed");
        Check(!ApplyReadiness.Evaluate(false, "", false).CanApply, "unloaded preview cannot be confirmed");
        var reviewed = new ThemeConfiguration { Name = "Reviewed theme" };
        OperationConfirmation? pending = OperationConfirmation.Create("apply", 3, reviewed, false, false, "");
        var token = pending.Id;
        Check(!OperationConfirmation.TryTake(ref pending, "wrong-token", 3, out _) && pending is not null, "wrong confirmation token cannot consume the reviewed operation");
        Check(OperationConfirmation.TryTake(ref pending, token, 3, out var approved) && approved!.Configuration == reviewed && pending is null, "confirmation consumes the exact reviewed configuration");
        Check(!OperationConfirmation.TryTake(ref pending, token, 3, out _), "confirmation cannot be replayed");
        pending = OperationConfirmation.Create("apply", 3, reviewed, false, false, ""); token = pending.Id;
        Check(!OperationConfirmation.TryTake(ref pending, token, 4, out _) && pending is null, "editing after review expires the confirmation");
        pending = OperationConfirmation.Create("apply", 3, reviewed, false, false, "Unsupported client"); token = pending.Id;
        Check(!OperationConfirmation.TryTake(ref pending, token, 3, out _), "unsupported installation cannot pass confirmation");
        var fixture = Path.Combine(Path.GetTempPath(), "ThemeLauncher-WindowsTests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(fixture);
        UiText.Use("de"); UiText.Save(fixture); UiText.Use("en"); UiText.Load(fixture);
        Check(UiText.Current == "de" && UiText.Text("Restore previous setup") == "Vorherige Einrichtung wiederherstellen", "German native recovery translation and language persistence round trip");
        Check(UiText.Text("Unsupported stylesheet rule: @layer. @media, @supports, @container and named animations are supported; global registrations and nested CSS need a reviewed adapter.").StartsWith("Nicht unterstützte"), "German compatibility errors preserve the reported CSS rule");
        UiText.Use("en");
        using (var longConsent = new DiscordCheckBox { Text = "Ich darf diese Schrift und ihre Lizenz einbetten und mit geteilten Themenkonfigurationen weitergeben.", Font = LauncherStyle.Font() })
        {
            var preferred = longConsent.GetPreferredSize(new Size(300, 100));
            Check(preferred.Width <= 300 && preferred.Height > 35, "long German native consent wraps within the proposed width");
        }
        form.Shown += async (_, _) =>
        {
            try
            {
                var jpg = Path.Combine(fixture, "original.jpg");
                using (var bitmap = new Bitmap(120, 80)) { using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White); bitmap.Save(jpg, ImageFormat.Jpeg); }
                var imported = AssetImport.Image(jpg);
                Check(imported.MediaType == "image/png" && imported.Name == "original.png", "JPEG import normalizes to a static PNG");
                var config = new ThemeConfiguration { Background = "custom", CustomBackground = imported, Guild = "987654321012345678", Font = "fantafort" };
                AssetImport.ValidateImages(config); Check(true, "normalized image can reopen from a shared package");
                var invalid = Path.Combine(fixture, "active.png"); File.WriteAllText(invalid, "<svg onload='alert(1)'></svg>");
                Reject(() => AssetImport.Image(invalid), "disguised SVG cannot reach the image renderer");
                var oversized = Path.Combine(fixture, "large.png");
                using (var bitmap = new Bitmap(4097, 1)) bitmap.Save(oversized, ImageFormat.Png);
                Reject(() => AssetImport.Image(oversized), "native importer enforces image dimensions");
                Reject(() => AssetImport.ValidateImages(config with { CustomBackground = ThemeAsset.Create("broken.png", "image/png", new byte[50]) }), "unreadable shared image fails native decoding");
                var noise = Path.Combine(fixture, "noise.png");
                using (var bitmap = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb))
                {
                    var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    var pixels = new byte[Math.Abs(data.Stride) * bitmap.Height]; new Random(42).NextBytes(pixels);
                    for (int i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
                    Marshal.Copy(pixels, 0, data.Scan0, pixels.Length); bitmap.UnlockBits(data); bitmap.Save(noise, ImageFormat.Png);
                }
                config = config with { CustomBackground = AssetImport.Image(noise) };
                Check(ThemePreview.Html(config).Length > 2 * 1024 * 1024, "large preview exercises content above NavigateToString's limit");
                preview.Render(config); await preview.InitializeAsync();
                async Task Loaded()
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    while (!preview.Ready) await Task.Delay(100, timeout.Token);
                }
                await Loaded(); Check(preview.Ready, "actual WebView2 loads embedded font and large offline image");
                var core = preview.Controls.OfType<WebView2>().Single().CoreWebView2;
                var accent = await core.ExecuteScriptAsync("getComputedStyle(document.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()");
                Check(accent == System.Text.Json.JsonSerializer.Serialize(config.Accent), "native preview renders generated CSS");
                await core.ExecuteScriptAsync("document.querySelector('[data-list-item-id=\"guildsnav___other\"]').click()");
                Check(await core.ExecuteScriptAsync("getComputedStyle(document.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") == "\"\"", "native preview releases styling on a different server");
                string source = core.Source;
                await core.ExecuteScriptAsync("location.href='https://example.invalid/blocked'"); await Task.Delay(200);
                Check(core.Source == source, "native preview blocks external navigation");
                preview.Render(config with { Palette = "blurple" });
                preview.Render(config with { Palette = "custom", Accent = "#55ccaa", Surface = "#11151b" });
                await Loaded();
                Check(await core.ExecuteScriptAsync("getComputedStyle(document.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") == "\"#55ccaa\"", "rapid preview changes render only the latest configuration");
                using var builder = new BuilderForm(true);
                builder.Show();
                var studio = builder.Controls.OfType<StudioSurface>().Single();
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready) await Task.Delay(100, timeout.Token);
                var studioCore = studio.Controls.OfType<WebView2>().Single().CoreWebView2;
                async Task StudioState(string predicate)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    while (await studioCore.ExecuteScriptAsync(predicate) != "true") await Task.Delay(50, timeout.Token);
                }
                var capturePath = Path.GetFullPath(Path.Combine(".local", "studio-native-preview.png")); Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);
                await using (var capture = File.Create(capturePath)) await studioCore.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, capture);
                foreach (var page in new[] { "style", "art" })
                {
                    await studioCore.ExecuteScriptAsync("document.querySelector('[data-page=\"" + page + "\"]').click()");
                    await Task.Delay(75);
                    await using var capture = File.Create(Path.GetFullPath(Path.Combine(".local", "studio-native-" + page + ".png")));
                    await studioCore.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, capture);
                }
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-page=\"server\"]').click()");
                Check(await studioCore.ExecuteScriptAsync("!document.getElementById('apply').disabled && !document.getElementById('restart-ready') && document.getElementById('mode-badge').textContent==='Preview'") == "true", "normal preview exposes Apply without a separate restart checkbox");
                await studioCore.ExecuteScriptAsync("document.getElementById('apply').click()");
                await StudioState("document.getElementById('operation-confirmation').open");
                Check(builder.SimulatedOperations == 0 && await studioCore.ExecuteScriptAsync("document.getElementById('confirmation-guild').textContent==='987654321012345678'") == "true", "Apply opens an exact-target confirmation before any operation");
                Directory.CreateDirectory(Path.GetFullPath(".local"));
                await using (var capture = File.Create(Path.GetFullPath(Path.Combine(".local", "studio-native-confirmation.png")))) await studioCore.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, capture);
                await studioCore.ExecuteScriptAsync("document.getElementById('confirmation-cancel').click()");
                await StudioState("!document.getElementById('operation-confirmation').open");
                Check(builder.SimulatedOperations == 0, "Back to preview performs no operation");
                await studioCore.ExecuteScriptAsync("document.getElementById('apply').click()");
                await StudioState("document.getElementById('operation-confirmation').open");
                var approvedId = await studioCore.ExecuteScriptAsync("state.confirmation.id");
                await studioCore.ExecuteScriptAsync("document.getElementById('confirmation-continue').click()");
                await StudioState("!document.getElementById('operation-confirmation').open && document.getElementById('notice').textContent.includes('Simulation complete')");
                Check(builder.SimulatedOperations == 1, "Continue and apply runs exactly one safe simulation through the real native flow");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'confirmOperation',id:" + approvedId + ",allowSetup:false})");
                Check(builder.SimulatedOperations == 1, "replayed UI confirmation cannot repeat the operation");
                await studioCore.ExecuteScriptAsync("document.getElementById('apply').click()");
                await StudioState("document.getElementById('operation-confirmation').open");
                var expiredId = await studioCore.ExecuteScriptAsync("state.confirmation.id");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,name:'Changed after review'}})");
                await StudioState("!document.getElementById('operation-confirmation').open && state.values.name==='Changed after review'");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'confirmOperation',id:" + expiredId + ",allowSetup:false})");
                Check(builder.SimulatedOperations == 1, "late edits invalidate the native confirmation snapshot");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,name:'Your community'}})");
                await StudioState("state.values.name==='Your community' && !document.getElementById('apply').disabled");
                Check(await studioCore.ExecuteScriptAsync("document.querySelector('label[for=guild]').textContent.includes('Server ID') && document.querySelectorAll('[data-preset]').length===4") == "true", "studio exposes labeled server selection and four credited community designs");
                Check(await studioCore.ExecuteScriptAsync("[...document.querySelectorAll('.preset-card img')].every(image=>image.complete&&image.naturalWidth>0)") == "true", "actual native studio loads all bundled preset artwork");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'preset',value:'fantafort'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready || await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") != "\"#f5db36\"") await Task.Delay(100, timeout.Token);
                Check(true, "native preset selection updates the real preview through the validated bridge");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-context=other]').click()");
                Check(await studioCore.ExecuteScriptAsync("document.getElementById('preview-status').textContent.includes('Outside your target server')") == "true", "native studio explains why the theme is off in another server");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-context=target]').click()");
                Check(await studioCore.ExecuteScriptAsync("(()=>{const f=document.getElementById('preview');const r=f.contentDocument.querySelector('.scrollableContainer__74017').getBoundingClientRect();return r.bottom<=f.clientHeight})()") == "true", "native preview keeps the composer within its frame");
                string studioSource = studioCore.Source;
                await studioCore.ExecuteScriptAsync("location.href='https://example.invalid/blocked'"); await Task.Delay(150);
                Check(studioCore.Source == studioSource, "studio blocks external top-level navigation");
                await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.location.href='https://example.invalid/blocked'"); await Task.Delay(150);
                Check(await studioCore.ExecuteScriptAsync("!document.getElementById('preview').contentDocument?.URL.startsWith('https://example.invalid')") == "true", "studio blocks external iframe navigation");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-action=refresh]').click()");
                await Task.Delay(200);
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready) await Task.Delay(100, timeout.Token);
                await studioCore.ExecuteScriptAsync("document.getElementById('guild').value='invalid';document.getElementById('guild').dispatchEvent(new Event('input'))");
                await Task.Delay(400);
                Check(await studioCore.ExecuteScriptAsync("!document.getElementById('preview-error').hidden && document.getElementById('apply').disabled") == "true", "invalid target is explained over the previous preview and cannot be applied");
                await studioCore.ExecuteScriptAsync("document.getElementById('guild').value='987654321012345678';document.getElementById('guild').dispatchEvent(new Event('input'))");
                await Task.Delay(250);
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready) await Task.Delay(100, timeout.Token);
                Check(await studioCore.ExecuteScriptAsync("document.getElementById('preview-error').hidden") == "true", "valid target recovers the preview without reopening the application");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'preset',value:'primebuild'});window.chrome.webview.postMessage({action:'preset',value:'primebuild-dev'});window.chrome.webview.postMessage({action:'preset',value:'fantafort'})");
                await Task.Delay(150);
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready || await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") != "\"#f5db36\"") await Task.Delay(100, timeout.Token);
                Check(true, "rapid studio preset changes settle on the latest selected design");
                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='it';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='it' && document.getElementById('apply').textContent.includes('Applica su Discord')");
                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='es';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='es' && document.getElementById('apply').textContent.includes('Aplicar en Discord')");
                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='ru';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='ru' && document.getElementById('apply').textContent.includes('Применить в Discord')");
                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='en';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='en' && document.getElementById('apply').textContent.includes('Apply to Discord') && document.querySelector('label[for=guild]').textContent.includes('Server ID')");
                Check(true, "studio switches among English, Italian, Spanish and Russian, then returns to English");
                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='de';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='de' && document.getElementById('apply').textContent.includes('Auf Discord anwenden')");
                Check(UiText.Current == "de", "German WebView selection reaches native localization");
                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='en';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='en'");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'preset',value:'primebuild'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready || await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") != "\"#ff6600\"") await Task.Delay(100, timeout.Token);
                // Edits made by this test are synthetic; do not open the user's unsaved-edits dialog on disposal.
                builder.Hide();
                using var importedBuilder = new BuilderForm(true);
                importedBuilder.Show();
                var importedSurface = importedBuilder.Controls.OfType<StudioSurface>().Single();
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!importedSurface.Ready) await Task.Delay(100, timeout.Token);
                var importedCore = importedSurface.Controls.OfType<WebView2>().Single().CoreWebView2;
                await importedCore.ExecuteScriptAsync("document.querySelector('[data-action=new]').click()");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!importedSurface.Ready || await importedCore.ExecuteScriptAsync("state.values.guild==='' && document.getElementById('apply').disabled && document.getElementById('preview-error').hidden") != "true") await Task.Delay(100, timeout.Token);
                Check(true, "a new native design previews a synthetic server while Apply requires an explicit target");
                await importedCore.ExecuteScriptAsync("document.querySelector('[data-preset=\"zenit-gold\"]').click()");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!importedSurface.Ready || await importedCore.ExecuteScriptAsync("Boolean(state.imported?.sha256)") != "true") await Task.Delay(100, timeout.Token);
                Check(await importedCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--zg-gold').trim()") == "\"#e6c15e\"", "real native gallery selects the original Zenit design and reviewed offline resources");
                await importedCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'importVariable',name:'--zg-gold',value:'#31c6b4'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!importedSurface.Ready || await importedCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--zg-gold').trim()") != "\"#31c6b4\"") await Task.Delay(100, timeout.Token);
                Check(importedBuilder.SimulatedOperations == 0, "native imported variable edits remain preview-only");
                await importedCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,guild:'987654321012345678'}})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!importedSurface.Ready || await importedCore.ExecuteScriptAsync("!document.getElementById('apply').disabled") != "true") await Task.Delay(100, timeout.Token);
                await importedCore.ExecuteScriptAsync("document.getElementById('apply').click()");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (await importedCore.ExecuteScriptAsync("document.getElementById('operation-confirmation').open") != "true") await Task.Delay(50, timeout.Token);
                await importedCore.ExecuteScriptAsync("document.getElementById('confirmation-cancel').click()");
                Check(importedBuilder.SimulatedOperations == 0, "native imported application cancellation leaves the profile untouched");
                importedBuilder.Hide();
                Console.WriteLine($"{checks} Windows checks passed. Fixture: {fixture}");
                Environment.ExitCode = 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            finally { form.Close(); }
        };
        Application.Run(form);
    }
}
