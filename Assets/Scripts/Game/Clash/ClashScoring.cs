using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game.Clash
{
    // A thin Game-layer wrapper over the public Scoring API - Engine stays entirely
    // Clash-unaware (no InternalsVisibleTo grant, no new Engine public API). Eclipse's
    // "nullify a band cell" is applied by computing the base score and subtracting
    // the band-cell bonus only for cells that were actually satisfied and nullified.
    public static class ClashScoring
    {
        public static int ScoreBoardWithNullifications(
            Board board,
            PublicObjective objective,
            Element privateElement,
            int unspentFavor,
            IReadOnlyList<NullifiedCell> nullifiedCells,
            int forPlayer,
            ScoringConfig? config = null)
        {
            var cfg = config ?? ScoringConfig.Default;
            int baseScore = Scoring.ScoreBoard(board, objective, privateElement, unspentFavor, cfg);

            int deduction = 0;
            foreach (var cell in nullifiedCells)
            {
                if (cell.Player != forPlayer)
                {
                    continue;
                }

                if (board.CellAt(cell.Row, cell.Col) is Cell.BandCell bandCell
                    && board.DieAt(cell.Row, cell.Col) is Die die
                    && Bands.Of(die.Face) == bandCell.Band)
                {
                    deduction += cfg.BandCellPoints;
                }
            }

            return baseScore - deduction;
        }
    }
}
