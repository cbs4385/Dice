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

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            _standardButton.onClick.AddListener(OnStandardClicked);
            _clashButton.onClick.AddListener(OnClashClicked);
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
            _standardButton.onClick.RemoveListener(OnStandardClicked);
            _clashButton.onClick.RemoveListener(OnClashClicked);
        }

        // Neither mode starts the match directly anymore - the host picks a
        // player count (2-4) and Human/AI per seat first (all-AI included),
        // and only PlayerSetupView's own Confirm actually calls
        // StartStandardMatch/StartClashMatch.
        private void OnStandardClicked() => _playerSetupView.Show(_controller.StartStandardMatch);

        private void OnClashClicked() => _playerSetupView.Show(_controller.StartClashMatch);

        private void Render() => _root.SetActive(_controller.State is null);
    }
}
