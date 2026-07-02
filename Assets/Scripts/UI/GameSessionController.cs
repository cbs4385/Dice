using System;
using UnityEngine;
using Quintessence.Engine;
using Quintessence.Game;

namespace Quintessence.UI
{
    // Owns the GameState and is the only place that calls into Quintessence.Game.
    // Every other view in this folder is a dumb renderer of State + the current
    // "armed" selection, driven entirely through this controller.
    public sealed class GameSessionController : MonoBehaviour
    {
        private const int HumanPlayerIndex = 0;

        private IRng _rng;
        private IAiPolicy _aiPolicy;

        public GameState State { get; private set; }

        public DieSource? ArmedSource { get; private set; }

        public int ArmedIndex { get; private set; }

        public Die ArmedDie { get; private set; }

        public event Action StateChanged;

        public bool IsHumanTurn =>
            !State.IsGameOver && State.CurrentPhase is not null && GameReducer.CurrentPlayer(State) == HumanPlayerIndex;

        private void Awake()
        {
            _aiPolicy = new NoviceAi();
            // UI is the sanctioned composition root for wall-clock seeding - Engine/Game
            // themselves must never touch platform time (enforced by noEngineReferences).
            // Daily mode would instead inject a published fixed seed here.
            _rng = Rng.Create(DateTime.Now.Ticks);
            State = GameSetup.NewGame(2, _rng);
            AdvanceUntilHumanTurnOrGameOver();
        }

        public void ArmDie(DieSource source, int index, Die die)
        {
            if (!IsHumanTurn)
            {
                return;
            }

            ArmedSource = source;
            ArmedIndex = index;
            ArmedDie = die;
            StateChanged?.Invoke();
        }

        public void ConfirmPlacement(int row, int col)
        {
            if (!IsHumanTurn || ArmedSource is null || ArmedDie is null)
            {
                return;
            }

            var choice = new DraftChoice(ArmedSource.Value, ArmedIndex, row, col);
            State = GameReducer.ApplyDraft(State, choice, _rng);
            ArmedSource = null;
            ArmedDie = null;
            StateChanged?.Invoke();
            AdvanceUntilHumanTurnOrGameOver();
        }

        public void ForfeitHumanTurn()
        {
            if (!IsHumanTurn)
            {
                return;
            }

            State = GameReducer.ApplyForfeit(State);
            StateChanged?.Invoke();
            AdvanceUntilHumanTurnOrGameOver();
        }

        private void AdvanceUntilHumanTurnOrGameOver()
        {
            while (!State.IsGameOver)
            {
                if (State.CurrentPhase is null)
                {
                    State = GameReducer.StartRound(State, _rng);
                    continue;
                }

                if (GameReducer.CurrentPlayer(State) == HumanPlayerIndex)
                {
                    break;
                }

                // AI turns resolve instantly (GDD SS4): no input, no delay.
                var move = _aiPolicy.ChooseMove(State, _rng);
                State = move is null ? GameReducer.ApplyForfeit(State) : GameReducer.ApplyDraft(State, move, _rng);
            }

            StateChanged?.Invoke();
        }
    }
}
