using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommunityThemes;

// In-app updates: only the version number comes from the GitHub API; download URLs are rebuilt
// from the pinned repository and the executable must match the release's SHA256SUMS.txt.
public static class AppUpdate
{
    private static readonly Regex VersionPattern = new(@"^(\d+)\.(\d+)\.(\d+)(?:-beta\.(\d+))?$");

    public static bool IsNewer(string candidate, string current)
    {
        long[] Key(string v)
        {
            var m = VersionPattern.Match(v);
            if (!m.Success) throw new InvalidDataException("Unsupported version.");
            // A final release sorts after every beta of the same version.
            return [long.Parse(m.Groups[1].Value), long.Parse(m.Groups[2].Value), long.Parse(m.Groups[3].Value), m.Groups[4].Success ? long.Parse(m.Groups[4].Value) : long.MaxValue];
        }
        var (a, b) = (Key(candidate), Key(current));
        for (int i = 0; i < 4; i++) if (a[i] != b[i]) return a[i] > b[i];
        return false;
    }

    // Newest published (non-draft) release above the current version, or null.
    public static string? Newest(string releasesJson, string current)
    {
        string? best = null;
        foreach (var release in JsonDocument.Parse(releasesJson).RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;
            var tag = release.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            if (!tag.StartsWith('v') || !VersionPattern.IsMatch(tag[1..])) continue;
            var version = tag[1..];
            if (IsNewer(version, current) && (best is null || IsNewer(version, best))) best = version;
        }
        return best;
    }

    public static Uri ReleasesApi(Catalog catalog) => new($"https://api.github.com/repos/{catalog.Repository}/releases?per_page=20");
    public static string ExecutableName(string version) => $"DiscordThemeLauncher-{version}-windows-x64.exe";
    public static Uri Asset(Catalog catalog, string version, string name) => new($"https://github.com/{catalog.Repository}/releases/download/v{version}/{name}");

    public static string ExpectedHash(string sums, string name)
    {
        foreach (var line in sums.Split('\n'))
        {
            var parts = line.Trim().Split("  ", 2);
            if (parts.Length == 2 && parts[1] == name && Regex.IsMatch(parts[0], "^[a-f0-9]{64}$")) return parts[0];
        }
        throw new InvalidDataException("The update checksum is missing. Nothing was installed.");
    }

    public static async Task<string?> CheckAsync(HttpClient http, Catalog catalog)
    {
        using var response = await http.GetAsync(ReleasesApi(catalog));
        response.EnsureSuccessStatusCode();
        return Newest(await response.Content.ReadAsStringAsync(), catalog.Version);
    }

    public static async Task<byte[]> DownloadAsync(HttpClient http, Catalog catalog, string version)
    {
        if (!VersionPattern.IsMatch(version)) throw new InvalidDataException("Unsupported version.");
        var sums = System.Text.Encoding.UTF8.GetString(await ReleaseDownload.FetchAsync(http, Asset(catalog, version, "SHA256SUMS.txt"), null, 64 * 1024));
        var name = ExecutableName(version);
        return await ReleaseDownload.FetchAsync(http, Asset(catalog, version, name), ExpectedHash(sums, name), 160 * 1024 * 1024);
    }
}
