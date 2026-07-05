using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // Visible only between rounds (GameSessionController.AwaitingTurnStart);
    // clicking it is what actually draws and rolls the round's pool.
    public sealed class StartTurnButtonView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _root;

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            _button.onClick.AddListener(OnClicked);
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked() => _controller.StartTurn();

        private void Render()
        {
            if (_controller.State is null)
            {
                return;
            }

            bool awaitingTurnStart = _controller.AwaitingTurnStart;
            _root.SetActive(awaitingTurnStart);

            if (awaitingTurnStart)
            {
                UiFocus.ClaimIfInvalid(_button);
            }
        }
    }
}
