using System;
using Quintessence.Game.Network;

namespace Quintessence.UI.Network
{
    // Single-process, always-host loopback: SendIntent stamps the next
    // sequence number and raises ActionConfirmed synchronously, simulating a
    // host that is also the only peer. This is the production default (no
    // remote seats configured today, so every action is already "local" -
    // this is observably identical to calling the reducers directly) and the
    // seam a convergence test drives to prove the action-relay design works
    // before any real transport exists.
    public sealed class LoopbackNetworkBridge : INetworkBridge
    {
        private int _nextSequenceNumber;

        public bool IsHost => true;

        public int LocalPeerId => 0;

        public event Action<NetworkAction> ActionConfirmed;

        public void SendIntent(NetworkAction action)
        {
            NetworkAction confirmed = action with { SequenceNumber = _nextSequenceNumber };
            _nextSequenceNumber++;
            ActionConfirmed?.Invoke(confirmed);
        }
    }
}
