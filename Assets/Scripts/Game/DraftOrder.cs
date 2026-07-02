using System.Collections.Generic;
using System.Linq;

namespace Quintessence.Game
{
    public interface IDraftOrderStrategy
    {
        IReadOnlyList<int> PickOrder(int startPlayerIndex, int playerCount, int pickNumber);
    }

    // PROVISIONAL default (see docs/progress.md open questions): the rulebook's
    // tabletop round structure literally is a snake draft - forward order for the
    // first pick, reverse for the second - and that is what this implements.
    // Whether a digital build should instead resolve picks simultaneously (hidden
    // submission then reveal) is a design fork the supervisor has not resolved.
    // This interface exists so that decision doesn't force an engine rewrite later.
    // Do not bake SnakeDraftOrderStrategy into UI or netcode.
    public sealed class SnakeDraftOrderStrategy : IDraftOrderStrategy
    {
        public IReadOnlyList<int> PickOrder(int startPlayerIndex, int playerCount, int pickNumber)
        {
            var forward = Enumerable.Range(0, playerCount)
                .Select(i => (startPlayerIndex + i) % playerCount)
                .ToArray();

            return pickNumber == 1 ? forward : forward.Reverse().ToArray();
        }
    }
}
