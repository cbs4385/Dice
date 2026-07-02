using System;
using System.Collections.Generic;
using System.Linq;

namespace Quintessence.Engine
{
    // Point values here are balance levers the rulebook's "Design notes" section calls
    // out as tunable (band cell value, empty-cell penalty, favor token value). Defaults
    // match the v0.1 rulebook; a human playtests and approves any change (see AGENTS.md).
    public sealed record ScoringConfig(
        int BandCellPoints,
        int PrivateElementPoints,
        int FavorTokenPoints,
        int EmptyCellPenalty)
    {
        public static readonly ScoringConfig Default = new(
            BandCellPoints: 4,
            PrivateElementPoints: 2,
            FavorTokenPoints: 1,
            EmptyCellPenalty: -2);
    }

    public enum PublicObjective
    {
        FirmamentRows,
        DeepColumns,
        Constellation,
        RisingTide,
        ElementalBounty,
        ScarcitysReward,
    }

    public static class Scoring
    {
        public static int ScoreBoard(
            Board board,
            PublicObjective objective,
            Element privateElement,
            int unspentFavor,
            ScoringConfig? config = null)
        {
            var cfg = config ?? ScoringConfig.Default;
            return ScoreBandCells(board, cfg)
                + ScoreObjective(board, objective)
                + ScorePrivateElement(board, privateElement, cfg)
                + (unspentFavor * cfg.FavorTokenPoints)
                + ScoreEmptyCells(board, cfg);
        }

        internal static int ScoreBandCells(Board board, ScoringConfig cfg)
        {
            int count = 0;
            ForEachCell(board, (r, c, cell) =>
            {
                if (cell is Cell.BandCell bandCell
                    && board.DieAt(r, c) is Die die
                    && Bands.Of(die.Face) == bandCell.Band)
                {
                    count++;
                }
            });
            return count * cfg.BandCellPoints;
        }

        internal static int ScorePrivateElement(Board board, Element privateElement, ScoringConfig cfg)
        {
            int count = 0;
            ForEachDie(board, die =>
            {
                if (die.Element == privateElement)
                {
                    count++;
                }
            });
            return count * cfg.PrivateElementPoints;
        }

        internal static int ScoreEmptyCells(Board board, ScoringConfig cfg)
        {
            int empty = 0;
            ForEachCell(board, (r, c, _) =>
            {
                if (board.DieAt(r, c) is null)
                {
                    empty++;
                }
            });
            return empty * cfg.EmptyCellPenalty;
        }

        private static int ScoreObjective(Board board, PublicObjective objective) => objective switch
        {
            PublicObjective.FirmamentRows => ScoreNoRepeatLines(board, rows: true) * 3,
            PublicObjective.DeepColumns => ScoreNoRepeatLines(board, rows: false) * 3,
            PublicObjective.Constellation => ScoreConstellation(board) * 5,
            PublicObjective.RisingTide => ScoreRisingTide(board) * 3,
            PublicObjective.ElementalBounty => ScoreElementalBounty(board) * 2,
            PublicObjective.ScarcitysReward => ScoreScarcitysReward(board),
            _ => throw new ArgumentOutOfRangeException(nameof(objective)),
        };

        internal static int ScoreNoRepeatLines(Board board, bool rows)
        {
            int outer = rows ? Board.Rows : Board.Columns;
            int inner = rows ? Board.Columns : Board.Rows;
            int qualifying = 0;

            for (int i = 0; i < outer; i++)
            {
                var seen = new HashSet<Element>();
                bool hasRepeat = false;
                for (int j = 0; j < inner; j++)
                {
                    int r = rows ? i : j;
                    int c = rows ? j : i;
                    if (board.DieAt(r, c) is Die die && !seen.Add(die.Element))
                    {
                        hasRepeat = true;
                        break;
                    }
                }

                if (!hasRepeat)
                {
                    qualifying++;
                }
            }

            return qualifying;
        }

        internal static int ScoreConstellation(Board board)
        {
            var counts = Elements.All.ToDictionary(e => e, _ => 0);
            ForEachDie(board, die => counts[die.Element]++);
            return counts.Values.Min();
        }

        internal static int ScoreRisingTide(Board board)
        {
            int count = 0;
            ForEachCell(board, (r, c, cell) =>
            {
                if (cell is Cell.BandCell bandCell
                    && bandCell.Band != Band.Low
                    && board.DieAt(r, c) is Die die
                    && Bands.Of(die.Face) == bandCell.Band)
                {
                    count++;
                }
            });
            return count;
        }

        internal static int ScoreElementalBounty(Board board)
        {
            var counts = Elements.All.ToDictionary(e => e, _ => 0);
            ForEachDie(board, die => counts[die.Element]++);
            return counts.Values.Count(c => c >= 2);
        }

        internal static int ScoreScarcitysReward(Board board)
        {
            int water = 0;
            int aether = 0;
            ForEachDie(board, die =>
            {
                if (die.Element == Element.Water)
                {
                    water++;
                }
                else if (die.Element == Element.Aether)
                {
                    aether++;
                }
            });
            return (water * 3) + (aether * 2);
        }

        private static void ForEachCell(Board board, Action<int, int, Cell> action)
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    action(r, c, board.CellAt(r, c));
                }
            }
        }

        private static void ForEachDie(Board board, Action<Die> action)
        {
            ForEachCell(board, (r, c, _) =>
            {
                if (board.DieAt(r, c) is Die die)
                {
                    action(die);
                }
            });
        }
    }
}
