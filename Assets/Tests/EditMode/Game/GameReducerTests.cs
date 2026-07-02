#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Engine;

namespace Quintessence.Game.Tests
{
    public class GameReducerTests
    {
        private static GameState ManualState(
            Board boardP0,
            Board boardP1,
            IReadOnlyList<Die> pool,
            int pickNumber = 1,
            int pickOrderIndex = 0,
            int[]? pickOrder = null,
            int favorP0 = 3,
            int favorP1 = 3)
        {
            var players = new List<PlayerState>
            {
                new(boardP0, favorP0, Element.Water),
                new(boardP1, favorP1, Element.Fire),
            };
            var phase = new RoundPhase(pickNumber, pickOrder ?? new[] { 0, 1 }, pickOrderIndex, pool);
            return new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false);
        }

        [Test]
        public void StartRound_DrawsCorrectPoolSizeAndSetsUpPickOrder()
        {
            var state = GameSetup.NewGame(2, Rng.Create(1));
            var result = GameReducer.StartRound(state, Rng.Create(2));

            Assert.That(result.CurrentPhase!.Pool, Has.Count.EqualTo(5)); // 2*2+1
            Assert.That(result.CurrentPhase.PickNumber, Is.EqualTo(1));
            Assert.That(result.CurrentPhase.PickOrderIndex, Is.EqualTo(0));
            Assert.That(result.CurrentPhase.PickOrder, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void StartRound_WhilePhaseInProgress_Throws()
        {
            var state = GameSetup.NewGame(2, Rng.Create(1));
            var started = GameReducer.StartRound(state, Rng.Create(2));

            Assert.Throws<InvalidOperationException>(() => GameReducer.StartRound(started, Rng.Create(3)));
        }

        [Test]
        public void ApplyDraft_FromPool_RemovesDieAndPlacesOnCurrentPlayersBoard()
        {
            var pool = new List<Die> { new(Element.Fire, 2) };
            var state = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool);

            var result = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 0), Rng.Create(1));

            Assert.That(result.CurrentPhase!.Pool, Is.Empty);
            Assert.That(result.Players[0].Board.DieAt(0, 0), Is.EqualTo(new Die(Element.Fire, 2)));
        }

        [Test]
        public void ApplyDraft_InvalidPoolIndex_Throws()
        {
            var pool = new List<Die> { new(Element.Fire, 1) };
            var state = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool);

            var badChoice = new DraftChoice(DieSource.Pool, 5, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => GameReducer.ApplyDraft(state, badChoice, Rng.Create(1)));
        }

        [Test]
        public void ApplyDraft_FirmamentIdNotFound_Throws()
        {
            var pool = new List<Die> { new(Element.Fire, 1) };
            var state = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool);

            var badChoice = new DraftChoice(DieSource.Firmament, 999, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => GameReducer.ApplyDraft(state, badChoice, Rng.Create(1)));
        }

        [Test]
        public void ApplyDraft_IllegalPlacement_ThrowsInvalidOperationException()
        {
            var prefilled = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 0, new Die(Element.Fire, 1)));
            var pool = new List<Die> { new(Element.Fire, 2) };
            var state = ManualState(prefilled, BoardLayouts.Tidewater(), pool);

            var choice = new DraftChoice(DieSource.Pool, 0, 0, 0); // already occupied

            Assert.Throws<InvalidOperationException>(() => GameReducer.ApplyDraft(state, choice, Rng.Create(1)));
        }

        [Test]
        public void ApplyDraft_DefyFavor_BypassesAdjacencyAndConsumesOneFavor()
        {
            // B2 (1,1) is Ashfall's Air element cell; A2 (0,1) already holds Air, orthogonally above it.
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 1, new Die(Element.Air, 3)));
            var pool = new List<Die> { new(Element.Air, 5) };
            var state = ManualState(board, BoardLayouts.Tidewater(), pool);

            var choice = new DraftChoice(DieSource.Pool, 0, 1, 1, new FavorAction.Defy());
            var result = GameReducer.ApplyDraft(state, choice, Rng.Create(1));

            Assert.That(result.Players[0].Board.DieAt(1, 1), Is.EqualTo(new Die(Element.Air, 5)));
            Assert.That(result.Players[0].FavorRemaining, Is.EqualTo(2));
        }

        [Test]
        public void ApplyDraft_SameAdjacencyWithoutDefy_Throws()
        {
            var board = BoardLayouts.Ashfall().WithPlacement(new Placement(0, 1, new Die(Element.Air, 3)));
            var pool = new List<Die> { new(Element.Air, 5) };
            var state = ManualState(board, BoardLayouts.Tidewater(), pool);

            var choice = new DraftChoice(DieSource.Pool, 0, 1, 1);
            Assert.Throws<InvalidOperationException>(() => GameReducer.ApplyDraft(state, choice, Rng.Create(1)));
        }

        [Test]
        public void ApplyDraft_AdjustFavor_ChangesFaceBeforePlacement()
        {
            var pool = new List<Die> { new(Element.Water, 14) };
            var state = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool);

            // B3 (1,2) is Ashfall's Celestial band cell.
            var choice = new DraftChoice(DieSource.Pool, 0, 1, 2, new FavorAction.Adjust(1));
            var result = GameReducer.ApplyDraft(state, choice, Rng.Create(1));

            Assert.That(result.Players[0].Board.DieAt(1, 2), Is.EqualTo(new Die(Element.Water, 15)));
            Assert.That(result.Players[0].FavorRemaining, Is.EqualTo(2));
        }

        [Test]
        public void ApplyDraft_RerollFavor_PlacesRerolledFaceDeterministically()
        {
            var poolA = new List<Die> { new(Element.Water, 5) };
            var poolB = new List<Die> { new(Element.Water, 5) };
            var stateA = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), poolA);
            var stateB = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), poolB);

            // C4 (2,3) is Ashfall's Water element cell.
            var choice = new DraftChoice(DieSource.Pool, 0, 2, 3, new FavorAction.Reroll());
            var resultA = GameReducer.ApplyDraft(stateA, choice, Rng.Create(42));
            var resultB = GameReducer.ApplyDraft(stateB, choice, Rng.Create(42));

            var dieA = resultA.Players[0].Board.DieAt(2, 3);
            Assert.That(dieA, Is.EqualTo(resultB.Players[0].Board.DieAt(2, 3)));
            Assert.That(dieA!.Element, Is.EqualTo(Element.Water));
            Assert.That(dieA.Face, Is.InRange(1, 20));
            Assert.That(resultA.Players[0].FavorRemaining, Is.EqualTo(2));
        }

        [Test]
        public void ApplyDraft_NoFavorRemaining_Throws()
        {
            var pool = new List<Die> { new(Element.Water, 14) };
            var state = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool, favorP0: 0);

            var choice = new DraftChoice(DieSource.Pool, 0, 1, 2, new FavorAction.Adjust(1));
            Assert.Throws<InvalidOperationException>(() => GameReducer.ApplyDraft(state, choice, Rng.Create(1)));
        }

        [Test]
        public void ApplyForfeit_AdvancesTurnWithoutPlacingOrConsumingPool()
        {
            var pool = new List<Die> { new(Element.Fire, 1) };
            var state = ManualState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool);

            var result = GameReducer.ApplyForfeit(state);

            Assert.That(result.CurrentPhase!.PickOrderIndex, Is.EqualTo(1));
            Assert.That(result.CurrentPhase.Pool, Has.Count.EqualTo(1));
            Assert.That(result.Players[0].Board.HasAnyDie(), Is.False);
        }

        [Test]
        public void FullRound_CompletesBothPicksAndMovesLeftoverToFirmament_AdvancingRoundAndStartPlayer()
        {
            var pool = new List<Die>
            {
                new(Element.Fire, 1),
                new(Element.Earth, 2),
                new(Element.Air, 3),
                new(Element.Water, 4),
                new(Element.Aether, 5),
            };
            var players = new List<PlayerState>
            {
                new(BoardLayouts.Ashfall(), 3, Element.Water),
                new(BoardLayouts.Tidewater(), 3, Element.Fire),
            };
            var phase = new RoundPhase(1, new[] { 0, 1 }, 0, pool);
            var state = new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false);
            var rng = Rng.Create(1);

            // Pick 1: player 0, then player 1.
            state = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 0), rng); // Fire -> Ashfall(0,0) elem
            state = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 1, 1), rng); // Earth -> Tidewater(1,1) elem

            Assert.That(state.CurrentPhase!.PickNumber, Is.EqualTo(2));
            Assert.That(state.CurrentPhase.PickOrder, Is.EqualTo(new[] { 1, 0 }));

            // Pick 2 (reversed): player 1, then player 0.
            state = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 2, 2), rng); // Air -> Tidewater(2,2) elem
            state = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 1), rng); // Water -> Ashfall(0,1) Low band

            Assert.That(state.CurrentPhase, Is.Null, "round complete");
            Assert.That(state.Round, Is.EqualTo(2));
            Assert.That(state.StartPlayerIndex, Is.EqualTo(1));
            Assert.That(state.Firmament, Has.Count.EqualTo(1));
            Assert.That(state.Firmament[0].Die, Is.EqualTo(new Die(Element.Aether, 5)));
        }
    }
}
