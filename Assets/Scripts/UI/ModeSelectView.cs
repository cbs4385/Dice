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

        private void OnStandardClicked() => _controller.StartStandardMatch();

        private void OnClashClicked() => _controller.StartClashMatch();

        private void Render() => _root.SetActive(_controller.State is null);
    }
}
