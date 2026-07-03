namespace Quintessence.Game.Clash
{
    // Balance values are human-gated (docs/clash.md SS2.4); these are the spec's own
    // provisional defaults, not tuned/approved numbers. Wired as data, never hardcoded
    // at call sites, so a human can retune via playtest without touching logic.
    public sealed record ClashConfig(
        int StormPerAttune,
        int StormCap,
        int InterventionCost,
        int BacklashFavor,
        int WardCost,
        int ScorchMaxPips,
        int PetrifyDurationRounds,
        int InterventionsPerMatch)
    {
        public static readonly ClashConfig Default = new(
            StormPerAttune: 1,
            StormCap: 5,
            InterventionCost: 4,
            BacklashFavor: 1,
            WardCost: 1,
            ScorchMaxPips: 3,
            PetrifyDurationRounds: 1,
            InterventionsPerMatch: 3);
    }
}
