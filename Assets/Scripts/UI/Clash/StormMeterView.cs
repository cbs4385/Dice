using System.Text;
using TMPro;
using UnityEngine;

namespace Quintessence.UI.Clash
{
    // Placeholder-feel text readout - a real Storm meter (bars, VFX, sound as it
    // fills) is a human-gated feel decision (docs/clash.md SS5). Hidden whenever
    // Clash is not enabled, so the shipped MainPlay.unity experience is unaffected.
    public sealed class StormMeterView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _text;

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

            var clash = _controller.State.Clash;
            _root.SetActive(clash is not null);
            if (clash is null)
            {
                return;
            }

            var sb = new StringBuilder("Storm:\n");
            for (int i = 0; i < clash.Storm.Count; i++)
            {
                sb.Append(i == 0 ? "You: " : "AI: ").Append(clash.Storm[i]).Append('/').Append(clash.Config.StormCap).Append('\n');
            }

            sb.Append("Dealt: ").Append(string.Join(", ", clash.InterventionsAvailable));
            _text.text = sb.ToString();
        }
    }
}
