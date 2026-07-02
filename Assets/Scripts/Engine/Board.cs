using System;

namespace Quintessence.Engine
{
    public sealed class Board
    {
        public const int Rows = 3;
        public const int Columns = 4;

        private static readonly (int Dr, int Dc)[] OrthogonalOffsets =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1),
        };

        private static readonly (int Dr, int Dc)[] AllNeighborOffsets =
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1), (0, 1),
            (1, -1), (1, 0), (1, 1),
        };

        private readonly Cell[,] _cells;
        private readonly Die?[,] _dice;

        public Board(Cell[,] cells, Die?[,] dice)
        {
            if (cells.GetLength(0) != Rows || cells.GetLength(1) != Columns)
            {
                throw new ArgumentException($"cells must be {Rows}x{Columns}.", nameof(cells));
            }

            if (dice.GetLength(0) != Rows || dice.GetLength(1) != Columns)
            {
                throw new ArgumentException($"dice must be {Rows}x{Columns}.", nameof(dice));
            }

            _cells = cells;
            _dice = dice;
        }

        public static Board Empty(Cell[,] cells) => new(cells, new Die?[Rows, Columns]);

        public Cell CellAt(int row, int col) => _cells[row, col];

        public Die? DieAt(int row, int col) => _dice[row, col];

        public Board WithPlacement(Placement placement)
        {
            var newDice = (Die?[,])_dice.Clone();
            newDice[placement.Row, placement.Col] = placement.Die;
            return new Board(_cells, newDice);
        }

        public bool HasAnyDie()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (_dice[r, c] is not null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsAdjacentToAnyDie(int row, int col)
        {
            foreach (var (dr, dc) in AllNeighborOffsets)
            {
                int r = row + dr;
                int c = col + dc;
                if (InBounds(r, c) && _dice[r, c] is not null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasOrthogonalNeighborOfElement(int row, int col, Element element)
        {
            foreach (var (dr, dc) in OrthogonalOffsets)
            {
                int r = row + dr;
                int c = col + dc;
                if (InBounds(r, c) && _dice[r, c] is Die die && die.Element == element)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InBounds(int row, int col) => row >= 0 && row < Rows && col >= 0 && col < Columns;
    }
}
