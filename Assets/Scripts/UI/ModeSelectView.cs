using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // Explicitly Show()n by TitleScreenView's "Play" button, not auto-shown -
    // State stays null through the *entire* pre-match flow (title screen,
    // mode-select, player-setup), not just at launch, so "show when State is
    // null" would compete with the title screen for the same condition.
    // Still auto-*hides* itself the instant a match starts. "Clash
    // (experimental)" starts a real Clash match using ClashConfig.Default
    // as-is - the honest way to expose that its balance values are
    // provisional and untuned (docs/clash.md SS2.4), not something this
    // scaffold decided was good. Lives on a stable, always-active parent
    // (not on the overlay it toggles) - see ScorePanel's fix in C5 for why
    // that matters.
    public sealed class ModeSelectView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _standardButton;
        [SerializeField] private Button _clashButton;
        [SerializeField] private PlayerSetupView _playerSetupView;
        [SerializeField] private Button _joinNetworkButton;
        [SerializeField] private JoinNetworkMatchView _joinNetworkMatchView;

        // Visible only when a save exists (M5 DoD: "save/resume works").
        [SerializeField] private Button _continueButton;

        private void OnEnable()
        {
            _controller.StateChanged += OnStateChanged;
            _standardButton.onClick.AddListener(OnStandardClicked);
            _clashButton.onClick.AddListener(OnClashClicked);
            _joinNetworkButton.onClick.AddListener(OnJoinNetworkClicked);
            _continueButton.onClick.AddListener(OnContinueClicked);
            RefreshContinueButton();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= OnStateChanged;
            _standardButton.onClick.RemoveListener(OnStandardClicked);
            _clashButton.onClick.RemoveListener(OnClashClicked);
            _joinNetworkButton.onClick.RemoveListener(OnJoinNetworkClicked);
            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        // Called by TitleScreenView's "Play" button.
        public void Show()
        {
            _root.SetActive(true);
            RefreshContinueButton();
            UiFocus.Claim(_standardButton);
        }

        // Neither mode starts the match directly anymore - the host picks a
        // player count (2-4) and Human/AI per seat first (all-AI included),
        // and only PlayerSetupView's own Confirm actually calls
        // StartStandardMatch/StartClashMatch.
        private void OnStandardClicked() => _playerSetupView.Show(_controller.StartStandardMatch);

        private void OnClashClicked() => _playerSetupView.Show(_controller.StartClashMatch);

        // The joining player never sees PlayerSetupView - they aren't
        // configuring seats, only connecting to a host who already did.
        private void OnJoinNetworkClicked() => _joinNetworkMatchView.Show();

        // Skips PlayerSetupView entirely - the seat configuration is already
        // baked into the save.
        private void OnContinueClicked() => _controller.LoadGame();

        // Only ever hides itself once a match starts - showing is only ever
        // triggered explicitly via Show() (TitleScreenView's Play button).
        private void OnStateChanged()
        {
            if (_controller.State is not null)
            {
                _root.SetActive(false);
                return;
            }

            RefreshContinueButton();
        }

        private void RefreshContinueButton() => _continueButton.gameObject.SetActive(_controller.HasSavedGame);
    }
}
