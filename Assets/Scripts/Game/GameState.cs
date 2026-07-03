using System.Collections.Generic;
using Quintessence.Engine;
using Quintessence.Game.Clash;

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
        bool IsGameOver,
        // Clash-only state; null in every other mode (docs/clash.md SS0 prime directive:
        // non-Clash modes must stay byte-identical). Every existing caller of
        // GameSetup.NewGame doesn't pass a ClashConfig, so this is always null for them.
        ClashState? Clash = null);
}
