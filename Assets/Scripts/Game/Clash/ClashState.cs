using System.Collections.Generic;

namespace Quintessence.Game.Clash
{
    // Storm is indexed by player position (0..N-1), matching GameState.Players'
    // existing indexing convention rather than a dictionary keyed by player id.
    public sealed record ClashState(
        IReadOnlyList<int> Storm,
        IReadOnlyList<InterventionKind> InterventionsAvailable,
        IReadOnlyList<PetrifyToken> PetrifyTokens,
        PendingIntervention? Pending,
        IReadOnlyList<ClashLogEntry> InterventionLog);
}
