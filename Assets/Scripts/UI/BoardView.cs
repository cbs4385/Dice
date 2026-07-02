using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private CellButton _cellButtonPrefab;
        [SerializeField] private Transform _container;

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

        private void Render()
        {
            // GameSessionController.Awake() may run after this OnEnable (Unity does
            // not guarantee cross-GameObject Awake/OnEnable order); bail until State
            // exists, and rely on the StateChanged event fired at the end of its Awake.
            if (_controller.State is null)
            {
                return;
            }

            // Slice 1 is human (seat 0) vs one AI - always show the human's board.
            var board = _controller.State.Players[0].Board;

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
            if (!_controller.IsHumanTurn || existingDie is not null || _controller.ArmedDie is not Die armed)
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
