using NUnit.Framework;

namespace Quintessence.Game.Tests
{
    public class DraftOrderTests
    {
        [Test]
        public void PickOne_StartsAtStartPlayer_GoesForward()
        {
            var strategy = new SnakeDraftOrderStrategy();
            var order = strategy.PickOrder(startPlayerIndex: 2, playerCount: 4, pickNumber: 1);

            Assert.That(order, Is.EqualTo(new[] { 2, 3, 0, 1 }));
        }

        [Test]
        public void PickTwo_IsReverseOfPickOne_EndingAtStartPlayer()
        {
            var strategy = new SnakeDraftOrderStrategy();
            var order = strategy.PickOrder(startPlayerIndex: 2, playerCount: 4, pickNumber: 2);

            Assert.That(order, Is.EqualTo(new[] { 1, 0, 3, 2 }));
            Assert.That(order[order.Count - 1], Is.EqualTo(2), "must end with the start player");
        }

        [Test]
        public void StartPlayerZero_TwoPlayers_MatchesSimpleAlternation()
        {
            var strategy = new SnakeDraftOrderStrategy();

            Assert.That(strategy.PickOrder(0, 2, 1), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(strategy.PickOrder(0, 2, 2), Is.EqualTo(new[] { 1, 0 }));
        }
    }
}
