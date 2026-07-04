using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Quintessence.Engine;

namespace Quintessence.UI
{
    // Builds one labeled board section per player, stacked vertically inside
    // _boardPanel, once the match's actual player count is known - replaces
    // two fixed board sections that only ever supported exactly 2 players.
    // Rebuilds from scratch whenever the player count changes (e.g. a fresh
    // match after returning to mode-select with a different player count).
    public sealed class MultiBoardLayoutController : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private RectTransform _boardPanel;
        [SerializeField] private CellButton _cellButtonPrefab;

        private int _builtForPlayerCount = -1;

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
            if (_controller.State is null || _controller.State.Players.Count == _builtForPlayerCount)
            {
                return;
            }

            BuildSections(_controller.State.Players.Count);
        }

        private void BuildSections(int playerCount)
        {
            for (int i = _boardPanel.childCount - 1; i >= 0; i--)
            {
                Destroy(_boardPanel.GetChild(i).gameObject);
            }

            float sectionHeight = 1f / playerCount;
            for (int i = 0; i < playerCount; i++)
            {
                BuildSection(i, playerCount, sectionHeight);
            }

            _builtForPlayerCount = playerCount;
        }

        private void BuildSection(int playerIndex, int playerCount, float sectionHeight)
        {
            // Stacked top-to-bottom in player order: player 0's section sits
            // at the top of the panel, the highest-index player at the bottom.
            float top = 1f - playerIndex * sectionHeight;
            float bottom = top - sectionHeight;

            var section = new GameObject($"BoardSection{playerIndex}", typeof(RectTransform));
            var sectionRt = (RectTransform)section.transform;
            sectionRt.SetParent(_boardPanel, false);
            sectionRt.anchorMin = new Vector2(0f, bottom);
            sectionRt.anchorMax = new Vector2(1f, top);
            sectionRt.offsetMin = Vector2.zero;
            sectionRt.offsetMax = Vector2.zero;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(sectionRt, false);
            labelRt.anchorMin = new Vector2(0f, 0.9f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = $"Player {playerIndex + 1}" + (_controller.IsHumanSlot(playerIndex) ? " (You)" : " (AI)");
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20;
            label.color = Color.white;

            // Player 0's container keeps the plain "BoardContainer" name -
            // existing PlayMode tests (and other UI code) look it up by that
            // exact name expecting the human's board; every other seat gets a
            // distinct suffixed name so GameObject.Find stays unambiguous.
            string containerName = playerIndex == 0 ? "BoardContainer" : $"BoardContainer{playerIndex}";
            var boardContainerGo = new GameObject(containerName, typeof(RectTransform));
            var containerRt = (RectTransform)boardContainerGo.transform;
            containerRt.SetParent(sectionRt, false);
            containerRt.anchorMin = new Vector2(0f, 0f);
            containerRt.anchorMax = new Vector2(1f, 0.9f);
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;

            var grid = boardContainerGo.AddComponent<GridLayoutGroup>();
            // Cell height scaled down as more players share the panel, so a
            // 4-player game's four quarter-height sections don't overflow -
            // width stays fixed since the board is always 4 columns wide
            // regardless of player count.
            grid.cellSize = new Vector2(80f, 55f * (2f / playerCount));
            grid.spacing = new Vector2(4f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Board.Columns;

            boardContainerGo.SetActive(false);
            var boardView = boardContainerGo.AddComponent<BoardView>();
            boardView.Configure(_controller, _cellButtonPrefab, containerRt, playerIndex);
            boardContainerGo.SetActive(true);
        }
    }
}
