namespace Quintessence.Game.Clash
{
    public enum ClashLogOutcome
    {
        Declared,
        Warded,
        Applied,
        Cancelled,
    }

    // Append-only, for deterministic replays and later spectator features
    // (docs/clash.md SS3). Data only for now - no consumer yet, per SS5's instruction
    // to make this *possible* later without building the feature now.
    public sealed record ClashLogEntry(int Round, int Actor, int Target, InterventionKind Kind, ClashLogOutcome Outcome);
}
