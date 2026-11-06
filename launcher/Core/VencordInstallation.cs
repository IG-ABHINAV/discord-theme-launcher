using System.Text;
using System.Text.RegularExpressions;

namespace CommunityThemes;

public static class VencordInstallation
{
    private static readonly string[] RuntimeFiles = ["patcher.js", "preload.js", "renderer.js", "renderer.css"];
    public static bool HasLoader(string asar) => File.Exists(asar) && new FileInfo(asar).Length < 65536
        && Encoding.UTF8.GetString(File.ReadAllBytes(asar)).Contains("Vencord", StringComparison.OrdinalIgnoreCase);

    // Vencord writes a tiny app.asar whose index.js is require("<absolute path>/patcher.js").
    public static string? RequiredModule(string asar)
    {
        if (!HasLoader(asar)) return null;
        var match = Regex.Match(Encoding.UTF8.GetString(File.ReadAllBytes(asar)), "require\\(\\s*[\"']((?:\\\\.|[^\"'\\\\])*)[\"']\\s*\\)");
        return match.Success ? Regex.Unescape(match.Groups[1].Value) : null;
    }

    public static bool StubIsUnbootable(string asar)
    {
        var required = RequiredModule(asar);
        return required is not null && Path.IsPathRooted(required) && !File.Exists(required);
    }

    // Put the official package back when the stub points at a missing patcher.js.
    // Leaves a healthy install alone, and refuses a backup that is too small to be Discord.
    public static bool TryRestoreOriginal(string resourcesDirectory)
    {
        var app = Path.Combine(resourcesDirectory, "app.asar");
        var original = Path.Combine(resourcesDirectory, "_app.asar");
        if (!StubIsUnbootable(app) || !File.Exists(original) || new FileInfo(original).Length < 100_000 || RequiredModule(original) is not null) return false;
        ThemeStore.CheckPath(app); ThemeStore.CheckPath(original);
        var stub = app + ".stub";
        File.Move(app, stub, true);
        try { File.Move(original, app, true); }
        catch
        {
            if (!File.Exists(app) && File.Exists(stub)) File.Move(stub, app, true);
            throw;
        }
        File.Delete(stub);
        return new FileInfo(app).Length >= 100_000;
    }

    public static string[] MissingRuntimeFiles(string vencordRoot) => RuntimeFiles
        .Where(name => !ReadableNonEmpty(Path.Combine(vencordRoot, "dist", name))).ToArray();

    public static bool IsReady(string asar, string vencordRoot)
    {
        if (!HasLoader(asar) || MissingRuntimeFiles(vencordRoot).Length != 0) return false;
        var required = RequiredModule(asar);
        return required is null || !Path.IsPathRooted(required) || ReadableNonEmpty(required);
    }

    private static bool ReadableNonEmpty(string path)
    {
        try { using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); return file.ReadByte() != -1; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    public static StartupState InspectWindowTitle(string title) => title.Equals("Error", StringComparison.OrdinalIgnoreCase)
        ? StartupState.Failed : title.EndsWith(" - Discord", StringComparison.OrdinalIgnoreCase) ? StartupState.Ready : StartupState.Waiting;
}

public enum StartupState { Waiting, Ready, Failed }
