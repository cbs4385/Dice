using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class ScoringTests
    {
        private static readonly ScoringConfig Cfg = ScoringConfig.Default;

        [Test]
        public void ScoreBandCells_InBandDie_Scores()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 1, new Die(Element.Water, 3)));
            Assert.That(Scoring.ScoreBandCells(board, Cfg), Is.EqualTo(4));
        }

        [Test]
        public void ScoreBandCells_OffBandDie_ScoresZero()
        {
            // A4 is a High (9-12) band cell; Air's max face is 8, so it can never satisfy it.
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 3, new Die(Element.Air, 6)));
            Assert.That(Scoring.ScoreBandCells(board, Cfg), Is.EqualTo(0));
        }

        [Test]
        public void ScoreBandCells_MultipleSatisfied_SumsAll()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 1, new Die(Element.Water, 3)))   // Low, in-band
                .WithPlacement(new Placement(1, 2, new Die(Element.Water, 15))) // Celestial, in-band
                .WithPlacement(new Placement(2, 0, new Die(Element.Earth, 6))); // Mid, in-band

            Assert.That(Scoring.ScoreBandCells(board, Cfg), Is.EqualTo(12));
        }

        [Test]
        public void ScorePrivateElement_CountsOnlyMatchingElement()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)))
                .WithPlacement(new Placement(2, 3, new Die(Element.Water, 5)))
                .WithPlacement(new Placement(1, 2, new Die(Element.Water, 8)));

            Assert.That(Scoring.ScorePrivateElement(board, Element.Water, Cfg), Is.EqualTo(4));
            Assert.That(Scoring.ScorePrivateElement(board, Element.Fire, Cfg), Is.EqualTo(2));
            Assert.That(Scoring.ScorePrivateElement(board, Element.Earth, Cfg), Is.EqualTo(0));
        }

        [Test]
        public void ScoreEmptyCells_PenalizesEachEmptyCell()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));

            // 11 of 12 cells empty.
            Assert.That(Scoring.ScoreEmptyCells(board, Cfg), Is.EqualTo(11 * -2));
        }

        [Test]
        public void ScoreEmptyCells_FullBoard_IsZero()
        {
            var board = FullNeutralBoard();
            Assert.That(Scoring.ScoreEmptyCells(board, Cfg), Is.EqualTo(0));
        }

        [Test]
        public void DeepColumns_NoRepeats_ScoresPerColumn()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)))   // col 0
                .WithPlacement(new Placement(1, 0, new Die(Element.Earth, 2))) // col 0
                .WithPlacement(new Placement(0, 1, new Die(Element.Water, 3))) // col 1
                .WithPlacement(new Placement(1, 1, new Die(Element.Air, 4)));  // col 1

            // Columns 0 and 1 each have distinct elements; columns 2 and 3 are empty (vacuously no repeat).
            Assert.That(Scoring.ScoreNoRepeatLines(board, rows: false), Is.EqualTo(4));
        }

        [Test]
        public void DeepColumns_RepeatedElementInColumn_DoesNotQualify()
        {
            // WithPlacement does not enforce legality, so this same-element-adjacent
            // setup (which would need a Defy favor at the legality layer) is fine here.
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 1, new Die(Element.Air, 3)))
                .WithPlacement(new Placement(1, 1, new Die(Element.Air, 5)));

            Assert.That(Scoring.ScoreNoRepeatLines(board, rows: false), Is.EqualTo(3), "columns 0, 2, 3 qualify vacuously; column 1 does not");
        }

        [Test]
        public void FirmamentRows_MirrorsColumnsLogicButByRow()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)))
                .WithPlacement(new Placement(0, 1, new Die(Element.Fire, 2)));

            Assert.That(Scoring.ScoreNoRepeatLines(board, rows: true), Is.EqualTo(2), "row A has a repeat; rows B, C qualify vacuously");
        }

        [Test]
        public void Constellation_CountsCompleteSets()
        {
            var board = FullNeutralBoard(); // one of each element plus extras, see helper.
            int sets = Scoring.ScoreConstellation(board);
            Assert.That(sets, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Constellation_MissingOneElement_IsZero()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)))
                .WithPlacement(new Placement(1, 1, new Die(Element.Air, 2)));

            Assert.That(Scoring.ScoreConstellation(board), Is.EqualTo(0));
        }

        [Test]
        public void RisingTide_CountsOnlyMidHighCelestial_NotLow()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 1, new Die(Element.Water, 3)))   // Low, in-band - excluded
                .WithPlacement(new Placement(2, 0, new Die(Element.Earth, 6)))  // Mid, in-band - counted
                .WithPlacement(new Placement(1, 2, new Die(Element.Water, 15))); // Celestial, in-band - counted

            Assert.That(Scoring.ScoreRisingTide(board), Is.EqualTo(2));
        }

        [Test]
        public void ElementalBounty_CountsElementsWithTwoOrMore()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)))
                .WithPlacement(new Placement(0, 1, new Die(Element.Fire, 2)))
                .WithPlacement(new Placement(1, 1, new Die(Element.Air, 3)));

            Assert.That(Scoring.ScoreElementalBounty(board), Is.EqualTo(1), "only Fire reaches two");
        }

        [Test]
        public void ScarcitysReward_CountsWaterAndAetherSeparately()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(2, 3, new Die(Element.Water, 5)))
                .WithPlacement(new Placement(0, 2, new Die(Element.Aether, 4)));

            Assert.That(Scoring.ScoreScarcitysReward(board), Is.EqualTo((1 * 3) + (1 * 2)));
        }

        [Test]
        public void ScoreBoard_CombinesAllComponents()
        {
            var board = BoardLayouts.Ashfall()
                .WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)))    // element cell, private match
                .WithPlacement(new Placement(0, 1, new Die(Element.Water, 3))); // Low band, in-band

            int expected =
                Scoring.ScoreBandCells(board, Cfg)
                + (Scoring.ScoreNoRepeatLines(board, rows: false) * 3)
                + Scoring.ScorePrivateElement(board, Element.Fire, Cfg)
                + (2 * Cfg.FavorTokenPoints)
                + Scoring.ScoreEmptyCells(board, Cfg);

            int actual = Scoring.ScoreBoard(board, PublicObjective.DeepColumns, Element.Fire, unspentFavor: 2);

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static Board FullNeutralBoard()
        {
            // Fills every cell respecting cell-lock only (adjacency/connection are not
            // scoring concerns), using each element enough times to guarantee at least
            // one complete Constellation set.
            var elements = new[]
            {
                Element.Fire, Element.Earth, Element.Air, Element.Aether, Element.Water,
                Element.Fire, Element.Earth, Element.Air, Element.Aether, Element.Water,
                Element.Fire, Element.Earth,
            };

            var board = BoardLayouts.Ashfall();
            int index = 0;
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    var cell = board.CellAt(r, c);
                    Element element = cell is Cell.ElementCell elementCell ? elementCell.Element : elements[index];
                    index++;
                    board = board.WithPlacement(new Placement(r, c, new Die(element, 1)));
                }
            }

            return board;
        }
    }
}
