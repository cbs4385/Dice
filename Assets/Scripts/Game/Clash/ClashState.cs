using System.Collections.Generic;

namespace Quintessence.Game.Clash
{
    // Storm is indexed by player position (0..N-1), matching GameState.Players'
    // existing indexing convention rather than a dictionary keyed by player id.
    //
    // Config is carried here (not threaded as a parameter through GameReducer/
    // ClashReducer methods) specifically so that adding Clash never changes any
    // existing method's signature - every call site that doesn't use Clash stays
    // untouched, and every Clash-aware call site reaches config via state.Clash.Config.
    public sealed record ClashState(
        ClashConfig Config,
        IReadOnlyList<int> Storm,
        IReadOnlyList<InterventionKind> InterventionsAvailable,
        IReadOnlyList<PetrifyToken> PetrifyTokens,
        PendingIntervention? Pending,
        IReadOnlyList<ClashLogEntry> InterventionLog,
        IReadOnlyList<NullifiedCell> NullifiedBandCells);
}
