using System.Collections.Generic;
using System.Linq;

namespace Quintessence.Game
{
    public interface IDraftOrderStrategy
    {
        IReadOnlyList<int> PickOrder(int startPlayerIndex, int playerCount, int pickNumber);
    }

    // CONFIRMED (2026-07-02, see docs/progress.md): snake is the actual draft
    // model, not just a provisional default. The rulebook's tabletop round
    // structure literally is a snake draft - forward order for the first pick,
    // reverse for the second - and that is what this implements. IDraftOrderStrategy
    // stays as a seam for good architecture (e.g. isolating this from UI/netcode),
    // not because the choice itself is still open.
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
