using System;
using System.Collections.Generic;

namespace Quintessence.Engine
{
    public sealed record Bag(IReadOnlyDictionary<Element, int> Remaining)
    {
        public static readonly Bag Default = new(new Dictionary<Element, int>
        {
            [Element.Fire] = 16,
            [Element.Earth] = 16,
            [Element.Air] = 12,
            [Element.Aether] = 8,
            [Element.Water] = 8,
        });

        // Dictionary does not implement value equality, so the compiler-generated
        // record Equals (which defers to EqualityComparer<T>.Default per property)
        // would silently fall back to reference equality here. That would break any
        // future determinism check that compares two Bag instances by value.
        public bool Equals(Bag? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            foreach (var element in Elements.All)
            {
                int mine = Remaining.TryGetValue(element, out var m) ? m : 0;
                int theirs = other.Remaining.TryGetValue(element, out var t) ? t : 0;
                if (mine != theirs)
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = default(HashCode);
            foreach (var element in Elements.All)
            {
                hash.Add(Remaining.TryGetValue(element, out var c) ? c : 0);
            }

            return hash.ToHashCode();
        }
    }

    public static class BagOps
    {
        public static (IReadOnlyList<Die> Dice, Bag Bag) DrawRoll(Bag bag, IRng rng, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative.");
            }

            var remaining = new Dictionary<Element, int>();
            foreach (var element in Elements.All)
            {
                remaining[element] = bag.Remaining.TryGetValue(element, out var c) ? c : 0;
            }

            var drawn = new List<Die>(count);
            for (int i = 0; i < count; i++)
            {
                int total = 0;
                foreach (var element in Elements.All)
                {
                    total += remaining[element];
                }

                if (total <= 0)
                {
                    throw new InvalidOperationException("Cannot draw from an empty bag.");
                }

                int pick = rng.NextInt(total);
                int cumulative = 0;
                Element chosen = Elements.All[0];
                foreach (var element in Elements.All)
                {
                    cumulative += remaining[element];
                    if (pick < cumulative)
                    {
                        chosen = element;
                        break;
                    }
                }

                remaining[chosen] -= 1;
                int face = rng.NextInt(Sides.Of(chosen)) + 1;
                drawn.Add(new Die(chosen, face));
            }

            return (drawn, new Bag(remaining));
        }
    }
}
