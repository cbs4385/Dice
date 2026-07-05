using System;

namespace Quintessence.Engine
{
    public interface IRng
    {
        // Returns a value in [0, maxExclusive).
        int NextInt(int maxExclusive);

        // Exposes the exact internal counter so a caller (e.g. save/resume)
        // can later reconstruct an RNG that continues the same stream via
        // Rng.CreateFromState - a save file has to capture exactly where the
        // stream is, or every draw after loading diverges from what would
        // have happened without saving, silently breaking determinism the
        // same way a wrong seed would.
        ulong ExportState();
    }

    public static class Rng
    {
        public static IRng Create(long seed) => new SplitMix64Rng(seed);

        // Resumes a stream from a previously exported state - NOT the same
        // as Create(long): that re-seeds from scratch, this continues
        // exactly where ExportState left off.
        public static IRng CreateFromState(ulong state) => new SplitMix64Rng(state);
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

        // Distinct overload (ulong, not long) - resumes an exported stream
        // directly rather than re-deriving a starting state from a seed.
        internal SplitMix64Rng(ulong state)
        {
            _state = state;
        }

        public ulong ExportState() => _state;

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
