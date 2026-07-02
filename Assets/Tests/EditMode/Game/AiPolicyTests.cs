using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Engine;

namespace Quintessence.Game.Tests
{
    public class AiPolicyTests
    {
        private static readonly IAiPolicy[] AllTiers = { new NoviceAi(), new AdeptAi(), new OracleAi() };

        [TestCaseSource(nameof(AllTiers))]
        public void ChooseMove_NoLegalPlacements_ReturnsNull(IAiPolicy policy)
        {
            var fullBoard = FullBoard(BoardLayouts.Ashfall());
            var players = new List<PlayerState> { new(fullBoard, 3, Element.Water), new(BoardLayouts.Tidewater(), 3, Element.Fire) };
            var phase = new RoundPhase(1, new[] { 0, 1 }, 0, new List<Die> { new(Element.Fire, 1) });
            var state = new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false);

            var move = policy.ChooseMove(state, Rng.Create(1));

            Assert.That(move, Is.Null);
        }

        [TestCaseSource(nameof(AllTiers))]
        public void ChooseMove_WhenLegalPlacementsExist_ReturnsOneOfThem(IAiPolicy policy)
        {
            var players = new List<PlayerState> { new(BoardLayouts.Ashfall(), 3, Element.Water), new(BoardLayouts.Tidewater(), 3, Element.Fire) };
            var pool = new List<Die> { new(Element.Fire, 2), new(Element.Water, 5) };
            var phase = new RoundPhase(1, new[] { 0, 1 }, 0, pool);
            var state = new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false);

            var move = policy.ChooseMove(state, Rng.Create(1));

            Assert.That(move, Is.Not.Null);
            var legal = Legality.IsLegalPlacement(players[0].Board, new Placement(move!.Row, move.Col, pool[move.Index]));
            Assert.That(legal.IsLegal, Is.True);
        }

        [TestCaseSource(nameof(AllTiers))]
        public void ChooseMove_SameSeed_IsDeterministic(IAiPolicy policy)
        {
            var players = new List<PlayerState> { new(BoardLayouts.Ashfall(), 3, Element.Water), new(BoardLayouts.Tidewater(), 3, Element.Fire) };
            var pool = new List<Die> { new(Element.Fire, 2), new(Element.Water, 5), new(Element.Air, 3) };
            var phase = new RoundPhase(1, new[] { 0, 1 }, 0, pool);
            var state = new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false);

            var moveA = policy.ChooseMove(state, Rng.Create(77));
            var moveB = policy.ChooseMove(state, Rng.Create(77));

            Assert.That(moveB, Is.EqualTo(moveA));
        }

        private static Board FullBoard(Board board)
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    // Only used to make every cell non-empty so EnumerateSimple finds
                    // zero candidates; this board is never checked for legality.
                    var element = board.CellAt(r, c) is Cell.ElementCell elementCell ? elementCell.Element : Element.Fire;
                    board = board.WithPlacement(new Placement(r, c, new Die(element, 1)));
                }
            }

            return board;
        }
    }
}
