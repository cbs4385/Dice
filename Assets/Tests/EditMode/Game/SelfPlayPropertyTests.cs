using System;
using NUnit.Framework;
using Quintessence.Engine;

namespace Quintessence.Game.Tests
{
    // Hand-rolled property-style tests (loops over many seeds), matching the Engine
    // package's approach - no property-testing library added without approval.
    public class SelfPlayPropertyTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void PlayRandomGame_ManySeeds_NeverThrowsAndTerminatesInSixRoundsWithIntegerScores(int playerCount)
        {
            // "Thousands of seeded games" per docs/agent-build-plan.md v0.2 SS9 M2 DoD.
            for (int seed = 0; seed < 2000; seed++)
            {
                // A thrown exception here fails the test on its own; no explicit
                // Assert.DoesNotThrow needed, and this way the stack trace survives.
                var result = SelfPlay.PlayRandomGame(playerCount, Rng.Create(seed));

                Assert.That(result.FinalState.IsGameOver, Is.True, $"seed {seed}");
                Assert.That(result.FinalState.Round, Is.EqualTo(GameReducer.TotalRounds), $"seed {seed}");
                Assert.That(result.FinalState.CurrentPhase, Is.Null, $"seed {seed}");
                Assert.That(result.Scores, Has.Count.EqualTo(playerCount), $"seed {seed}");
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void PlayRandomGame_SameSeed_IsFullyDeterministic(int playerCount)
        {
            for (int seed = 0; seed < 300; seed++)
            {
                var a = SelfPlay.PlayRandomGame(playerCount, Rng.Create(seed));
                var b = SelfPlay.PlayRandomGame(playerCount, Rng.Create(seed));

                Assert.That(b.Scores, Is.EqualTo(a.Scores), $"seed {seed}");
                Assert.That(b.FinalState.Objective, Is.EqualTo(a.FinalState.Objective), $"seed {seed}");

                for (int p = 0; p < playerCount; p++)
                {
                    AssertSameBoard(a.FinalState.Players[p].Board, b.FinalState.Players[p].Board, seed, p);
                    Assert.That(b.FinalState.Players[p].FavorRemaining, Is.EqualTo(a.FinalState.Players[p].FavorRemaining), $"seed {seed} player {p}");
                }
            }
        }

        [Test]
        public void StartRound_AfterSelfPlayGameOver_Throws()
        {
            var result = SelfPlay.PlayRandomGame(2, Rng.Create(1));
            Assert.Throws<InvalidOperationException>(() => GameReducer.StartRound(result.FinalState, Rng.Create(2)));
        }

        private static void AssertSameBoard(Board a, Board b, int seed, int player)
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    Assert.That(b.DieAt(r, c), Is.EqualTo(a.DieAt(r, c)), $"seed {seed} player {player} cell ({r},{c})");
                }
            }
        }
    }
}
