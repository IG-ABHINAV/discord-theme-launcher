using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CommunityThemes;

public sealed record Theme(string Id, string Guild, string Name, string Server, string File, string Accent, string Tagline, string Sha256, int Bytes,
    string Author = "Prime Build", string Source = "https://github.com/PrimeBuild-pc/discord-theme-launcher", string License = "MIT",
    string[]? Capabilities = null);
public sealed record Catalog(string Version, string Repository, Theme[] Themes)
{
    public static Catalog Parse(string json)
    {
        var catalog = JsonSerializer.Deserialize<Catalog>(json, JsonOptions) ?? throw new InvalidDataException("Invalid theme catalog.");
        if (catalog.Repository != "PrimeBuild-pc/discord-theme-launcher" || catalog.Themes is null || catalog.Themes.Length is < 1 or > 16 ||
            catalog.Themes.Select(t => t.File).Distinct(StringComparer.OrdinalIgnoreCase).Count() != catalog.Themes.Length ||
            catalog.Themes.Select(t => t.Id).Distinct().Count() != catalog.Themes.Length ||
            catalog.Themes.Any(t => t.File is null || !System.Text.RegularExpressions.Regex.IsMatch(t.File, "^[A-Za-z][A-Za-z0-9-]{0,63}\\.theme\\.css$") ||
                t.File.Equals(ThemeCompiler.PersonalFile, StringComparison.OrdinalIgnoreCase) ||
                t.Id is null || !System.Text.RegularExpressions.Regex.IsMatch(t.Id, "^[a-z][a-z0-9-]{0,63}$") ||
                t.Bytes is <= 0 or > 2 * 1024 * 1024 || t.Sha256 is null || !System.Text.RegularExpressions.Regex.IsMatch(t.Sha256, "^[a-f0-9]{64}$") ||
                t.Guild != ThemeConfiguration.ParseGuild(t.Guild)) ||
            !System.Text.RegularExpressions.Regex.IsMatch(catalog.Version, @"^\d+\.\d+\.\d+(?:-beta\.\d+)?$"))
            throw new InvalidDataException("Unsupported theme catalog.");
        return catalog;
    }
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
}

public static class ReleaseDownload
{
    // Both coordinates and digest are pinned; no downloaded scripts or mutable main-branch code are executed.
    public const string InstallerVersion = "1.4.2";
    public const string InstallerSha256 = "15268aba25625797bf562187dd87ddadf42882e079c7b6192880ad3e83353ef5";
    public static readonly Uri InstallerUri = new("https://github.com/Vencord/Installer/releases/download/v1.4.2/VencordInstallerCli.exe");
    public static Uri ThemeUri(Catalog catalog, Theme theme) => new($"https://github.com/{catalog.Repository}/releases/download/v{catalog.Version}/{theme.File}");

    public static void Verify(byte[] bytes, string hash, int? expectedSize = null)
    {
        if ((expectedSize.HasValue && bytes.Length != expectedSize) ||
            !Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The download failed its integrity check. Nothing was installed.");
    }

    public static async Task<byte[]> FetchAsync(HttpClient http, Uri uri, string hash, int limit, int? expectedSize = null)
    {
        if (uri.Scheme != "https" || uri.Host != "github.com") throw new InvalidDataException("Unsupported download source.");
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("Download exceeds the size limit.");
        await using var input = await response.Content.ReadAsStreamAsync();
        using var output = new MemoryStream();
        var buffer = new byte[32768];
        int count;
        while ((count = await input.ReadAsync(buffer)) != 0)
        {
            if (output.Length + count > limit) throw new InvalidDataException("Download exceeds the size limit.");
            output.Write(buffer, 0, count);
        }
        var bytes = output.ToArray();
        Verify(bytes, hash, expectedSize);
        return bytes;
    }
}

public sealed record SavedTheme(string File, bool Existed, bool Enabled, string? Sha256 = null);
public sealed record Snapshot(SavedTheme[] Themes);

public sealed class ThemeStore(string vencordRoot, string stateRoot, Catalog catalog)
{
    private string[] ManagedFiles => catalog.Themes.Select(t => t.File).Append(ThemeCompiler.PersonalFile).ToArray();
    public string ThemesDirectory => Path.Combine(vencordRoot, "themes");
    public string SettingsPath => Path.Combine(vencordRoot, "settings", "settings.json");
    public string StateDirectory => stateRoot;
    private string LastPath => Path.Combine(stateRoot, "last-backup.txt");
    public bool CanRestore => File.Exists(LastPath);

    public static void CheckPath(string path)
    {
        // Do not follow junctions/symlinks while writing a profile or restoring a backup.
        var current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("The selected location contains a linked folder or file. Use a regular local profile.");
            current = Path.GetDirectoryName(current);
        }
    }

    private JsonObject ReadSettings()
    {
        CheckPath(SettingsPath);
        if (!File.Exists(SettingsPath)) return new JsonObject();
        var result = JsonNode.Parse(File.ReadAllText(SettingsPath)) as JsonObject ?? throw new InvalidDataException("Vencord settings are not a JSON object. Restore or repair them before continuing.");
        if (result["enabledThemes"] is not null and not JsonArray) throw new InvalidDataException("Vencord enabledThemes has an unsupported format.");
        return result;
    }
    private static string[] Enabled(JsonObject settings) => (settings["enabledThemes"] as JsonArray)?.Select(node => node?.GetValue<string>() ?? "").ToArray() ?? [];
    public string[] InstalledSelection()
    {
        var enabled = Enabled(ReadSettings());
        return catalog.Themes.Where(t => enabled.Contains(t.File) && File.Exists(Path.Combine(ThemesDirectory, t.File))).Select(t => t.Id).ToArray();
    }

    public static void AtomicWrite(string path, byte[] bytes)
    {
        CheckPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void WriteJson<T>(string path, T value) => AtomicWrite(path, System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Catalog.JsonOptions)));
    private void WriteEnabled(JsonObject settings, IEnumerable<string> selected, string[]? managed = null)
    {
        var unmanaged = Enabled(settings).Where(name => !(managed ?? ManagedFiles).Contains(name));
        settings["enabledThemes"] = new JsonArray(unmanaged.Concat(selected).Distinct().Select(s => (JsonNode?)JsonValue.Create(s)).ToArray());
        AtomicWrite(SettingsPath, System.Text.Encoding.UTF8.GetBytes(settings.ToJsonString(Catalog.JsonOptions)));
    }

    // Fetch and verify ALL selected files before any profile mutation. Caller must have fully stopped Discord.
    public void Apply(string[] selectedIds, IReadOnlyDictionary<string, byte[]> downloads)
    {
        if (selectedIds.Distinct().Count() != selectedIds.Length || selectedIds.Any(id => !catalog.Themes.Any(t => t.Id == id)))
            throw new InvalidDataException("Unknown theme selection.");
        var selected = catalog.Themes.Where(t => selectedIds.Contains(t.Id)).ToArray();
        foreach (var theme in selected)
        {
            if (!downloads.TryGetValue(theme.File, out var bytes)) throw new InvalidDataException("A selected download is missing.");
            ReleaseDownload.Verify(bytes, theme.Sha256, theme.Bytes);
        }
        var settings = ReadSettings();
        CheckPath(ThemesDirectory); CheckPath(stateRoot); CheckPath(LastPath);
        foreach (var theme in catalog.Themes) CheckPath(Path.Combine(ThemesDirectory, theme.File));
        var folder = SaveSnapshot(settings);
        // Persist recovery location before the first mutation, including if the process is interrupted.
        AtomicWrite(LastPath, System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(folder)));
        try
        {
            Directory.CreateDirectory(ThemesDirectory);
            foreach (var theme in catalog.Themes)
            {
                var destination = Path.Combine(ThemesDirectory, theme.File);
                if (selectedIds.Contains(theme.Id)) AtomicWrite(destination, downloads[theme.File]);
                else if (File.Exists(destination)) File.Delete(destination); // Recoverable from the snapshot above.
            }
            var personal = Path.Combine(ThemesDirectory, ThemeCompiler.PersonalFile);
            if (File.Exists(personal)) File.Delete(personal);
            WriteEnabled(settings, selected.Select(t => t.File));
        }
        catch { Restore(folder); throw; }
    }

    public bool PersonalEnabled => Enabled(ReadSettings()).Contains(ThemeCompiler.PersonalFile) && File.Exists(Path.Combine(ThemesDirectory, ThemeCompiler.PersonalFile));

    public void ApplyPersonal(ThemeConfiguration? configuration)
    {
        // Compile only validated data; shared packages never supply executable CSS.
        byte[]? css = configuration is null ? null : System.Text.Encoding.UTF8.GetBytes(ThemeCompiler.Compile(configuration));
        var settings = ReadSettings();
        var enabled = Enabled(settings).Where(name => name != ThemeCompiler.PersonalFile &&
            !catalog.Themes.Any(t => t.File == name && t.Guild == configuration?.Guild)).ToList();
        if (css is not null) enabled.Add(ThemeCompiler.PersonalFile);
        var folder = SaveSnapshot(settings);
        AtomicWrite(LastPath, System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(folder)));
        try
        {
            var path = Path.Combine(ThemesDirectory, ThemeCompiler.PersonalFile); CheckPath(path);
            if (css is not null) AtomicWrite(path, css);
            else if (File.Exists(path)) File.Delete(path);
            WriteEnabled(settings, enabled);
        }
        catch { Restore(folder); throw; }
    }

    public void RestoreLast()
    {
        CheckPath(LastPath);
        var name = File.ReadAllText(LastPath).Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-f0-9]{32}$")) throw new InvalidDataException("Invalid backup reference.");
        var beforeRestore = SaveSnapshot(ReadSettings());
        Restore(Path.Combine(stateRoot, "backups", name));
        AtomicWrite(LastPath, System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(beforeRestore)));
    }

    private string SaveSnapshot(JsonObject settings)
    {
        CheckPath(ThemesDirectory); CheckPath(stateRoot);
        var folder = Path.Combine(stateRoot, "backups", Guid.NewGuid().ToString("N"));
        CheckPath(folder);
        foreach (var file in ManagedFiles) CheckPath(Path.Combine(ThemesDirectory, file));
        var enabled = Enabled(settings);
        var snapshot = new Snapshot(ManagedFiles.Select(file => {
            var path = Path.Combine(ThemesDirectory, file); bool exists = File.Exists(path);
            return new SavedTheme(file, exists, enabled.Contains(file), exists ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() : null);
        }).ToArray());
        Directory.CreateDirectory(folder);
        foreach (var file in snapshot.Themes.Where(t => t.Existed)) File.Copy(Path.Combine(ThemesDirectory, file.File), Path.Combine(folder, file.File));
        WriteJson(Path.Combine(folder, "snapshot.json"), snapshot);
        return folder;
    }

    private void Restore(string folder)
    {
        CheckPath(folder);
        var snapshotPath = Path.Combine(folder, "snapshot.json"); CheckPath(snapshotPath);
        var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(snapshotPath), Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid backup.");
        // Historical snapshots touch only their recorded managed subset; newer catalog entries are left alone.
        if (snapshot.Themes is null || snapshot.Themes.Length < 1 || snapshot.Themes.Length > ManagedFiles.Length ||
            snapshot.Themes.Select(t => t.File).Distinct().Count() != snapshot.Themes.Length ||
            snapshot.Themes.Any(t => !ManagedFiles.Contains(t.File)))
            throw new InvalidDataException("The backup does not match this launcher.");
        var settings = ReadSettings();
        // Validate every source/destination first; never partially restore an incomplete backup.
        foreach (var file in snapshot.Themes)
        {
            CheckPath(Path.Combine(ThemesDirectory, file.File)); CheckPath(Path.Combine(folder, file.File));
            if (file.Existed && !File.Exists(Path.Combine(folder, file.File))) throw new InvalidDataException("A backup file is missing.");
            if (file.Existed && file.Sha256 is not null) ReleaseDownload.Verify(File.ReadAllBytes(Path.Combine(folder, file.File)), file.Sha256);
        }
        Directory.CreateDirectory(ThemesDirectory);
        foreach (var file in snapshot.Themes)
        {
            var destination = Path.Combine(ThemesDirectory, file.File);
            if (file.Existed) AtomicWrite(destination, File.ReadAllBytes(Path.Combine(folder, file.File)));
            else if (File.Exists(destination)) File.Delete(destination);
        }
        WriteEnabled(settings, snapshot.Themes.Where(t => t.Enabled).Select(t => t.File), snapshot.Themes.Select(t => t.File).ToArray());
    }
}
