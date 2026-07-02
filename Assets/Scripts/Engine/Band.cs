using System;

namespace Quintessence.Engine
{
    public enum Band
    {
        Low,
        Mid,
        High,
        Celestial,
    }

    public static class BandRange
    {
        public static (int Min, int Max) Of(Band band) => band switch
        {
            Band.Low => (1, 4),
            Band.Mid => (5, 8),
            Band.High => (9, 12),
            Band.Celestial => (13, 20),
            _ => throw new ArgumentOutOfRangeException(nameof(band)),
        };
    }

    public static class Bands
    {
        public static Band Of(int face)
        {
            if (face is >= 1 and <= 4) return Band.Low;
            if (face is >= 5 and <= 8) return Band.Mid;
            if (face is >= 9 and <= 12) return Band.High;
            if (face is >= 13 and <= 20) return Band.Celestial;
            throw new ArgumentOutOfRangeException(nameof(face), face, "face must be between 1 and 20.");
        }

        // A die can only ever land in a band whose minimum is within its own face range.
        public static bool CanReach(Element element, Band band) => Sides.Of(element) >= BandRange.Of(band).Min;
    }
}
