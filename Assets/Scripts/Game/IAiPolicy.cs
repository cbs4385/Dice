using Quintessence.Engine;

namespace Quintessence.Game
{
    public interface IAiPolicy
    {
        // Returns null to mean "no legal placement exists - forfeit this pick",
        // matching the rulebook's forfeit rule. Never returns an illegal choice:
        // implementations must choose only from LegalDrafts.EnumerateSimple.
        DraftChoice? ChooseMove(GameState state, IRng rng);
    }
}
