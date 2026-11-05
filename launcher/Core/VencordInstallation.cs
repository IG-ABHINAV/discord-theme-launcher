using System.Text;

namespace CommunityThemes;

public static class VencordInstallation
{
    private static readonly string[] RuntimeFiles = ["patcher.js", "preload.js", "renderer.js", "renderer.css"];
    public static bool HasLoader(string asar) => File.Exists(asar) && new FileInfo(asar).Length < 65536
        && Encoding.UTF8.GetString(File.ReadAllBytes(asar)).Contains("Vencord", StringComparison.OrdinalIgnoreCase);

    public static string[] MissingRuntimeFiles(string vencordRoot) => RuntimeFiles
        .Where(name => !ReadableNonEmpty(Path.Combine(vencordRoot, "dist", name))).ToArray();

    public static bool IsReady(string asar, string vencordRoot) => HasLoader(asar) && MissingRuntimeFiles(vencordRoot).Length == 0;

    private static bool ReadableNonEmpty(string path)
    {
        try { using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); return file.ReadByte() != -1; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    public static StartupState InspectWindowTitle(string title) => title.Equals("Error", StringComparison.OrdinalIgnoreCase)
        ? StartupState.Failed : title.EndsWith(" - Discord", StringComparison.OrdinalIgnoreCase) ? StartupState.Ready : StartupState.Waiting;
}

public enum StartupState { Waiting, Ready, Failed }
