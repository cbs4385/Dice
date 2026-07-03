using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game.Tests.Clash
{
    // Canonical Clash acceptance test (docs/clash.md SS4): Player A charges to
    // interventionCost Storm by attuning cells, Scorches Player B's d20 sitting on
    // a Celestial cell from 15 down to 12 (out of the Celestial band), B receives
    // Backlash favor, declines to Ward, and loses that cell's 4 band points.
    // Encoded the same way as the core engine's 28-point test: real ApplyDraft
    // calls through the actual reducer, not just hand-set state.
    public class ClashWorkedExampleTests
    {
        [Test]
        public void PlayerA_ScorchesPlayerBsCelestialDie_ExactStormFavorAndScoreDeltas()
        {
            var config = ClashConfig.Default; // interventionCost=4, stormPerAttune=1, backlashFavor=1, scorchMaxPips=3.
            var players = new List<PlayerState>
            {
                new(BoardLayouts.Ashfall(), 3, Element.Aether),
                new(BoardLayouts.Ashfall(), 3, Element.Fire),
            };
            var clash = new ClashState(
                config,
                Storm: new List<int> { 0, 0 },
                InterventionsAvailable: new List<InterventionKind> { InterventionKind.Scorch },
                PetrifyTokens: new List<PetrifyToken>(),
                Pending: null,
                InterventionLog: new List<ClashLogEntry>(),
                NullifiedBandCells: new List<NullifiedCell>());
            var state = new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.ElementalBounty, null, 0, false, clash);

            // Ashfall's four band cells (Low(0,1)/High(0,3)/Celestial(1,2)/Mid(2,0))
            // are not all mutually adjacent, so this walks a connected path: start
            // at Celestial, reach Low and High diagonally from it, then bridge
            // through the Air element cell (1,1) - not a band cell, no storm charge
            // - to reach Mid, which is otherwise unconnected to the other three.
            state = AttuneOneCell(state, row: 1, col: 2, new Die(Element.Water, 18)); // Celestial, in-band.
            Assert.That(state.Clash!.Storm[0], Is.EqualTo(1));

            state = AttuneOneCell(state, row: 0, col: 1, new Die(Element.Fire, 2));   // Low, in-band.
            Assert.That(state.Clash!.Storm[0], Is.EqualTo(2));

            state = AttuneOneCell(state, row: 0, col: 3, new Die(Element.Aether, 10)); // High, in-band.
            Assert.That(state.Clash!.Storm[0], Is.EqualTo(3));

            state = AttuneOneCell(state, row: 1, col: 1, new Die(Element.Air, 5));    // Air element cell, bridge only.
            Assert.That(state.Clash!.Storm[0], Is.EqualTo(3), "not a band cell - no charge");

            state = AttuneOneCell(state, row: 2, col: 0, new Die(Element.Earth, 6));  // Mid, in-band.
            Assert.That(state.Clash!.Storm[0], Is.EqualTo(4), "exactly interventionCost after 4 attunes");

            // Player B independently places their own Celestial die: a d20 showing 15.
            state = AttuneOneCell(state, row: 1, col: 2, new Die(Element.Water, 15), player: 1);

            int scoreBeforeScorch = Scoring.ScoreBoard(state.Players[1].Board, PublicObjective.ElementalBounty, Element.Fire, unspentFavor: 4);

            var declared = ClashReducer.DeclareIntervention(state, actor: 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(TargetPlayer: 1, Row: 1, Col: 2, Pips: 3), Rng.Create(1));

            Assert.That(declared.Clash!.Storm[0], Is.EqualTo(0), "interventionCost (4) fully spent");
            Assert.That(declared.Players[1].FavorRemaining, Is.EqualTo(4), "3 starting + 1 backlash");

            var resolved = ClashReducer.DeclineWard(declared, target: 1);

            Assert.That(resolved.Players[1].Board.DieAt(1, 2), Is.EqualTo(new Die(Element.Water, 12)), "15 - 3 pips = 12");
            Assert.That(Bands.Of(12), Is.Not.EqualTo(Band.Celestial), "12 has fallen out of the Celestial band (13-20)");
            Assert.That(resolved.Clash!.Pending, Is.Null);

            int scoreAfterScorch = Scoring.ScoreBoard(resolved.Players[1].Board, PublicObjective.ElementalBounty, Element.Fire, unspentFavor: 4);

            Assert.That(scoreBeforeScorch, Is.EqualTo(-14), "band(4) + objective(0) + private(0) + favor(4) + empty(11*-2)");
            Assert.That(scoreAfterScorch, Is.EqualTo(-18), "band(0) + objective(0) + private(0) + favor(4) + empty(11*-2)");
            Assert.That(scoreAfterScorch - scoreBeforeScorch, Is.EqualTo(-ScoringConfig.Default.BandCellPoints), "loses exactly that cell's 4 points");
        }

        // Attunes exactly one cell for the given player via a real, isolated
        // ApplyDraft call (pickOrder of just that player, so round/phase transition
        // mechanics never engage) - deliberately not a full 6-round replay, since
        // this test is about intervention mechanics, not the drafting loop (already
        // covered by M2's GameReducerTests/SelfPlayPropertyTests).
        private static GameState AttuneOneCell(GameState state, int row, int col, Die die, int player = 0)
        {
            var pickOrder = new[] { player };
            var phase = new RoundPhase(1, pickOrder, 0, new List<Die> { die });
            var isolated = state with { CurrentPhase = phase };

            return GameReducer.ApplyDraft(isolated, new DraftChoice(DieSource.Pool, 0, row, col), Rng.Create(1));
        }
    }
}
