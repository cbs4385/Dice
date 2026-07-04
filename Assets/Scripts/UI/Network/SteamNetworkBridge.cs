#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Quintessence.Game.Network;
using Steamworks;
using Steamworks.Data;

namespace Quintessence.UI.Network
{
    // A real Steam P2P implementation of INetworkBridge, built against
    // SteamNetworkingSockets' relay API - Steam's own relay network handles
    // NAT traversal, so there's no direct-IP setup here. Direct 2-peer only
    // for this slice: the host listens via CreateRelaySocket, a single peer
    // connects via ConnectRelay to a manually-supplied SteamId - no lobby
    // creation/browsing yet (see the plan's own Deferred section; this
    // requires SteamService.TryInit to have already succeeded).
    //
    // Host-authoritative, matching LoopbackNetworkBridge's design exactly: a
    // peer's SendIntent only ever reaches the host, never applies locally;
    // the host stamps a SequenceNumber and rebroadcasts to every connected
    // peer (including itself, via a direct local ActionConfirmed) - the only
    // place any client's ActionConfirmed ever fires from an action that
    // didn't originate as its own local click.
    public sealed class SteamNetworkBridge : INetworkBridge, ISocketManager, IConnectionManager, IDisposable
    {
        private const int VirtualPort = 0;

        private SocketManager? _hostSocket;
        private ConnectionManager? _peerConnection;
        private int _nextSequenceNumber;

        public bool IsHost { get; }

        // Not yet meaningful - seat-to-peer assignment is a later slice's
        // job (lobby/seat UI, per the plan's Deferred section). No existing
        // caller reads this today.
        public int LocalPeerId { get; private set; }

        public event Action<NetworkAction>? ActionConfirmed;

        private SteamNetworkBridge(bool isHost)
        {
            IsHost = isHost;
        }

        public static SteamNetworkBridge CreateHost()
        {
            var bridge = new SteamNetworkBridge(isHost: true) { LocalPeerId = 0 };
            bridge._hostSocket = SteamNetworkingSockets.CreateRelaySocket(VirtualPort, bridge);
            return bridge;
        }

        public static SteamNetworkBridge CreateClient(SteamId hostId)
        {
            var bridge = new SteamNetworkBridge(isHost: false);
            bridge._peerConnection = SteamNetworkingSockets.ConnectRelay(hostId, VirtualPort, bridge);
            return bridge;
        }

        public void SendIntent(NetworkAction action)
        {
            if (IsHost)
            {
                ApplyAndBroadcast(action);
            }
            else
            {
                byte[] bytes = NetworkActionWireFormat.Encode(action);
                _peerConnection?.Connection.SendMessage(bytes, SendType.Reliable);
            }
        }

        // The only place State-affecting actions are ever stamped with a
        // canonical order - called both for the host's own local intents and
        // for intents received from a peer via OnMessage below.
        private void ApplyAndBroadcast(NetworkAction action)
        {
            NetworkAction confirmed = action with { SequenceNumber = _nextSequenceNumber };
            _nextSequenceNumber++;

            if (_hostSocket is not null)
            {
                byte[] bytes = NetworkActionWireFormat.Encode(confirmed);
                foreach (var connection in _hostSocket.Connected)
                {
                    connection.SendMessage(bytes, SendType.Reliable);
                }
            }

            ActionConfirmed?.Invoke(confirmed);
        }

        // ISocketManager - host side, one callback set per connected peer.
        void ISocketManager.OnConnecting(Connection connection, ConnectionInfo info) => connection.Accept();

        void ISocketManager.OnConnected(Connection connection, ConnectionInfo info)
        {
        }

        void ISocketManager.OnDisconnected(Connection connection, ConnectionInfo info)
        {
        }

        void ISocketManager.OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            ApplyAndBroadcast(NetworkActionWireFormat.Decode(CopyToManaged(data, size)));
        }

        // IConnectionManager - peer side, this client's single connection to the host.
        void IConnectionManager.OnConnecting(ConnectionInfo info)
        {
        }

        void IConnectionManager.OnConnected(ConnectionInfo info)
        {
        }

        void IConnectionManager.OnDisconnected(ConnectionInfo info)
        {
        }

        void IConnectionManager.OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            ActionConfirmed?.Invoke(NetworkActionWireFormat.Decode(CopyToManaged(data, size)));
        }

        private static byte[] CopyToManaged(IntPtr data, int size)
        {
            var bytes = new byte[size];
            Marshal.Copy(data, bytes, 0, size);
            return bytes;
        }

        public void Dispose()
        {
            _hostSocket?.Close();
            _peerConnection?.Close();
        }
    }
}
