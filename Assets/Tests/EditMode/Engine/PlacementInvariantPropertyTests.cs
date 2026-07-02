using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    // Hand-rolled property-style tests (loops over many seeds) in place of a
    // property-testing library such as FsCheck - adding one is a new dependency
    // and needs approval first (see docs/agent-build-plan.md v0.2 §3, §8).
    public class PlacementInvariantPropertyTests
    {
        private const int SeedCount = 200;

        [Test]
        public void RandomLegalPlacementSequences_NeverViolateAdjacencyOrOverfill()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var board = RunRandomLegalPlacements(seed);
                AssertNoOrthogonalSameElementPairs(board, seed);
                Assert.That(CountDice(board), Is.LessThanOrEqualTo(Board.Rows * Board.Columns), $"seed {seed}");
            }
        }

        [Test]
        public void RandomLegalPlacementSequences_AreDeterministicGivenSameSeed()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var boardA = RunRandomLegalPlacements(seed);
                var boardB = RunRandomLegalPlacements(seed);

                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        Assert.That(boardB.DieAt(r, c), Is.EqualTo(boardA.DieAt(r, c)), $"seed {seed} cell ({r},{c})");
                    }
                }
            }
        }

        private static Board RunRandomLegalPlacements(int seed)
        {
            var rng = Rng.Create(seed);
            var board = BoardLayouts.Ashfall();

            for (int attempt = 0; attempt < 200 && CountDice(board) < Board.Rows * Board.Columns; attempt++)
            {
                int row = rng.NextInt(Board.Rows);
                int col = rng.NextInt(Board.Columns);
                var element = Elements.All[rng.NextInt(Elements.All.Count)];
                int face = rng.NextInt(Sides.Of(element)) + 1;

                var placement = new Placement(row, col, new Die(element, face));
                if (Legality.IsLegalPlacement(board, placement).IsLegal)
                {
                    board = board.WithPlacement(placement);
                }
            }

            return board;
        }

        private static int CountDice(Board board)
        {
            int count = 0;
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    if (board.DieAt(r, c) is not null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void AssertNoOrthogonalSameElementPairs(Board board, int seed)
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    if (board.DieAt(r, c) is not Die die)
                    {
                        continue;
                    }

                    if (c + 1 < Board.Columns && board.DieAt(r, c + 1) is Die right && right.Element == die.Element)
                    {
                        Assert.Fail($"seed {seed}: orthogonal same-element pair at ({r},{c})-({r},{c + 1})");
                    }

                    if (r + 1 < Board.Rows && board.DieAt(r + 1, c) is Die below && below.Element == die.Element)
                    {
                        Assert.Fail($"seed {seed}: orthogonal same-element pair at ({r},{c})-({r + 1},{c})");
                    }
                }
            }
        }
    }
}
