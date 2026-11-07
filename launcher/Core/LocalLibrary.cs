using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public sealed record LocalTheme(string Id, string Name);
public sealed record PaletteProfile(string Id, string Name, string Accent, string Surface, string Panel);
public sealed record SavedImage(string Id, string Name, ThemeAsset Asset);
public sealed record LocalImage(string Id, string Name, string Sha256);

public sealed class LocalLibrary(string root)
{
    public string Root => root;
    private LocalTheme[]? themes;
    private PaletteProfile[]? palettes;
    private LocalImage[]? images;
    public void Refresh() { themes = null; palettes = null; images = null; }
    private string FilePath(string kind, string id, string extension)
    {
        if (!Regex.IsMatch(id, "^[a-z0-9][a-z0-9-]{0,63}$")) throw new InvalidDataException("Invalid local library identifier.");
        string path = Path.Combine(root, kind, id + extension); ThemeStore.CheckPath(path); return path;
    }
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl)) throw new InvalidDataException("Use a name of 1–80 characters.");
    }
    private IEnumerable<string> Ids(string kind, string extension)
    {
        string folder = Path.Combine(root, kind); ThemeStore.CheckPath(folder);
        return Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*" + extension).Select(Path.GetFileNameWithoutExtension).OfType<string>()
            .Where(id => Regex.IsMatch(id, "^[a-z0-9][a-z0-9-]{0,63}$")) : [];
    }
    private T Read<T>(string kind, string id)
    {
        string path = FilePath(kind, id, ".json");
        if (new FileInfo(path).Length > ThemeConfiguration.MaxFileBytes) throw new InvalidDataException("Library item exceeds its size limit.");
        return JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid local library item.");
    }
    private void Write<T>(string kind, string id, T value) => ThemeStore.AtomicWrite(FilePath(kind, id, ".json"), JsonSerializer.SerializeToUtf8Bytes(value, Catalog.JsonOptions));
    private static T[] ReadValid<T>(IEnumerable<string> ids, Func<string, T> load)
    {
        var values = new List<T>();
        foreach (string id in ids)
            try { values.Add(load(id)); }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException) { /* Keep unreadable originals for manual recovery. */ }
        return values.ToArray();
    }
    public LocalTheme[] Themes() => themes ??= ReadValid(Ids("themes", ".dtheme"), id => new LocalTheme(id, LoadTheme(id).Name)).OrderBy(t => t.Name).ToArray();
    public ThemeConfiguration LoadTheme(string id) => ThemeConfiguration.Load(FilePath("themes", id, ".dtheme"));
    public string SaveTheme(ThemeConfiguration value, string? id = null)
    {
        id ??= Guid.NewGuid().ToString("N"); value.Save(FilePath("themes", id, ".dtheme")); themes = null; return id;
    }
    public PaletteProfile[] Palettes() => palettes ??= ReadValid(Ids("palettes", ".json"), LoadPalette).OrderBy(p => p.Name).ToArray();
    public PaletteProfile LoadPalette(string id)
    {
        var value = Read<PaletteProfile>("palettes", id);
        if (value.Id != id) throw new InvalidDataException("Invalid local library identifier.");
        ValidatePalette(value); return value;
    }
    private static void ValidatePalette(PaletteProfile value)
    {
        if (value.Id is null || value.Panel is null) throw new InvalidDataException("Invalid local library item.");
        ValidateName(value.Name);
        new ThemeConfiguration { Name = value.Name, Accent = value.Accent, Surface = value.Surface, Panel = value.Panel }.Validate(false);
    }
    public string SavePalette(PaletteProfile value)
    {
        ValidatePalette(value); string id = value.Id.Length == 0 ? Guid.NewGuid().ToString("N") : value.Id;
        Write("palettes", id, value with { Id = id }); palettes = null; return id;
    }
    public LocalImage[] Images() => images ??= ReadValid(Ids("images", ".json"), id => { var image = LoadImage(id); return new LocalImage(id, image.Name, image.Asset.Sha256); }).OrderBy(i => i.Name).ToArray();
    public SavedImage LoadImage(string id)
    {
        var value = Read<SavedImage>("images", id);
        if (value.Id != id || value.Asset is null) throw new InvalidDataException("Invalid local library identifier.");
        ValidateName(value.Name); ThemeConfiguration.ValidateAsset(value.Asset, false); return value;
    }
    public string SaveImage(string name, ThemeAsset asset)
    {
        ValidateName(name); ThemeConfiguration.ValidateAsset(asset, false);
        var existing = Images().FirstOrDefault(i => i.Name == name && i.Sha256 == asset.Sha256);
        if (existing is not null) return existing.Id;
        string id = Guid.NewGuid().ToString("N"); Write("images", id, new SavedImage(id, name, asset)); images = null; return id;
    }
}
