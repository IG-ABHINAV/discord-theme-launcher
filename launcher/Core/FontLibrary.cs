using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public sealed record DownloadableFont(string Id, string Family, string Category, string Path, string Sha256, int Bytes, string LicenseSha256);
public sealed record FontManifest(string Commit, DownloadableFont[] Fonts);

public static class FontLibrary
{
    private static readonly FontManifest Manifest = ReadManifest();
    public static IReadOnlyList<DownloadableFont> Choices => Manifest.Fonts;
    public static string SourceCommit => Manifest.Commit;

    private static FontManifest ReadManifest()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("font-catalog.json")!;
        var manifest = JsonSerializer.Deserialize<FontManifest>(stream, Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid font catalog.");
        if (!Regex.IsMatch(manifest.Commit, "^[a-f0-9]{40}$") || manifest.Fonts.Length is < 50 or > 128 ||
            manifest.Fonts.Select(f => f.Id).Distinct().Count() != manifest.Fonts.Length ||
            manifest.Fonts.Any(f => !Regex.IsMatch(f.Id, "^[a-z0-9]+$") ||
                !Regex.IsMatch(f.Path, "^ofl/" + f.Id + "/[A-Za-z0-9-]+\\.ttf$") ||
                !Regex.IsMatch(f.Sha256, "^[a-f0-9]{64}$") || !Regex.IsMatch(f.LicenseSha256, "^[a-f0-9]{64}$") || f.Bytes is < 16 or > 2 * 1024 * 1024))
            throw new InvalidDataException("Invalid font catalog.");
        return manifest;
    }

    public static async Task<ThemeAsset> LoadAsync(HttpClient http, string id, string cacheRoot, CancellationToken cancellationToken = default)
    {
        var font = Choices.SingleOrDefault(f => f.Id == id) ?? throw new InvalidDataException("Unknown font selection.");
        var fontPath = System.IO.Path.Combine(cacheRoot, font.Id + ".ttf");
        var licensePath = System.IO.Path.Combine(cacheRoot, font.Id + ".OFL.txt");
        ThemeStore.CheckPath(fontPath); ThemeStore.CheckPath(licensePath);
        ThemeAsset Validate(byte[] bytes, byte[] license)
        {
            ReleaseDownload.Verify(bytes, font.Sha256, font.Bytes);
            ReleaseDownload.Verify(license, font.LicenseSha256);
            var asset = ThemeAsset.Create(System.IO.Path.GetFileName(font.Path), "font/ttf", bytes, new UTF8Encoding(false, true).GetString(license));
            ThemeConfiguration.ValidateAsset(asset, true);
            return asset;
        }
        if (File.Exists(fontPath) && File.Exists(licensePath) && new FileInfo(fontPath).Length <= 2 * 1024 * 1024 && new FileInfo(licensePath).Length <= 64000)
        {
            try { return Validate(await File.ReadAllBytesAsync(fontPath, cancellationToken), await File.ReadAllBytesAsync(licensePath, cancellationToken)); }
            catch (InvalidDataException) { /* Replace only this application's damaged cache after verification. */ }
        }
        var prefix = "https://raw.githubusercontent.com/google/fonts/" + Manifest.Commit + "/";
        var bytes = await ReadBoundedAsync(http, new Uri(prefix + font.Path), 2 * 1024 * 1024, cancellationToken);
        var license = await ReadBoundedAsync(http, new Uri(prefix + "ofl/" + font.Id + "/OFL.txt"), 64000, cancellationToken);
        var validated = Validate(bytes, license);
        ThemeStore.AtomicWrite(fontPath, bytes);
        ThemeStore.AtomicWrite(licensePath, license);
        return validated;
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpClient http, Uri uri, int limit, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("Font download exceeds its size limit.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[32768];
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (output.Length + count > limit) throw new InvalidDataException("Font download exceeds its size limit.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }
}
