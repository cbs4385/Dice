using UnityEngine;
using Quintessence.Engine;
using Quintessence.Game;

namespace Quintessence.UI
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private CellButton _cellButtonPrefab;
        [SerializeField] private Transform _container;

        // Serialized (not just settable via ShowPlayer) so each BoardView
        // instance placed in the scene - one per player, shown simultaneously
        // - keeps its own assigned player across a scene save/reload. A
        // private, non-serialized default here was a real bug found live:
        // every BoardView instance silently rendered player 0's board because
        // nothing had ever actually changed this value away from its default.
        [SerializeField] private int _playerIndex;

        private readonly CellButton[,] _cells = new CellButton[Board.Rows, Board.Columns];

        // Edge-triggered, not level-triggered: claiming focus every render
        // while a die stays armed would yank the player back to the first
        // legal cell any time something else causes a re-render (e.g. an AI
        // turn elsewhere), undoing their own board navigation. Only the
        // render where ArmedDie first becomes non-null should move focus.
        private bool _wasArmedLastRender;

        // For instances built at runtime (MultiBoardLayoutController, one
        // section per player, 2-4 of them) rather than wired in the Inspector
        // - must be called while this GameObject is still inactive, since
        // OnEnable below needs _controller already set the moment it fires.
        public void Configure(GameSessionController controller, CellButton cellButtonPrefab, Transform container, int playerIndex)
        {
            _controller = controller;
            _cellButtonPrefab = cellButtonPrefab;
            _container = container;
            _playerIndex = playerIndex;
        }

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
        // Viewing anyone other than the seat currently acting is automatically
        // read-only - see IsLegalTarget - since it wouldn't make sense to place
        // the current drafter's dice onto a different player's board.
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
            CellButton firstLegal = null;

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
                    bool legal = IsLegalTarget(board, r, c, die);

                    cellButton.SetLabel(DescribeCell(cellDef, die));
                    cellButton.SetColor(die is not null ? new Color(0.3f, 0.3f, 0.3f) : ColorForCell(cellDef));
                    cellButton.SetInteractable(legal);

                    if (legal && firstLegal is null)
                    {
                        firstLegal = cellButton;
                    }
                }
            }

            // Once a die is armed, the board (this player's own, if it's
            // theirs to act on) is the next keyboard/controller region to
            // move to - without this there's no path from the pool/Firmament
            // row to the board at all, since MultiBoardLayoutController spawns
            // one BoardView per player and Automatic navigation isn't
            // guaranteed to bridge to the right one. Unconditional (not
            // ClaimIfInvalid): the pool/Firmament button that was just
            // submitted to arm this die is still active/interactable by
            // design (so the player can re-arm a different die later), so
            // the "don't steal a still-valid selection" guard would
            // otherwise never let this transition happen at all.
            bool isArmedNow = _controller.ArmedDie is not null;
            if (firstLegal is not null && isArmedNow && !_wasArmedLastRender)
            {
                UiFocus.Claim(firstLegal.Button);
            }

            _wasArmedLastRender = isArmedNow;
        }

        private bool IsLegalTarget(Board board, int row, int col, Die existingDie)
        {
            // In hotseat with multiple human seats, only whichever seat's
            // board matches the seat currently acting is ever clickable - not
            // just "the" human seat, since there may be several.
            // IsHumanTurn must be checked before CurrentPlayer - it's the
            // thing guaranteeing a phase is actually in progress; calling
            // CurrentPlayer first threw "No phase in progress" on every
            // render before the first round ever starts (found live: every
            // PlayMode test failed at SetUp).
            if (!_controller.IsHumanTurn || _playerIndex != GameReducer.CurrentPlayer(_controller.State)
                || existingDie is not null || _controller.ArmedDie is not Die armed)
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
