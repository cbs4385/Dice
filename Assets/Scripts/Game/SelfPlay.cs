using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    public sealed record SelfPlayResult(GameState FinalState, IReadOnlyList<int> Scores);

    // Headless: no I/O, no UnityEngine, just Reduce-shaped calls driven by an
    // injected IRng end to end.
    public static class SelfPlay
    {
        public static SelfPlayResult PlayRandomGame(int playerCount, IRng rng)
        {
            var state = GameSetup.NewGame(playerCount, rng);

            while (!state.IsGameOver)
            {
                if (state.CurrentPhase is null)
                {
                    state = GameReducer.StartRound(state, rng);
                    continue;
                }

                var candidates = LegalDrafts.EnumerateSimple(state);
                if (candidates.Count == 0)
                {
                    state = GameReducer.ApplyForfeit(state);
                }
                else
                {
                    var choice = candidates[rng.NextInt(candidates.Count)];
                    state = GameReducer.ApplyDraft(state, choice, rng);
                }
            }

            var scores = new List<int>(playerCount);
            foreach (var player in state.Players)
            {
                scores.Add(Scoring.ScoreBoard(player.Board, state.Objective, player.PrivateElement, player.FavorRemaining));
            }

            return new SelfPlayResult(state, scores);
        }
    }
}
