using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private CellButton _cellButtonPrefab;
        [SerializeField] private Transform _container;

        // Matches GameSessionController's own HumanPlayerIndex (private there,
        // so not directly shared) - index 0 was already implicitly "the human"
        // before this view could show any other player; ShowPlayer makes that
        // explicit instead of hardcoding it into Render.
        private const int HumanPlayerIndex = 0;

        // Serialized (not just settable via ShowPlayer) so each BoardView
        // instance placed in the scene - one per player, shown simultaneously
        // - keeps its own assigned player across a scene save/reload. A
        // private, non-serialized default here was a real bug found live:
        // every BoardView instance silently rendered player 0's board because
        // nothing had ever actually changed this value away from its default.
        [SerializeField] private int _playerIndex = HumanPlayerIndex;

        private readonly CellButton[,] _cells = new CellButton[Board.Rows, Board.Columns];

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
        }

        // Switches which player's board this view displays (e.g. wired to
        // "Your Board" / "AI Board" toggle buttons) and re-renders immediately.
        // Viewing anyone other than the human is automatically read-only - see
        // IsLegalTarget - since it wouldn't make sense to place the human's
        // drafted dice onto an opponent's board.
        public void ShowPlayer(int playerIndex)
        {
            _playerIndex = playerIndex;
            Render();
        }

        private void Render()
        {
            // GameSessionController.Awake() may run after this OnEnable (Unity does
            // not guarantee cross-GameObject Awake/OnEnable order); bail until State
            // exists, and rely on the StateChanged event fired at the end of its Awake.
            if (_controller.State is null || _playerIndex >= _controller.State.Players.Count)
            {
                return;
            }

            var board = _controller.State.Players[_playerIndex].Board;

            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    if (_cells[r, c] == null)
                    {
                        var cell = Instantiate(_cellButtonPrefab, _container);
                        cell.Initialize(r, c, OnCellClicked);
                        _cells[r, c] = cell;
                    }

                    var cellButton = _cells[r, c];
                    var cellDef = board.CellAt(r, c);
                    var die = board.DieAt(r, c);

                    cellButton.SetLabel(DescribeCell(cellDef, die));
                    cellButton.SetColor(die is not null ? new Color(0.3f, 0.3f, 0.3f) : ColorForCell(cellDef));
                    cellButton.SetInteractable(IsLegalTarget(board, r, c, die));
                }
            }
        }

        private bool IsLegalTarget(Board board, int row, int col, Die existingDie)
        {
            if (_playerIndex != HumanPlayerIndex || !_controller.IsHumanTurn || existingDie is not null || _controller.ArmedDie is not Die armed)
            {
                return false;
            }

            return Legality.IsLegalPlacement(board, new Placement(row, col, armed)).IsLegal;
        }

        private void OnCellClicked(int row, int col) => _controller.ConfirmPlacement(row, col);

        private static string DescribeCell(Cell cell, Die die)
        {
            string cellLabel = cell switch
            {
                Cell.ElementCell e => e.Element.ToString(),
                Cell.BandCell b => b.Band.ToString(),
                _ => "Wild",
            };

            return die is Die d ? $"{cellLabel}\n{d.Element} {d.Face}" : cellLabel;
        }

        private static Color ColorForCell(Cell cell) => cell switch
        {
            Cell.ElementCell => new Color(0.5f, 0.4f, 0.2f),
            Cell.BandCell => new Color(0.2f, 0.3f, 0.5f),
            _ => new Color(0.35f, 0.35f, 0.35f),
        };
    }
}
