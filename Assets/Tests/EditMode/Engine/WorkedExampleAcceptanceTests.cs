using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    // Encodes the rulebook's worked example (docs/rulebook.md, "Worked example"):
    // Board α (Ashfall), objective Deep Columns, total score 28.
    //
    // The rulebook describes the *outcome* (which bands are satisfied, which column
    // repeats, private-element count, favor spend) rather than a cell-by-cell diagram.
    // This test reconstructs one concrete, fully-legal board consistent with every
    // stated fact, places it through the real legality pipeline (so the reconstruction
    // is provably reachable, not just asserted), and checks the final score.
    //
    // Reconciling the facts:
    //   - Low, Mid, Celestial bands satisfied; High cell holds an off-band d8 (Air,
    //     whose 1-8 range can never reach High's 9-12, so any Air placed there is
    //     necessarily "off-band" - matching the example's wording exactly).
    //   - Column 2 "repeats Air": the Air element cell (B2) and the Low band cell
    //     directly above it (A2) both hold Air dice. Placing the second one requires
    //     a Defy favor, since same-element orthogonal adjacency is normally illegal.
    //   - Private element Water x3, one Adjust favor (a d20 nudged to 15 for the
    //     Celestial cell) - together with the Defy, that is the example's "spent 2".
    //   - No empty cells: the board is filled via a fully connected placement order.
    public class WorkedExampleAcceptanceTests
    {
        [Test]
        public void AshfallDeepColumns_WorkedExample_ScoresExactly28()
        {
            var board = BoardLayouts.Ashfall();

            board = PlaceLegal(board, 0, 0, new Die(Element.Fire, 2));     // A1 Fire (element cell)
            board = PlaceLegal(board, 0, 1, new Die(Element.Air, 3));     // A2 Low band, in-band
            board = PlaceLegal(board, 0, 2, new Die(Element.Aether, 4));  // A3 Wild
            board = PlaceLegal(board, 0, 3, new Die(Element.Air, 6));     // A4 High band, off-band
            board = PlaceLegal(board, 1, 0, new Die(Element.Earth, 4));   // B1 Wild
            board = PlaceLegal(board, 1, 1, new Die(Element.Air, 5), defy: true); // B2 Air (element cell)
            board = PlaceLegal(board, 1, 2, new Die(Element.Water, 15)); // B3 Celestial band, in-band
            board = PlaceLegal(board, 1, 3, new Die(Element.Fire, 3));    // B4 Wild
            board = PlaceLegal(board, 2, 0, new Die(Element.Aether, 7));  // C1 Mid band, in-band
            board = PlaceLegal(board, 2, 1, new Die(Element.Water, 9));   // C2 Wild
            board = PlaceLegal(board, 2, 2, new Die(Element.Earth, 3));   // C3 Earth (element cell)
            board = PlaceLegal(board, 2, 3, new Die(Element.Water, 18));  // C4 Water (element cell)

            Assert.That(Scoring.ScoreBandCells(board, ScoringConfig.Default), Is.EqualTo(12), "3 of 4 band cells satisfied x4");
            Assert.That(Scoring.ScoreNoRepeatLines(board, rows: false), Is.EqualTo(3), "columns 1, 3, 4 qualify; column 2 repeats Air");
            Assert.That(Scoring.ScorePrivateElement(board, Element.Water, ScoringConfig.Default), Is.EqualTo(6), "3 Water dice");
            Assert.That(Scoring.ScoreEmptyCells(board, ScoringConfig.Default), Is.EqualTo(0), "board is full");

            int total = Scoring.ScoreBoard(board, PublicObjective.DeepColumns, Element.Water, unspentFavor: 1);

            Assert.That(total, Is.EqualTo(28));
        }

        private static Board PlaceLegal(Board board, int row, int col, Die die, bool defy = false)
        {
            var placement = new Placement(row, col, die);
            var result = Legality.IsLegalPlacement(board, placement, defyElementAdjacency: defy);
            Assert.That(result.IsLegal, Is.True, $"expected placement at ({row},{col}) to be legal, was illegal: {result.Reason}");
            return board.WithPlacement(placement);
        }
    }
}
