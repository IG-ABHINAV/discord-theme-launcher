using System.Text;

namespace CommunityThemes;

// Strict, bounded CSS subset parser. Strings, comments and function/attribute nesting
// are tokenized before interpreting rules or declarations; unsupported grammar fails closed.
internal sealed record CssRule(string Header, string? Body);
internal static class CssSyntax
{
    public static List<CssRule> Rules(string source)
    {
        var rules = new List<CssRule>(); int position = 0;
        while (position < source.Length)
        {
            Skip(source, ref position);
            if (position == source.Length) break;
            string header = Read(source, ref position, "{;}");
            if (header.Length == 0 || position == source.Length) throw Invalid();
            char delimiter = source[position++];
            if (delimiter == '}') throw Invalid();
            if (delimiter == ';') rules.Add(new(header, null));
            else
            {
                int start = position, depth = 1;
                while (position < source.Length && depth > 0)
                {
                    char c = source[position];
                    if (c is '\'' or '"') { String(source, ref position); continue; }
                    if (Comment(source, ref position)) continue;
                    if (c == '{') depth++; else if (c == '}') depth--;
                    if (depth > 32) throw Invalid();
                    position++;
                }
                if (depth != 0) throw Invalid();
                rules.Add(new(header, source[start..(position - 1)]));
            }
            if (rules.Count > 4096) throw Invalid();
        }
        return rules;
    }

    public static List<(string Property, string Value)> Declarations(string body)
    {
        var values = new List<(string, string)>(); int position = 0;
        while (position < body.Length)
        {
            Skip(body, ref position); if (position == body.Length) break;
            string property = Read(body, ref position, ":;{}");
            if (position == body.Length || body[position++] != ':' || property.Length == 0) throw Invalid();
            string value = Read(body, ref position, ";{}");
            if (value.Length == 0 || position < body.Length && body[position] != ';') throw Invalid();
            if (position < body.Length) position++;
            values.Add((property, value));
            if (values.Count > 4096) throw Invalid();
        }
        return values;
    }

    public static List<string> Selectors(string text)
    {
        List<string> result = []; int position = 0;
        while (position < text.Length)
        {
            var selector = Read(text, ref position, ","); if (selector.Length == 0) throw Invalid();
            result.Add(selector); if (position < text.Length) position++;
            if (result.Count > 256) throw Invalid();
        }
        return result;
    }

    private static string Read(string source, ref int position, string delimiters)
    {
        StringBuilder value = new(); var nesting = new Stack<char>();
        while (position < source.Length)
        {
            char c = source[position];
            if (c is '\'' or '"') { int start = position; String(source, ref position); value.Append(source[start..position]); continue; }
            if (Comment(source, ref position)) { value.Append(' '); continue; }
            if (nesting.Count == 0 && delimiters.Contains(c)) break;
            if (c is '{' or '}') throw Invalid();
            if (c is '(' or '[') nesting.Push(c);
            else if (c is ')' or ']')
            {
                if (nesting.Count == 0 || nesting.Pop() != (c == ')' ? '(' : '[')) throw Invalid();
            }
            if (nesting.Count > 32 || c is '\\' or '\0' || char.IsControl(c) && !char.IsWhiteSpace(c)) throw Invalid();
            value.Append(c); position++;
        }
        if (nesting.Count != 0) throw Invalid();
        return value.ToString().Trim();
    }

    private static void Skip(string source, ref int position)
    {
        while (position < source.Length)
        {
            if (char.IsWhiteSpace(source[position])) { position++; continue; }
            if (!Comment(source, ref position)) break;
        }
    }
    private static bool Comment(string source, ref int position)
    {
        if (position + 1 >= source.Length || source[position] != '/' || source[position + 1] != '*') return false;
        int end = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
        if (end < 0) throw Invalid(); position = end + 2; return true;
    }
    private static void String(string source, ref int position)
    {
        char quote = source[position++];
        while (position < source.Length)
        {
            char c = source[position++];
            // Escaped CSS identifiers/URLs are deliberately outside this version's supported grammar.
            if (c is '\\' or '\n' or '\r' or '\0') throw Invalid();
            if (c == quote) return;
        }
        throw Invalid();
    }
    public static InvalidDataException Invalid() => new("Unsupported or malformed CSS syntax. Use a self-contained stylesheet without nesting or escaped identifiers.");
}
