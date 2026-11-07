using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public sealed record ThemeAsset(string Name, string MediaType, byte[] Data, string Sha256, string License = "")
{
    public static ThemeAsset Create(string name, string mediaType, byte[] data, string license = "") =>
        new(Path.GetFileName(name), mediaType, data, Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(), license);
    public string DataUri => $"data:{MediaType};base64,{Convert.ToBase64String(Data)}";
}

public sealed record ThemeConfiguration
{
    public int Schema { get; init; } = 1;
    public string Name { get; init; } = "My server theme";
    public string Guild { get; init; } = "";
    public string Preset { get; init; } = "fluent";
    public string Palette { get; init; } = "custom";
    public string Accent { get; init; } = "#8791ff";
    public string Surface { get; init; } = "#202020";
    public string Font { get; init; } = "native";
    public string Banner { get; init; } = "none";
    public string Background { get; init; } = "none";
    public ThemeAsset? CustomFont { get; init; }
    public ThemeAsset? CustomBanner { get; init; }
    public ThemeAsset? CustomBackground { get; init; }
    public ImportedTheme? Imported { get; init; }
    public const int MaxFileBytes = 24 * 1024 * 1024;
    public static string[] Presets => ThemeCompiler.BundledCatalog.Themes.Select(t => t.Id).ToArray();
    public static readonly string[] Palettes = ["primebuild", "primebuild-dev", "fantafort", "blurple", "custom"];

    public static string ParseGuild(string input)
    {
        input = (input ?? "").Trim();
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
            (uri.Host == "discord.com" || uri.Host == "canary.discord.com" || uri.Host == "ptb.discord.com"))
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is 3 or 4 && parts[0] == "channels" && Regex.IsMatch(parts[2], "^[0-9]{17,20}$")) input = parts[1];
        }
        if (!Regex.IsMatch(input, "^[0-9]{17,20}$") || !ulong.TryParse(input, out var id) || id == 0)
            throw new InvalidDataException("Paste a server ID (17–20 digits) or an https://discord.com/channels/server/channel link. Invite links do not contain a server ID.");
        return input;
    }

    public void Validate()
    {
        if (Schema != 1) throw new InvalidDataException("This configuration version is not supported.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || Name.Any(char.IsControl)) throw new InvalidDataException("Use a theme name of 1–80 characters.");
        if (Guild != ParseGuild(Guild)) throw new InvalidDataException("Save the target as a server ID.");
        if (Imported is not null) _ = CssThemeImporter.Compile(Imported, Guild);
        if (!Presets.Append("fluent").Contains(Preset) || !Palettes.Contains(Palette) || !Presets.Append("native").Append("custom").Contains(Font) ||
            !Presets.Append("none").Append("custom").Contains(Banner) || !Presets.Append("none").Append("custom").Contains(Background))
            throw new InvalidDataException("Unknown preset, palette, font or artwork choice.");
        if (Accent is null || Surface is null || !Regex.IsMatch(Accent, "^#[a-fA-F0-9]{6}$") || !Regex.IsMatch(Surface, "^#[a-fA-F0-9]{6}$")) throw new InvalidDataException("Colors must use #RRGGBB.");
        // Fixed light message text needs a dark surface, including underneath bright imported images.
        if (Palette == "custom" && Luminance(Surface) > .055) throw new InvalidDataException("Choose a darker surface to keep messages and the composer readable.");
        ValidateAsset(CustomFont, true); ValidateAsset(CustomBanner, false); ValidateAsset(CustomBackground, false);
        if (Font == "custom" && CustomFont is null || Banner == "custom" && CustomBanner is null || Background == "custom" && CustomBackground is null)
            throw new InvalidDataException("Import the selected custom asset first.");
    }

    public static double Luminance(string hex) => Enumerable.Range(0, 3).Select(i => Convert.ToInt32(hex.Substring(1 + i * 2, 2), 16) / 255d)
        .Select(c => c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4)).Zip(new[] { .2126, .7152, .0722 }, (c, w) => c * w).Sum();

    public static void ValidateAsset(ThemeAsset? asset, bool font)
    {
        if (asset is null) return;
        if (asset.Name is null || asset.Name.Length is < 1 or > 160 || asset.Name.IndexOfAny(['/', '\\']) >= 0 || asset.Name.Any(char.IsControl) ||
            asset.Data is null || asset.Data.Length is < 16 || asset.Data.Length > (font ? 2 : 8) * 1024 * 1024 || asset.License is null || asset.License.Length > 64000 ||
            asset.Sha256 is null || !Regex.IsMatch(asset.Sha256, "^[a-f0-9]{64}$")) throw new InvalidDataException("Invalid asset metadata or asset size exceeds its limit.");
        ReleaseDownload.Verify(asset.Data, asset.Sha256);
        if (font)
        {
            if (asset.MediaType is not ("font/ttf" or "font/otf") || string.IsNullOrWhiteSpace(asset.License))
                throw new InvalidDataException("Custom fonts require TTF/OTF bytes and their redistribution license text.");
            ValidateFont(asset.Data, asset.MediaType);
        }
        else
        {
            // Imports are normalized to PNG by the Windows boundary; shared files cannot introduce SVG or active content.
            var d = asset.Data;
            if (asset.MediaType != "image/png" || !d.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || d.Length < 33 || Encoding.ASCII.GetString(d, 12, 4) != "IHDR")
                throw new InvalidDataException("Shared images must be normalized PNG files. Import a PNG or JPEG in the launcher.");
            uint width = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(16)), height = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(20));
            if (width is < 1 or > 4096 || height is < 1 or > 4096 || (long)width * height > 16_000_000)
                throw new InvalidDataException("Images must be at most 4096 × 4096 and 16 million pixels.");
        }
    }

    private static void ValidateFont(byte[] d, string mediaType)
    {
        bool trueType = d.AsSpan(0, 4).SequenceEqual(new byte[] { 0, 1, 0, 0 });
        bool openType = Encoding.ASCII.GetString(d, 0, 4) == "OTTO";
        if (mediaType == "font/ttf" ? !trueType : !openType) throw new InvalidDataException("The font signature does not match its format. Collections, WOFF and variable fonts are not imported in this version.");
        int count = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(4));
        if (count is < 1 or > 128 || 12 + count * 16 > d.Length) throw new InvalidDataException("Invalid font table directory.");
        var tags = new HashSet<string>();
        for (int i = 0; i < count; i++)
        {
            int p = 12 + i * 16; string tag = Encoding.ASCII.GetString(d, p, 4);
            uint offset = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(p + 8)), length = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(p + 12));
            if (!tags.Add(tag) || (ulong)offset + length > (ulong)d.Length) throw new InvalidDataException("Invalid font table bounds.");
            if (tag == "fvar") throw new InvalidDataException("Use a static TTF/OTF font; variable font import is not supported.");
            if (tag == "OS/2")
            {
                if (length < 10) throw new InvalidDataException("Invalid font embedding flags.");
                int flags = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan((int)offset + 8));
                if ((flags & 0x0206) != 0) throw new InvalidDataException("This font restricts embedding. Choose a font licensed for editable embedding and redistribution.");
            }
        }
        if (!new[] { "head", "cmap", "name", "OS/2", "maxp" }.All(tags.Contains) || !(tags.Contains("glyf") || tags.Contains("CFF ")))
            throw new InvalidDataException("The font is missing required tables.");
    }

    public static ThemeConfiguration Load(string path)
    {
        ThemeStore.CheckPath(path);
        if (new FileInfo(path).Length > MaxFileBytes) throw new InvalidDataException("Configuration exceeds 24 MiB.");
        var value = JsonSerializer.Deserialize<ThemeConfiguration>(File.ReadAllBytes(path), Catalog.JsonOptions) ?? throw new InvalidDataException("Invalid configuration.");
        value.Validate(); return value;
    }

    public void Save(string path)
    {
        Validate(); var bytes = JsonSerializer.SerializeToUtf8Bytes(this, Catalog.JsonOptions);
        if (bytes.Length > MaxFileBytes) throw new InvalidDataException("Configuration exceeds 24 MiB. Use smaller images.");
        ThemeStore.AtomicWrite(path, bytes);
    }
}
