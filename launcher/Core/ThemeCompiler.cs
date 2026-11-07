using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public static class ThemeCompiler
{
    public const string PersonalFile = "PersonalServer.theme.css";
    internal static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidDataException("Bundled theme resource is missing.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    public static Catalog BundledCatalog => Catalog.Parse(Resource("catalog.json"));
    public static string ServerScope(string guild)
    {
        guild = ThemeConfiguration.ParseGuild(guild);
        string css = PresetCss("primebuild"); int start = css.IndexOf("@scope", StringComparison.Ordinal);
        return css[start..css.IndexOf('{', start)].Trim().Replace(BundledCatalog.Themes.Single(t => t.Id == "primebuild").Guild, guild);
    }
    public static string PresetCss(string id)
    {
        var theme = BundledCatalog.Themes.SingleOrDefault(t => t.Id == id) ?? throw new InvalidDataException("Unknown preset.");
        var css = Resource(theme.File); ReleaseDownload.Verify(Encoding.UTF8.GetBytes(css), theme.Sha256, theme.Bytes); return css;
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
        if (config.Imported is not null) return CssThemeImporter.Compile(config.Imported, config.Guild);
        var preset = BundledCatalog.Themes.Single(t => t.Id == (config.Preset == "fluent" ? "primebuild" : config.Preset));
        string css = PresetCss(preset.Id);
        css = css[css.IndexOf("@scope", StringComparison.Ordinal)..].Replace(preset.Guild, config.Guild);
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
            foreach (var key in new[] { "chat", "sidebar", "panel", "panel-raised" }) css = Set(css, key, surface);
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
        css = Set(css, "hardware-art", Art(config.Banner, config.CustomBanner, "hardware-art"));
        css = Set(css, "chat-art", Art(config.Background, config.CustomBackground, "chat-art"));
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
        css = Set(css, "profile", "\"personal\"");
        if (config.Preset == "fluent")
        {
            css = Set(css, "header-height", config.Banner == "none" ? "56px" : "112px");
            // Neutral headings use Discord's native sizing as well as its native family.
            css = Regex.Replace(css, @"font-size:\s*(?:22|18|16)px;", "font-size: inherit;");
        }
        return "/* @name Personal Server Theme\n * @author PrimeBuild\n * @version " + BundledCatalog.Version + "\n * Local client styling for server " + config.Guild + ". Generated from a validated .dtheme configuration.\n */\n" + face + css;
    }

    private static string Art(string choice, ThemeAsset? asset, string key) => choice switch
    {
        "none" => "none", "custom" => "url(\"" + asset!.DataUri + "\")", _ => Value(PresetCss(choice), key)
    };
}
