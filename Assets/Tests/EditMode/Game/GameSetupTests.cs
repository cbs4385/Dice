using System;
using System.Linq;
using NUnit.Framework;
using Quintessence.Engine;

namespace Quintessence.Game.Tests
{
    public class GameSetupTests
    {
        [TestCase(1)]
        [TestCase(5)]
        public void NewGame_PlayerCountOutOfRange_Throws(int playerCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GameSetup.NewGame(playerCount, Rng.Create(1)));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void NewGame_CreatesOnePlayerStatePerPlayer_WithDistinctBoardsAndElements(int playerCount)
        {
            var state = GameSetup.NewGame(playerCount, Rng.Create(7));

            Assert.That(state.Players, Has.Count.EqualTo(playerCount));
            Assert.That(state.Players.Select(p => p.PrivateElement).Distinct().Count(), Is.EqualTo(playerCount));
            Assert.That(state.Round, Is.EqualTo(1));
            Assert.That(state.StartPlayerIndex, Is.EqualTo(0));
            Assert.That(state.CurrentPhase, Is.Null);
            Assert.That(state.IsGameOver, Is.False);
            Assert.That(state.Firmament, Is.Empty);

            foreach (var player in state.Players)
            {
                Assert.That(player.FavorRemaining, Is.EqualTo(3));
            }
        }

        [Test]
        public void NewGame_SameSeed_IsFullyDeterministic()
        {
            var a = GameSetup.NewGame(4, Rng.Create(99));
            var b = GameSetup.NewGame(4, Rng.Create(99));

            Assert.That(b.Objective, Is.EqualTo(a.Objective));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(b.Players[i].PrivateElement, Is.EqualTo(a.Players[i].PrivateElement));
            }
        }
    }
}
