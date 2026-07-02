using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    // Strongest tier: greedily maximizes the true final score (Scoring.ScoreBoard,
    // aware of the actual public objective) one ply ahead, rather than a local
    // proxy heuristic like AdeptAi. Ties broken uniformly at random via rng.
    public sealed class OracleAi : IAiPolicy
    {
        public DraftChoice? ChooseMove(GameState state, IRng rng)
        {
            var candidates = LegalDrafts.EnumerateSimple(state);
            if (candidates.Count == 0)
            {
                return null;
            }

            int player = GameReducer.CurrentPlayer(state);
            var playerState = state.Players[player];

            var best = new List<DraftChoice> { candidates[0] };
            int bestScore = ScoreOf(candidates[0], state, playerState);

            for (int i = 1; i < candidates.Count; i++)
            {
                int score = ScoreOf(candidates[i], state, playerState);
                if (score > bestScore)
                {
                    bestScore = score;
                    best.Clear();
                    best.Add(candidates[i]);
                }
                else if (score == bestScore)
                {
                    best.Add(candidates[i]);
                }
            }

            return best[rng.NextInt(best.Count)];
        }

        private static int ScoreOf(DraftChoice choice, GameState state, PlayerState playerState)
        {
            Die die = LegalDrafts.ResolveDie(state, choice);
            var hypotheticalBoard = playerState.Board.WithPlacement(new Placement(choice.Row, choice.Col, die));
            return Scoring.ScoreBoard(hypotheticalBoard, state.Objective, playerState.PrivateElement, playerState.FavorRemaining);
        }
    }
}
