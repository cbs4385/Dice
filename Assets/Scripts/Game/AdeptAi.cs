using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    // AI weights are a human-gated balance value (AGENTS.md); defaults are a
    // reasonable starting point, not a tuned/approved value.
    public sealed record AdeptWeights(int BandCellPoints, int PrivateElementPoints)
    {
        public static readonly AdeptWeights Default = new(BandCellPoints: 4, PrivateElementPoints: 2);
    }

    // Middle tier: a one-ply local heuristic (band-cell fit + private-element match)
    // that ignores the public objective entirely - deliberately weaker than Oracle,
    // which evaluates the true final score.
    public sealed class AdeptAi : IAiPolicy
    {
        private readonly AdeptWeights _weights;

        public AdeptAi(AdeptWeights? weights = null)
        {
            _weights = weights ?? AdeptWeights.Default;
        }

        public DraftChoice? ChooseMove(GameState state, IRng rng)
        {
            var candidates = LegalDrafts.EnumerateSimple(state);
            if (candidates.Count == 0)
            {
                return null;
            }

            int player = GameReducer.CurrentPlayer(state);
            var playerState = state.Players[player];

            return PickBest(candidates, state, playerState, rng);
        }

        private DraftChoice PickBest(IReadOnlyList<DraftChoice> candidates, GameState state, PlayerState playerState, IRng rng)
        {
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

        private int ScoreOf(DraftChoice choice, GameState state, PlayerState playerState)
        {
            Die die = LegalDrafts.ResolveDie(state, choice);

            int score = 0;
            if (playerState.Board.CellAt(choice.Row, choice.Col) is Cell.BandCell bandCell
                && Bands.Of(die.Face) == bandCell.Band)
            {
                score += _weights.BandCellPoints;
            }

            if (die.Element == playerState.PrivateElement)
            {
                score += _weights.PrivateElementPoints;
            }

            return score;
        }
    }
}
