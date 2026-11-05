namespace CommunityThemes;

internal sealed record ApplyPrerequisites(bool PreviewMode, bool Busy, bool Elevated, bool ProfileHealthy,
    string Validation, bool PreviewReady, bool DiscordFound, bool VencordReady, bool NeedsRepair, bool InstallRequested, bool Acknowledged);

internal sealed record ApplyReadiness(string Reason, string Action = "", string ActionLabel = "")
{
    public bool CanApply => Reason.Length == 0;

    public static ApplyReadiness Evaluate(ApplyPrerequisites state)
    {
        if (state.PreviewMode) return new("Preview mode cannot apply themes. Close this window and open the EXE normally.");
        if (state.Busy) return new("An operation is running. Wait for it to finish.");
        if (state.Elevated) return new("Run the launcher normally, without Administrator privileges.");
        if (!state.ProfileHealthy) return new("Vencord settings could not be read. Check Recovery and the manual recovery guide.", "recovery", "Review setup");
        if (state.Validation.Length > 0) return new(state.Validation);
        if (!state.PreviewReady) return new("Wait for the preview to finish loading, or reload it if it is stuck.", "refresh", "Reload preview");
        if (!state.DiscordFound) return new("Standard Discord desktop was not found. Install the regular Windows desktop app, then reopen the launcher.", "recovery", "Review setup");
        if (!state.VencordReady && !state.InstallRequested)
            return new(state.NeedsRepair ? "Vencord needs repair. In Recovery, enable official Vencord installation / repair." : "Vencord is required. In Recovery, enable official Vencord installation, or install it manually first.", "recovery", "Set up Vencord");
        if (!state.Acknowledged) return new("Finish calls and save drafts, then tick the restart checkbox to enable Apply.", "restart", "Restart checkbox");
        return new("");
    }
}
