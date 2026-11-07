using System.Text.Json.Nodes;
using CommunityThemes;

if (args is ["--restore-profile", var isolatedProfile, var isolatedState])
{
    new ThemeStore(isolatedProfile, isolatedState, ThemeCompiler.BundledCatalog).RestoreLast();
    Console.WriteLine("Isolated profile recovery completed in a fresh process.");
    return;
}

var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
var catalog = Catalog.Parse(File.ReadAllText(Path.Combine(root, "catalog.json")));
// Public catalog seam: an additional locally verified theme is data, not a launcher allowlist edit.
var extendedJson = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "catalog.json")))!;
var additional = extendedJson["themes"]![0]!.DeepClone();
additional["id"] = "fourth"; additional["file"] = "Fourth.theme.css"; additional["guild"] = "987654321012345678";
extendedJson["themes"]!.AsArray().Add(additional);
var extendedCatalog = Catalog.Parse(extendedJson.ToJsonString());
if (extendedCatalog.Themes.Length != 4) throw new Exception("Extensible catalog lost fourth entry");
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
var resources = Path.Combine(fixture, "resources"); Directory.CreateDirectory(resources);
var stub = Path.Combine(resources, "app.asar"); var original = Path.Combine(resources, "_app.asar");
var missingPatcher = Path.Combine(fixture, "missing", "Vencord", "dist", "patcher.js");
var missingStub = "require(" + System.Text.Json.JsonSerializer.Serialize(missingPatcher) + ")";
File.WriteAllText(stub, missingStub);
File.WriteAllBytes(original, new byte[100_000]);
Check(VencordInstallation.StubIsUnbootable(stub) && !VencordInstallation.IsReady(stub, vencord), "a stub whose patcher.js is missing must not boot");
Check(VencordInstallation.TryRestoreOriginal(resources) && new FileInfo(stub).Length == 100_000 && !File.Exists(original), "missing patcher.js restores the official Discord package and does not start the stub");
File.WriteAllText(stub, missingStub);
File.WriteAllBytes(original, new byte[50]);
Check(!VencordInstallation.TryRestoreOriginal(resources) && File.ReadAllText(stub).Contains("patcher.js"), "a tiny backup is not treated as the official package");
var present = Path.Combine(vencord, "dist", "patcher.js");
File.WriteAllText(stub, "require(" + System.Text.Json.JsonSerializer.Serialize(present) + ")");
File.WriteAllBytes(original, new byte[100_000]);
Check(!VencordInstallation.StubIsUnbootable(stub) && !VencordInstallation.TryRestoreOriginal(resources) && File.Exists(original), "an existing patcher.js is left installed");
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
Check(new ThemeConfiguration().Guild == "" && new ThemeConfiguration().Preset == "fluent", "new themes are neutral and have no implicit target server");
var cssReview = CssThemeImporter.Review(System.Text.Encoding.UTF8.GetBytes("/** @name Teal glass */\n:root{--accent:#18bfae} body{background:#122026} .chat_f75fb0{border:2px solid var(--accent)}"), "Teal.theme.css");
Check(cssReview.Supported && cssReview.Theme is not null, "self-contained imported design passes compatibility review");
var importedCss = CssThemeImporter.Compile(cssReview.Theme!, "987654321012345678");
Check(importedCss.Contains("--accent:#18bfae") && importedCss.Contains("border:2px solid var(--accent)"), "import compilation retains original design rather than substituting a preset");
var zenit = CommunityGallery.Create("zenit-gold", config.Guild);
Check(CssThemeImporter.Load(Path.Combine(root, "assets/community/zenit-gold/ZenitGold.theme.css")).Sha256 == CssThemeImporter.ZenitHash, "a downloaded .theme.css file follows the same pinned import seam used by the native picker");
Check(zenit.Imported!.Sha256 == CssThemeImporter.ZenitHash && ThemeCompiler.Compile(zenit).Contains("--zg-gold:#e6c15e"), "pinned Zenit original imports with its author design and offline font");
var draculaReview = CssThemeImporter.Review(File.ReadAllBytes(Path.Combine(root, "tests/imports/dracula/DraculaBase.theme.css")), "Dracula.theme.css", "https://github.com/dracula/betterdiscord/tree/77e73629086e5b77381f87b255b9f96401c61ced", File.ReadAllText(Path.Combine(root, "tests/imports/dracula/LICENSE")));
Check(draculaReview.Supported && draculaReview.Variables.Any(v => v.Name == "--dracula-purple"), "materially different pinned Dracula design imports and exposes author variables");
Check(!CssThemeImporter.Review(System.Text.Encoding.UTF8.GetBytes(".safe} .outside{color:red}"), "Breakout.theme.css").Supported, "unbalanced selector cannot escape the server scope");
Check(!CssThemeImporter.Review(System.Text.Encoding.UTF8.GetBytes(":root:where(.theme-dark){all:unset!important;display:contents!important} .chat{position:fixed}"), "Containment.theme.css").Supported, "import cannot remove server containment through root resets");
foreach (string badCss in new[] {
    "@import url(\"https://example.invalid/all.css\"); .chat{color:red}",
    ":root{--image:url(\"https://example.invalid/image.png\")}",
    ".chat{background:url(\"file:///C:/private.png\")}",
    ".chat{background:url(\"../private.png\")}",
    ".chat{background:image-set(\"//example.invalid/image.png\" 1x)}",
    ".chat{background:u\\72l(\"https://example.invalid/image.png\")}",
    ".chat{background:url(\"data:image/svg+xml;base64,PHN2Zz4=\")}",
    "@property --evil{syntax:'*';inherits:true;initial-value:red}",
    ".chat{color:red;.nested{color:blue}}", "body .chat{color:red", ".chat:is(.safe} .outside){color:red}",
    ".chat{background:url(\"data:image/png;base64,AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\")}",
    "html body .chat{color:red}", "@font-face{font-family:X;src:url(\"https://example.invalid/font.ttf\")}" })
    Check(!CssThemeImporter.Review(System.Text.Encoding.UTF8.GetBytes(badCss), "Unsupported.theme.css").Supported, "unsupported CSS rejected before mutation: " + badCss[..Math.Min(50, badCss.Length)]);
Check(!CssThemeImporter.Review(new byte[CssThemeImporter.MaxBytes + 1], "TooLarge.theme.css").Supported, "oversized CSS is rejected");
Check(!CssThemeImporter.Review([255,254,0], "BadEncoding.theme.css").Supported, "invalid UTF-8 CSS is rejected");
Check(!CssThemeImporter.Review(zenit.Imported!.Original, "../escape.theme.css").Supported, "CSS import traversal name is rejected");
Reject(() => CssThemeImporter.Compile(zenit.Imported! with { Sha256 = new string('0',64) }, config.Guild), "corrupt imported source rejected on reopen/compile");
Reject(() => CssThemeImporter.Compile(zenit.Imported! with { Edits = new() { ["--zg-gold"] = "red;}body{color:red" } }, config.Guild), "CSS injection through imported variable edit rejected");
Reject(() => CssThemeImporter.Compile(zenit.Imported!, ""), "import application requires an explicit target");
var legacyConfiguration = Path.Combine(fixture, "legacy-beta7.dtheme");
File.WriteAllText(legacyConfiguration, """{"schema":1,"name":"Legacy saved theme","guild":"987654321012345678","preset":"primebuild-dev","palette":"primebuild-dev","accent":"#ff782e","surface":"#10131b","font":"primebuild-dev","banner":"primebuild-dev","background":"primebuild-dev"}""");
Check(ThemeConfiguration.Load(legacyConfiguration).Preset == "primebuild-dev" && ThemeCompiler.Compile(ThemeConfiguration.Load(legacyConfiguration)).Contains("#ff782e"), "existing beta.7 schema-one configurations retain their explicit design choices");
foreach (var design in new[] { zenit, new ThemeConfiguration { Name = "Dracula", Guild = config.Guild, Imported = draculaReview.Theme } })
{
    var path = Path.Combine(fixture, design.Name.Replace(' ', '-') + ".dtheme"); design.Save(path);
    var copy = ThemeConfiguration.Load(path);
    Check(copy.Imported!.Original.SequenceEqual(design.Imported!.Original) && ThemeCompiler.Compile(copy) == ThemeCompiler.Compile(design), "imported portable save/reopen retains original bytes, license and exact generated design: " + design.Name);
    store.ApplyPersonal(design); var exact = File.ReadAllBytes(Path.Combine(store.ThemesDirectory, ThemeCompiler.PersonalFile));
    store.ApplyPersonal(design);
    using var recoveryProcess = new System.Diagnostics.Process { StartInfo = new("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true } };
    foreach (var argument in new[] { System.Reflection.Assembly.GetExecutingAssembly().Location, "--restore-profile", vencord, state }) recoveryProcess.StartInfo.ArgumentList.Add(argument);
    recoveryProcess.Start();
    if (!recoveryProcess.WaitForExit(15000)) { recoveryProcess.Kill(); throw new Exception("Fresh-process profile recovery timed out"); }
    Check(recoveryProcess.ExitCode == 0 && File.ReadAllBytes(Path.Combine(store.ThemesDirectory, ThemeCompiler.PersonalFile)).SequenceEqual(exact), "repeated imported apply and fresh-process recovery preserves exact bytes: " + design.Name);
}
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
var pixelBudgetCss = new System.Text.StringBuilder(":root{");
for (int image = 0; image < 3; image++)
{
    var manyPixels = png.Data.ToArray();
    System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(manyPixels.AsSpan(16), 4000);
    System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(manyPixels.AsSpan(20), 4000);
    manyPixels[^1] = (byte)image;
    pixelBudgetCss.Append("--image").Append(image).Append(":url(\"data:image/png;base64,").Append(Convert.ToBase64String(manyPixels)).Append("\");");
}
pixelBudgetCss.Append('}');
Check(!CssThemeImporter.Review(System.Text.Encoding.UTF8.GetBytes(pixelBudgetCss.ToString()), "PixelBudget.theme.css").Supported, "aggregate declared image pixels are bounded before allocating preview bitmaps");
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
Check(!CssThemeImporter.Review(System.Text.Encoding.UTF8.GetBytes(":root{--image:url(\"data:image/png;base64," + Convert.ToBase64String(oversized) + "\")}"), "LargeImage.theme.css").Supported, "embedded CSS image dimensions are bounded before preview decoding");
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
// Reopening an old snapshot after catalog growth must neither reject it nor delete new files.
var fourthPath = Path.Combine(store.ThemesDirectory, "Fourth.theme.css");
File.WriteAllBytes(fourthPath, [0, 255, 13, 10]);
var grownStore = new ThemeStore(vencord, state, extendedCatalog);
File.WriteAllText(Path.Combine(state, "last-backup.txt"), legacyName);
grownStore.RestoreLast();
Check(File.ReadAllBytes(fourthPath).SequenceEqual(new byte[] { 0, 255, 13, 10 }) && grownStore.PersonalEnabled, "catalog growth preserves newer files and personal slot when restoring historical snapshots");
Check(File.ReadAllText(Path.Combine(vencord, "dist", "patcher.js")) == "// fixture patcher", "apply and recovery preserve Vencord runtime files");
var previewDirectory = Path.Combine(root, ".local", "builder-fixtures"); Directory.CreateDirectory(previewDirectory);
File.WriteAllText(Path.Combine(previewDirectory, "fluent.html"), ThemePreview.Html(new ThemeConfiguration { Guild = config.Guild }));
File.WriteAllText(Path.Combine(previewDirectory, "zenit-gold.html"), ThemePreview.Html(zenit));
var editedZenit = zenit with { Imported = zenit.Imported! with { Edits = new() { ["--zg-gold"] = "#31c6b4" } } };
File.WriteAllText(Path.Combine(previewDirectory, "zenit-edited.html"), ThemePreview.Html(editedZenit));
File.WriteAllText(Path.Combine(previewDirectory, "dracula.html"), ThemePreview.Html(new ThemeConfiguration { Guild = config.Guild, Name = "Dracula", Imported = draculaReview.Theme }));
File.WriteAllText(Path.Combine(previewDirectory, "zenit-review.json"), System.Text.Json.JsonSerializer.Serialize(new { supported = true, name = zenit.Name, author = zenit.Imported!.Author, source = zenit.Imported.Source, sha256 = zenit.Imported.Sha256, license = zenit.Imported.License, notes = CssThemeImporter.Inspect(zenit.Imported).Notes, resources = CssThemeImporter.Inspect(zenit.Imported).Resources, variables = CssThemeImporter.Inspect(zenit.Imported).Variables.Select(v => new { name = v.Name, value = v.Value, kind = v.Kind }) }));
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
