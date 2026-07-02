using System;
using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class FavorTests
    {
        [Test]
        public void Adjust_PositiveDelta_IncreasesFace()
        {
            var die = new Die(Element.Water, 12);
            var adjusted = Favor.Adjust(die, 1);

            Assert.That(adjusted.Face, Is.EqualTo(13));
            Assert.That(adjusted.Element, Is.EqualTo(Element.Water));
        }

        [Test]
        public void Adjust_NegativeDelta_DecreasesFace()
        {
            var die = new Die(Element.Water, 12);
            var adjusted = Favor.Adjust(die, -1);

            Assert.That(adjusted.Face, Is.EqualTo(11));
        }

        [Test]
        public void Adjust_BelowMinimum_Throws()
        {
            var die = new Die(Element.Fire, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => Favor.Adjust(die, -1));
        }

        [Test]
        public void Adjust_AboveMaximum_Throws()
        {
            var die = new Die(Element.Fire, 4);
            Assert.Throws<ArgumentOutOfRangeException>(() => Favor.Adjust(die, 1));
        }

        [Test]
        public void Adjust_NeverWraps()
        {
            var die = new Die(Element.Fire, 4);
            Assert.Throws<ArgumentOutOfRangeException>(() => Favor.Adjust(die, 1));
            // A wrapping implementation would silently return face 1; this must not happen.
        }

        [Test]
        public void Reroll_IsDeterministicGivenSameRngState()
        {
            var die = new Die(Element.Water, 5);

            var rerolledA = Favor.Reroll(die, Rng.Create(99));
            var rerolledB = Favor.Reroll(die, Rng.Create(99));

            Assert.That(rerolledB, Is.EqualTo(rerolledA));
        }

        [Test]
        public void Reroll_FaceWithinDieRange()
        {
            var die = new Die(Element.Water, 5);
            var rng = Rng.Create(1);

            for (int i = 0; i < 100; i++)
            {
                die = Favor.Reroll(die, rng);
                Assert.That(die.Face, Is.InRange(1, 20));
            }
        }
    }
}
