using Quintessence.Game.Clash;

namespace Quintessence.Game.Network
{
    // A serializable wrapper around every existing action shape that mutates
    // GameState (DraftChoice, forfeit, Clash's DeclareIntervention/Ward/
    // DeclineWard) plus the metadata a network bridge needs: which seat
    // issued it, and its position in the host's canonical order. Every
    // variant's payload is already a plain enum/record with no UnityEngine
    // types or delegates (confirmed against the existing types this wraps),
    // so this whole hierarchy is trivially serializable as-is.
    public abstract record NetworkAction
    {
        private NetworkAction()
        {
        }

        public int ActingPlayer { get; init; }

        public int SequenceNumber { get; init; }

        public sealed record Draft(DraftChoice Choice) : NetworkAction;

        public sealed record Forfeit : NetworkAction;

        public sealed record Declare(InterventionKind Kind, InterventionParams Params) : NetworkAction;

        public sealed record Ward : NetworkAction;

        public sealed record DeclineWard : NetworkAction;
    }
}
