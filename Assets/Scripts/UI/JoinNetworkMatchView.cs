using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

namespace Quintessence.UI
{
    // A separate entry point from PlayerSetupView - the joining player never
    // configures seats (that's the host's job via PlayerSetupView), they
    // only connect to an already-hosted match via a manually-shared Steam
    // ID (no lobby/invite system yet - see the plan's Deferred section).
    // Shown from ModeSelectView's own "Join Network Game" button.
    public sealed class JoinNetworkMatchView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_InputField _steamIdInput;
        [SerializeField] private Button _connectButton;
        [SerializeField] private TMP_Text _statusText;

        private void Awake()
        {
            _connectButton.onClick.AddListener(Connect);
        }

        private void OnEnable()
        {
            _controller.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            _controller.StateChanged -= OnStateChanged;
        }

        public void Show()
        {
            _statusText.text = string.Empty;
            _root.SetActive(true);
        }

        private void Connect()
        {
            if (!ulong.TryParse(_steamIdInput.text.Trim(), out ulong rawId))
            {
                _statusText.text = "Enter a valid Steam ID (numbers only).";
                return;
            }

            if (!_controller.JoinNetworkMatch(new SteamId { Value = rawId }))
            {
                _statusText.text = "Could not connect - is Steam running?";
                return;
            }

            // Waits for the host's MatchStart to arrive - see
            // OnStateChanged, which is what actually hides this panel once
            // State stops being null.
            _statusText.text = "Connected - waiting for host to start...";
        }

        // State only ever becomes non-null once the host's MatchStart
        // action has arrived and been applied (see GameSessionController.
        // ApplyNetworkAction) - this is the signal the join succeeded and
        // the match has actually begun, not just that the socket connected.
        private void OnStateChanged()
        {
            if (_controller.State is not null)
            {
                _root.SetActive(false);
            }
        }
    }
}
