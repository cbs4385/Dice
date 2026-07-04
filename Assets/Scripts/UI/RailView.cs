using System.Text;
using TMPro;
using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI
{
    // Bag counts only (not counts+odds) per the GDD SS6 stated default; the Oracle
    // overlay toggle for exact odds is not built in this slice.
    public sealed class RailView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private TMP_Text _bagCountsText;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private TMP_Text _favorText;

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

            var state = _controller.State;
            var sb = new StringBuilder("Bag:\n");
            foreach (var element in Elements.All)
            {
                int count = state.Bag.Remaining.TryGetValue(element, out var c) ? c : 0;
                sb.Append(element).Append(": ").Append(count).Append('\n');
            }

            _bagCountsText.text = sb.ToString();
            _objectiveText.text = "Objective: " + state.Objective;

            var favorSb = new StringBuilder("Favor:\n");
            for (int i = 0; i < state.Players.Count; i++)
            {
                favorSb.Append("P").Append(i + 1).Append(_controller.IsHumanSlot(i) ? " (You): " : " (AI): ").Append(state.Players[i].FavorRemaining).Append('\n');
            }

            _favorText.text = favorSb.ToString();
        }
    }
}
