using System;

namespace Quintessence.Engine
{
    public static class Favor
    {
        public static Die Adjust(Die die, int delta)
        {
            int newFace = die.Face + delta;
            int max = Sides.Of(die.Element);
            if (newFace < 1 || newFace > max)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delta), delta, "Adjust cannot move the face below 1 or above the die's top face (no wrapping).");
            }

            return die with { Face = newFace };
        }

        public static Die Reroll(Die die, IRng rng)
        {
            int face = rng.NextInt(Sides.Of(die.Element)) + 1;
            return die with { Face = face };
        }
    }
}
