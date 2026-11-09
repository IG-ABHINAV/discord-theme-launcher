using System.Text.Json;

namespace CommunityThemes;

internal sealed record StudioValues(string Name, string Guild, string Preset, string Palette, string Accent, string Surface, string Font, string Banner, string Background, string? Panel = null, string Dms = "", string Description = "")
{
    public static StudioValues From(ThemeConfiguration c) => new(c.Name, string.Join("\n", c.TargetGuilds), c.Preset, c.Palette, c.Accent, c.Surface, c.Font, c.Banner, c.Background, c.Panel ?? c.Surface, string.Join("\n", c.DmChannels), c.Description);
}
internal sealed record StudioLocation(string Page = "home", string EditorTab = "server", string TemplatesTab = "standard", string PreviewContext = "target");
internal sealed record StudioSession(ThemeConfiguration Configuration, string? LocalThemeId, string PaletteId, bool Saved, StudioLocation Location);

internal sealed class BuilderForm : Form
{
    private readonly bool dryRun;
    private readonly DiscordClient client = new();
    private readonly ThemeStore store;
    private readonly LocalLibrary library;
    private readonly string documentRoot;
    private readonly bool persistSession;
    private StudioLocation location = new();
    private ThemeConfiguration cleanConfiguration = new();
    private string? cleanLocalThemeId;
    private string cleanPaletteId = "", cleanKey = "", discardProblem = "";
    private bool cleanSaved, closeApproved;
    private string? pendingPaletteName;
    private string? localThemeId;
    private string paletteId = "";
    private readonly StudioSurface surface = new();
    private ThemeConfiguration configuration = new();
    private int revision, renderedRevision = -1;
    private bool busy, dirty, saved;
    private OperationConfirmation? confirmation;
    private CssImportReview? importReview;
    private JsonElement? pendingDiscard;
    private bool pendingClose, discardApproved;
    private string[]? importPaths;
    private object[]? importCandidates;
    internal int SimulatedOperations { get; private set; }
    private string notice = "Design here. Preview here. Apply when you're ready.", validation = "";
    private string DraftPath => Path.Combine(documentRoot, "last-configuration.dtheme");
    private string SessionPath => Path.Combine(documentRoot, "studio-session.json");

    public BuilderForm(bool safePreview, string? sessionRoot = null)
    {
        dryRun = safePreview;
        store = new ThemeStore(client.VencordRoot, client.StateRoot, ThemeCompiler.BundledCatalog);
        documentRoot = sessionRoot ?? (dryRun ? Path.Combine(Path.GetTempPath(), "ThemeStudio-library-" + Guid.NewGuid().ToString("N")) : client.StateRoot);
        persistSession = !dryRun || sessionRoot is not null;
        library = new LocalLibrary(Path.Combine(documentRoot, "library"));
        surface.LocalImageRequested = id => library.LoadImage(id).Asset;
        BackColor = LauncherStyle.Surface; ForeColor = LauncherStyle.Text; Font = LauncherStyle.Font(); AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.Format("Theme Studio · Discord Theme Launcher — {0}", ThemeCompiler.BundledCatalog.Version);
        ClientSize = new Size(1420, 900); MinimumSize = new Size(1020, 700); StartPosition = FormStartPosition.CenterScreen;
        using (var iconStream = new MemoryStream(StudioSurface.Resource("app-icon.ico")))
        using (var icon = new Icon(iconStream)) Icon = (Icon)icon.Clone();
        Controls.Add(surface);
        surface.Loaded += Publish;
        surface.MessageReceived += async message => await HandleMessageAsync(message);
        surface.RuntimeUnavailable += ShowRuntimeFallback;
        if (dryRun) configuration = new ThemeConfiguration { Name = "Your community", Guild = "987654321012345678" };
        if (persistSession && File.Exists(DraftPath))
        {
            try { var draft = ThemeConfiguration.Load(DraftPath); AssetImport.ValidateImages(draft); configuration = draft; saved = true;
                var encoded = JsonSerializer.Serialize(configuration, Catalog.JsonOptions);
                localThemeId = library.Themes().Where(t => t.Name == configuration.Name).FirstOrDefault(t => JsonSerializer.Serialize(library.LoadTheme(t.Id), Catalog.JsonOptions) == encoded)?.Id; }
            catch { notice = "The last configuration could not be reopened. Open another .dtheme file or start a new one."; }
        }
        if (persistSession && File.Exists(SessionPath))
        {
            try
            {
                ThemeStore.CheckPath(SessionPath);
                if (new FileInfo(SessionPath).Length > ThemeConfiguration.MaxFileBytes + 4096) throw new InvalidDataException();
                var session = JsonSerializer.Deserialize<StudioSession>(File.ReadAllText(SessionPath), Catalog.JsonOptions) ?? throw new InvalidDataException();
                if (session.Configuration is null) throw new InvalidDataException();
                session.Configuration.Validate(false); AssetImport.ValidateImages(session.Configuration);
                if (!ValidLocation(session.Location)) throw new InvalidDataException();
                configuration = session.Configuration; localThemeId = library.Themes().Any(t => t.Id == session.LocalThemeId) ? session.LocalThemeId : null;
                paletteId = library.Palettes().Any(p => p.Id == session.PaletteId) ? session.PaletteId : "";
                saved = session.Saved; location = session.Location;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException) { }
        }
        MarkClean();
        FormClosing += async (_, e) => {
            if (busy) { e.Cancel = true; return; }
            if (!closeApproved && surface.Available)
            {
                e.Cancel = true;
                await surface.PrepareCloseAsync();
                return;
            }
            if (dirty && !ConfirmDiscard()) { e.Cancel = true; return; }
            try { SaveSession(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                e.Cancel = true; closeApproved = false; notice = "The session could not be saved. Check disk space and permissions, then try closing again."; Publish();
            }
        };
        Load += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width - 16), Math.Min(MinimumSize.Height, area.Height - 16));
            Size = new Size(Math.Min(Width, area.Width - 16), Math.Min(Height, area.Height - 16));
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        };
        Shown += async (_, _) =>
        {
            LauncherStyle.DarkCaption(this);
            await surface.InitializeAsync();
        };
        RenderPreview();
    }

    private ThemeConfiguration ReadConfiguration(bool requireTarget = true)
    {
        var guilds = ThemeConfiguration.ParseTargets(string.Join("\n", configuration.TargetGuilds));
        var dms = ThemeConfiguration.ParseTargets(string.Join("\n", configuration.DmChannels), true);
        var value = configuration with { Name = configuration.Name.Trim(), Guild = guilds.FirstOrDefault() ?? "", AdditionalGuilds = guilds.Skip(1).ToArray(), DmChannels = dms,
            CustomFont = configuration.Font == "custom" ? configuration.CustomFont : null,
            CustomBanner = configuration.Banner == "custom" ? configuration.CustomBanner : null,
            CustomBackground = configuration.Background == "custom" ? configuration.CustomBackground : null };
        value.Validate(requireTarget); return value;
    }
    private void ShowRuntimeFallback()
    {
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 70, Padding = new Padding(30, 8, 0, 0), BackColor = LauncherStyle.Surface };
        foreach (var (label, action) in new (string, Action)[] { ("Open configuration", OpenDocument), ("Save a copy", () => SaveDocument(true)), ("Help", Help) })
        {
            var button = new DiscordButton { Text = UiText.Text(label), AutoSize = true, Padding = new Padding(12, 7, 12, 7) };
            button.Click += (_, _) => { try { action(); } catch { LauncherStyle.ShowMessage(this, UiText.Text("The configuration could not be processed. Check the format, size and permissions."), UiText.Text("Configuration action")); } };
            actions.Controls.Add(button);
        }
        surface.Controls.Add(actions); actions.BringToFront();
    }
    private string DocumentKey(ThemeConfiguration? value = null)
    {
        value ??= configuration;
        return JsonSerializer.Serialize(new { values = StudioValues.From(value), font = value.CustomFont?.Sha256, banner = value.CustomBanner?.Sha256, background = value.CustomBackground?.Sha256, value.Imported }, Catalog.JsonOptions);
    }
    private void MarkClean()
    {
        dirty = false; cleanConfiguration = configuration; cleanKey = DocumentKey();
        cleanLocalThemeId = localThemeId; cleanPaletteId = paletteId; cleanSaved = saved;
    }
    private void Changed() { confirmation = null; revision++; dirty = DocumentKey() != cleanKey; RenderPreview(); }
    private static bool ValidLocation(StudioLocation? value) => value is not null &&
        new[] { "home", "community", "editor", "recovery" }.Contains(value.Page) &&
        new[] { "server", "style", "art", "library" }.Contains(value.EditorTab) &&
        new[] { "standard", "local" }.Contains(value.TemplatesTab) && new[] { "target", "dm" }.Contains(value.PreviewContext);
    private void SaveSession()
    {
        if (!persistSession) return;
        var value = dirty ? cleanConfiguration : ReadConfiguration(false);
        ThemeStore.AtomicWrite(SessionPath, JsonSerializer.SerializeToUtf8Bytes(new StudioSession(value, dirty ? cleanLocalThemeId : localThemeId, dirty ? cleanPaletteId : paletteId, dirty ? cleanSaved : saved, location), Catalog.JsonOptions));
    }
    private void CloseWhenReady(string paletteName)
    {
        var palette = library.Palettes().FirstOrDefault(p => p.Id == paletteId);
        pendingPaletteName = configuration.Palette == "custom" && (palette is not null || paletteName.Length > 0) &&
            (palette is null || palette.Name != paletteName || !string.Equals(palette.Accent, configuration.Accent, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(palette.Surface, configuration.Surface, StringComparison.OrdinalIgnoreCase) || !string.Equals(palette.Panel, configuration.Panel ?? configuration.Surface, StringComparison.OrdinalIgnoreCase)) ? paletteName : null;
        if (dirty || pendingPaletteName is not null) { pendingClose = true; discardProblem = ""; Publish(); }
        else { closeApproved = true; Close(); }
    }
    private async Task ContinuePendingAsync(bool save)
    {
        if (!pendingClose && pendingDiscard is null) return;
        if (save)
        {
            if (pendingPaletteName is not null) paletteId = library.SavePalette(new PaletteProfile(paletteId, pendingPaletteName, configuration.Accent, configuration.Surface, configuration.Panel ?? configuration.Surface));
            SaveDocument(false);
        }
        var nextAction = pendingDiscard; bool close = pendingClose;
        pendingDiscard = null; pendingClose = false; discardProblem = ""; pendingPaletteName = null;
        if (close)
        {
            if (!save) { configuration = cleanConfiguration; localThemeId = cleanLocalThemeId; paletteId = cleanPaletteId; saved = cleanSaved; MarkClean(); }
            closeApproved = true; Close();
        }
        else if (nextAction is { } next)
        {
            discardApproved = true;
            try { await HandleMessageAsync(next); }
            finally { discardApproved = false; }
        }
        Publish();
    }
    private void RenderPreview()
    {
        surface.SetArtwork(configuration);
        try
        {
            // A synthetic target lets a new design render without choosing a real server implicitly.
            var current = ReadConfiguration(false);
            bool targetMissing = current.TargetGuilds.Length == 0 && current.DmChannels.Length == 0;
            var value = targetMissing ? current with { Guild = "987654321012345678" } : current;
            validation = targetMissing ? "Choose at least one server or DM chat." : "";
            renderedRevision = revision; surface.Render(value);
        }
        catch (InvalidDataException error) { validation = error.Message; renderedRevision = -1; }
        Publish();
    }
    private void Publish()
    {
        bool canDisable = false, canRestore = false;
        RecoveryEntry[] history = [];
        try { if (!dryRun) { canDisable = store.PersonalEnabled; canRestore = store.CanRestore; history = store.RecoveryHistory(); } }
        catch { notice = "The Vencord profile could not be read. Check the manual recovery guide."; }
        var patched = client.Patched;
        var readiness = ApplyReadiness.Evaluate(busy, validation, renderedRevision == revision && surface.Ready);
        surface.Publish(new { type = "state", values = StudioValues.From(configuration), assets = new { font = configuration.CustomFont?.Name, banner = configuration.CustomBanner?.Name, background = configuration.CustomBackground?.Name, bannerHash = configuration.CustomBanner?.Sha256, backgroundHash = configuration.CustomBackground?.Sha256 },
            busy, dirty, saved, version = ThemeCompiler.BundledCatalog.Version, notice = UiText.Text(notice),
            validation = UiText.Text(validation), previewId = surface.PreviewId, previewUri = renderedRevision == revision && (validation.Length == 0 || validation == "Choose at least one server or DM chat.") ? surface.PreviewUri : "",
            canApply = readiness.CanApply, applyReason = UiText.Text(readiness.Reason), applyAction = readiness.Action, applyActionLabel = readiness.ActionLabel.Length == 0 ? "" : UiText.Text(readiness.ActionLabel),
            canDisable = canDisable && !busy, canRestore = canRestore && !busy,
            designs = CommunityGallery.Designs,
            fonts = FontLibrary.Choices.Select(f => new { f.Id, f.Family, f.Category, fileName = Path.GetFileName(f.Path) }),
            recoveryHistory = history, localThemes = library.Themes(), localPalettes = library.Palettes(), localImages = library.Images(), paletteId, localThemeId,
            discardPending = pendingDiscard.HasValue || pendingClose, closing = pendingClose, discardProblem = UiText.Text(discardProblem), location, importCandidates,
            imported = configuration.Imported is { } imported ? ReviewState(CssThemeImporter.Inspect(imported)) : null,
            importReview = importReview is null ? null : ReviewState(importReview),
            confirmation = confirmation is { } pending ? new { pending.Id, pending.Operation, themeName = pending.Configuration?.Name,
                guild = pending.Configuration is { } targets ? string.Join(", ", targets.TargetGuilds) : "", dms = pending.Configuration?.DmChannels, serverName = pending.Configuration is { } scope && (scope.TargetGuilds.Length != 1 || scope.DmChannels.Length > 0) ? UiText.Text("Selected targets") : ThemeCompiler.BundledCatalog.Themes.FirstOrDefault(t => t.Guild == pending.Configuration?.Guild)?.Server ?? UiText.Text("Selected server"),
                pending.RequiresSetup, pending.Repair, pending.RecoveryId, problem = UiText.Text(pending.Problem) } : null,
            installation = UiText.Text(patched ? "Discord + Vencord ready. No installation needed." : client.Unbootable ? "Discord cannot start because Vencord's patcher.js is missing. The launcher repairs it with the official installer." : client.Found ? "Discord found. Apply will offer official Vencord setup if needed." : "Standard Discord desktop was not found for this Windows user."),
            language = UiText.Current });
    }

    private async Task HandleMessageAsync(JsonElement message)
    {
        string? action = message.GetProperty("action").GetString();
        if (busy && action != "previewReady") return;
        try
        {
            if (dirty && !discardApproved && action is ("community" or "new" or "open" or "importCss" or "importFolder" or "loadLocalTheme"))
            { pendingDiscard = message.Clone(); discardProblem = ""; Publish(); return; }
            switch (action)
            {
                case "loaded": Publish(); return;
                case "location":
                    var nextLocation = message.GetProperty("value").Deserialize<StudioLocation>(Catalog.JsonOptions);
                    if (ValidLocation(nextLocation)) location = nextLocation!;
                    return;
                case "closeReady": CloseWhenReady(message.TryGetProperty("paletteName", out var pendingPalette) ? (pendingPalette.GetString() ?? "").Trim() : ""); return;
                case "libraryRefresh": library.Refresh(); Publish(); return;
                case "loadLocalTheme":
                    string themeId = message.GetProperty("value").GetString() ?? "";
                    var loaded = library.LoadTheme(themeId); AssetImport.ValidateImages(loaded);
                    configuration = loaded; localThemeId = themeId; paletteId = ""; saved = true; MarkClean(); confirmation = null; revision++; RenderPreview(); return;
                case "renameLocalTheme":
                    string renameId = message.GetProperty("value").GetString() ?? "";
                    string newName = message.GetProperty("name").GetString() ?? "";
                    library.SaveTheme(library.LoadTheme(renameId) with { Name = newName }, renameId);
                    if (localThemeId == renameId)
                    {
                        configuration = configuration with { Name = newName }; cleanConfiguration = cleanConfiguration with { Name = newName };
                        cleanKey = DocumentKey(cleanConfiguration); Changed();
                    }
                    Publish(); return;
                case "duplicateLocalTheme":
                    var sourceTheme = library.LoadTheme(message.GetProperty("value").GetString() ?? "");
                    library.SaveTheme(sourceTheme with { Name = message.GetProperty("name").GetString() ?? "" });
                    Publish(); return;
                case "savePalette":
                    paletteId = library.SavePalette(new PaletteProfile(paletteId, message.GetProperty("name").GetString() ?? "", configuration.Accent, configuration.Surface, configuration.Panel ?? configuration.Surface)); Publish(); return;
                case "loadPalette":
                    var palette = library.LoadPalette(message.GetProperty("value").GetString() ?? ""); paletteId = palette.Id;
                    configuration = configuration with { Palette = "custom", Accent = palette.Accent, Surface = palette.Surface, Panel = palette.Panel }; Changed(); return;
                case "newPalette": paletteId = ""; configuration = configuration with { Palette = "custom" }; Changed(); return;
                case "saveImage":
                    var selectedImage = message.GetProperty("kind").GetString() == "banner" ? configuration.CustomBanner : configuration.CustomBackground;
                    if (selectedImage is null) return;
                    library.SaveImage(message.GetProperty("name").GetString() ?? "", selectedImage); Publish(); return;
                case "loadImage":
                    var image = library.LoadImage(message.GetProperty("value").GetString() ?? "");
                    var nextImage = message.GetProperty("kind").GetString() == "banner" ? configuration with { Banner = "custom", CustomBanner = image.Asset } : configuration with { Background = "custom", CustomBackground = image.Asset };
                    AssetImport.ValidateImages(nextImage); configuration = nextImage; Changed(); return;
                case "cancelDiscard": pendingDiscard = null; pendingClose = false; discardProblem = ""; pendingPaletteName = null; Publish(); return;
                case "saveAndContinue": await ContinuePendingAsync(true); return;
                case "discardEdits": await ContinuePendingAsync(false); return;
                case "importFolder": ImportFolder(); return;
                case "cancelFolder": importPaths = null; importCandidates = null; Publish(); return;
                case "chooseImport":
                    if (importPaths is null || !int.TryParse(message.GetProperty("value").GetString(), out int index) || index < 0 || index >= importPaths.Length) return;
                    var chosen = importPaths[index]; importPaths = null; importCandidates = null;
                    ImportPath(chosen); return;
                case "language":
                    var code = message.GetProperty("value").GetString();
                    if (!UiText.Supported(code)) return;
                    UiText.Use(code);
                    if (!dryRun) UiText.Save(client.StateRoot);
                    Text = UiText.Format("Theme Studio · Discord Theme Launcher — {0}", ThemeCompiler.BundledCatalog.Version);
                    Publish(); return;
                case "edit":
                    var edits = message.GetProperty("values").Deserialize<StudioValues>(Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid editor values.");
                    var strings = new[] { edits.Name, edits.Guild, edits.Preset, edits.Palette, edits.Accent, edits.Surface, edits.Font, edits.Banner, edits.Background, edits.Panel ?? edits.Surface, edits.Dms, edits.Description };
                    if (strings.Any(s => s is null || s.Length > 2048)) throw new InvalidDataException("Editor values exceed the supported limits.");
                    if (configuration.Palette != edits.Palette) paletteId = "";
                    configuration = configuration with { Name = edits.Name, Description = edits.Description, Guild = edits.Guild, AdditionalGuilds = [], DmChannels = System.Text.RegularExpressions.Regex.Split(edits.Dms.Trim(), @"[\s,;]+").Where(v => v.Length > 0).ToArray(), Panel = edits.Panel ?? edits.Surface, Preset = edits.Preset, Palette = edits.Palette, Accent = edits.Accent, Surface = edits.Surface, Font = edits.Font, Banner = edits.Banner, Background = edits.Background };
                    Changed(); return;
                case "preset":
                    string id = message.GetProperty("value").GetString() ?? "";
                    if (!ThemeConfiguration.Presets.Contains(id)) return;
                    configuration = CommunityGallery.Create(id, configuration.Guild) with { AdditionalGuilds = configuration.AdditionalGuilds, DmChannels = configuration.DmChannels }; Changed(); return;
                case "community":
                    configuration = CommunityGallery.Create(message.GetProperty("value").GetString() ?? "", configuration.Guild) with { AdditionalGuilds = configuration.AdditionalGuilds, DmChannels = configuration.DmChannels };
                    localThemeId = null; paletteId = ""; saved = false; Changed(); return;
                case "importCss": ReviewCss(); return;
                case "cancelImport": importReview = null; Publish(); return;
                case "acceptImport":
                    if (importReview is not { Supported: true, Theme: { } theme }) return;
                    configuration = new ThemeConfiguration { Name = theme.Name.Length > 80 ? theme.Name[..80] : theme.Name, Guild = configuration.Guild, AdditionalGuilds = configuration.AdditionalGuilds, DmChannels = configuration.DmChannels, Imported = theme };
                    importReview = null; localThemeId = null; paletteId = ""; saved = false; Changed(); return;
                case "importVariable":
                    if (configuration.Imported is null) return;
                    string key = message.GetProperty("name").GetString() ?? "", edit = message.GetProperty("value").GetString() ?? "";
                    var importedTheme = configuration.Imported with { Edits = new(configuration.Imported.Edits) { [key] = edit } };
                    _ = CssThemeImporter.Inspect(importedTheme);
                    configuration = configuration with { Imported = importedTheme }; Changed(); return;
                case "browseThemes": OpenExternal("https://betterdiscord.app/themes"); return;
                case "submitTheme": OpenExternal(SubmissionDestination()); return;
                case "communitySource":
                    OpenExternal(CommunityGallery.Designs.Single(d => d.Id == message.GetProperty("value").GetString()).Source); return;
                case "communityAuthor":
                    OpenExternal(CommunityGallery.Designs.Single(d => d.Id == message.GetProperty("value").GetString()).AuthorUrl); return;
                case "previewReady":
                    if (renderedRevision == revision) validation = !surface.Ready ? "The design or display font did not load. Choose another font or reload the preview." : configuration.TargetGuilds.Length == 0 && configuration.DmChannels.Length == 0 ? "Choose at least one server or DM chat." : "";
                    Publish(); return;
                case "refresh": RenderPreview(); return;
                case "cancelConfirmation":
                    if (confirmation?.Id == message.GetProperty("id").GetString()) confirmation = null;
                    Publish(); return;
                case "confirmOperation":
                    string confirmationId = message.GetProperty("id").GetString() ?? "";
                    bool allowSetup = message.TryGetProperty("allowSetup", out var setupConsent) && setupConsent.GetBoolean();
                    if (confirmation?.Id != confirmationId || confirmation.Problem.Length > 0 || confirmation.RequiresSetup && !allowSetup) return;
                    allowSetup = confirmation.RequiresSetup && allowSetup;
                    if (!OperationConfirmation.TryTake(ref confirmation, confirmationId, revision, out var confirmed)) { Publish(); return; }
                    await OperateAsync(confirmed!, allowSetup); return;
                case "new":
                    confirmation = null; configuration = new ThemeConfiguration { Name = UiText.Text("My server theme"), Guild = "" }; localThemeId = null; paletteId = ""; saved = false; MarkClean(); revision++; RenderPreview(); return;
                case "open": OpenDocument(); return;
                case "save": SaveDocument(false); return;
                case "saveLocalCopy":
                    var copy = ReadConfiguration(false) with { Name = message.GetProperty("name").GetString() ?? "" };
                    copy.Validate(false); localThemeId = library.SaveTheme(copy);
                    configuration = copy; saved = true; MarkClean(); confirmation = null; revision++;
                    if (persistSession) copy.Save(DraftPath); SaveSession(); RenderPreview(); return;
                case "share": SaveDocument(true); return;
                case "exportCss": ExportOriginalCss(); return;
                case "downloadFont": await DownloadFontAsync(message.GetProperty("value").GetString() ?? ""); return;
                case "importFont":
                    var asset = AssetImport.Font(this); if (asset is null) return;
                    configuration = configuration with { Font = "custom", CustomFont = asset }; notice = "Font imported with its redistribution license: " + asset.Name; Changed(); return;
                case "importBanner": ImportImage(true); return;
                case "importBackground": ImportImage(false); return;
                case "serverHelp": ServerHelp(); return;
                case "help": Help(); return;

                case "disableManaged": case "disableAll":
                    if (confirmation is not null && confirmation.Operation is not ("disableManaged" or "disableAll")) return;
                    confirmation = null; RequestConfirmation(action); return;
                case "restore": RequestConfirmation(action, message.TryGetProperty("value", out var recovery) ? recovery.GetString() : null); return;
                case "apply": case "disable": RequestConfirmation(action); return;
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException or InvalidOperationException or KeyNotFoundException or HttpRequestException or TaskCanceledException)
        {
            notice = error is InvalidDataException ? error.Message : "The action could not finish. Check the file's format, size and permissions. Your saved configurations are safe.";
            if (pendingClose || pendingDiscard is not null) discardProblem = notice;
            Publish();
        }
    }
    private void OpenDocument()
    {
        using var dialog = new OpenFileDialog { Filter = UiText.Text("Theme files") + " (*.dtheme;*.theme.css)|*.dtheme;*.theme.css", Title = UiText.Text("Import a theme") };
        if (dialog.ShowDialog(this) == DialogResult.OK) ImportPath(dialog.FileName);
    }
    private void ImportPath(string path)
    {
        ThemeStore.CheckPath(path);
        if (path.EndsWith(".theme.css", StringComparison.OrdinalIgnoreCase)) { importReview = CssThemeImporter.ReviewFile(path); Publish(); return; }
        var value = ThemeConfiguration.Load(path); AssetImport.ValidateImages(value);
        confirmation = null; configuration = value; localThemeId = null; paletteId = ""; saved = true; MarkClean(); revision++;
        notice = "Configuration opened. Review the target server before applying."; RenderPreview();
    }
    private void ReviewCss() => OpenDocument();
    private void ImportFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = UiText.Text("Import a folder"), UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ReviewFolder(dialog.SelectedPath);
    }
    internal void ReviewFolder(string folder)
    {
        ThemeStore.CheckPath(folder);
        var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = true, MaxRecursionDepth = 8 };
        var paths = Directory.EnumerateFiles(folder, "*", options)
            .Where(p => p.EndsWith(".dtheme", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".theme.css", StringComparison.OrdinalIgnoreCase)).Take(101).ToArray();
        if (paths.Length > 100) throw new InvalidDataException("Choose a folder with at most 100 theme files.");
        importPaths = paths;
        importCandidates = paths.Select((path, index) => {
            bool supported = true; string reason = "";
            try
            {
                ThemeStore.CheckPath(path);
                if (path.EndsWith(".theme.css", StringComparison.OrdinalIgnoreCase)) { var review = CssThemeImporter.ReviewFile(path); supported = review.Supported; reason = string.Join(" ", review.Notes.Select(UiText.Text)); }
                else { var value = ThemeConfiguration.Load(path); AssetImport.ValidateImages(value); }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or ArgumentException) { supported = false; reason = UiText.Text(error.Message); }
            return (object)new { id = index.ToString(System.Globalization.CultureInfo.InvariantCulture), name = Path.GetRelativePath(folder, path), supported, reason = supported ? "" : reason };
        }).ToArray();
        Publish();
    }
    private static object ReviewState(CssImportReview review) => new
    {
        review.Supported, name = review.Theme?.Name, author = review.Theme?.Author, source = review.Theme?.Source,
        sha256 = review.Theme?.Sha256, license = review.Theme?.License,
        notes = review.Notes.Select(UiText.Text), resources = review.Resources.Select(r => r.Replace("bytes · embedded", UiText.Text("bytes · embedded")).Replace("reviewed bundled font", UiText.Text("reviewed bundled font"))),
        variables = review.Variables.Select(v => new { v.Name, value = review.Theme?.Edits.GetValueOrDefault(v.Name) ?? v.Value, v.Kind })
    };
    private static void OpenExternal(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidDataException("Use an HTTPS destination without credentials.");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
    private static string SubmissionDestination() => "https://github.com/PrimeBuild-pc/discord-theme-launcher/issues/new?template=theme_submission.yml";
    private void SaveDocument(bool share)
    {
        var value = ReadConfiguration(false);
        if (share)
        {
            var name = string.Concat(value.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c));
            using var dialog = new SaveFileDialog { Filter = UiText.Text("Theme configuration (*.dtheme)") + "|*.dtheme", DefaultExt = "dtheme", AddExtension = true, FileName = name + ".dtheme", Title = UiText.Text("Share a copy with embedded assets") };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            value.Save(dialog.FileName);
        }
        else
        {
            localThemeId = library.SaveTheme(value, localThemeId); configuration = value; saved = true; MarkClean();
            if (persistSession) value.Save(DraftPath);
            SaveSession();
        }
        notice = share ? "Portable copy saved with selected assets and font license. Send it to members yourself; each member applies it locally." : "Theme saved in your local templates.";
        Publish();
    }
    private void ExportOriginalCss()
    {
        if (configuration.Imported is not { } imported) return;
        _ = CssThemeImporter.Inspect(imported);
        using var dialog = new SaveFileDialog { Filter = "BetterDiscord (*.theme.css)|*.theme.css", DefaultExt = "theme.css", AddExtension = true, FileName = imported.FileName, Title = UiText.Text("Export original CSS") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ThemeStore.AtomicWrite(dialog.FileName, imported.Original);
    }
    private async Task DownloadFontAsync(string id)
    {
        if (busy || configuration.Imported is not null) return;
        busy = true; notice = "Downloading and verifying the selected font…"; Publish();
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(40) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ThemeStudio/" + ThemeCompiler.BundledCatalog.Version);
            string root = dryRun ? Path.Combine(Path.GetTempPath(), "ThemeStudio-font-cache") : Path.Combine(client.StateRoot, "font-cache");
            var asset = await FontLibrary.LoadAsync(http, id, root);
            configuration = configuration with { Font = "custom", CustomFont = asset };
            notice = "Font ready. It is saved with the theme and its license.";
            Changed();
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or IOException or InvalidDataException or UnauthorizedAccessException)
        { notice = "The font could not be loaded. Your previous font is unchanged. Check your connection and retry."; }
        finally { busy = false; Publish(); }
    }
    private void ImportImage(bool banner)
    {
        using var dialog = new OpenFileDialog { Filter = UiText.Text("Images (*.png;*.jpg;*.jpeg)") + "|*.png;*.jpg;*.jpeg", Title = UiText.Text("Import an image you have permission to use and share") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var asset = AssetImport.Image(dialog.FileName);
        configuration = banner ? configuration with { Banner = "custom", CustomBanner = asset } : configuration with { Background = "custom", CustomBackground = asset };
        notice = "Imported " + asset.Name + ". The image is embedded; the original path is not shared."; Changed();
    }
    private bool ConfirmDiscard()
    {
        if (!dirty) return true;
        using var dialog = new Form { Text = UiText.Text("Unsaved configuration"), BackColor = LauncherStyle.Surface, ForeColor = LauncherStyle.Text, Font = LauncherStyle.Font(), ClientSize = new Size(560, 230), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog };
        var text = new Label { Text = UiText.Text("This configuration has unsaved edits. Save it before continuing if you want to keep them."), Dock = DockStyle.Top, Height = 130, Padding = new Padding(24) };
        var row = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(24, 0, 0, 0) };
        var keep = new DiscordButton { Text = UiText.Text("Keep editing"), Primary = true, AutoSize = true, Padding = new Padding(12, 7, 12, 7), DialogResult = DialogResult.Cancel };
        var discard = new DiscordButton { Text = UiText.Text("Discard edits"), AutoSize = true, Padding = new Padding(12, 7, 12, 7), DialogResult = DialogResult.OK };
        var save = new DiscordButton { Text = UiText.Text("Save"), AutoSize = true, Padding = new Padding(12, 7, 12, 7) };
        save.Click += (_, _) =>
        {
            try { SaveDocument(false); dialog.DialogResult = DialogResult.OK; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException) { text.Text = UiText.Text(error is InvalidDataException ? error.Message : "The session could not be saved. Check disk space and permissions, then try closing again."); }
        };
        row.Controls.Add(keep); row.Controls.Add(discard); row.Controls.Add(save); dialog.Controls.Add(text); dialog.Controls.Add(row); dialog.CancelButton = keep; dialog.AcceptButton = keep; dialog.Shown += (_, _) => LauncherStyle.DarkCaption(dialog);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }
    private void ServerHelp() => LauncherStyle.ShowMessage(this, UiText.Text(UiText.ServerHelpBody), UiText.Text(UiText.ServerHelpTitle), true);
    private void Help() => LauncherStyle.ShowMessage(this, UiText.Text(UiText.HelpBody), UiText.Text(UiText.HelpTitle), true);

    private void RequestConfirmation(string operation, string? recoveryId = null)
    {
        if (busy || confirmation is not null) return;
        ThemeConfiguration? value = operation == "apply" ? ReadConfiguration() : null;
        if (value is not null && !ApplyReadiness.Evaluate(busy, validation, renderedRevision == revision && surface.Ready).CanApply) return;
        if (operation == "restore" && recoveryId is not null && !store.RecoveryHistory().Any(e => e.Id == recoveryId)) return;
        string problem = "";
        bool setup = false, repair = false;
        if (!dryRun)
        {
            try
            {
                client.ValidateEnvironment();
                _ = store.InstalledSelection();
                setup = operation == "apply" && !client.Patched; repair = setup && client.NeedsRepair;
            }
            catch (Exception error)
            {
                problem = error is InvalidOperationException or InvalidDataException ? error.Message : "The client or Vencord profile could not be checked. Review the installation and manual recovery guide.";
            }
        }
        confirmation = OperationConfirmation.Create(operation, revision, value, setup, repair, problem, recoveryId);
        Publish();
    }

    private async Task OperateAsync(OperationConfirmation confirmed, bool allowSetup)
    {
        if (busy) return;
        string operation = confirmed.Operation;
        var value = confirmed.Configuration;
        if (dryRun)
        {
            SimulatedOperations++;
            notice = "Simulation complete. No Discord or Vencord files or processes were changed.";
            Publish(); return;
        }
        bool stopped = false;
        try
        {
            if (value is not null && (!surface.Ready || renderedRevision != revision)) throw new InvalidDataException("Wait for a valid preview of your latest edits before applying.");
            client.ValidateEnvironment(); bool repair = client.NeedsRepair, setup = operation == "apply" && !client.Patched;
            if (setup && !allowSetup) throw new InvalidOperationException("Vencord now needs installation / repair. Click Apply again and review the setup option before continuing.");
            busy = true; Publish(); byte[]? installer = null;
            if (setup)
            {
                notice = "Downloading and verifying the official Vencord installer…"; Publish();
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBuild-DiscordThemeLauncher/" + ThemeCompiler.BundledCatalog.Version);
                installer = await ReleaseDownload.FetchAsync(http, ReleaseDownload.InstallerUri, ReleaseDownload.InstallerSha256, 32 * 1024 * 1024);
            }
            if (value is not null) await Task.Run(() => value.Save(DraftPath));
            notice = "Closing Discord after your confirmation…"; Publish(); await client.StopAsync(); stopped = true;
            if (installer is not null) { notice = "Installing / repairing official Vencord…"; Publish(); await client.InstallVencordAsync(installer, repair); }
            notice = "Saving a recovery snapshot and updating managed themes…"; Publish();
            await Task.Run(() => {
                if (operation == "restore") { if (confirmed.RecoveryId is { } id) store.RestoreSnapshot(id); else store.RestoreLast(); }
                else if (operation is "disableManaged" or "disableAll") store.DisableThemes(operation == "disableAll");
                else store.ApplyPersonal(value);
            });
            notice = "Opening Discord and checking startup…"; Publish(); await client.OpenAsync(); stopped = false;
            notice = operation == "apply" ? "Theme applied. Discord startup confirmed. Changes are local to you." : operation == "disable" ? "Personal theme disabled. Discord startup confirmed. Restore can undo this." : operation is "disableManaged" or "disableAll" ? "Theme styling disabled. Files preserved. Discord startup confirmed." : "Previous managed setup restored. Discord startup confirmed.";
        }
        catch (Exception error)
        {
            notice = error is InvalidDataException or InvalidOperationException ? error.Message : "Setup could not finish. Check the profile, disk space and manual recovery guide.";
            if (stopped) notice += " No automatic retry was made.";

        }
        finally { busy = false; Publish(); }
    }
}
