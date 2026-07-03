#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game.Tests.Clash
{
    public class ClashInterventionsTests
    {
        private static GameState TwoPlayerState(
            Board boardP0,
            Board boardP1,
            ClashConfig config,
            IReadOnlyList<Die>? pool = null,
            IReadOnlyList<FirmamentDie>? firmament = null,
            int[]? pickOrder = null,
            int stormP0 = 10,
            int stormP1 = 10,
            int favorP0 = 3,
            int favorP1 = 3,
            int round = 1)
        {
            var players = new List<PlayerState>
            {
                new(boardP0, favorP0, Element.Water),
                new(boardP1, favorP1, Element.Fire),
            };
            RoundPhase? phase = pool is null ? null : new RoundPhase(1, pickOrder ?? new[] { 0, 1 }, 0, pool);
            var clash = new ClashState(
                Config: config,
                Storm: new List<int> { stormP0, stormP1 },
                InterventionsAvailable: new List<InterventionKind> { InterventionKind.Scorch, InterventionKind.Riptide, InterventionKind.Gust, InterventionKind.Petrify, InterventionKind.Eclipse },
                PetrifyTokens: new List<PetrifyToken>(),
                Pending: null,
                InterventionLog: new List<ClashLogEntry>(),
                NullifiedBandCells: new List<NullifiedCell>());

            return new GameState(round, 0, players, Bag.Default, firmament ?? new List<FirmamentDie>(), PublicObjective.DeepColumns, phase, 0, false, clash);
        }

        // ---- Scorch ----

        [Test]
        public void Scorch_DeclineWard_ReducesFaceAndAppliesBacklash()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var declared = ClashReducer.DeclareIntervention(state, actor: 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(TargetPlayer: 1, Row: 1, Col: 2, Pips: 3), Rng.Create(1));

            Assert.That(declared.Clash!.Storm[0], Is.EqualTo(10 - ClashConfig.Default.InterventionCost));
            Assert.That(declared.Players[1].FavorRemaining, Is.EqualTo(3 + ClashConfig.Default.BacklashFavor));
            Assert.That(declared.Clash.Pending, Is.Not.Null);

            var resolved = ClashReducer.DeclineWard(declared, target: 1);

            Assert.That(resolved.Players[1].Board.DieAt(1, 2)!.Face, Is.EqualTo(12));
            Assert.That(resolved.Clash!.Pending, Is.Null);
        }

        [Test]
        public void Scorch_NeverReducesBelowFaceOne()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 2)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 3), Rng.Create(1));
            var resolved = ClashReducer.DeclineWard(declared, 1);

            Assert.That(resolved.Players[1].Board.DieAt(1, 2)!.Face, Is.EqualTo(1));
        }

        [Test]
        public void Scorch_OnEmptyCell_IsIllegalToDeclare()
        {
            var state = TwoPlayerState(BoardLayouts.Tidewater(), BoardLayouts.Ashfall(), ClashConfig.Default);

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                    new InterventionParams.Scorch(1, 1, 2, Pips: 2), Rng.Create(1)));
        }

        [Test]
        public void Scorch_PipsOutsideConfiguredRange_IsIllegalToDeclare()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                    new InterventionParams.Scorch(1, 1, 2, Pips: ClashConfig.Default.ScorchMaxPips + 1), Rng.Create(1)));
        }

        // ---- Ward ----

        [Test]
        public void Ward_NegatesEffect_AndNetsZeroFavor()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 3), Rng.Create(1));
            int favorAfterBacklash = declared.Players[1].FavorRemaining;

            var warded = ClashReducer.Ward(declared, target: 1);

            Assert.That(warded.Players[1].Board.DieAt(1, 2)!.Face, Is.EqualTo(15), "effect must be negated");
            Assert.That(warded.Players[1].FavorRemaining, Is.EqualTo(favorAfterBacklash - ClashConfig.Default.WardCost));
            Assert.That(warded.Players[1].FavorRemaining, Is.EqualTo(3), "backlash then ward at equal defaults nets zero");
            Assert.That(warded.Clash!.Pending, Is.Null);
        }

        [Test]
        public void Ward_WithoutEnoughFavor_Throws()
        {
            // Backlash grants 1 favor by default, so use an inflated wardCost to
            // force insufficiency regardless of the backlash bump.
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var config = ClashConfig.Default with { WardCost = 99 };
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, config, favorP1: 0);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 3), Rng.Create(1));

            Assert.Throws<InvalidOperationException>(() => ClashReducer.Ward(declared, 1));
        }

        // ---- Riptide (shared-Firmament reinterpretation) ----

        [Test]
        public void Riptide_DeclineWard_ClaimsFirmamentDieOntoActorsBoard()
        {
            var firmament = new List<FirmamentDie> { new(5, new Die(Element.Water, 10)) };
            var state = TwoPlayerState(
                BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default,
                pool: new List<Die> { new(Element.Fire, 1) }, firmament: firmament, pickOrder: new[] { 1, 0 });

            var declared = ClashReducer.DeclareIntervention(state, actor: 0, InterventionKind.Riptide,
                new InterventionParams.Riptide(FirmamentId: 5, Row: 2, Col: 3), Rng.Create(1));

            Assert.That(declared.Clash!.Pending!.Target, Is.EqualTo(1), "targets whoever currently holds priority");

            var resolved = ClashReducer.DeclineWard(declared, target: 1);

            Assert.That(resolved.Players[0].Board.DieAt(2, 3), Is.EqualTo(new Die(Element.Water, 10)));
            Assert.That(resolved.Firmament, Is.Empty);
        }

        [Test]
        public void Riptide_OnEmptyFirmament_IsIllegalToDeclare()
        {
            var state = TwoPlayerState(
                BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default,
                pool: new List<Die> { new(Element.Fire, 1) }, pickOrder: new[] { 1, 0 });

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Riptide,
                    new InterventionParams.Riptide(FirmamentId: 999, Row: 2, Col: 3), Rng.Create(1)));
        }

        // ---- Gust ----

        [Test]
        public void Gust_DeclineWard_DraftsPoolDieImmediately_PickOrderIndexUnchanged()
        {
            var pool = new List<Die> { new(Element.Fire, 2) };
            var state = TwoPlayerState(
                BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default,
                pool: pool, pickOrder: new[] { 1, 0 });

            var declared = ClashReducer.DeclareIntervention(state, actor: 0, InterventionKind.Gust,
                new InterventionParams.Gust(PoolIndex: 0, Row: 0, Col: 0), Rng.Create(1));
            var resolved = ClashReducer.DeclineWard(declared, target: 1);

            Assert.That(resolved.Players[0].Board.DieAt(0, 0), Is.EqualTo(new Die(Element.Fire, 2)));
            Assert.That(resolved.CurrentPhase!.Pool, Is.Empty);
            Assert.That(resolved.CurrentPhase.PickOrderIndex, Is.EqualTo(0), "total picks in the round are unchanged");
        }

        // ---- Petrify ----

        [Test]
        public void Petrify_DeclineWard_BlocksPlacementUntilItExpires_ThenAllows()
        {
            var pool = new List<Die> { new(Element.Fire, 1) };
            var state = TwoPlayerState(
                BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default,
                pool: pool, pickOrder: new[] { 1, 0 }, round: 1);

            var declared = ClashReducer.DeclareIntervention(state, actor: 0, InterventionKind.Petrify,
                new InterventionParams.Petrify(TargetPlayer: 1, Row: 0, Col: 1), Rng.Create(1));
            var resolved = ClashReducer.DeclineWard(declared, target: 1);

            Assert.That(ClashReducer.IsCellPetrified(resolved.Clash, 1, 0, 1, currentRound: 1), Is.True);
            Assert.That(ClashReducer.IsCellPetrified(resolved.Clash, 1, 0, 1, currentRound: 2), Is.False, "petrifyDurationRounds default is 1");

            var blockedAttempt = resolved with { CurrentPhase = new RoundPhase(1, new[] { 1, 0 }, 0, new List<Die> { new(Element.Fire, 1) }) };
            Assert.Throws<InvalidOperationException>(() =>
                GameReducer.ApplyDraft(blockedAttempt, new DraftChoice(DieSource.Pool, 0, 0, 1), Rng.Create(1)));

            // Once the token has expired (round 2), the same placement succeeds.
            var allowedAttempt = resolved with
            {
                Round = 2,
                CurrentPhase = new RoundPhase(1, new[] { 1, 0 }, 0, new List<Die> { new(Element.Fire, 1) }),
            };
            var afterExpiry = GameReducer.ApplyDraft(allowedAttempt, new DraftChoice(DieSource.Pool, 0, 0, 1), Rng.Create(1));
            Assert.That(afterExpiry.Players[1].Board.DieAt(0, 1), Is.EqualTo(new Die(Element.Fire, 1)));
        }

        [Test]
        public void LegalDrafts_ExcludesPetrifiedCellsFromCandidates()
        {
            var pool = new List<Die> { new(Element.Water, 3) }; // Would be legal on Tidewater's Wild cell (0,1) if not petrified.
            var state = TwoPlayerState(
                BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default,
                pool: pool, pickOrder: new[] { 1, 0 });
            var clashWithToken = state.Clash! with { PetrifyTokens = new List<PetrifyToken> { new(1, 0, 1, ExpiresRound: 5) } };
            state = state with { Clash = clashWithToken };

            var candidates = LegalDrafts.EnumerateSimple(state);

            Assert.That(candidates, Has.None.Matches<DraftChoice>(c => c.Row == 0 && c.Col == 1));
        }

        [Test]
        public void LegalDrafts_ReturnsNothing_WhileInterventionPending()
        {
            var pool = new List<Die> { new(Element.Fire, 1) };
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default, pool: pool);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1));

            Assert.That(LegalDrafts.EnumerateSimple(declared), Is.Empty);
        }

        [Test]
        public void Petrify_OnOccupiedCell_IsIllegalToDeclare()
        {
            var occupiedBoard = BoardLayouts.Tidewater().WithPlacement(new Placement(0, 1, new Die(Element.Earth, 1)));
            var state = TwoPlayerState(BoardLayouts.Ashfall(), occupiedBoard, ClashConfig.Default);

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Petrify,
                    new InterventionParams.Petrify(1, 0, 1), Rng.Create(1)));
        }

        [Test]
        public void Shatter_ClearsTokenEarly_ForOneFavor()
        {
            var config = ClashConfig.Default;
            var state = TwoPlayerState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), config, favorP1: 3);
            var clashWithToken = state.Clash! with
            {
                PetrifyTokens = new List<PetrifyToken> { new(1, 0, 1, ExpiresRound: 5) },
            };
            state = state with { Clash = clashWithToken };

            var result = ClashReducer.Shatter(state, owner: 1, row: 0, col: 1);

            Assert.That(result.Players[1].FavorRemaining, Is.EqualTo(2));
            Assert.That(result.Clash!.PetrifyTokens, Is.Empty);
        }

        // ---- Eclipse: nullify ----

        [Test]
        public void EclipseNullify_DeclineWard_ZeroesTheCellsScoreContribution()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Eclipse,
                new InterventionParams.EclipseNullifyBand(TargetPlayer: 1, Row: 1, Col: 2), Rng.Create(1));
            var resolved = ClashReducer.DeclineWard(declared, target: 1);

            int plainScore = Scoring.ScoreBoard(resolved.Players[1].Board, PublicObjective.DeepColumns, Element.Fire, 3);
            int nullifiedScore = ClashScoring.ScoreBoardWithNullifications(
                resolved.Players[1].Board, PublicObjective.DeepColumns, Element.Fire, 3,
                resolved.Clash!.NullifiedBandCells, forPlayer: 1);

            Assert.That(nullifiedScore, Is.EqualTo(plainScore - ScoringConfig.Default.BandCellPoints));
        }

        [Test]
        public void EclipseNullify_OnNonBandCell_IsIllegalToDeclare()
        {
            var state = TwoPlayerState(BoardLayouts.Tidewater(), BoardLayouts.Ashfall(), ClashConfig.Default);

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Eclipse,
                    new InterventionParams.EclipseNullifyBand(1, 0, 0), Rng.Create(1))); // Ashfall (0,0) is Fire's element cell.
        }

        // ---- Eclipse: cancel-as-reaction ----

        [Test]
        public void EclipseCancel_NegatesPendingIntervention_ChargingStormNotFavor()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default, stormP1: 10);

            var declared = ClashReducer.DeclareIntervention(state, actor: 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 3), Rng.Create(1));
            int favorAfterBacklash = declared.Players[1].FavorRemaining;

            var cancelled = ClashReducer.DeclareIntervention(declared, actor: 1, InterventionKind.Eclipse,
                new InterventionParams.EclipseCancel(), Rng.Create(1));

            Assert.That(cancelled.Clash!.Pending, Is.Null);
            Assert.That(cancelled.Clash.Storm[1], Is.EqualTo(10 - ClashConfig.Default.InterventionCost));
            Assert.That(cancelled.Players[1].FavorRemaining, Is.EqualTo(favorAfterBacklash), "backlash favor is not reversed");
            Assert.That(cancelled.Players[1].Board.DieAt(1, 2)!.Face, Is.EqualTo(15), "the original effect never applies");
        }

        [Test]
        public void EclipseCancel_ByNonTarget_Throws()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 3), Rng.Create(1));

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(declared, actor: 0, InterventionKind.Eclipse,
                    new InterventionParams.EclipseCancel(), Rng.Create(1)));
        }

        // ---- General declaration validation ----

        [Test]
        public void DeclareIntervention_InsufficientStorm_Throws()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default, stormP0: 0);

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                    new InterventionParams.Scorch(1, 1, 2, Pips: 2), Rng.Create(1)));
        }

        [Test]
        public void DeclareIntervention_KindNotDealtThisMatch_Throws()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);
            var restricted = state.Clash! with { InterventionsAvailable = new List<InterventionKind> { InterventionKind.Riptide } };
            state = state with { Clash = restricted };

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                    new InterventionParams.Scorch(1, 1, 2, Pips: 2), Rng.Create(1)));
        }

        [Test]
        public void DeclareIntervention_WhileAnotherIsPending_Throws()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1));

            Assert.Throws<InvalidOperationException>(() =>
                ClashReducer.DeclareIntervention(declared, 0, InterventionKind.Scorch,
                    new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1)));
        }

        // ---- GameReducer refuses other actions while an intervention is pending ----

        [Test]
        public void GameReducer_RefusesDraftAndForfeit_WhileInterventionPending()
        {
            var pool = new List<Die> { new(Element.Fire, 1) };
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default, pool: pool);

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1));

            Assert.Throws<InvalidOperationException>(() =>
                GameReducer.ApplyDraft(declared, new DraftChoice(DieSource.Pool, 0, 0, 0), Rng.Create(1)));
            Assert.Throws<InvalidOperationException>(() => GameReducer.ApplyForfeit(declared));
        }
    }
}
