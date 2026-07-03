using System;
using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game.Clash
{
    // Headless Clash-aware self-play, mirroring Quintessence.Game.SelfPlay's shape:
    // no I/O, no UnityEngine, driven end-to-end by an injected IRng. Uses the
    // minimal ClashAiPolicy (NoviceAi placement + legal-only, unaggressive
    // intervention/Ward choices) purely to exercise every Clash code path under
    // load - the resulting play quality says nothing about balance.
    public static class ClashSelfPlay
    {
        // A safety net against a genuine infinite-loop bug (e.g. a Pending that
        // never clears), not a gameplay rule - ordinary games finish in a small
        // fraction of this many steps.
        private const int MaxIterations = 100_000;

        public static SelfPlayResult PlayRandomGame(int playerCount, IRng rng, ClashConfig clashConfig)
        {
            var state = GameSetup.NewGame(playerCount, rng, clashConfig: clashConfig);
            var policy = new ClashAiPolicy(new NoviceAi());

            int iterations = 0;
            while (!state.IsGameOver)
            {
                if (++iterations > MaxIterations)
                {
                    throw new InvalidOperationException("ClashSelfPlay exceeded max iterations - possible infinite loop.");
                }

                var clash = state.Clash!;
                if (clash.Pending is PendingIntervention pending)
                {
                    state = policy.ShouldWard(state, pending.Target)
                        ? ClashReducer.Ward(state, pending.Target)
                        : ClashReducer.DeclineWard(state, pending.Target);
                    continue;
                }

                if (state.CurrentPhase is null)
                {
                    state = GameReducer.StartRound(state, rng);
                    continue;
                }

                int currentPlayer = GameReducer.CurrentPlayer(state);

                // Occasionally declare an intervention before drafting, rather than
                // always doing so the instant one becomes affordable.
                if (rng.NextInt(4) == 0)
                {
                    var declaration = policy.ChooseIntervention(state, currentPlayer, rng);
                    if (declaration is not null)
                    {
                        state = ClashReducer.DeclareIntervention(state, currentPlayer, declaration.Value.Kind, declaration.Value.Params, rng);
                        continue;
                    }
                }

                var candidates = LegalDrafts.EnumerateSimple(state);
                state = candidates.Count == 0
                    ? GameReducer.ApplyForfeit(state)
                    : GameReducer.ApplyDraft(state, candidates[rng.NextInt(candidates.Count)], rng);
            }

            var scores = new List<int>(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                var player = state.Players[i];
                scores.Add(ClashScoring.ScoreBoardWithNullifications(
                    player.Board, state.Objective, player.PrivateElement, player.FavorRemaining,
                    state.Clash!.NullifiedBandCells, forPlayer: i));
            }

            return new SelfPlayResult(state, scores);
        }
    }
}
