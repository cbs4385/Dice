using System;
using System.Linq;
using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class BagOpsTests
    {
        [Test]
        public void DrawRoll_Determinism_SameSeedSameBagYieldsIdenticalResult()
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var (diceA, bagA) = BagOps.DrawRoll(Bag.Default, Rng.Create(seed), 9);
                var (diceB, bagB) = BagOps.DrawRoll(Bag.Default, Rng.Create(seed), 9);

                Assert.That(diceB, Is.EqualTo(diceA), $"seed {seed}");
                Assert.That(bagB, Is.EqualTo(bagA), $"seed {seed}");
            }
        }

        [Test]
        public void DrawRoll_WithoutReplacement_DecrementsRemainingByExactlyDrawnCount()
        {
            var rng = Rng.Create(123);
            var (dice, bag) = BagOps.DrawRoll(Bag.Default, rng, 9);

            foreach (var element in Elements.All)
            {
                int drawnCount = dice.Count(d => d.Element == element);
                int expectedRemaining = Bag.Default.Remaining[element] - drawnCount;
                Assert.That(bag.Remaining[element], Is.EqualTo(expectedRemaining), element.ToString());
            }
        }

        [Test]
        public void DrawRoll_EachDieFaceIsWithinItsOwnRange()
        {
            var rng = Rng.Create(456);
            var (dice, _) = BagOps.DrawRoll(Bag.Default, rng, 60);

            foreach (var die in dice)
            {
                Assert.That(die.Face, Is.InRange(1, Sides.Of(die.Element)), die.Element.ToString());
            }
        }

        [Test]
        public void DrawRoll_DrawingEntireBag_EmptiesIt()
        {
            var rng = Rng.Create(789);
            var (dice, bag) = BagOps.DrawRoll(Bag.Default, rng, 60);

            Assert.That(dice, Has.Count.EqualTo(60));
            Assert.That(bag.Remaining.Values, Is.All.EqualTo(0));
        }

        [Test]
        public void DrawRoll_FromEmptyBag_Throws()
        {
            var rng = Rng.Create(1);
            var empty = new Bag(Elements.All.ToDictionary(e => e, _ => 0));

            Assert.Throws<InvalidOperationException>(() => BagOps.DrawRoll(empty, rng, 1));
        }

        [Test]
        public void DrawRoll_NegativeCount_Throws()
        {
            var rng = Rng.Create(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => BagOps.DrawRoll(Bag.Default, rng, -1));
        }

        [Test]
        public void DrawRoll_ZeroCount_ReturnsEmptyAndUnchangedBag()
        {
            var (dice, bag) = BagOps.DrawRoll(Bag.Default, Rng.Create(1), 0);

            Assert.That(dice, Is.Empty);
            Assert.That(bag, Is.EqualTo(Bag.Default));
        }
    }
}
