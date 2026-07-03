using NUnit.Framework;
using Quintessence.Engine;

namespace Quintessence.Game.Tests.Clash
{
    // The prime directive of docs/clash.md: Standard/Daily/Endless/Puzzle/Solo play
    // must be byte-identical before and after this feature. GameSetup.NewGame's
    // clashConfig parameter defaults to null and ClashSetup.Deal is only ever called
    // when it is non-null (a ternary short-circuit, not a branch inside shared
    // logic) - so a non-Clash call draws zero additional RNG values and executes
    // zero new code. These golden scores were captured from an actual run right
    // after the Clash scaffolding landed (GameState/GameSetup changes only, no
    // touch to GameReducer/LegalDrafts yet) and must never change as C1-C4 add
    // Clash-only behavior gated behind `state.Clash is not null`.
    public class ClashRegressionTests
    {
        [Test]
        public void NewGame_WithoutClashConfig_ClashStateIsNull()
        {
            var state = GameSetup.NewGame(2, Rng.Create(0));
            Assert.That(state.Clash, Is.Null);
        }

        [TestCase(2, 0, new[] { 17, 17 })]
        [TestCase(2, 1, new[] { 14, 21 })]
        [TestCase(2, 2, new[] { 10, 14 })]
        [TestCase(2, 3, new[] { 10, 14 })]
        [TestCase(2, 4, new[] { 22, 12 })]
        [TestCase(3, 0, new[] { 11, 5, 17 })]
        [TestCase(3, 1, new[] { 12, 23, 19 })]
        [TestCase(3, 2, new[] { 7, 1, 17 })]
        [TestCase(3, 3, new[] { 16, 20, 14 })]
        [TestCase(3, 4, new[] { 9, 17, 21 })]
        [TestCase(4, 0, new[] { 9, 17, 21, 7 })]
        [TestCase(4, 1, new[] { 21, 13, 12, 15 })]
        [TestCase(4, 2, new[] { 7, 14, 12, 7 })]
        [TestCase(4, 3, new[] { 10, 12, 10, 16 })]
        [TestCase(4, 4, new[] { 10, 9, 13, 15 })]
        public void PlayRandomGame_WithoutClash_MatchesGoldenBaseline(int playerCount, int seed, int[] expectedScores)
        {
            var result = SelfPlay.PlayRandomGame(playerCount, Rng.Create(seed));

            Assert.That(result.Scores, Is.EqualTo(expectedScores));
            Assert.That(result.FinalState.Clash, Is.Null);
        }
    }
}
