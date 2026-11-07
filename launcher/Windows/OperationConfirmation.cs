namespace CommunityThemes;

internal sealed record OperationConfirmation(string Id, string Operation, int Revision, ThemeConfiguration? Configuration,
    bool RequiresSetup, bool Repair, string Problem, string? RecoveryId = null)
{
    public static OperationConfirmation Create(string operation, int revision, ThemeConfiguration? configuration,
        bool requiresSetup, bool repair, string problem, string? recoveryId = null) => new(Guid.NewGuid().ToString("N"), operation, revision, configuration, requiresSetup, repair, problem, recoveryId);

    // Consume the exact reviewed snapshot once, before downloads, file writes or process operations.
    public static bool TryTake(ref OperationConfirmation? pending, string id, int revision, out OperationConfirmation? confirmed)
    {
        confirmed = null;
        if (pending is null || pending.Id != id) return false;
        var candidate = pending; pending = null;
        if (candidate.Revision != revision || candidate.Problem.Length > 0) return false;
        confirmed = candidate; return true;
    }
}
