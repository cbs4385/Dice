using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class BoardTests
    {
        [Test]
        public void Empty_AllCellsHaveNoDie()
        {
            var board = BoardLayouts.Ashfall();

            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    Assert.That(board.DieAt(r, c), Is.Null);
                }
            }
        }

        [Test]
        public void WithPlacement_ReturnsNewBoard_OriginalUnchanged()
        {
            var original = BoardLayouts.Ashfall();
            var die = new Die(Element.Fire, 2);

            var updated = original.WithPlacement(new Placement(0, 0, die));

            Assert.That(original.DieAt(0, 0), Is.Null);
            Assert.That(updated.DieAt(0, 0), Is.EqualTo(die));
        }

        [Test]
        public void HasAnyDie_FalseForEmptyBoard_TrueAfterPlacement()
        {
            var board = BoardLayouts.Ashfall();
            Assert.That(board.HasAnyDie(), Is.False);

            var updated = board.WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));
            Assert.That(updated.HasAnyDie(), Is.True);
        }

        [Test]
        public void IsAdjacentToAnyDie_ChecksBothOrthogonalAndDiagonalNeighbors()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 1, new Die(Element.Air, 1)));

            Assert.That(board.IsAdjacentToAnyDie(0, 0), Is.True, "diagonal neighbor");
            Assert.That(board.IsAdjacentToAnyDie(0, 1), Is.True, "orthogonal neighbor");
            Assert.That(board.IsAdjacentToAnyDie(2, 3), Is.False, "far corner");
        }

        [Test]
        public void HasOrthogonalNeighborOfElement_IgnoresDiagonalNeighbors()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            Assert.That(board.HasOrthogonalNeighborOfElement(0, 1, Element.Fire), Is.True, "orthogonal");
            Assert.That(board.HasOrthogonalNeighborOfElement(1, 1, Element.Fire), Is.False, "diagonal only");
        }
    }
}
