using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game.Tests.Clash
{
    public class ClashAiTests
    {
        private static GameState TwoPlayerState(Board boardP0, Board boardP1, ClashConfig config, int stormP0 = 10)
        {
            var players = new List<PlayerState> { new(boardP0, 3, Element.Water), new(boardP1, 3, Element.Fire) };
            var clash = new ClashState(
                config,
                Storm: new List<int> { stormP0, 0 },
                InterventionsAvailable: new List<InterventionKind> { InterventionKind.Scorch, InterventionKind.Riptide, InterventionKind.Gust, InterventionKind.Petrify, InterventionKind.Eclipse },
                PetrifyTokens: new List<PetrifyToken>(),
                Pending: null,
                InterventionLog: new List<ClashLogEntry>(),
                NullifiedBandCells: new List<NullifiedCell>());

            return new GameState(1, 0, players, Bag.Default, new List<FirmamentDie>(), PublicObjective.DeepColumns, null, 0, false, clash);
        }

        [Test]
        public void EnumerateDeclarations_InsufficientStorm_ReturnsEmpty()
        {
            var state = TwoPlayerState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default, stormP0: 0);

            Assert.That(ClashLegalMoves.EnumerateDeclarations(state, 0), Is.Empty);
        }

        [Test]
        public void EnumerateDeclarations_WhilePending_ReturnsEmpty()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);
            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1));

            Assert.That(ClashLegalMoves.EnumerateDeclarations(declared, 0), Is.Empty);
        }

        [Test]
        public void EnumerateDeclarations_IncludesScorchAndPetrifyAndEclipseCandidates_ForOpponentsBoard()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var candidates = ClashLegalMoves.EnumerateDeclarations(state, actor: 0);

            Assert.That(candidates, Has.Some.Matches<(InterventionKind Kind, InterventionParams Params)>(
                x => x.Kind == InterventionKind.Scorch && x.Params is InterventionParams.Scorch s && s.TargetPlayer == 1 && s.Row == 1 && s.Col == 2));
            Assert.That(candidates, Has.Some.Matches<(InterventionKind Kind, InterventionParams Params)>(
                x => x.Kind == InterventionKind.Eclipse && x.Params is InterventionParams.EclipseNullifyBand e && e.Row == 1 && e.Col == 2));
            Assert.That(candidates, Has.Some.Matches<(InterventionKind Kind, InterventionParams Params)>(
                x => x.Kind == InterventionKind.Petrify)); // Tidewater has plenty of empty cells for player B.
        }

        [Test]
        public void EnumerateDeclarations_NeverIncludesEclipseCancel()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);

            var candidates = ClashLegalMoves.EnumerateDeclarations(state, actor: 0);

            Assert.That(candidates.Select(c => c.Params), Has.None.InstanceOf<InterventionParams.EclipseCancel>());
        }

        [Test]
        public void ClashAiPolicy_ChooseIntervention_OnlyEverPicksFromEnumeratedCandidates()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);
            var policy = new ClashAiPolicy(new NoviceAi());
            var candidates = ClashLegalMoves.EnumerateDeclarations(state, 0);

            for (int seed = 0; seed < 20; seed++)
            {
                var chosen = policy.ChooseIntervention(state, 0, Rng.Create(seed));
                Assert.That(chosen, Is.Not.Null);
                Assert.That(candidates, Has.Some.Matches<(InterventionKind Kind, InterventionParams Params)>(
                    x => x.Kind == chosen!.Value.Kind && x.Params == chosen.Value.Params));
            }
        }

        [Test]
        public void ClashAiPolicy_ShouldWard_FalseWhenNotTargeted()
        {
            var state = TwoPlayerState(BoardLayouts.Ashfall(), BoardLayouts.Tidewater(), ClashConfig.Default);
            var policy = new ClashAiPolicy(new NoviceAi());

            Assert.That(policy.ShouldWard(state, 1), Is.False);
        }

        [Test]
        public void ClashAiPolicy_ShouldWard_TrueWhenAffordable_FalseWhenNot()
        {
            var boardB = BoardLayouts.Ashfall().WithPlacement(new Placement(1, 2, new Die(Element.Water, 15)));
            var state = TwoPlayerState(BoardLayouts.Tidewater(), boardB, ClashConfig.Default);
            var policy = new ClashAiPolicy(new NoviceAi());

            var declared = ClashReducer.DeclareIntervention(state, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1));
            Assert.That(policy.ShouldWard(declared, 1), Is.True);

            var expensiveConfig = ClashConfig.Default with { WardCost = 99 };
            var expensiveState = TwoPlayerState(BoardLayouts.Tidewater(), boardB, expensiveConfig);
            var expensiveDeclared = ClashReducer.DeclareIntervention(expensiveState, 0, InterventionKind.Scorch,
                new InterventionParams.Scorch(1, 1, 2, Pips: 1), Rng.Create(1));
            Assert.That(policy.ShouldWard(expensiveDeclared, 1), Is.False);
        }
    }
}
