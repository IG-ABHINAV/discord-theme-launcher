using System.Reflection;
using System.Text.Json;

namespace CommunityThemes;

public sealed record CommunityDesign(string Id, string Name, string Author, string AuthorUrl, string Source, string License,
    string Version, string Adapter, string[] Capabilities, string? Revision = null, string? Sha256 = null, string? Adaptation = null);

public static class CommunityGallery
{
    public static CommunityDesign[] Designs => JsonSerializer.Deserialize<CommunityDesign[]>(ThemeCompiler.Resource("community.json"), Catalog.JsonOptions)!;
    internal static string ZenitFontCss
    {
        get
        {
            using var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("montserrat.ttf")!;
            using var buffer = new MemoryStream(); input.CopyTo(buffer);
            ReleaseDownload.Verify(buffer.ToArray(), "0f7b311b2f3279e4eef9b2f968bcdbab6e28f4daeb1f049f4f278a902bcd82f7");
            return "/* Montserrat, SIL OFL 1.1:\n" + ThemeCompiler.Resource("montserrat-license.txt").Replace("*/", "* /") +
                "\n*/\n@font-face{font-family:\"ts-" + CssThemeImporter.ZenitHash[..12] + "-Montserrat\";src:url(\"data:font/ttf;base64," + Convert.ToBase64String(buffer.ToArray()) +
                "\");font-style:normal;font-weight:100 900;font-display:swap;}\n";
        }
    }
    public static ThemeConfiguration Create(string id, string guild = "")
    {
        var design = Designs.SingleOrDefault(d => d.Id == id) ?? throw new InvalidDataException("Unknown community design.");
        if (design.Adapter == "preset") return new ThemeConfiguration { Name = design.Name, Guild = guild, Preset = id, Palette = id, Font = id, Banner = id, Background = id };
        if (design.Adapter != "reviewed-css" || design.Id != "zenit-gold") throw new InvalidDataException("This community adapter is not supported.");
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("zenit.css")!;
        using var buffer = new MemoryStream(); stream.CopyTo(buffer);
        ReleaseDownload.Verify(buffer.ToArray(), design.Sha256!);
        var review = CssThemeImporter.Review(buffer.ToArray(), "ZenitGold.theme.css", design.Source + "/tree/" + design.Revision, ThemeCompiler.Resource("zenit-license.txt"));
        return new ThemeConfiguration { Name = design.Name, Guild = guild, Imported = review.Theme ?? throw new InvalidDataException(string.Join("\n", review.Notes)) };
    }
}
