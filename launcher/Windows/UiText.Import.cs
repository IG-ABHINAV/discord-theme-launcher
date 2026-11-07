using System.Reflection;
using System.Text.Json;

namespace CommunityThemes;

internal static partial class UiText
{
    private static void LoadImportText()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ui/host-import.json")!;
        var rows = JsonSerializer.Deserialize<string[][]>(stream)!;
        foreach (var row in rows)
        {
            Italian[row[0]] = row[1]; Spanish[row[0]] = row[2]; Russian[row[0]] = row[3];
        }
    }
}
