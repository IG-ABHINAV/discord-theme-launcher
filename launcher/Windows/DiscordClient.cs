using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace CommunityThemes;

internal sealed class DiscordClient
{
    public string Root { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord");
    public string VencordRoot { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord");
    public string StateRoot { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeBuild", "DiscordThemeLauncher");
    private string Update => Path.Combine(Root, "Update.exe");
    public bool Found => File.Exists(Update);
    private string? LatestApp => Directory.Exists(Root) ? Directory.EnumerateDirectories(Root, "app-*").Where(d => Version.TryParse(Path.GetFileName(d)[4..], out _)).OrderByDescending(d => Version.Parse(Path.GetFileName(d)[4..])).FirstOrDefault() : null;
    public bool Patched
    {
        get
        {
            var asar = LatestApp is string app ? Path.Combine(app, "resources", "app.asar") : "";
            return File.Exists(asar) && new FileInfo(asar).Length < 65536 && Encoding.UTF8.GetString(File.ReadAllBytes(asar)).Contains("Vencord", StringComparison.OrdinalIgnoreCase)
                && File.Exists(Path.Combine(VencordRoot, "dist", "renderer.js"));
        }
    }
    public static bool Elevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public void ValidateEnvironment()
    {
        if (Elevated) throw new InvalidOperationException("Close the launcher and run it normally, without Administrator privileges.");
        if (!Found) throw new InvalidOperationException("Install the standard Discord desktop client for this Windows user first. PTB, Canary, Store and custom installations require manual setup.");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VENCORD_USER_DATA_DIR")))
            throw new InvalidOperationException("A custom Vencord data directory is configured. Use manual theme installation for this profile.");
        ThemeStore.CheckPath(Root); ThemeStore.CheckPath(Update); ThemeStore.CheckPath(VencordRoot); ThemeStore.CheckPath(StateRoot);
        var asar = Path.Combine(LatestApp ?? throw new InvalidOperationException("Discord's app folder was not found."), "resources", "app.asar");
        ThemeStore.CheckPath(asar);
        if (!Patched && new FileInfo(asar).Length < 65536)
            throw new InvalidOperationException("An unrecognized client modification was detected. Use the manual installation route to avoid replacing it.");
    }

    public async Task StopAsync()
    {
        // Explicitly authorized by Apply & open Discord plus the calls/drafts checkbox.
        // Match executable paths, not merely a process name; do not stop PTB/Canary/other users' clients.
        foreach (var process in Process.GetProcessesByName("Discord"))
        {
            using (process)
            {
                if (process.HasExited) continue;
                string? path;
                try { path = process.MainModule?.FileName; }
                catch { throw new InvalidOperationException("Could not verify a running Discord process. Fully quit Discord, including the tray, and try again."); }
                if (path is null || !Path.GetFullPath(path).StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                process.Kill();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await process.WaitForExitAsync(timeout.Token);
            }
        }
    }

    public async Task InstallVencordAsync(byte[] installer)
    {
        ReleaseDownload.Verify(installer, ReleaseDownload.InstallerSha256);
        var directory = Path.Combine(StateRoot, "downloads"); ThemeStore.CheckPath(directory); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "VencordInstallerCli-" + ReleaseDownload.InstallerVersion + ".exe"); ThemeStore.CheckPath(path);
        await File.WriteAllBytesAsync(path, installer);
        using var process = new Process { StartInfo = new ProcessStartInfo(path) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true } };
        process.StartInfo.ArgumentList.Add("--install"); process.StartInfo.ArgumentList.Add("--branch"); process.StartInfo.ArgumentList.Add("stable");
        if (!process.Start()) throw new InvalidOperationException("The official Vencord installer could not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new InvalidOperationException("The official installer timed out. Retry or use the official manual installer."); }
        await Task.WhenAll(output, error);
        if (process.ExitCode != 0 || !Patched) throw new InvalidOperationException("The official Vencord installer did not complete. Use the official manual installer, then reopen this launcher.");
    }

    public void Open()
    {
        var start = new ProcessStartInfo(Update) { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("--processStart"); start.ArgumentList.Add("Discord.exe");
        Process.Start(start)?.Dispose();
    }
}
