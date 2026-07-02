using TMPro;
using UnityEngine;

namespace Quintessence.UI
{
    // Matches wireframe 1a's "Round X/6" label in the top roll strip.
    public sealed class RoundIndicatorView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private TMP_Text _label;

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
        }

        private void Render()
        {
            if (_controller.State is null)
            {
                return;
            }

            _label.text = $"Round {_controller.State.Round}/{Quintessence.Game.GameReducer.TotalRounds}";
        }
    }
}
