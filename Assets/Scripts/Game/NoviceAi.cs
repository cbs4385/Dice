using Quintessence.Engine;

namespace Quintessence.Game
{
    // Weakest tier: picks uniformly at random among legal, favor-free placements.
    public sealed class NoviceAi : IAiPolicy
    {
        public DraftChoice? ChooseMove(GameState state, IRng rng)
        {
            var candidates = LegalDrafts.EnumerateSimple(state);
            return candidates.Count == 0 ? null : candidates[rng.NextInt(candidates.Count)];
        }
    }
}
