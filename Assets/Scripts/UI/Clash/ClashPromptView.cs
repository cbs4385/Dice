using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI.Clash
{
    // Blocks the board/pool (via GameSessionController.IsHumanTurn) until answered.
    // Plain text + two buttons - no drama/urgency feel, which is a human-gated
    // decision (docs/clash.md SS5: "Scorch's gut-punch" is exactly this prompt's
    // eventual feel pass, not this scaffold's job).
    public sealed class ClashPromptView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Button _wardButton;
        [SerializeField] private Button _declineButton;

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            _wardButton.onClick.AddListener(OnWardClicked);
            _declineButton.onClick.AddListener(OnDeclineClicked);
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
            _wardButton.onClick.RemoveListener(OnWardClicked);
            _declineButton.onClick.RemoveListener(OnDeclineClicked);
        }

        private void OnWardClicked() => _controller.RespondWard();

        private void OnDeclineClicked() => _controller.RespondDeclineWard();

        private void Render()
        {
            if (_controller.State is null)
            {
                return;
            }

            bool visible = _controller.HumanHasPendingResponse;
            _root.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var pending = _controller.State.Clash.Pending;
            int wardCost = _controller.State.Clash.Config.WardCost;
            _text.text = $"P{pending.Actor + 1} used {pending.Kind} against you.\nWard for {wardCost} favor, or decline?";
        }
    }
}
