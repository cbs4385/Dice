using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Quintessence.Engine;
using Quintessence.Game;
using Quintessence.Game.Network;

namespace Quintessence.UI.Tests
{
    // Drives GameSessionController.ApplyNetworkAction's private MatchStart
    // case directly via reflection (this project's established pattern for
    // verifying private-method logic) and confirms it produces byte-identical
    // state to a direct GameSetup.NewGame call with the same seed - proving
    // the "a joining peer converges to the same state the host has" premise
    // actually holds, the same way NetworkActionConvergenceTests already
    // proved it for the other 5 action types.
    public class GameSessionControllerNetworkTests
    {
        [Test]
        public void ApplyNetworkAction_MatchStart_MatchesDirectGameSetupNewGame()
        {
            var go = new GameObject("GameSessionControllerNetworkTest");
            try
            {
                var controller = go.AddComponent<GameSessionController>();
                var applyMethod = typeof(GameSessionController).GetMethod("ApplyNetworkAction", BindingFlags.NonPublic | BindingFlags.Instance);

                var seats = new[] { SeatControl.LocalHuman, SeatControl.Ai };
                var matchStart = new NetworkAction.MatchStart(42L, 2, seats, IsClash: false) { ActingPlayer = 0, SequenceNumber = 0 };
                applyMethod.Invoke(controller, new object[] { matchStart });

                var expected = GameSetup.NewGame(2, Rng.Create(42L));

                Assert.That(controller.State, Is.Not.Null);
                Assert.That(controller.State.Round, Is.EqualTo(expected.Round));
                Assert.That(controller.State.Objective, Is.EqualTo(expected.Objective));
                Assert.That(controller.State.Clash, Is.Null);

                for (int p = 0; p < 2; p++)
                {
                    Assert.That(controller.State.Players[p].FavorRemaining, Is.EqualTo(expected.Players[p].FavorRemaining), $"player {p}");
                    for (int r = 0; r < Board.Rows; r++)
                    {
                        for (int c = 0; c < Board.Columns; c++)
                        {
                            Assert.That(controller.State.Players[p].Board.DieAt(r, c), Is.EqualTo(expected.Players[p].Board.DieAt(r, c)), $"player {p} cell ({r},{c})");
                        }
                    }
                }
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        [Test]
        public void ApplyNetworkAction_MatchStartWithIsClash_ProducesAClashMatch()
        {
            var go = new GameObject("GameSessionControllerNetworkTest");
            try
            {
                var controller = go.AddComponent<GameSessionController>();
                var applyMethod = typeof(GameSessionController).GetMethod("ApplyNetworkAction", BindingFlags.NonPublic | BindingFlags.Instance);

                var seats = new[] { SeatControl.LocalHuman, SeatControl.Ai };
                var matchStart = new NetworkAction.MatchStart(7L, 2, seats, IsClash: true) { ActingPlayer = 0, SequenceNumber = 0 };
                applyMethod.Invoke(controller, new object[] { matchStart });

                Assert.That(controller.State, Is.Not.Null);
                Assert.That(controller.State.Clash, Is.Not.Null);
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
