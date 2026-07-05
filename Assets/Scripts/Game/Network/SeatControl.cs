namespace Quintessence.Game.Network
{
    // Whether a seat is AI, a human seat a given client directly controls
    // (local hotseat), or a human seat controlled by a remote peer over the
    // network. Lives in the pure Network layer, not the UI layer where it
    // originated, because it needs to travel over the wire as part of
    // NetworkAction.MatchStart - every connecting peer needs to know the
    // full seat configuration to construct identical starting state.
    public enum SeatControl { Ai, LocalHuman, RemoteHuman }
}
