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
                Check(builder.Icon is not null && builder.Icon.Width >= 16, "native window has the application icon");
                Check(await studioCore.ExecuteScriptAsync("document.querySelectorAll('#font [data-catalog]').length>=50 && Boolean(document.querySelector('[data-action=importFont]'))") == "true", "native editor exposes fifty-plus on-demand fonts and keeps font import");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-page=editor]').click()");
                var capturePath = Path.GetFullPath(Path.Combine(".local", "studio-native-preview.png")); Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);
                await using (var capture = File.Create(capturePath)) await studioCore.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, capture);
                foreach (var page in new[] { "style", "art" })
                {
                    await studioCore.ExecuteScriptAsync("document.querySelector('[data-tab=\"" + page + "\"]').click()");
                    await Task.Delay(75);
                    await using var capture = File.Create(Path.GetFullPath(Path.Combine(".local", "studio-native-" + page + ".png")));
                    await studioCore.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, capture);
                }
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-tab=\"server\"]').click()");
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
                Check(await studioCore.ExecuteScriptAsync("document.querySelector('label[for=guild]').textContent.includes('Server ID') && document.querySelectorAll('[data-preset]').length===5") == "true", "studio exposes labeled server selection and four credited community designs");
                Check(await studioCore.ExecuteScriptAsync("[...document.querySelectorAll('.preset-card img')].every(image=>image.complete&&image.naturalWidth>0)") == "true", "actual native studio loads all bundled preset artwork");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'preset',value:'fantafort'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready || await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") != "\"#f5db36\"") await Task.Delay(100, timeout.Token);
                Check(true, "native preset selection updates the real preview through the validated bridge");
                await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.setPreviewContext('other')");
                Check(await studioCore.ExecuteScriptAsync("document.getElementById('preview-status').textContent.includes('Outside your target server')") == "true", "native studio explains why the theme is off in another server");
                await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.setPreviewContext('target')");
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
                Check(await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentDocument.querySelector('.welcome-title').textContent==='Ein Ort, der sich nach dir anfühlt.'") == "true", "German translates the native illustrative preview");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-action=help]').click()");
                Check(await studioCore.ExecuteScriptAsync("document.getElementById('info-dialog').open && document.getElementById('info-body').textContent.includes('Wähle eine Vorlage')") == "true", "help is translated and embedded in the same WebView");
                await studioCore.ExecuteScriptAsync("document.getElementById('info-close').click();document.querySelector('[data-page=recovery]').click();document.getElementById('restore').click()");
                await StudioState("document.getElementById('reset-dialog').open");
                Check(builder.SimulatedOperations == 1, "opening recovery does not perform an operation");
                await studioCore.ExecuteScriptAsync("document.getElementById('reset-scope').value='disableAll';document.getElementById('reset-scope').dispatchEvent(new Event('change'))");
                await StudioState("state.confirmation?.operation==='disableAll' && !document.getElementById('reset-apply').disabled");
                var resetId = await studioCore.ExecuteScriptAsync("state.confirmation.id");
                await studioCore.ExecuteScriptAsync("document.getElementById('reset-apply').click()");
                await StudioState("!document.getElementById('reset-dialog').open");
                Check(builder.SimulatedOperations == 2, "all-themes recovery executes only the bound confirmed operation");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'confirmOperation',id:" + resetId + ",allowSetup:false})");
                Check(builder.SimulatedOperations == 2, "recovery confirmation cannot be replayed");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-action=new]').click()");
                await StudioState("document.getElementById('discard-dialog').open");
                await studioCore.ExecuteScriptAsync("document.getElementById('discard-cancel').click()");
                await StudioState("!document.getElementById('discard-dialog').open");
                Check(await studioCore.ExecuteScriptAsync("state.values.guild==='987654321012345678'") == "true", "canceling embedded discard preserves the current theme");

                await studioCore.ExecuteScriptAsync("document.getElementById('language').value='en';document.getElementById('language').dispatchEvent(new Event('change'))");
                await StudioState("document.documentElement.lang==='en'");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'preset',value:'primebuild'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!studio.Ready || await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--pb-accent').trim()") != "\"#ff6600\"") await Task.Delay(100, timeout.Token);
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,guild:'987654321012345678\\n1206415718728601600',dms:'876543210123456789',palette:'custom',accent:'#aabbff',surface:'#101218',panel:'#252939'}})");
                await StudioState("state.values.guild.includes('1206415718728601600') && !document.getElementById('apply').disabled");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-context=dm]').click()");
                await StudioState("document.getElementById('preview').contentDocument.body.dataset.context==='dm'");
                Check(await studioCore.ExecuteScriptAsync("document.getElementById('preview').contentWindow.getComputedStyle(document.getElementById('preview').contentDocument.querySelector('.base__5e434')).getPropertyValue('--pb-profile').trim().includes('personal')") == "true", "native multi-target configuration themes its selected DM conversation");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'savePalette',name:'Native violet'})");
                await StudioState("state.localPalettes.some(p=>p.name==='Native violet' && p.panel==='#252939')");
                Check(true,"native named palette retains all three custom colors");
                await studioCore.ExecuteScriptAsync("document.querySelector('[data-context=target]').click();window.chrome.webview.postMessage({action:'saveLocalCopy',name:'Native saved profile'})");
                await StudioState("state.localThemes.some(t=>t.name==='Native saved profile') && !state.dirty");
                await studioCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'renameLocalTheme',value:state.localThemeId,name:'Renamed native profile'})");
                await StudioState("state.localThemes.some(t=>t.name==='Renamed native profile')");
                Check(true,"native saved themes can be named and renamed in the library");
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
                var importFolder = Path.Combine(fixture, "ImportFolder"); Directory.CreateDirectory(Path.Combine(importFolder,"nested"));
                File.Copy("assets/community/zenit-gold/ZenitGold.theme.css", Path.Combine(importFolder,"Zenit.theme.css"));
                File.WriteAllText(Path.Combine(importFolder,"Unsupported.theme.css"), "@import url('https://example.invalid/remote.css');");
                new ThemeConfiguration { Name = "Reusable folder theme", Guild = "", Banner = "custom", CustomBanner = imported, Background = "custom", CustomBackground = imported }.Save(Path.Combine(importFolder,"nested","Reusable.dtheme"));
                importedBuilder.ReviewFolder(importFolder);
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (await importedCore.ExecuteScriptAsync("document.getElementById('folder-dialog').open && document.querySelectorAll('#folder-files button').length===3") != "true") await Task.Delay(50, timeout.Token);
                Check(await importedCore.ExecuteScriptAsync("[...document.querySelectorAll('#folder-files button')].find(b=>b.textContent==='Unsupported.theme.css').disabled && document.getElementById('folder-files').textContent.includes('@import')") == "true", "folder import reports incompatible CSS and includes nested portable themes");
                await importedCore.ExecuteScriptAsync("[...document.querySelectorAll('#folder-files button')].find(b=>b.textContent.includes('Reusable.dtheme')).click()");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (!importedSurface.Ready || await importedCore.ExecuteScriptAsync("state.values.name==='Reusable folder theme' && !document.getElementById('folder-dialog').open") != "true") await Task.Delay(50, timeout.Token);
                Check(importedBuilder.SimulatedOperations == 0, "selecting an imported folder design opens it in preview without applying");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (await importedCore.ExecuteScriptAsync("[...document.querySelectorAll('[data-banner=custom] img,[data-background=custom] img')].length===2 && [...document.querySelectorAll('[data-banner=custom] img,[data-background=custom] img')].every(i=>i.complete&&i.naturalWidth>0)") != "true") await Task.Delay(50,timeout.Token);
                Check(true, "actual selected-art endpoints load both custom image thumbnails");
                await importedCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'saveImage',kind:'banner',name:'Reusable native image'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (await importedCore.ExecuteScriptAsync("state.localImages.length===1") != "true") await Task.Delay(50,timeout.Token);
                await importedCore.ExecuteScriptAsync("document.querySelector('[data-images-kind=background]').click()");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (await importedCore.ExecuteScriptAsync("document.querySelector('#saved-images img')?.naturalWidth>0") != "true") await Task.Delay(50,timeout.Token);
                Check(true, "saved image endpoint renders the reusable image library");
                await importedCore.ExecuteScriptAsync("document.getElementById('image-library-close').click();window.chrome.webview.postMessage({action:'save'})");
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    while (await importedCore.ExecuteScriptAsync("state.localThemes.length===1 && Boolean(state.localThemeId)") != "true") await Task.Delay(50,timeout.Token);
                Check(importedBuilder.SimulatedOperations==0, "native local Save retains the reusable profile without restarting Discord or a file picker");


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
                var sessionRoot = Path.Combine(fixture, "StudioSession");
                async Task<(BuilderForm Form, CoreWebView2 Core)> OpenSession()
                {
                    var window = new BuilderForm(true, sessionRoot); window.Show();
                    var view = window.Controls.OfType<StudioSurface>().Single();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    while (!view.Ready) await Task.Delay(50, timeout.Token);
                    return (window, view.Controls.OfType<WebView2>().Single().CoreWebView2);
                }
                async Task SessionState(CoreWebView2 core, string expression)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    while (await core.ExecuteScriptAsync(expression) != "true") await Task.Delay(50, timeout.Token);
                }
                async Task SessionClosed(BuilderForm window)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    while (window.Visible) await Task.Delay(50, timeout.Token);
                }
                var firstSession = await OpenSession();
                await firstSession.Core.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,dms:'876543210123456789'}})");
                await SessionState(firstSession.Core, "state.values.dms==='876543210123456789' && document.getElementById('preview-loading').hidden");
                await firstSession.Core.ExecuteScriptAsync("document.querySelector('[data-context=dm]').click()");
                await SessionState(firstSession.Core, "currentContext==='dm'");
                await firstSession.Core.ExecuteScriptAsync("document.querySelector('[data-page=editor]').click();document.querySelector('[data-tab=style]').click();document.getElementById('theme-name').value='Last keystroke';document.getElementById('theme-name').dispatchEvent(new Event('input'))");
                firstSession.Form.Close();
                await SessionState(firstSession.Core, "document.getElementById('discard-dialog').open && state.values.name==='Last keystroke' && document.getElementById('discard-save').textContent==='Save & close'");
                Check(firstSession.Form.Visible, "closing flushes the final debounced keystroke and offers embedded Save and close");
                await firstSession.Core.ExecuteScriptAsync("document.getElementById('discard-cancel').click()");
                await SessionState(firstSession.Core, "!document.getElementById('discard-dialog').open");
                Check(!File.Exists(Path.Combine(sessionRoot,"studio-session.json")), "canceling close preserves the unsaved editor without saving implicitly");
                firstSession.Form.Close();
                await SessionState(firstSession.Core, "document.getElementById('discard-dialog').open");
                await firstSession.Core.ExecuteScriptAsync("document.getElementById('discard-save').click()");
                await SessionClosed(firstSession.Form);
                using var resumedSessionForm = (await OpenSession()).Form;
                var resumedCore = resumedSessionForm.Controls.OfType<StudioSurface>().Single().Controls.OfType<WebView2>().Single().CoreWebView2;
                await SessionState(resumedCore, "state.values.name==='Last keystroke' && currentPage==='editor' && editorTab==='style' && currentContext==='dm' && document.getElementById('preview').contentWindow.getPreviewContext()==='dm' && !state.dirty && Boolean(state.localThemeId) && document.querySelector('[data-action=save]').disabled");
                Check(true, "fresh studio resumes the saved document, local identity and exact editor section");
                await resumedCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,name:'Temporary name'}})");
                await SessionState(resumedCore, "state.dirty");
                await resumedCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,name:'Last keystroke'}})");
                await SessionState(resumedCore, "!state.dirty && document.querySelector('[data-action=save]').disabled");
                Check(true, "reverting document edits clears dirty state rather than requiring a redundant save");
                await resumedCore.ExecuteScriptAsync("window.chrome.webview.postMessage({action:'edit',values:{...state.values,surface:'#ffffff'}})");
                await SessionState(resumedCore, "state.dirty");
                resumedSessionForm.Close();
                await SessionState(resumedCore, "document.getElementById('discard-dialog').open");
                await resumedCore.ExecuteScriptAsync("document.getElementById('discard-save').click()");
                await SessionState(resumedCore, "document.getElementById('discard-problem').textContent.length>0");
                Check(resumedSessionForm.Visible, "failed validation keeps the close dialog open and explains why Save failed");
                await resumedCore.ExecuteScriptAsync("document.getElementById('discard-continue').click()");
                await SessionClosed(resumedSessionForm);
                using var discardedSessionForm = (await OpenSession()).Form;
                var discardedCore = discardedSessionForm.Controls.OfType<StudioSurface>().Single().Controls.OfType<WebView2>().Single().CoreWebView2;
                await SessionState(discardedCore, "state.values.name==='Last keystroke' && state.values.surface==='#202020' && !state.dirty");
                Check(true, "discarded changes do not return on the next launch or overwrite the saved profile");
                await discardedCore.ExecuteScriptAsync("document.querySelector('[data-page=community]').click();document.getElementById('template-tab-local').click()");
                discardedSessionForm.Close(); await SessionClosed(discardedSessionForm);
                using var finalSessionForm = (await OpenSession()).Form;
                var finalCore = finalSessionForm.Controls.OfType<StudioSurface>().Single().Controls.OfType<WebView2>().Single().CoreWebView2;
                await SessionState(finalCore, "currentPage==='community' && document.getElementById('template-tab-local').getAttribute('aria-selected')==='true'");
                Check(true, "clean exit resumes Templates and its local library tab without asking to save");
                await finalCore.ExecuteScriptAsync("document.querySelector('[data-page=editor]').click();document.querySelector('[data-tab=style]').click();document.getElementById('palette-name').value='Palette name only'");
                finalSessionForm.Close();
                await SessionState(finalCore, "document.getElementById('discard-dialog').open && !state.dirty");
                Check(true, "an unsaved palette name also prompts on close even when the theme itself is clean");
                await finalCore.ExecuteScriptAsync("document.getElementById('discard-save').click()");
                await SessionClosed(finalSessionForm);
                using var paletteSessionForm = (await OpenSession()).Form;
                var paletteCore = paletteSessionForm.Controls.OfType<StudioSurface>().Single().Controls.OfType<WebView2>().Single().CoreWebView2;
                await SessionState(paletteCore, "state.localPalettes.some(p=>p.name==='Palette name only') && document.getElementById('save-palette').disabled");
                Check(true, "Save and close preserves pending palette changes as well as the current theme");
                paletteSessionForm.Close(); await SessionClosed(paletteSessionForm);
                var brokenSessionRoot = Path.Combine(fixture, "BrokenStudioSession"); Directory.CreateDirectory(brokenSessionRoot);
                File.WriteAllText(Path.Combine(brokenSessionRoot, "studio-session.json"), "{\"configuration\":null}");
                using var brokenSession = new BuilderForm(true, brokenSessionRoot);
                Check(true, "a corrupt session falls back safely instead of preventing application startup");
                Console.WriteLine($"{checks} Windows checks passed. Fixture: {fixture}");
                Environment.ExitCode = 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            finally { form.Close(); }
        };
        Application.Run(form);
    }
}
