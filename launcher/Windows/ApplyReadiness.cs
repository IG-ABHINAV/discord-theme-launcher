namespace CommunityThemes;

internal sealed record ApplyReadiness(string Reason, string Action = "", string ActionLabel = "")
{
    public bool CanApply => Reason.Length == 0;

    public static ApplyReadiness Evaluate(bool busy, string validation, bool previewReady)
    {
        if (busy) return new("An operation is running. Wait for it to finish.");
        if (validation.Length > 0) return new(validation);
        if (!previewReady) return new("Wait for the preview to finish loading, or reload it if it is stuck.", "refresh", "Reload preview");
        return new("");
    }
}
