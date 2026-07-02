using System;
using System.Collections;
using System.Collections.Generic;
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
        // Roughly matches the roll-cycle duration DieButton.PlayRollAnimation uses,
        // so AI turns don't start resolving mid-animation. A placeholder pacing
        // value, not a tuned "feel" decision - see AGENTS.md on feel/juice.
        public const float RollAnimationSeconds = 0.9f;

        private const int HumanPlayerIndex = 0;

        private IRng _rng;
        private IAiPolicy _aiPolicy;

        public GameState State { get; private set; }

        public DieSource? ArmedSource { get; private set; }

        public int ArmedIndex { get; private set; }

        public Die ArmedDie { get; private set; }

        public event Action StateChanged;

        // Fired once per StartTurn() call with the freshly-drawn (already-final)
        // pool, so PoolView can play its roll animation. Separate from StateChanged
        // because it should fire exactly once per round-start, not on every render.
        public event Action<IReadOnlyList<Die>> RoundStarted;

        public bool IsHumanTurn =>
            !State.IsGameOver && State.CurrentPhase is not null && GameReducer.CurrentPlayer(State) == HumanPlayerIndex;

        public bool AwaitingTurnStart => !State.IsGameOver && State.CurrentPhase is null;

        private void Awake()
        {
            _aiPolicy = new NoviceAi();
            // UI is the sanctioned composition root for wall-clock seeding - Engine/Game
            // themselves must never touch platform time (enforced by noEngineReferences).
            // Daily mode would instead inject a published fixed seed here.
            _rng = Rng.Create(DateTime.Now.Ticks);
            State = GameSetup.NewGame(2, _rng);
            StateChanged?.Invoke();
        }

        public void StartTurn()
        {
            if (!AwaitingTurnStart)
            {
                return;
            }

            State = GameReducer.StartRound(State, _rng);
            StateChanged?.Invoke();
            RoundStarted?.Invoke(State.CurrentPhase.Pool);
            StartCoroutine(RollThenAdvance());
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
            AdvanceAiTurnsUntilHumanTurnOrRoundEnd();
        }

        public void ForfeitHumanTurn()
        {
            if (!IsHumanTurn)
            {
                return;
            }

            State = GameReducer.ApplyForfeit(State);
            StateChanged?.Invoke();
            AdvanceAiTurnsUntilHumanTurnOrRoundEnd();
        }

        private IEnumerator RollThenAdvance()
        {
            yield return new WaitForSeconds(RollAnimationSeconds);
            AdvanceAiTurnsUntilHumanTurnOrRoundEnd();
        }

        // Only advances AI turns within an already-started round; it never starts a
        // new round itself. A round only ever starts via an explicit StartTurn().
        private void AdvanceAiTurnsUntilHumanTurnOrRoundEnd()
        {
            while (!State.IsGameOver
                && State.CurrentPhase is not null
                && GameReducer.CurrentPlayer(State) != HumanPlayerIndex)
            {
                // AI turns resolve instantly (GDD SS4): no input, no delay.
                var move = _aiPolicy.ChooseMove(State, _rng);
                State = move is null ? GameReducer.ApplyForfeit(State) : GameReducer.ApplyDraft(State, move, _rng);
            }

            StateChanged?.Invoke();
        }
    }
}
