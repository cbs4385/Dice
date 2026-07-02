using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    // Like SelfPlay, but each player is driven by its own IAiPolicy instead of a
    // single uniform-random policy. Used for AI-sanity tests (win-rate thresholds).
    public static class AiSelfPlay
    {
        public static SelfPlayResult PlayWithPolicies(IReadOnlyList<IAiPolicy> policies, IRng rng)
        {
            var state = GameSetup.NewGame(policies.Count, rng);

            while (!state.IsGameOver)
            {
                if (state.CurrentPhase is null)
                {
                    state = GameReducer.StartRound(state, rng);
                    continue;
                }

                int player = GameReducer.CurrentPlayer(state);
                var move = policies[player].ChooseMove(state, rng);
                state = move is null ? GameReducer.ApplyForfeit(state) : GameReducer.ApplyDraft(state, move, rng);
            }

            var scores = new List<int>(policies.Count);
            foreach (var player in state.Players)
            {
                scores.Add(Scoring.ScoreBoard(player.Board, state.Objective, player.PrivateElement, player.FavorRemaining));
            }

            return new SelfPlayResult(state, scores);
        }
    }
}
