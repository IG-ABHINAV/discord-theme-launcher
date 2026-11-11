using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public static class ThemeCompiler
{
    public const string PersonalFile = "PersonalServer.theme.css";
    private static readonly Lazy<Catalog> catalog = new(() => Catalog.Parse(Resource("catalog.json")));
    private static readonly Lazy<Dictionary<string, string>> presets = new(() => catalog.Value.Themes.ToDictionary(theme => theme.Id, theme =>
    {
        var css = Resource(theme.File);
        ReleaseDownload.Verify(Encoding.UTF8.GetBytes(css), theme.Sha256, theme.Bytes);
        return css;
    }));
    internal static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidDataException("Bundled theme resource is missing.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    public static Catalog BundledCatalog => catalog.Value with { Themes = catalog.Value.Themes.ToArray() };
    public static string ServerScope(string guild)
    {
        guild = ThemeConfiguration.ParseGuild(guild);
        string css = PresetCss("primebuild"); int start = css.IndexOf("@scope", StringComparison.Ordinal);
        return css[start..css.IndexOf('{', start)].Trim().Replace(catalog.Value.Themes.Single(t => t.Id == "primebuild").Guild, guild);
    }
    public static string TargetScope(ThemeConfiguration config)
    {
        var roots = config.TargetGuilds.Select(g => ServerScope(g)[8..^1]).ToList();
        foreach (var channel in config.DmChannels)
        {
            const string probe = "987654321012345678";
            var home = ServerScope(probe)[8..^1].Replace("guildsnav___" + probe, "guildsnav___home");
            roots.Add(home + ":has(> :is(.content__5e434, .content_pog62O) > :is(.page__5e434, .page_pog62O) [data-list-id=\"chat-messages\"]:not([hidden]):not([aria-hidden=\"true\"]) [id^=\"chat-messages-" + channel + "-\"])");
        }
        if (roots.Count == 0) throw new InvalidDataException("Choose at least one server or DM chat.");
        return roots.Count == 1 ? "@scope (" + roots[0] + ")" : "@scope (:is(" + string.Join(", ", roots) + "))";
    }
    public static string PresetCss(string id)
    {
        return presets.Value.TryGetValue(id, out var css) ? css : throw new InvalidDataException("Unknown preset.");
    }
    private static string Pattern(string key) => "(--pb-" + key + ":\\s*)(url\\(\"[^\"]*\"\\)|[^;]+);";
    private static string Value(string css, string key) => Regex.Match(css, Pattern(key)).Groups[2].Value;
    private static string Set(string css, string key, string value) => Regex.Replace(css, Pattern(key), m => m.Groups[1].Value + value + ";");

    public static string ArtworkDataUri(string preset, string area)
    {
        if (area is not ("header" or "chat")) throw new InvalidDataException("Unknown artwork area.");
        if (preset == "zenit-gold")
        {
            var root = CssSyntax.Rules(Resource("zenit.css")).First(r => r.Header == ":root");
            var image = CssSyntax.Declarations(root.Body!).First(d => d.Property == "--zg-banner").Value;
            return image[5..^2];
        }
        var value = Value(PresetCss(preset), area == "header" ? "hardware-art" : "chat-art");
        return value[5..^2];
    }

    public static string Compile(ThemeConfiguration config)
    {
        config.Validate();
        string Metadata(string value) => Regex.Replace(value.Trim(), @"\s+", " ").Replace("/*", "/ *").Replace("*/", "* /");
        string designLabel = "/* Theme Studio design: " + Metadata(config.Name) + " */\n";
        string author = config.Imported is { } imported ? (string.IsNullOrWhiteSpace(imported.Author) ? "Unknown Author" : imported.Author) : "you";
        string description = string.IsNullOrWhiteSpace(config.Description) ? "Personal theme created with Theme Studio." : config.Description;
        string header = "/**\n * @name " + Metadata(config.Name) + "\n * @author " + Metadata(author) + "\n * @description " + Metadata(description) + "\n * @version " + BundledCatalog.Version + "\n */\n";
        string scopeGuild = config.TargetGuilds.FirstOrDefault() ?? "987654321012345678";
        if (config.Imported is not null)
        {
            long extraPixels = new[] { config.Banner == "custom" ? config.CustomBanner : null, config.Background == "custom" ? config.CustomBackground : null }.OfType<ThemeAsset>()
                .Sum(asset => (long)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(asset.Data.AsSpan(16)) * System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(asset.Data.AsSpan(20)));
            string importedCss = CssThemeImporter.Compile(config.Imported, scopeGuild, extraPixels);
            string overrides = "";
            if (config.Banner != "none") overrides += ":scope :is(.header_f37cb1,.header_XH4OpX){background:linear-gradient(#10101388,#10101388)," + Art(config.Banner, config.CustomBanner, "hardware-art") + " center/cover no-repeat!important;min-height:112px;}";
            if (config.Background != "none") overrides += ":scope :is(.chatContent_f75fb0,.chatContent_z_gUBg){background-image:linear-gradient(#101013ed,#101013ed)," + Art(config.Background, config.CustomBackground, "chat-art") + "!important;background-size:cover!important;background-position:center!important;}";
            if (config.Banner != "none" && config.DmChannels.Length > 0) overrides += ":scope:has([data-list-item-id=\"guildsnav___home\"][aria-selected=\"true\"]) :is(.container__9293f,.container_ybUK7Z){background:linear-gradient(#10101388,#10101388)," + Art(config.Banner, config.CustomBanner, "hardware-art") + " center/cover no-repeat!important;min-height:112px;}";
            if (overrides.Length > 0) importedCss += ServerScope(scopeGuild) + "{" + overrides + "}";
            return designLabel + header + Gate(importedCss.Replace(ServerScope(scopeGuild), TargetScope(config)));
        }
        var preset = catalog.Value.Themes.Single(t => t.Id == (config.Preset == "fluent" ? "primebuild" : config.Preset));
        string css = PresetCss(preset.Id);
        css = css[css.IndexOf("@scope", StringComparison.Ordinal)..].Replace(preset.Guild, scopeGuild);
        // Keep embedded images out of repeated palette, typography and target rewrites.
        css = Regex.Replace(css, Pattern("(?:hardware-art|chat-art|photo|sidebar-art|members-art)"), m => m.Groups[1].Value + "none;");
        string face = "";
        if (config.Font == "custom")
        {
            var asset = config.CustomFont!;
            face = "/* Imported font license:\n" + asset.License.Replace("*/", "* /") + "\n*/\n@font-face { font-family: \"PrimeBuild Personal Display\"; src: url(\"" + asset.DataUri + "\") format(\"" + (asset.MediaType == "font/ttf" ? "truetype" : "opentype") + "\"); font-style: normal; font-weight: 400; font-display: swap; }\n";
        }
        else if (config.Font != "native")
        {
            string fontCss = PresetCss(config.Font);
            face = fontCss[..fontCss.IndexOf("@scope", StringComparison.Ordinal)];
            face = face[face.IndexOf("/* Embedded", StringComparison.Ordinal)..];
            face = Regex.Replace(face, "PrimeBuild (?:Hardware|Dev|FantaFort) Display", "PrimeBuild Personal Display");
        }
        css = Regex.Replace(css, @"font-family:\s*[^;]+;", config.Font == "native" ? "font-family: inherit;" : "font-family: \"PrimeBuild Personal Display\", sans-serif;");
        if (config.Font == "custom") css = Regex.Replace(css, @"font-weight:\s*\d+;", "font-weight: 400;");
        if (config.Palette == "custom" || config.Palette == "blurple")
        {
            string accent = config.Palette == "blurple" ? "#8791ff" : config.Accent;
            string surface = config.Palette == "blurple" ? "#171821" : config.Surface;
            string rgb = string.Join(" ", Enumerable.Range(0, 3).Select(i => Convert.ToInt32(accent.Substring(1 + i * 2, 2), 16)));
            css = Set(css, "accent", accent); css = Set(css, "accent-rgb", rgb);
            // Light links stay legible even when the chosen accent is very dark.
            css = Set(css, "accent-text", ThemeConfiguration.Luminance(accent) >= .4 ? accent : "#c4c9ff");
            foreach (var key in new[] { "chat", "sidebar" }) css = Set(css, key, surface);
            foreach (var key in new[] { "panel", "panel-raised" }) css = Set(css, key, config.Palette == "blurple" ? "#242536" : config.Panel ?? surface);
            css = Set(css, "border", "#626573"); css = Set(css, "text-muted", "#bfc1cc");
            css = Set(css, "border-strong", "#969eaf");
            int last = css.LastIndexOf('}');
            string foreground = ThemeConfiguration.Luminance(accent) < .2 ? "#fafafb" : "#16100b";
            css = css.Insert(last, "\n  :is(.lookFilled__201d5, .lookFilled_9OQtot):is(.colorBrand__201d5, .colorBrand_9OQtot), :is(.unreadPill__908e2, .unreadPill_4TfzT5) { color: " + foreground + "; }\n  :is(.lookFilled__201d5, .lookFilled_9OQtot):is(.colorBrand__201d5, .colorBrand_9OQtot):hover { color: #16100b; }\n");
        }
        else
        {
            string palette = PresetCss(config.Palette);
            foreach (var key in new[] { "accent", "accent-text", "accent-rgb", "chat", "sidebar", "panel", "panel-raised", "border", "text-muted" }) css = Set(css, key, Value(palette, key));
        }
        css = Set(css, "hardware-art", "__PERSONAL_BANNER__");
        css = Set(css, "chat-art", "__PERSONAL_BACKGROUND__");
        css = Set(css, "photo", config.Background is "custom" or "none" ? "none" : Value(PresetCss(config.Background), "photo"));
        if (config.Background is "custom" or "none")
            foreach (var key in new[] { "sidebar-art", "members-art" }) css = Set(css, key, "none");
        else
            foreach (var key in new[] { "sidebar-art", "members-art" }) css = Set(css, key, Value(PresetCss(config.Background), key));
        // A strong scrim makes arbitrary imported artwork safe under fixed light message text.
        if (config.Background == "custom")
        {
            int last = css.LastIndexOf('}');
            css = css.Insert(last, "\n  :is(.chatContent_f75fb0, .chatContent_z_gUBg), :is(.container_f369db, .container_BkGrJJ) { background-image: linear-gradient(#101013ed, #101013ed), var(--pb-chat-art); background-size: cover; background-position: center; }\n");
        }
        if (config.Banner != "none" && config.DmChannels.Length > 0)
            css = css.Insert(css.LastIndexOf('}'), "\n :scope:has([data-list-item-id=\"guildsnav___home\"][aria-selected=\"true\"]) :is(.container__9293f,.container_ybUK7Z){background-image:linear-gradient(#10101388,#10101388),var(--pb-hardware-art);background-size:cover;min-height:112px;}\n");
        css = Set(css, "profile", "\"personal\"");
        if (config.Preset == "fluent")
        {
            css = Set(css, "header-height", config.Banner == "none" ? "56px" : "112px");
            // Neutral headings use Discord's native sizing as well as its native family.
            css = Regex.Replace(css, @"font-size:\s*(?:22|18|16)px;", "font-size: inherit;");
        }
        css = css.Replace(ServerScope(scopeGuild), TargetScope(config));
        // Insert large imported payloads once, after all other CSS rewrites.
        css = Regex.Replace(css, "__PERSONAL_BANNER__|__PERSONAL_BACKGROUND__", match => match.Value == "__PERSONAL_BANNER__"
            ? Art(config.Banner, config.CustomBanner, "hardware-art") : Art(config.Background, config.CustomBackground, "chat-art"));
        return designLabel + header + face + Gate(css);
    }

    // A :has() scope root makes Chromium re-match the whole Discord tree on every DOM change (~460 ms per
    // mutation measured on 9k nodes). Match it once on the root, then gate descendants with an inherited style query.
    internal static string Gate(string css)
    {
        int start = css.IndexOf("@scope", StringComparison.Ordinal);
        if (start < 0) return css;
        var output = new StringBuilder(css.Length + 4096).Append(css, 0, start);
        foreach (var rule in CssSyntax.Rules(css[start..]))
        {
            if (rule.Body is null || !rule.Header.StartsWith("@scope (", StringComparison.Ordinal) || !rule.Header.EndsWith(')')) throw CssSyntax.Invalid();
            string root = rule.Header[8..^1].Trim();
            string gate = "--pb-gate-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(root)))[..12].ToLowerInvariant();
            StringBuilder outside = new(), inside = new();
            Split(rule.Body, root, outside, inside);
            output.Append(root).Append('{').Append(gate).Append(":1;}\n").Append(outside)
                .Append("@container style(").Append(gate).Append(": 1) {\n").Append(inside).Append("}\n");
        }
        return output.ToString();
    }

    private static void Split(string body, string root, StringBuilder outside, StringBuilder inside)
    {
        foreach (var rule in CssSyntax.Rules(body))
        {
            if (rule.Body is null) throw CssSyntax.Invalid();
            if (Regex.IsMatch(rule.Header, @"^@(?:media|supports|container)\b"))
            {
                StringBuilder o = new(), i = new();
                Split(rule.Body, root, o, i);
                if (o.Length > 0) outside.Append(rule.Header).Append("{\n").Append(o).Append("}\n");
                if (i.Length > 0) inside.Append(rule.Header).Append("{\n").Append(i).Append("}\n");
                continue;
            }
            if (rule.Header.StartsWith('@')) { inside.Append(rule.Header).Append('{').Append(rule.Body).Append("}\n"); continue; }
            var selectors = CssSyntax.Selectors(rule.Header);
            var own = selectors.Where(StylesRoot).Select(s => root + s[6..]).ToList();
            var rest = selectors.Where(s => !StylesRoot(s)).Select(s => s.Replace(":scope", ":is(.base__5e434, .base_pog62O)")).ToList();
            if (own.Count > 0) outside.AppendJoin(", ", own).Append('{').Append(rule.Body).Append("}\n");
            if (rest.Count > 0) inside.AppendJoin(", ", rest).Append('{').Append(rule.Body).Append("}\n");
        }
    }

    // True when the selector's subject is the scope root itself (":scope", ":scope:has(...)", ":scope::before").
    private static bool StylesRoot(string selector)
    {
        if (!selector.StartsWith(":scope", StringComparison.Ordinal)) return false;
        int depth = 0; char quote = ' ';
        foreach (char c in selector.AsSpan(6))
        {
            if (quote != ' ') { if (c == quote) quote = ' '; continue; }
            if (c is '"' or '\'') quote = c;
            else if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;
            else if (depth == 0 && (char.IsWhiteSpace(c) || c is '>' or '+' or '~')) return false;
        }
        return true;
    }

    private static string Art(string choice, ThemeAsset? asset, string key) => choice switch
    {
        "none" => "none", "custom" => "url(\"" + asset!.DataUri + "\")", _ => Value(PresetCss(choice), key)
    };
}
