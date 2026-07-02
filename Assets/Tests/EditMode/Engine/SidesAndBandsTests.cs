using System;
using NUnit.Framework;

namespace Quintessence.Engine.Tests
{
    public class SidesAndBandsTests
    {
        [TestCase(Element.Fire, 4)]
        [TestCase(Element.Earth, 6)]
        [TestCase(Element.Air, 8)]
        [TestCase(Element.Aether, 12)]
        [TestCase(Element.Water, 20)]
        public void Sides_MatchesRulebook(Element element, int expectedSides)
        {
            Assert.That(Sides.Of(element), Is.EqualTo(expectedSides));
        }

        [TestCase(1, Band.Low)]
        [TestCase(4, Band.Low)]
        [TestCase(5, Band.Mid)]
        [TestCase(8, Band.Mid)]
        [TestCase(9, Band.High)]
        [TestCase(12, Band.High)]
        [TestCase(13, Band.Celestial)]
        [TestCase(20, Band.Celestial)]
        public void BandsOf_BoundaryFaces_MapCorrectly(int face, Band expectedBand)
        {
            Assert.That(Bands.Of(face), Is.EqualTo(expectedBand));
        }

        [TestCase(0)]
        [TestCase(21)]
        public void BandsOf_OutOfRangeFace_Throws(int face)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Bands.Of(face));
        }

        [TestCase(Element.Fire, Band.Low, true)]
        [TestCase(Element.Fire, Band.Mid, false)]
        [TestCase(Element.Earth, Band.Low, true)]
        [TestCase(Element.Earth, Band.Mid, true)]
        [TestCase(Element.Earth, Band.High, false)]
        [TestCase(Element.Air, Band.Mid, true)]
        [TestCase(Element.Air, Band.High, false)]
        [TestCase(Element.Aether, Band.High, true)]
        [TestCase(Element.Aether, Band.Celestial, false)]
        [TestCase(Element.Water, Band.Celestial, true)]
        public void CanReach_MatchesRulebookTable(Element element, Band band, bool expected)
        {
            Assert.That(Bands.CanReach(element, band), Is.EqualTo(expected));
        }
    }
}
