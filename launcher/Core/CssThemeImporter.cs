using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public sealed record ImportedTheme(string FileName, byte[] Original, string Sha256, string Name, string Author,
    string Source, string License, Dictionary<string, string> Edits);
public sealed record ImportVariable(string Name, string Value, string Kind);
public sealed record CssImportReview(bool Supported, ImportedTheme? Theme, string[] Notes, string[] Resources, ImportVariable[] Variables);

public static class CssThemeImporter
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public const string ZenitHash = "3a196069c3fe38a9c2831e95955ba50d31381c2e7a9ae2878583049e24d61006";
    private const string FontImport = "url(\"https://fonts.googleapis.com/css2?family=Montserrat:wght@500;600;700;800;900&display=swap\")";
    private static readonly Regex Identifier = new("^--[a-zA-Z][a-zA-Z0-9-]{0,79}$");
    private static readonly Regex Color = new("^#[a-fA-F0-9]{6}$");
    private static readonly Regex Number = new("^(?:0|[1-9][0-9]{0,2})(?:\\.[0-9]{1,3})?$|^0?\\.[0-9]{1,3}$");

    public static ImportedTheme Load(string path)
    {
        var review = ReviewFile(path);
        return review.Theme ?? throw new InvalidDataException(string.Join("\n", review.Notes));
    }
    public static CssImportReview ReviewFile(string path)
    {
        ThemeStore.CheckPath(path);
        if (!path.EndsWith(".theme.css", StringComparison.OrdinalIgnoreCase) || new FileInfo(path).Length > MaxBytes)
            throw new InvalidDataException("Choose a downloaded .theme.css file up to 2 MiB.");
        return Review(File.ReadAllBytes(path), Path.GetFileName(path));
    }

    public static CssImportReview Review(byte[] bytes, string fileName, string source = "", string license = "")
    {
        try
        {
            if (bytes is null || bytes.Length is < 1 or > MaxBytes || fileName is null || fileName.Length > 160 ||
                fileName.IndexOfAny(['/', '\\', ':']) >= 0 || fileName.Any(char.IsControl) || !fileName.EndsWith(".theme.css", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Choose a downloaded .theme.css file up to 2 MiB.");
            string css = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
            string header = css[..Math.Min(css.Length, 8192)];
            string Metadata(string key) => Regex.Match(header, @"(?m)^[\t ]*\*?[\t ]*@" + key + @"[\t ]+([^\r\n*]+)").Groups[1].Value.Trim();
            string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (hash == ZenitHash)
            {
                if (license.Length == 0) license = ThemeCompiler.Resource("zenit-license.txt");
                if (source.Length == 0) source = "https://github.com/JACKPOT71/zenit-gold/tree/64cc7a1eaf77dc3b081d5fd0637d8dc165b273c6";
            }
            var theme = new ImportedTheme(fileName, bytes.ToArray(), hash, Metadata("name") is { Length: > 0 } name ? name : fileName,
                Metadata("author"), source.Length > 0 ? source : Metadata("source"), license, []);
            var analysis = Analyze(theme, "987654321012345678");
            return new(true, theme, analysis.Notes.ToArray(), analysis.Resources.ToArray(), analysis.Variables.Values.ToArray());
        }
        catch (Exception error) when (error is InvalidDataException or DecoderFallbackException or FormatException)
        { return new(false, null, [error is InvalidDataException ? error.Message : "The stylesheet is not valid UTF-8 or contains a damaged embedded resource."], [], []); }
    }

    public static string Compile(ImportedTheme theme, string guild)
    {
        guild = ThemeConfiguration.ParseGuild(guild);
        var result = Analyze(theme, guild);
        string Comment(string text) => text.Replace("*/", "* /");
        return "/* Imported server adaptation; original author: " + Comment(theme.Author) + "\nSource: " + Comment(theme.Source) +
            "\nOriginal SHA-256: " + theme.Sha256 + "\n" + Comment(theme.License) + "\n*/\n" + result.Css;
    }

    public static CssImportReview Inspect(ImportedTheme theme)
    {
        var result = Analyze(theme, "987654321012345678");
        return new(true, theme, result.Notes.ToArray(), result.Resources.ToArray(), result.Variables.Values.ToArray());
    }

    private sealed class Analysis
    {
        public string Css = "";
        public readonly HashSet<string> Notes = ["Server adaptation: document roots map to the selected server pane. Fixed layers are clipped to it; detached settings and popouts keep their normal appearance."];
        public readonly HashSet<string> Resources = [];
        public readonly Dictionary<string, ImportVariable> Variables = [];
    }

    private static Analysis Analyze(ImportedTheme theme, string guild)
    {
        if (theme.Original is null || theme.Original.Length is < 1 or > MaxBytes || theme.Sha256 is null || theme.Edits is null || theme.Edits.Count > 128 ||
            theme.Name is null || theme.Name.Length > 160 || theme.Author is null || theme.Author.Length > 200 ||
            theme.Source is null || theme.Source.Length > 2048 || theme.License is null || theme.License.Length > 64000 ||
            theme.FileName is null || theme.FileName.Length > 160 || theme.FileName.IndexOfAny(['/', '\\', ':']) >= 0 || theme.FileName.Any(char.IsControl))
            throw new InvalidDataException("Imported theme metadata exceeds supported limits.");
        ReleaseDownload.Verify(theme.Original, theme.Sha256);
        string css = new UTF8Encoding(false, true).GetString(theme.Original).TrimStart('\uFEFF');
        var rules = CssSyntax.Rules(css); var result = new Analysis();
        string alias = "ts-" + theme.Sha256[..12] + "-";
        var keyframes = new Dictionary<string, string>();
        var imageHashes = new HashSet<string>(); long imagePixels = 0;
        void FindAnimations(List<CssRule> nodes)
        {
            foreach (var rule in nodes)
            {
                if (rule.Header.StartsWith("@keyframes ", StringComparison.OrdinalIgnoreCase))
                {
                    string name = rule.Header[11..].Trim();
                    if (!Regex.IsMatch(name, "^[a-zA-Z][a-zA-Z0-9-]{0,79}$")) throw CssSyntax.Invalid();
                    keyframes[name] = alias + name;
                }
                else if (rule.Header.StartsWith("@media ", StringComparison.OrdinalIgnoreCase) || rule.Header.StartsWith("@supports ", StringComparison.OrdinalIgnoreCase))
                    FindAnimations(CssSyntax.Rules(rule.Body ?? throw CssSyntax.Invalid()));
            }
        }
        FindAnimations(rules);
        void CheckValue(string value)
        {
            // No network/filesystem requests, HTML execution, or escaped URL spelling, including unused variables.
            if (value.Contains('\\') || Regex.IsMatch(value, @"(?i)(?:https?:|file:|javascript:|expression\s*\(|-moz-binding|behavior\s*:|image-set\s*\(|src\s*\(|attr\s*\()"))
                throw new InvalidDataException("External or active CSS resources are unsupported. Embed licensed images/fonts locally; no resource was downloaded.");
            foreach (Match match in Regex.Matches(value, @"(?i)url\(\s*(['""']?)([^)]*?)\1\s*\)"))
            {
                string uri = match.Groups[2].Value.Trim();
                var data = Regex.Match(uri, @"^data:(image/(?:png|jpeg|webp)|font/(?:ttf|woff2));base64,([A-Za-z0-9+/=]+)$");
                if (!data.Success) throw new InvalidDataException("Only embedded PNG, JPEG, WebP, TTF and WOFF2 resources are supported. Relative paths, SVG and network URLs are blocked.");
                byte[] raw = Convert.FromBase64String(data.Groups[2].Value);
                if (raw.Length < 16 || raw.Length > MaxBytes) throw new InvalidDataException("Embedded resource is damaged or oversized.");
                string mime = data.Groups[1].Value;
                bool valid = mime switch
                {
                    "image/png" => raw.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}),
                    "image/jpeg" => raw[0] == 255 && raw[1] == 216 && raw[2] == 255,
                    "image/webp" => Encoding.ASCII.GetString(raw, 0, 4) == "RIFF" && Encoding.ASCII.GetString(raw, 8, 4) == "WEBP",
                    "font/woff2" => Encoding.ASCII.GetString(raw, 0, 4) == "wOF2",
                    "font/ttf" => raw.AsSpan(0, 4).SequenceEqual(new byte[] {0,1,0,0}), _ => false
                };
                if (!valid) throw new InvalidDataException("Embedded resource signature does not match its declared format.");
                if (mime.StartsWith("image/", StringComparison.Ordinal) && imageHashes.Add(Convert.ToHexString(SHA256.HashData(raw))))
                {
                    imagePixels += ValidateImageSize(mime, raw);
                    if (imagePixels > 32_000_000) throw new InvalidDataException("Embedded images exceed the combined 32 million pixel preview budget. Use fewer or smaller images.");
                }
                result.Resources.Add(mime + " · " + raw.Length + " bytes · embedded");
            }
        }
        bool documentBackdrop = false;
        string MapSelector(string selector)
        {
            // Document anchors are grammar tokens, not blind string prefixes.
            var root = Regex.Match(selector, @"^(?::root|html|body|#app-mount)(?=$|[\s:.#>+~\[])", RegexOptions.IgnoreCase);
            if (root.Value.Equals("body", StringComparison.OrdinalIgnoreCase) && selector[root.Length..] == "::before") documentBackdrop = true;
            if (root.Success) selector = ":scope" + selector[root.Length..];
            else
            {
                if (selector.Contains(":scope", StringComparison.OrdinalIgnoreCase)) throw CssSyntax.Invalid();
                // Ordinary selectors can style descendants, never the containing server pane itself.
                selector = ":scope " + selector;
            }
            if (Regex.IsMatch(selector.Replace(":scope", ""), @"(?i)(?<![\w-])(?:body|html)(?![\w-])|:root|#app-mount") ||
                selector.Contains('&') || selector.Contains('\\') || selector.Contains('@')) throw new InvalidDataException("Unsupported document-root selector. Keep html/body/:root/#app-mount at the beginning of a selector, without CSS nesting.");
            return selector;
        }
        string Declarations(string body, bool root = false)
        {
            StringBuilder output = new();
            foreach (var (property, original) in CssSyntax.Declarations(body))
            {
                if (!Regex.IsMatch(property, "^(?:--)?[a-zA-Z-][a-zA-Z0-9-]{0,99}$") || property.Equals("behavior", StringComparison.OrdinalIgnoreCase)) throw CssSyntax.Invalid();
                string normalized = property.ToLowerInvariant();
                if (normalized == "all" || normalized == "contain" && !Regex.IsMatch(original, @"^(?:paint|strict|content)(?:\s*!important)?$", RegexOptions.IgnoreCase) ||
                    normalized == "isolation" && !Regex.IsMatch(original, @"^isolate(?:\s*!important)?$", RegexOptions.IgnoreCase) ||
                    normalized == "display" && !Regex.IsMatch(original, @"^(?:none|block|flex|grid|inline-block|inline-flex|inline-grid|flow-root)(?:\s*!important)?$", RegexOptions.IgnoreCase))
                    throw new InvalidDataException("This CSS can remove server containment. all resets, unbounded containment and indirect display values require a reviewed adapter.");
                if (root && !property.StartsWith("--", StringComparison.Ordinal) &&
                    !Regex.IsMatch(normalized, @"^(?:background(?:-[a-z-]+)?|color|font(?:-[a-z-]+)?|line-height|text-[a-z-]+|border(?:-[a-z-]+)?|outline(?:-[a-z-]+)?|box-shadow|opacity)$") &&
                    !(normalized == "position" && Regex.IsMatch(original, @"^relative(?:\s*!important)?$", RegexOptions.IgnoreCase)) &&
                    !(normalized == "z-index" && Regex.IsMatch(original, @"^[01](?:\s*!important)?$", RegexOptions.IgnoreCase)))
                    throw new InvalidDataException("Document-root layout changes cannot preserve the server pane bounds. Move layout rules to a descendant selector or use a reviewed adapter.");
                CheckValue(original); string value = theme.Sha256 == ZenitHash ? original.Replace("\"Montserrat\"", "\"" + alias + "Montserrat\"") : original;
                // Document stacking belongs to Discord; the imported pane must stay below detached portals.
                if (root && normalized == "z-index") value = "auto!important";
                string editable = Regex.Replace(value, @"\s*!important$", "", RegexOptions.IgnoreCase).Trim();
                if (root && Identifier.IsMatch(property) && (Color.IsMatch(editable) || Number.IsMatch(editable)))
                    result.Variables[property] = new(property, editable, Color.IsMatch(editable) ? "color" : "number");
                if (property is "animation" or "animation-name")
                    foreach (var animation in keyframes) value = Regex.Replace(value, @"(?<![\w-])" + Regex.Escape(animation.Key) + @"(?![\w-])", animation.Value);
                if (property == "position" && value.StartsWith("fixed", StringComparison.OrdinalIgnoreCase))
                    result.Notes.Add("Fixed backgrounds and decorations are contained inside the selected server pane.");
                output.Append(property).Append(':').Append(value).Append(';');
            }
            return output.ToString();
        }
        string Emit(List<CssRule> nodes, int depth = 0)
        {
            if (depth > 16) throw CssSyntax.Invalid(); StringBuilder output = new();
            foreach (var rule in nodes)
            {
                if (rule.Header.StartsWith('@'))
                {
                    int split = rule.Header.IndexOfAny([' ', '\t', '\r', '\n', '(']);
                    string kind = (split < 0 ? rule.Header : rule.Header[..split]).ToLowerInvariant();
                    string parameters = split < 0 ? "" : rule.Header[split..].Trim();
                    if (kind == "@import")
                    {
                        if (theme.Sha256 != ZenitHash || parameters != FontImport || rule.Body is not null)
                            throw new InvalidDataException("A stylesheet-level @import needs a reviewed offline adapter. Download a self-contained CSS design instead. No resource was fetched.");
                        result.Resources.Add("Montserrat · reviewed bundled font · SIL OFL 1.1 (Google Fonts)"); continue;
                    }
                    if (kind is "@media" or "@supports" or "@container")
                    {
                        CheckValue(parameters);
                        output.Append(rule.Header).Append('{').Append(Emit(CssSyntax.Rules(rule.Body ?? throw CssSyntax.Invalid()), depth + 1)).Append('}');
                    }
                    else if (kind == "@keyframes")
                    {
                        output.Append(kind).Append(' ').Append(keyframes[parameters]).Append('{');
                        foreach (var frame in CssSyntax.Rules(rule.Body ?? throw CssSyntax.Invalid()))
                        {
                            if (CssSyntax.Selectors(frame.Header).Any(s => s is not ("from" or "to") && !Regex.IsMatch(s, "^[0-9]{1,3}(?:\\.[0-9]+)?%$"))) throw CssSyntax.Invalid();
                            output.Append(frame.Header).Append('{').Append(Declarations(frame.Body ?? throw CssSyntax.Invalid())).Append('}');
                        }
                        output.Append('}');
                    }
                    else throw new InvalidDataException("Unsupported stylesheet rule: " + kind + ". @media, @supports, @container and named animations are supported; global registrations and nested CSS need a reviewed adapter.");
                }
                else
                {
                    var selectors = CssSyntax.Selectors(rule.Header).Select(MapSelector).ToArray();
                    bool stylesRoot = selectors.Any(s => s.StartsWith(":scope", StringComparison.Ordinal) && !s.StartsWith(":scope ", StringComparison.Ordinal) && !s.StartsWith(":scope::", StringComparison.Ordinal));
                    output.AppendJoin(',', selectors).Append('{').Append(Declarations(rule.Body ?? throw CssSyntax.Invalid(), stylesRoot)).Append('}');
                }
            }
            return output.ToString();
        }
        string converted = Emit(rules);
        // body and app-mount become one pane: keep the former body's backdrop behind its content.
        if (documentBackdrop) converted += ":scope::before{z-index:-1!important;}";
        if (result.Variables.Count > 128) throw new InvalidDataException("The stylesheet exposes too many editable variables (maximum 128).");
        foreach (var edit in theme.Edits)
        {
            if (!result.Variables.TryGetValue(edit.Key, out var variable) || edit.Value is null ||
                !(variable.Kind == "color" ? Color.IsMatch(edit.Value) : Number.IsMatch(edit.Value)))
                throw new InvalidDataException("Unsupported imported variable edit. Use the color or numeric controls shown in the compatibility review.");
        }
        string guard = ThemeCompiler.ServerScope(guild);
        string font = theme.Sha256 == ZenitHash ? CommunityGallery.ZenitFontCss : "";
        result.Css = font + guard + "{:scope{--pb-profile:\"personal\";position:relative;isolation:isolate;contain:paint;}" + converted +
            ":scope{--pb-profile:\"personal\"!important;position:relative!important;z-index:auto!important;isolation:isolate!important;contain:paint!important;" +
            string.Join("", theme.Edits.Select(e => e.Key + ":" + e.Value + "!important;")) + "}}";
        return result;
    }

    private static long ValidateImageSize(string mime, byte[] bytes)
    {
        int width = 0, height = 0;
        if (mime == "image/png" && bytes.Length >= 33 && Encoding.ASCII.GetString(bytes, 12, 4) == "IHDR")
        {
            uint w = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16)), h = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20));
            if (w <= int.MaxValue && h <= int.MaxValue) { width = (int)w; height = (int)h; }
        }
        else if (mime == "image/webp" && bytes.Length >= 30 && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) == bytes.Length - 8)
        {
            string chunk = Encoding.ASCII.GetString(bytes, 12, 4);
            if (chunk == "VP8X") { width = 1 + bytes[24] + (bytes[25] << 8) + (bytes[26] << 16); height = 1 + bytes[27] + (bytes[28] << 8) + (bytes[29] << 16); }
            else if (chunk == "VP8 " && bytes[23] == 0x9d && bytes[24] == 1 && bytes[25] == 0x2a)
            { width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26)) & 0x3fff; height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28)) & 0x3fff; }
            else if (chunk == "VP8L" && bytes[20] == 0x2f)
            { uint size = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(21)); width = (int)(size & 0x3fff) + 1; height = (int)((size >> 14) & 0x3fff) + 1; }
        }
        else if (mime == "image/jpeg")
        {
            int p = 2;
            while (p + 9 < bytes.Length && bytes[p] == 255)
            {
                int marker = bytes[p + 1], length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(p + 2));
                if (length < 2 || p + 2 + length > bytes.Length) break;
                if (marker is 0xc0 or 0xc1 or 0xc2) { height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(p + 5)); width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(p + 7)); break; }
                p += 2 + length;
            }
        }
        if (width is < 1 or > 4096 || height is < 1 or > 4096 || (long)width * height > 16_000_000)
            throw new InvalidDataException("Embedded images must have valid dimensions up to 4096 × 4096 and 16 million pixels.");
        return (long)width * height;
    }
}
