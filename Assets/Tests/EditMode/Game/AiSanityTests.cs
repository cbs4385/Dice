using NUnit.Framework;
using Quintessence.Engine;

namespace Quintessence.Game.Tests
{
    public class AiSanityTests
    {
        // Per docs/agent-build-plan.md v0.2 SS8: "Oracle beats Novice at a rate above
        // an agreed threshold." 60% is a starting threshold, not a playtest-approved
        // balance number - see AGENTS.md's human-gated balance-value rule.
        private const double OracleVsNoviceThreshold = 0.60;
        private const int GamesPerSeat = 150;

        [Test]
        public void Oracle_BeatsNovice_AboveThreshold()
        {
            double winRate = WinRate(() => new OracleAi(), () => new NoviceAi());
            Assert.That(winRate, Is.GreaterThan(OracleVsNoviceThreshold), $"Oracle win rate vs Novice was {winRate:P0}");
        }

        [Test]
        public void Oracle_BeatsAdept_AboveHalf()
        {
            double winRate = WinRate(() => new OracleAi(), () => new AdeptAi());
            Assert.That(winRate, Is.GreaterThan(0.50), $"Oracle win rate vs Adept was {winRate:P0}");
        }

        [Test]
        public void Adept_BeatsNovice_AboveHalf()
        {
            double winRate = WinRate(() => new AdeptAi(), () => new NoviceAi());
            Assert.That(winRate, Is.GreaterThan(0.50), $"Adept win rate vs Novice was {winRate:P0}");
        }

        [Test]
        public void AllTierMatchups_ManySeeds_NeverAttemptAnIllegalMove()
        {
            IAiPolicy[] policies = { new NoviceAi(), new AdeptAi(), new OracleAi() };

            for (int seed = 0; seed < 500; seed++)
            {
                // A thrown exception (e.g. GameReducer rejecting a policy's choice as
                // illegal) fails the test on its own.
                var result = AiSelfPlay.PlayWithPolicies(policies, Rng.Create(seed));

                Assert.That(result.FinalState.IsGameOver, Is.True, $"seed {seed}");
                Assert.That(result.FinalState.Round, Is.EqualTo(GameReducer.TotalRounds), $"seed {seed}");
            }
        }

        // Alternates which seat each policy sits in across seeds, so a first-player
        // (or seat-order) advantage cannot masquerade as AI-strength difference.
        private static double WinRate(System.Func<IAiPolicy> strongerFactory, System.Func<IAiPolicy> weakerFactory)
        {
            int strongerWins = 0;
            int totalGames = GamesPerSeat * 2;

            for (int seed = 0; seed < GamesPerSeat; seed++)
            {
                var gameA = AiSelfPlay.PlayWithPolicies(new[] { strongerFactory(), weakerFactory() }, Rng.Create(seed));
                if (gameA.Scores[0] > gameA.Scores[1])
                {
                    strongerWins++;
                }

                var gameB = AiSelfPlay.PlayWithPolicies(new[] { weakerFactory(), strongerFactory() }, Rng.Create(seed + 1_000_000));
                if (gameB.Scores[1] > gameB.Scores[0])
                {
                    strongerWins++;
                }
            }

            return (double)strongerWins / totalGames;
        }
    }
}
