namespace CommunityThemes;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        int updated = Array.IndexOf(args, "--after-update");
        if (updated >= 0) FinishUpdate(updated + 1 < args.Length ? args[updated + 1] : "");
        UiText.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeBuild", "DiscordThemeLauncher"));
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => LauncherStyle.ShowMessage(null, UiText.Text("The launcher encountered an unexpected error. Close it and reopen your saved configuration. Check the manual recovery guide if an operation was interrupted."), UiText.Text("Discord Theme Launcher"));
        using var typography = new LauncherStyle.Typography();
        using var instance = new Mutex(true, "Local\\PrimeBuild.DiscordThemeLauncher." + System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value + (args.Contains("--safe-preview") ? ".Preview" : ""), out bool first);
        if (!first) { LauncherStyle.ShowMessage(null, UiText.Text("The launcher is already open for this Windows user."), UiText.Text("Discord Theme Launcher")); return; }
        // Normal editing is always a preview until the in-app confirmation is accepted.
        // --preview remains a compatible way to open that workflow; --safe-preview is for non-mutating QA.
        Application.Run(new BuilderForm(args.Contains("--safe-preview"), null, updated >= 0));
    }

    // The previous version renamed itself to .old before starting this one; wait for it, then remove it.
    private static void FinishUpdate(string pid)
    {
        try { if (int.TryParse(pid, out var id)) using (var previous = System.Diagnostics.Process.GetProcessById(id)) previous.WaitForExit(30000); }
        catch (ArgumentException) { /* Already closed. */ }
        var old = Environment.ProcessPath + ".old";
        for (int i = 0; i < 20 && File.Exists(old); i++)
            try { File.Delete(old); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { Thread.Sleep(250); }
    }
}
