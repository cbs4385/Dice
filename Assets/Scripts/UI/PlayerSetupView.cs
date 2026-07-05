using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Quintessence.Game.Network;
using Quintessence.UI.Network;

namespace Quintessence.UI
{
    // Host-facing player setup, shown after picking a mode from
    // ModeSelectView and before the match actually starts: pick a player
    // count (2-4) and AI/Human (Local)/Human (Remote) per seat - all-AI is a
    // valid configuration (the host just watches). A deliberately plain,
    // structural placeholder; real setup-screen feel (art, layout polish) is
    // human-gated.
    public sealed class PlayerSetupView : MonoBehaviour
    {
        private const int MinPlayers = 2;
        private const int MaxPlayers = 4;

        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;

        // Index i is the button/label for player count (i + MinPlayers).
        [SerializeField] private Button[] _playerCountButtons;

        // Index i is seat i (0-3). Rows beyond the current player count are
        // hidden; toggle buttons cycle Ai/LocalHuman/RemoteHuman on click.
        [SerializeField] private GameObject[] _seatRows;
        [SerializeField] private TMP_Text[] _seatToggleLabels;
        [SerializeField] private Button[] _seatToggleButtons;

        [SerializeField] private Button _confirmButton;

        // Shown instead of the normal setup panel once a Remote seat exists
        // and Confirm has been pressed - a real peer has to actually connect
        // before the match can start, so this isn't instant like the
        // all-local case.
        [SerializeField] private GameObject _hostingStatusPanel;
        [SerializeField] private TMP_Text _hostingStatusText;
        [SerializeField] private Button _hostingBackButton;

        private int _playerCount = MinPlayers;
        private readonly SeatControl[] _seatControl = { SeatControl.LocalHuman, SeatControl.Ai, SeatControl.Ai, SeatControl.Ai };
        private Action _onConfirm;
        private SteamNetworkBridge _hostBridge;

        private void Awake()
        {
            for (int i = 0; i < _playerCountButtons.Length; i++)
            {
                int count = i + MinPlayers;
                _playerCountButtons[i].onClick.AddListener(() => SetPlayerCount(count));
            }

            for (int i = 0; i < _seatToggleButtons.Length; i++)
            {
                int seat = i;
                _seatToggleButtons[i].onClick.AddListener(() => ToggleSeat(seat));
            }

            _confirmButton.onClick.AddListener(Confirm);
            _hostingBackButton.onClick.AddListener(CancelHosting);
            // Deliberately not also _root.SetActive(false) here: the overlay
            // is already saved inactive in the scene, and Show()'s own
            // SetActive(true) is what triggers this very Awake() the first
            // time (a GameObject's Awake fires on its first activation) - a
            // redundant SetActive(false) here would immediately undo that
            // same call before Show() ever got to Render(), found live as
            // the setup screen never actually appearing on its first open.
        }

        // Called by ModeSelectView instead of starting the match directly -
        // onConfirm (StartStandardMatch or StartClashMatch) only runs once
        // the host actually confirms their setup here (and, if a Remote seat
        // is chosen, only once a real peer has connected - see Confirm).
        public void Show(Action onConfirm)
        {
            _onConfirm = onConfirm;
            _root.SetActive(true);
            _hostingStatusPanel.SetActive(false);
            Render();
            // Unconditional: ModeSelectOverlay's button is still active at
            // this exact moment (its own deactivation is driven by
            // ModeSelectView's separate StateChanged subscription, which
            // hasn't fired yet), so ClaimIfInvalid's guard would wrongly
            // refuse to move focus here.
            UiFocus.Claim(_playerCountButtons[0]);
        }

        private void SetPlayerCount(int count)
        {
            _playerCount = Mathf.Clamp(count, MinPlayers, MaxPlayers);
            Render();
        }

        // Cycles Ai -> LocalHuman -> RemoteHuman -> Ai. At most one Remote
        // seat is allowed this slice - SteamNetworkBridge is direct 2-peer
        // only, so toggling a second seat to Remote instead cycles the
        // *previous* Remote seat back to Ai first, keeping the UI unable to
        // express a configuration the transport can't support.
        private void ToggleSeat(int seat)
        {
            SeatControl next = _seatControl[seat] switch
            {
                SeatControl.Ai => SeatControl.LocalHuman,
                SeatControl.LocalHuman => SeatControl.RemoteHuman,
                _ => SeatControl.Ai,
            };

            if (next == SeatControl.RemoteHuman)
            {
                for (int i = 0; i < _seatControl.Length; i++)
                {
                    if (i != seat && _seatControl[i] == SeatControl.RemoteHuman)
                    {
                        _seatControl[i] = SeatControl.Ai;
                    }
                }
            }

            _seatControl[seat] = next;
            Render();
        }

        private void Render()
        {
            for (int i = 0; i < _seatRows.Length; i++)
            {
                _seatRows[i].SetActive(i < _playerCount);
            }

            for (int i = 0; i < _seatToggleLabels.Length; i++)
            {
                string label = _seatControl[i] switch
                {
                    SeatControl.LocalHuman => "Human (Local)",
                    SeatControl.RemoteHuman => "Human (Remote)",
                    _ => "AI",
                };
                _seatToggleLabels[i].text = $"Player {i + 1}: {label}";
            }
        }

        private void Confirm()
        {
            bool hasRemoteSeat = false;
            for (int i = 0; i < _playerCount; i++)
            {
                if (_seatControl[i] == SeatControl.RemoteHuman)
                {
                    hasRemoteSeat = true;
                    break;
                }
            }

            if (!hasRemoteSeat)
            {
                ConfigureAndStart();
                _root.SetActive(false);
                _onConfirm?.Invoke();
                return;
            }

            StartHosting();
        }

        private void ConfigureAndStart()
        {
            var slots = new SeatControl[_playerCount];
            Array.Copy(_seatControl, slots, _playerCount);
            _controller.ConfigureMatch(_playerCount, slots);
        }

        private void StartHosting()
        {
            if (!_controller.HostNetworkMatch())
            {
                _hostingStatusText.text = "Could not start hosting - is Steam running?";
                _root.SetActive(false);
                _hostingStatusPanel.SetActive(true);
                UiFocus.ClaimIfInvalid(_hostingBackButton);
                return;
            }

            ConfigureAndStart();

            _hostBridge = (SteamNetworkBridge)_controller.Bridge;
            _hostBridge.PeerConnected += OnPeerConnected;

            _hostingStatusText.text = "Hosting - your Steam ID: " + Steamworks.SteamClient.SteamId.Value
                + "\nShare this with your remote player. Waiting for them to connect...";
            _root.SetActive(false);
            _hostingStatusPanel.SetActive(true);
            UiFocus.ClaimIfInvalid(_hostingBackButton);
        }

        private void OnPeerConnected()
        {
            _hostBridge.PeerConnected -= OnPeerConnected;
            _hostBridge = null;

            _hostingStatusPanel.SetActive(false);
            _onConfirm?.Invoke();
        }

        // Lets the host back out of a failed or abandoned hosting attempt
        // rather than being stuck on the waiting panel indefinitely.
        private void CancelHosting()
        {
            if (_hostBridge is not null)
            {
                _hostBridge.PeerConnected -= OnPeerConnected;
                _hostBridge = null;
            }

            _hostingStatusPanel.SetActive(false);
            _root.SetActive(true);
            Render();
            UiFocus.ClaimIfInvalid(_playerCountButtons[0]);
        }
    }
}
