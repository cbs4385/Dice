using System;
using Quintessence.Game.Network;

namespace Quintessence.UI.Network
{
    // The mocked seam a real transport (Steamworks P2P, per docs/gdd.md's
    // async same-seed drafting design) will eventually implement against -
    // per AGENTS.md's existing mandate for Steam integration, no real
    // SDK/dependency exists yet. LoopbackNetworkBridge is the only
    // implementation today.
    public interface INetworkBridge
    {
        bool IsHost { get; }

        int LocalPeerId { get; }

        // Sends an action this client wants to happen. The host applies it
        // and rebroadcasts it (stamped with a canonical SequenceNumber) via
        // ActionConfirmed - a non-host peer never mutates GameState from its
        // own SendIntent call directly, only from a later ActionConfirmed.
        void SendIntent(NetworkAction action);

        // Fired once per action, in the host's canonical order, on every
        // client (including the one that sent it) - the only path any
        // client should use to actually mutate GameState.
        event Action<NetworkAction> ActionConfirmed;
    }
}
