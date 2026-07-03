using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game.Tests.Clash
{
    public class ClashReducerTests
    {
        private static GameState ManualClashState(
            Board boardP0,
            Board boardP1,
            IReadOnlyList<Die> pool,
            ClashState clash)
        {
            var players = new List<PlayerState>
            {
                new(boardP0, 3, Element.Water),
                new(boardP1, 3, Element.Fire),
            };
            var phase = new RoundPhase(1, new[] { 0, 1 }, 0, pool);
            return new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false, clash);
        }

        private static ClashState FreshClash(int playerCount = 2) =>
            ClashSetup.Deal(playerCount, ClashConfig.Default, Rng.Create(1));

        [Test]
        public void ApplyDraft_AttuningABandCell_ChargesStormForActingPlayerOnly()
        {
            var pool = new List<Die> { new(Element.Water, 3) }; // Ashfall (0,1) is Low band; 3 is in-band.
            var state = ManualClashState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool, FreshClash());

            var result = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 1), Rng.Create(1));

            Assert.That(result.Clash!.Storm[0], Is.EqualTo(ClashConfig.Default.StormPerAttune));
            Assert.That(result.Clash.Storm[1], Is.EqualTo(0));
        }

        [Test]
        public void ApplyDraft_OffBandPlacement_DoesNotChargeStorm()
        {
            // Ashfall (0,3) is a High band cell (9-12); an Air die (max face 8) can never be in-band there.
            var pool = new List<Die> { new(Element.Air, 6) };
            var state = ManualClashState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool, FreshClash());

            var result = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 3), Rng.Create(1));

            Assert.That(result.Clash!.Storm[0], Is.EqualTo(0));
        }

        [Test]
        public void ApplyDraft_NonBandCellPlacement_DoesNotChargeStorm()
        {
            // Ashfall (0,0) is Fire's element cell, not a band cell.
            var pool = new List<Die> { new(Element.Fire, 2) };
            var state = ManualClashState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), pool, FreshClash());

            var result = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 0), Rng.Create(1));

            Assert.That(result.Clash!.Storm[0], Is.EqualTo(0));
        }

        [Test]
        public void ApplyDraft_NonClashGame_LeavesClashNull()
        {
            var pool = new List<Die> { new(Element.Water, 3) };
            var players = new List<PlayerState>
            {
                new(BoardLayouts.Ashfall(), 3, Element.Water),
                new(BoardLayouts.Tidewater(), 3, Element.Fire),
            };
            var phase = new RoundPhase(1, new[] { 0, 1 }, 0, pool);
            var state = new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false);

            var result = GameReducer.ApplyDraft(state, new DraftChoice(DieSource.Pool, 0, 0, 1), Rng.Create(1));

            Assert.That(result.Clash, Is.Null);
        }

        [Test]
        public void ChargeStormOnAttune_CapsAtStormCap()
        {
            var config = ClashConfig.Default with { StormPerAttune = 3, StormCap = 5 };
            var clash = ClashSetup.Deal(2, config, Rng.Create(1));

            clash = ClashReducer.ChargeStormOnAttune(clash, player: 0);
            Assert.That(clash.Storm[0], Is.EqualTo(3));

            clash = ClashReducer.ChargeStormOnAttune(clash, player: 0);
            Assert.That(clash.Storm[0], Is.EqualTo(5), "3+3=6 should clamp to the cap of 5");
        }
    }
}
