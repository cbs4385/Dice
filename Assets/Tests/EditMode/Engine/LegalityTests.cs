using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class LegalityTests
    {
        [Test]
        public void FirstPlacement_AnyEmptyCellMatchingCellLock_IsLegal()
        {
            var board = BoardLayouts.Ashfall();
            var result = Legality.IsLegalPlacement(board, new Placement(0, 0, new Die(Element.Fire, 1)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void CellOccupied_IsIllegal()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            var result = Legality.IsLegalPlacement(board, new Placement(0, 0, new Die(Element.Fire, 2)));

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.CellOccupied));
        }

        [Test]
        public void ElementCell_WrongElement_IsIllegal()
        {
            var board = BoardLayouts.Ashfall();

            var result = Legality.IsLegalPlacement(board, new Placement(0, 0, new Die(Element.Water, 5)));

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.ElementCellMismatch));
        }

        [Test]
        public void ElementCell_MatchingElement_IsLegal()
        {
            var board = BoardLayouts.Ashfall();

            var result = Legality.IsLegalPlacement(board, new Placement(0, 0, new Die(Element.Fire, 3)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void BandCell_AcceptsAnyElement()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            // A2 is a Low band cell; adjacent to A1 so connection holds.
            var result = Legality.IsLegalPlacement(board, new Placement(0, 1, new Die(Element.Water, 2)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void WildCell_AcceptsAnyElement()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            // A3 is a Wild cell; not adjacent to A1, so use a die that is legally placeable
            // by chaining through A2 first is unnecessary - test connection separately below.
            var board2 = board.WithPlacement(new Placement(0, 1, new Die(Element.Water, 2)));
            var result = Legality.IsLegalPlacement(board2, new Placement(0, 2, new Die(Element.Aether, 4)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void NonFirstPlacement_NotAdjacentToAnyDie_IsIllegal()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            // C4 (row2, col3) is far from A1 (row0, col0).
            var result = Legality.IsLegalPlacement(board, new Placement(2, 3, new Die(Element.Water, 5)));

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.NotConnected));
        }

        [Test]
        public void NonFirstPlacement_OrthogonallyAdjacent_SatisfiesConnection()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            var result = Legality.IsLegalPlacement(board, new Placement(0, 1, new Die(Element.Water, 2)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void NonFirstPlacement_DiagonallyAdjacent_SatisfiesConnection()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            // B2 (row1, col1) is diagonal to A1 (row0, col0).
            var result = Legality.IsLegalPlacement(board, new Placement(1, 1, new Die(Element.Air, 3)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void OrthogonalSameElementNeighbor_IsIllegal()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 1, new Die(Element.Air, 3)));

            // B2 (row1, col1) is an Air element cell, orthogonally below A2 (row0, col1).
            var result = Legality.IsLegalPlacement(board, new Placement(1, 1, new Die(Element.Air, 5)));

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.ElementAdjacencyViolation));
        }

        [Test]
        public void DiagonalSameElementNeighbor_IsLegal()
        {
            // B2 (row1, col1) is the Air element cell.
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 1, new Die(Element.Air, 3)));

            // A3 (row0, col2) is diagonal to B2, not orthogonal, so the same-element
            // rule (which only forbids orthogonal neighbors) does not apply.
            var result = Legality.IsLegalPlacement(board, new Placement(0, 2, new Die(Element.Air, 5)));

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void Defy_BypassesElementAdjacencyOnly()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 1, new Die(Element.Air, 3)));

            var result = Legality.IsLegalPlacement(
                board, new Placement(1, 1, new Die(Element.Air, 5)), defyElementAdjacency: true);

            Assert.That(result.IsLegal, Is.True);
        }

        [Test]
        public void Defy_DoesNotBypassCellOccupied()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            var result = Legality.IsLegalPlacement(
                board, new Placement(0, 0, new Die(Element.Fire, 2)), defyElementAdjacency: true);

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.CellOccupied));
        }

        [Test]
        public void Defy_DoesNotBypassElementCellMismatch()
        {
            var board = BoardLayouts.Ashfall();

            var result = Legality.IsLegalPlacement(
                board, new Placement(0, 0, new Die(Element.Water, 5)), defyElementAdjacency: true);

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.ElementCellMismatch));
        }

        [Test]
        public void Defy_DoesNotBypassConnection()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            var result = Legality.IsLegalPlacement(
                board, new Placement(2, 3, new Die(Element.Water, 5)), defyElementAdjacency: true);

            Assert.That(result.IsLegal, Is.False);
            Assert.That(result.Reason, Is.EqualTo(IllegalPlacementReason.NotConnected));
        }
    }
}
