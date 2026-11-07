using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public static class ThemePreview
{
    public static string Html(ThemeConfiguration configuration, bool embedded = false)
    {
        string css = ThemeCompiler.Compile(configuration);
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("builder-preview.html")!;
        using var reader = new StreamReader(stream);
        // No user-authored HTML, scripts, CSS or URLs are accepted. License text cannot terminate a style element.
        return Regex.Replace(reader.ReadToEnd(), "__TARGET_GUILD__|__TARGET_DM__|__INITIAL_CONTEXT__|__SERVER_NAME__|__PERSONAL_CSS__", match => match.Value switch
            { "__TARGET_GUILD__" => configuration.TargetGuilds.FirstOrDefault() ?? "987654321012345678", "__TARGET_DM__" => configuration.DmChannels.FirstOrDefault() ?? "876543210123456789", "__INITIAL_CONTEXT__" => configuration.TargetGuilds.Length == 0 && configuration.DmChannels.Length > 0 ? "dm" : "target", "__SERVER_NAME__" => WebUtility.HtmlEncode(configuration.Name), _ => css.Replace("<", "\\3c ") })
            .Replace("<body>", embedded ? "<body data-embedded=\"true\">" : "<body>")
            .Replace("<head>", "<head><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; img-src data:; font-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'none'; form-action 'none'; base-uri 'none'\">");
    }
}
