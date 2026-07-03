using Quintessence.Engine;

namespace Quintessence.Game.Clash
{
    // Deliberately minimal: reuses an existing placement policy (Novice/Adept/
    // Oracle) for drafting, and adds only the bare-minimum legal Clash behavior on
    // top - it only ever acts on ClashLegalMoves-enumerated candidates and Wards
    // whenever it can afford to. This is a safe, testable baseline, NOT tuned
    // "aggressiveness" - when to attack and how eagerly to Ward are balance
    // decisions docs/clash.md SS5 reserves for a human.
    public sealed class ClashAiPolicy
    {
        private readonly IAiPolicy _placementPolicy;

        public ClashAiPolicy(IAiPolicy placementPolicy)
        {
            _placementPolicy = placementPolicy;
        }

        public DraftChoice? ChooseMove(GameState state, IRng rng) => _placementPolicy.ChooseMove(state, rng);

        public (InterventionKind Kind, InterventionParams Params)? ChooseIntervention(GameState state, int actor, IRng rng)
        {
            var candidates = ClashLegalMoves.EnumerateDeclarations(state, actor);
            return candidates.Count == 0 ? null : candidates[rng.NextInt(candidates.Count)];
        }

        public bool ShouldWard(GameState state, int target)
        {
            var clash = state.Clash;
            if (clash?.Pending is null || clash.Pending.Target != target)
            {
                return false;
            }

            return state.Players[target].FavorRemaining >= clash.Config.WardCost;
        }
    }
}
