using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // Shown whenever no match has started yet. "Clash (experimental)" starts a
    // real Clash match using ClashConfig.Default as-is - the honest way to expose
    // that its balance values are provisional and untuned (docs/clash.md SS2.4),
    // not something this scaffold decided was good. Lives on a stable, always-
    // active parent (not on the overlay it toggles) - see ScorePanel's fix in C5
    // for why that matters.
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
            _controller.StateChanged += Render;
            _standardButton.onClick.AddListener(OnStandardClicked);
            _clashButton.onClick.AddListener(OnClashClicked);
            _joinNetworkButton.onClick.AddListener(OnJoinNetworkClicked);
            _continueButton.onClick.AddListener(OnContinueClicked);
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
            _standardButton.onClick.RemoveListener(OnStandardClicked);
            _clashButton.onClick.RemoveListener(OnClashClicked);
            _joinNetworkButton.onClick.RemoveListener(OnJoinNetworkClicked);
            _continueButton.onClick.RemoveListener(OnContinueClicked);
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

        private void Render()
        {
            bool visible = _controller.State is null;
            _root.SetActive(visible);
            _continueButton.gameObject.SetActive(_controller.HasSavedGame);

            if (visible)
            {
                UiFocus.ClaimIfInvalid(_standardButton);
            }
        }
    }
}
