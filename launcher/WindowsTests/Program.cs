using CommunityThemes;
using Microsoft.Web.WebView2.WinForms;
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
        var fixture = Path.Combine(Path.GetTempPath(), "ThemeLauncher-WindowsTests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(fixture);
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
                Check(accent == "\"#ff6600\"", "native preview renders generated CSS");
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
                IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
                var controls = Descendants(builder).ToArray();
                Check(controls.OfType<Button>().Where(b => b.Text is "Apply theme" or "Disable personal theme" or "Restore previous setup").All(b => !b.Enabled), "preview mode disables all Discord mutations");
                Check(controls.OfType<TextBox>().Any(t => t.AccessibleName == "Server ID or channel link") && controls.OfType<ComboBox>().Count() == 5, "native configurator exposes labeled server and appearance controls");
                Console.WriteLine($"{checks} Windows checks passed. Fixture: {fixture}");
                Environment.ExitCode = 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            finally { form.Close(); }
        };
        Application.Run(form);
    }
}
