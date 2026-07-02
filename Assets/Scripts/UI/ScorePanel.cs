using System.Text;
using TMPro;
using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI
{
    public sealed class ScorePanel : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _scoreText;

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
            _root.SetActive(state.IsGameOver);
            if (!state.IsGameOver)
            {
                return;
            }

            var sb = new StringBuilder("Game Over\n");
            for (int i = 0; i < state.Players.Count; i++)
            {
                var player = state.Players[i];
                int score = Scoring.ScoreBoard(player.Board, state.Objective, player.PrivateElement, player.FavorRemaining);
                sb.Append(i == 0 ? "You: " : "AI: ").Append(score).Append('\n');
            }

            _scoreText.text = sb.ToString();
        }
    }
}
