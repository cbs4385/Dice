using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // Host-facing player setup, shown after picking a mode from
    // ModeSelectView and before the match actually starts: pick a player
    // count (2-4) and Human or AI per seat - all-AI is a valid configuration
    // (the host just watches). A deliberately plain, structural placeholder;
    // real setup-screen feel (art, layout polish) is human-gated.
    public sealed class PlayerSetupView : MonoBehaviour
    {
        private const int MinPlayers = 2;
        private const int MaxPlayers = 4;

        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;

        // Index i is the button/label for player count (i + MinPlayers).
        [SerializeField] private Button[] _playerCountButtons;

        // Index i is seat i (0-3). Rows beyond the current player count are
        // hidden; toggle buttons cycle Human/AI on click.
        [SerializeField] private GameObject[] _seatRows;
        [SerializeField] private TMP_Text[] _seatToggleLabels;
        [SerializeField] private Button[] _seatToggleButtons;

        [SerializeField] private Button _confirmButton;

        private int _playerCount = MinPlayers;
        private readonly bool[] _isHumanSlot = { true, false, false, false };
        private Action _onConfirm;

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
        // the host actually confirms their setup here.
        public void Show(Action onConfirm)
        {
            _onConfirm = onConfirm;
            _root.SetActive(true);
            Render();
        }

        private void SetPlayerCount(int count)
        {
            _playerCount = Mathf.Clamp(count, MinPlayers, MaxPlayers);
            Render();
        }

        private void ToggleSeat(int seat)
        {
            _isHumanSlot[seat] = !_isHumanSlot[seat];
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
                _seatToggleLabels[i].text = $"Player {i + 1}: " + (_isHumanSlot[i] ? "Human" : "AI");
            }
        }

        private void Confirm()
        {
            var slots = new bool[_playerCount];
            Array.Copy(_isHumanSlot, slots, _playerCount);
            _controller.ConfigureMatch(_playerCount, slots);
            _root.SetActive(false);
            _onConfirm?.Invoke();
        }
    }
}
