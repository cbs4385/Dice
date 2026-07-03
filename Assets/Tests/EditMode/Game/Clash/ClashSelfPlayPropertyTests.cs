using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game.Tests.Clash
{
    // Property tests over the minimal Clash self-play harness: exercises real
    // gameplay under load (thousands of seeds) to prove the invariants that
    // docs/clash.md SS2.3 states as hard requirements, not just the individually
    // hand-crafted scenarios in ClashInterventionsTests/ClashWorkedExampleTests.
    public class ClashSelfPlayPropertyTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void PlayRandomGame_AcrossManySeeds_NeverThrowsAndKeepsInvariants(int playerCount)
        {
            var config = ClashConfig.Default;
            for (int seed = 0; seed < 500; seed++)
            {
                var result = ClashSelfPlay.PlayRandomGame(playerCount, Rng.Create(seed), config);

                Assert.That(result.FinalState.IsGameOver, Is.True);
                Assert.That(result.FinalState.Round, Is.EqualTo(GameReducer.TotalRounds));
                Assert.That(result.Scores, Has.Count.EqualTo(playerCount));

                var clash = result.FinalState.Clash!;
                Assert.That(clash.Pending, Is.Null, "must never end with an unresolved intervention");

                foreach (var storm in clash.Storm)
                {
                    Assert.That(storm, Is.InRange(0, config.StormCap), $"seed {seed}: storm must never leave [0, stormCap]");
                }

                foreach (var player in result.FinalState.Players)
                {
                    Assert.That(player.FavorRemaining, Is.GreaterThanOrEqualTo(0), $"seed {seed}: favor must never go negative");
                }
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        public void PlayRandomGame_SameSeed_IsFullyDeterministic(int playerCount)
        {
            var config = ClashConfig.Default;
            for (int seed = 0; seed < 100; seed++)
            {
                var a = ClashSelfPlay.PlayRandomGame(playerCount, Rng.Create(seed), config);
                var b = ClashSelfPlay.PlayRandomGame(playerCount, Rng.Create(seed), config);

                Assert.That(b.Scores, Is.EqualTo(a.Scores), $"seed {seed}");
                Assert.That(b.FinalState.Clash!.Storm, Is.EqualTo(a.FinalState.Clash!.Storm), $"seed {seed}");
                Assert.That(b.FinalState.Clash!.InterventionLog.Count, Is.EqualTo(a.FinalState.Clash!.InterventionLog.Count), $"seed {seed}");
            }
        }

        [Test]
        public void PlayRandomGame_AtLeastSomeSeeds_ExerciseEveryInterventionKind()
        {
            // The minimal self-play AI attunes band cells uniformly at random
            // rather than deliberately (that "when to attack" intelligence is
            // human-tunable per docs/clash.md SS5) - under the *default* Storm
            // economy, reaching interventionCost by chance compounds across all 4
            // band cells and is too rare to reliably hit within a practical seed
            // budget (empirically: 0/1200 player-games at the default cost of 4).
            // This test's purpose is proving every intervention mechanically works
            // end-to-end through the self-play harness, not exercising the tuned
            // default economy - so it deals all five kinds and lowers the cost to
            // make that reachable, leaving ClashConfig.Default itself untouched.
            var config = ClashConfig.Default with { InterventionsPerMatch = 5, InterventionCost = 1 };
            var seenKinds = new HashSet<InterventionKind>();

            for (int seed = 0; seed < 800 && seenKinds.Count < 5; seed++)
            {
                var result = ClashSelfPlay.PlayRandomGame(4, Rng.Create(seed), config);
                foreach (var entry in result.FinalState.Clash!.InterventionLog)
                {
                    seenKinds.Add(entry.Kind);
                }
            }

            Assert.That(seenKinds, Is.EquivalentTo(new[]
            {
                InterventionKind.Scorch, InterventionKind.Riptide, InterventionKind.Gust,
                InterventionKind.Petrify, InterventionKind.Eclipse,
            }));
        }
    }
}
