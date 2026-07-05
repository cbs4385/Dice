using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class RngTests
    {
        [Test]
        public void NextInt_SameSeed_ProducesIdenticalSequence()
        {
            var a = Rng.Create(42);
            var b = Rng.Create(42);

            var sequenceA = Enumerable.Range(0, 20).Select(_ => a.NextInt(1000)).ToArray();
            var sequenceB = Enumerable.Range(0, 20).Select(_ => b.NextInt(1000)).ToArray();

            Assert.That(sequenceB, Is.EqualTo(sequenceA));
        }

        [Test]
        public void NextInt_Seed42_MatchesGoldenSequence()
        {
            // Golden/seed test: guards Daily-mode reproducibility. If this ever
            // legitimately needs to change, every existing Daily seed changes too -
            // that is a rules-affecting change, not a refactor.
            var rng = Rng.Create(42);
            var sequence = Enumerable.Range(0, 10).Select(_ => rng.NextInt(100)).ToArray();

            Assert.That(sequence, Is.EqualTo(new[] { 21, 19, 30, 48, 70, 38, 81, 12, 85, 66 }));
        }

        [Test]
        public void NextInt_DifferentSeeds_DivergeQuickly()
        {
            var a = Rng.Create(1);
            var b = Rng.Create(2);

            var sequenceA = Enumerable.Range(0, 10).Select(_ => a.NextInt(int.MaxValue)).ToArray();
            var sequenceB = Enumerable.Range(0, 10).Select(_ => b.NextInt(int.MaxValue)).ToArray();

            Assert.That(sequenceB, Is.Not.EqualTo(sequenceA));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NextInt_NonPositiveBound_Throws(int bound)
        {
            var rng = Rng.Create(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(bound));
        }

        [Test]
        public void NextInt_AlwaysWithinBound()
        {
            var rng = Rng.Create(7);
            for (int i = 0; i < 5000; i++)
            {
                int value = rng.NextInt(37);
                Assert.That(value, Is.InRange(0, 36));
            }
        }

        [Test]
        public void NextInt_SmallBound_CoversFullRange()
        {
            var rng = Rng.Create(9001);
            var seen = new HashSet<int>();
            for (int i = 0; i < 1000; i++)
            {
                seen.Add(rng.NextInt(2));
            }

            Assert.That(seen, Is.EquivalentTo(new[] { 0, 1 }));
        }

        [Test]
        public void ExportState_CreateFromState_ContinuesTheExactSameStream()
        {
            // Save/resume's correctness rests on this: the resumed RNG's next
            // draws must match what the *original* stream would have drawn
            // next, not just "produces some deterministic sequence."
            var original = Rng.Create(12345);
            for (int i = 0; i < 7; i++)
            {
                original.NextInt(500);
            }

            ulong exported = original.ExportState();
            var expectedContinuation = Enumerable.Range(0, 10).Select(_ => original.NextInt(500)).ToArray();

            var resumed = Rng.CreateFromState(exported);
            var actualContinuation = Enumerable.Range(0, 10).Select(_ => resumed.NextInt(500)).ToArray();

            Assert.That(actualContinuation, Is.EqualTo(expectedContinuation));
        }
    }
}
