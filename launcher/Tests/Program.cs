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
Console.WriteLine($"{checks} checks passed. Fixture: {fixture}");
// Intentional: leave the temporary test fixture for inspection; never recurse over an unchecked root.
