using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    public sealed record PlayerState(Board Board, int FavorRemaining, Element PrivateElement);

    public sealed record FirmamentDie(int Id, Die Die);

    public sealed record RoundPhase(int PickNumber, IReadOnlyList<int> PickOrder, int PickOrderIndex, IReadOnlyList<Die> Pool);

    public sealed record GameState(
        int Round,
        int StartPlayerIndex,
        IReadOnlyList<PlayerState> Players,
        Bag Bag,
        IReadOnlyList<FirmamentDie> Firmament,
        PublicObjective Objective,
        RoundPhase? CurrentPhase,
        int NextFirmamentId,
        bool IsGameOver);
}
