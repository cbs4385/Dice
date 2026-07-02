using System;

namespace Quintessence.Engine
{
    public interface IRng
    {
        // Returns a value in [0, maxExclusive).
        int NextInt(int maxExclusive);
    }

    public static class Rng
    {
        public static IRng Create(long seed) => new SplitMix64Rng(seed);
    }

    // splitmix64 (Sebastiano Vigna, public domain): fixed, deterministic, no platform RNG.
    // Never replace with System.Random - its output is not guaranteed stable across
    // .NET versions or platforms, which would break daily-seed reproducibility.
    internal sealed class SplitMix64Rng : IRng
    {
        private ulong _state;

        internal SplitMix64Rng(long seed)
        {
            _state = unchecked((ulong)seed);
        }

        internal ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "maxExclusive must be positive.");
            }

            // Unbiased bounded draw via rejection sampling (Java Random.nextInt style),
            // done in 32-bit arithmetic so it needs no BCL API beyond ulong/uint.
            uint bound = (uint)maxExclusive;
            uint threshold = (uint)((0x1_0000_0000UL - bound) % bound);
            while (true)
            {
                uint candidate = (uint)NextUInt64();
                if (candidate >= threshold)
                {
                    return (int)(candidate % bound);
                }
            }
        }
    }
}
