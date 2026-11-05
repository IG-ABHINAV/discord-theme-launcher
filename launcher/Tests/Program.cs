using System.Text.Json.Nodes;
using CommunityThemes;

var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
var catalog = Catalog.Parse(File.ReadAllText(Path.Combine(root, "catalog.json")));
var fixture = Path.Combine(Path.GetTempPath(), "CommunityThemes-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixture);
int checks = 0;
void Check(bool result, string name) { if (!result) throw new Exception("FAILED: " + name); checks++; Console.WriteLine("PASS " + name); }
void Reject(Action action, string name) { bool rejected = false; try { action(); } catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or IOException) { rejected = true; } Check(rejected, name); }
var vencord = Path.Combine(fixture, "Vencord"); var state = Path.Combine(fixture, "State");
var store = new ThemeStore(vencord, state, catalog);
var asar = Path.Combine(fixture, "app.asar"); File.WriteAllText(asar, "require('Vencord/dist/patcher.js')");
Directory.CreateDirectory(Path.Combine(vencord, "dist"));
File.WriteAllText(Path.Combine(vencord, "dist", "renderer.js"), "// renderer exists, main-process patcher missing");
Check(!VencordInstallation.IsReady(asar, vencord), "renderer without patcher.js must not be reported ready");
foreach (var file in new[] {"patcher.js","preload.js","renderer.css"}) File.WriteAllText(Path.Combine(vencord,"dist",file),"// complete fixture");
Check(VencordInstallation.IsReady(asar, vencord), "all required readable runtime files permit startup");
File.WriteAllText(Path.Combine(vencord,"dist","patcher.js"), "");
Check(!VencordInstallation.IsReady(asar, vencord), "empty patcher also rejects startup");
File.WriteAllText(Path.Combine(vencord,"dist","patcher.js"), "// fixture patcher");
File.Delete(Path.Combine(vencord,"dist","preload.js"));
Check(!VencordInstallation.IsReady(asar,vencord) && VencordInstallation.MissingRuntimeFiles(vencord).SequenceEqual(new[]{"preload.js"}), "missing preload is identified before starting Discord");
Check(VencordInstallation.InspectWindowTitle("Error") == StartupState.Failed && VencordInstallation.InspectWindowTitle("Discord") == StartupState.Waiting && VencordInstallation.InspectWindowTitle("Friends - Discord") == StartupState.Ready, "native error and splash windows cannot be reported as a successful startup");
Directory.CreateDirectory(store.ThemesDirectory); Directory.CreateDirectory(Path.GetDirectoryName(store.SettingsPath)!);
File.WriteAllText(store.SettingsPath, """{"enabledThemes":["SomeoneElse.theme.css","PrimeBuild.theme.css"],"useQuickCss":true,"plugins":{"Other":{"enabled":true}},"themeLinks":["https://example.invalid/other.css"]}""");
File.WriteAllText(Path.Combine(store.ThemesDirectory, "SomeoneElse.theme.css"), "/* unrelated */");
File.WriteAllText(Path.Combine(store.ThemesDirectory, "PrimeBuild.theme.css"), "/* custom old version */");
var quickCss = Path.Combine(vencord, "settings", "quickCss.css"); File.WriteAllText(quickCss, "/* existing customization */");
var all = catalog.Themes.ToDictionary(t => t.File, t => File.ReadAllBytes(Path.Combine(root, t.File)));
store.Apply(catalog.Themes.Select(t => t.Id).ToArray(), all);
Check(store.InstalledSelection().Length == 3, "all selected themes installed and enabled");
Check(File.ReadAllText(Path.Combine(store.ThemesDirectory, "SomeoneElse.theme.css")) == "/* unrelated */", "unrelated theme bytes preserved");
var settings = JsonNode.Parse(File.ReadAllText(store.SettingsPath))!;
Check(settings["plugins"]!["Other"]!["enabled"]!.GetValue<bool>() && settings["themeLinks"]![0]!.GetValue<string>() == "https://example.invalid/other.css" && File.ReadAllText(quickCss) == "/* existing customization */", "plugins, online themes and QuickCSS unchanged");
Check(!Directory.EnumerateFiles(state, "*", SearchOption.AllDirectories).Any(path => File.ReadAllText(path).Contains("https://example.invalid")), "backup does not copy unrelated settings or credentials");
// Settings changed after applying must survive restoring the managed subset.
settings["newPreference"] = "keep-me"; File.WriteAllText(store.SettingsPath, settings.ToJsonString());
store.RestoreLast();
Check(File.ReadAllText(Path.Combine(store.ThemesDirectory, "PrimeBuild.theme.css")) == "/* custom old version */" && !File.Exists(Path.Combine(store.ThemesDirectory, "FantaFort.theme.css")), "restore recovers exact prior custom bytes and removes newly installed files");
settings = JsonNode.Parse(File.ReadAllText(store.SettingsPath))!;
Check(settings["newPreference"]!.GetValue<string>() == "keep-me" && settings["enabledThemes"]!.AsArray().Count == 2, "restore merges managed enablement without reverting unrelated new preferences");
store.RestoreLast(); Check(store.InstalledSelection().Length == 3, "restore itself is reversible with a new safety snapshot");
store.Apply(["fantafort"], all);
Check(store.InstalledSelection().SequenceEqual(new[] { "fantafort" }) && !File.Exists(Path.Combine(store.ThemesDirectory, "PrimeBuild.theme.css")), "selection installs one and removes only other managed themes");
var before = File.ReadAllText(store.SettingsPath);
var corrupt = all.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()); corrupt["FantaFort.theme.css"][0] ^= 1;
Reject(() => store.Apply(["fantafort"], corrupt), "corrupt download rejected");
Check(File.ReadAllText(store.SettingsPath) == before && store.InstalledSelection().SequenceEqual(new[] { "fantafort" }), "bad download causes no profile mutation");
Reject(() => store.Apply(["../escape"], all), "unknown selection rejected");
Reject(() => store.Apply(["primebuild"], new Dictionary<string,byte[]>()), "missing download rejected");
File.WriteAllText(store.SettingsPath, "{broken json");
Reject(() => store.Apply([], all), "malformed settings rejected before any file change");
Check(File.Exists(Path.Combine(store.ThemesDirectory, "FantaFort.theme.css")), "malformed settings leave installed file intact");
File.WriteAllText(store.SettingsPath, before);
store.Apply([], new Dictionary<string,byte[]>());
Check(store.InstalledSelection().Length == 0 && File.Exists(Path.Combine(store.ThemesDirectory, "SomeoneElse.theme.css")), "empty selection removes managed themes only");
store.RestoreLast(); Check(store.InstalledSelection().SequenceEqual(new[] { "fantafort" }), "empty-selection removal is reversible");
File.WriteAllText(Path.Combine(state, "last-backup.txt"), "../escape");
Reject(store.RestoreLast, "traversal in backup reference rejected");
Reject(() => Catalog.Parse(File.ReadAllText(Path.Combine(root, "catalog.json")).Replace("PrimeBuild.theme.css", "../escape.css")), "catalog file traversal rejected");
Reject(() => ReleaseDownload.Verify([1,2,3], ReleaseDownload.InstallerSha256), "unverified installer rejected");
// Link refusal is also tested on Linux CI, where creating a symlink needs no elevation.
if (!OperatingSystem.IsWindows())
{
    var linked = Path.Combine(fixture, "linked"); Directory.CreateSymbolicLink(linked, vencord);
    Reject(() => ThemeStore.CheckPath(Path.Combine(linked,"settings","settings.json")), "linked profile refused");
}
var config = new ThemeConfiguration { Name = "Synthetic community", Guild = "987654321012345678", Palette = "blurple" };
Check(ThemeConfiguration.ParseGuild(" https://discord.com/channels/987654321012345678/123456789012345678 ") == config.Guild && ThemeConfiguration.ParseGuild(config.Guild) == config.Guild, "server ID and channel-link selection");
foreach (var invalid in new[] { "", "@me", "123", "https://discord.gg/invite", "https://evil.invalid/channels/987654321012345678/123456789012345678", "99999999999999999999", "12345678901234567\"]){body{color:red}}" })
    Reject(() => ThemeConfiguration.ParseGuild(invalid), "invalid server target refused: " + invalid);
Reject(() => (config with { Schema = 2 }).Validate(), "future schema refused");
Reject(() => (config with { Guild = null! }).Validate(), "null target refused");
Reject(() => (config with { Accent = null! }).Validate(), "null color refused");
Reject(() => (config with { Palette = "custom", Surface = "#ffffff" }).Validate(), "unreadable custom surface refused");
Reject(() => (config with { Accent = "red; } body {color:red}" }).Validate(), "CSS injection through color refused");
Reject(() => (config with { Font = "custom" }).Validate(), "missing selected custom asset refused");
var png = ThemeAsset.Create("sample.png", "image/png", Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLttAAAAABJRU5ErkJggg=="));
var customFont = ThemeAsset.Create("Bangers-Regular.ttf", "font/ttf", File.ReadAllBytes(Path.Combine(root, "assets/fonts/bangers/Bangers-Regular.ttf")), File.ReadAllText(Path.Combine(root, "assets/fonts/bangers/OFL.txt")));
var portable = config with { Font = "custom", CustomFont = customFont, Banner = "custom", CustomBanner = png, Background = "custom", CustomBackground = png, Palette = "custom", Accent = "#48cfae", Surface = "#11151b" };
var package = Path.Combine(fixture, "portable.dtheme"); portable.Save(package); var reopened = ThemeConfiguration.Load(package);
Check(reopened.CustomFont!.Data.SequenceEqual(customFont.Data) && reopened.CustomBanner!.Data.SequenceEqual(png.Data) && reopened.CustomFont.License == customFont.License, "configuration round trip includes exact assets and license");
Check(ThemeCompiler.Compile(portable) == ThemeCompiler.Compile(reopened), "reopened configuration generates identical CSS");
Reject(() => (portable with { CustomBanner = png with { Sha256 = new string('0', 64) } }).Validate(), "tampered shared image rejected");
Reject(() => (portable with { CustomFont = customFont with { License = "" } }).Validate(), "unlicensed custom font rejected");
Reject(() => (portable with { CustomBanner = ThemeAsset.Create("active.svg", "image/svg+xml", System.Text.Encoding.UTF8.GetBytes("<svg onload='alert(1)'></svg>")) }).Validate(), "active SVG import rejected");
Reject(() => (portable with { CustomBanner = png with { Name = "../escape.png" } }).Validate(), "asset traversal metadata rejected");
var oversized = png.Data.ToArray(); System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(oversized.AsSpan(16), 5000);
Reject(() => (portable with { CustomBanner = ThemeAsset.Create("large.png", "image/png", oversized) }).Validate(), "oversized image dimensions rejected");
var restricted = customFont.Data.ToArray();
int tables = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(restricted.AsSpan(4));
for (int i = 0; i < tables; i++) if (System.Text.Encoding.ASCII.GetString(restricted, 12 + i * 16, 4) == "OS/2")
{
    int offset = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(restricted.AsSpan(20 + i * 16));
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(restricted.AsSpan(offset + 8), 2);
}
Reject(() => (portable with { CustomFont = ThemeAsset.Create("restricted.ttf", "font/ttf", restricted, "test license") }).Validate(), "restricted font embedding rejected");
var brokenFont = customFont.Data.ToArray(); System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(brokenFont.AsSpan(20), uint.MaxValue);
Reject(() => (portable with { CustomFont = ThemeAsset.Create("broken.ttf", "font/ttf", brokenFont, "test license") }).Validate(), "out-of-bounds font tables rejected");
File.WriteAllText(Path.Combine(state, "last-backup.txt"), "invalid");
store.Apply(catalog.Themes.Select(t => t.Id).ToArray(), all);
store.ApplyPersonal(portable with { Guild = catalog.Themes[0].Guild });
settings = JsonNode.Parse(File.ReadAllText(store.SettingsPath))!;
Check(store.PersonalEnabled && !store.InstalledSelection().Contains("primebuild") && store.InstalledSelection().Length == 2 && File.Exists(Path.Combine(store.ThemesDirectory, "PrimeBuild.theme.css")), "personal apply disables only the preset for the same server and preserves its bytes");
Check(settings["plugins"]!["Other"]!["enabled"]!.GetValue<bool>() && settings["newPreference"]!.GetValue<string>() == "keep-me" && File.ReadAllText(quickCss) == "/* existing customization */", "personal apply preserves plugins, preferences and QuickCSS");
store.ApplyPersonal(config);
Check(store.PersonalEnabled && File.ReadAllText(Path.Combine(store.ThemesDirectory, ThemeCompiler.PersonalFile)).Contains(config.Guild), "personal slot can be retargeted without accumulating managed files");
store.RestoreLast();
Check(File.ReadAllText(Path.Combine(store.ThemesDirectory, ThemeCompiler.PersonalFile)) == ThemeCompiler.Compile(portable with { Guild = catalog.Themes[0].Guild }), "restore recovers exact prior personal CSS");
store.ApplyPersonal(null); Check(!store.PersonalEnabled && store.InstalledSelection().Length == 2, "disable removes only the personal slot");
store.RestoreLast(); Check(store.PersonalEnabled, "personal disable is reversible");
var lastBackup = Path.Combine(state, "backups", File.ReadAllText(Path.Combine(state, "last-backup.txt")).Trim());
// This snapshot predates restore and has no personal file; corrupt a preset that did exist.
var savedCss = Path.Combine(lastBackup, "PrimeBuildDev.theme.css"); File.AppendAllText(savedCss, "corrupt");
before = File.ReadAllText(store.SettingsPath);
Reject(store.RestoreLast, "corrupt backup bytes rejected before restoration");
Check(File.ReadAllText(store.SettingsPath) == before && store.PersonalEnabled, "corrupt backup does not partially mutate active themes");
var legacyName = Guid.NewGuid().ToString("N"); var legacyFolder = Path.Combine(state, "backups", legacyName); Directory.CreateDirectory(legacyFolder);
File.WriteAllText(Path.Combine(legacyFolder, "snapshot.json"), System.Text.Json.JsonSerializer.Serialize(new Snapshot(catalog.Themes.Select(t => new SavedTheme(t.File, false, false)).ToArray())));
File.WriteAllText(Path.Combine(state, "last-backup.txt"), legacyName); store.RestoreLast();
Check(store.PersonalEnabled && store.InstalledSelection().Length == 0, "legacy three-file backups restore without changing the personal slot");
var previewDirectory = Path.Combine(root, ".local", "builder-fixtures"); Directory.CreateDirectory(previewDirectory);
foreach (var id in ThemeConfiguration.Presets)
{
    var example = config with { Preset = id, Palette = id, Font = id, Banner = id, Background = id };
    File.WriteAllText(Path.Combine(previewDirectory, id + ".html"), ThemePreview.Html(example));
}
File.WriteAllText(Path.Combine(previewDirectory, "custom.html"), ThemePreview.Html(portable));
File.WriteAllText(Path.Combine(previewDirectory, "native.html"), ThemePreview.Html(config with { Font = "native", Banner = "none", Background = "none" }));
File.WriteAllText(Path.Combine(previewDirectory, "escaped.html"), ThemePreview.Html(portable with { Name = "</style><script>alert('name')</script>", CustomFont = customFont with { License = "</style><script>alert('license')</script>*/" } }));
Check(!ThemePreview.Html(config with { Name = "<untrusted>" }).Contains("<untrusted>"), "preview escapes user-authored labels");
Console.WriteLine($"{checks} checks passed. Fixture: {fixture}");
// Intentional: leave the temporary test fixture for inspection; never recurse over an unchecked root.
