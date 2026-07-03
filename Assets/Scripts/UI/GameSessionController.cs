using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quintessence.Engine;
using Quintessence.Game;
using Quintessence.Game.Clash;

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

        // Testing seam, NOT mode wiring (docs/clash.md C6 is human-gated and not
        // started): stays false in the shipped MainPlay.unity, so the real game is
        // byte-for-byte unaffected. Only ClashPlayTest.unity (a PlayMode test
        // fixture, not in Build Settings) flips this to true.
        [SerializeField] private bool _enableClashForTesting;
        [SerializeField] private int _testClashInterventionCost = ClashConfig.Default.InterventionCost;

        private IRng _rng;
        private IAiPolicy _aiPolicy;
        private ClashAiPolicy _clashAiPolicy;

        public GameState State { get; private set; }

        public DieSource? ArmedSource { get; private set; }

        public int ArmedIndex { get; private set; }

        public Die ArmedDie { get; private set; }

        public event Action StateChanged;

        // Fired once per StartTurn() call with the freshly-drawn (already-final)
        // pool, so PoolView can play its roll animation. Separate from StateChanged
        // because it should fire exactly once per round-start, not on every render.
        public event Action<IReadOnlyList<Die>> RoundStarted;

        // State.Clash?.Pending is always null for non-Clash games, so this extra
        // condition is a provable no-op there - it only ever matters once Clash is
        // enabled, where it correctly freezes drafting until a Ward/Decline prompt
        // targeting the human is answered.
        public bool IsHumanTurn =>
            !State.IsGameOver && State.CurrentPhase is not null
            && GameReducer.CurrentPlayer(State) == HumanPlayerIndex
            && State.Clash?.Pending is null;

        public bool AwaitingTurnStart => !State.IsGameOver && State.CurrentPhase is null;

        public bool HumanHasPendingResponse => State.Clash?.Pending?.Target == HumanPlayerIndex;

        private void Awake()
        {
            _aiPolicy = new NoviceAi();
            _clashAiPolicy = new ClashAiPolicy(_aiPolicy);
            // UI is the sanctioned composition root for wall-clock seeding - Engine/Game
            // themselves must never touch platform time (enforced by noEngineReferences).
            // Daily mode would instead inject a published fixed seed here.
            _rng = Rng.Create(DateTime.Now.Ticks);
            ClashConfig clashConfig = _enableClashForTesting
                ? ClashConfig.Default with { InterventionCost = _testClashInterventionCost }
                : null;
            State = GameSetup.NewGame(2, _rng, clashConfig: clashConfig);
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

        public void DeclareIntervention(InterventionKind kind, InterventionParams parameters)
        {
            if (!IsHumanTurn)
            {
                return;
            }

            State = ClashReducer.DeclareIntervention(State, HumanPlayerIndex, kind, parameters, _rng);
            StateChanged?.Invoke();
            AdvanceAiTurnsUntilHumanTurnOrRoundEnd();
        }

        public void RespondWard()
        {
            if (!HumanHasPendingResponse)
            {
                return;
            }

            State = ClashReducer.Ward(State, HumanPlayerIndex);
            StateChanged?.Invoke();
            AdvanceAiTurnsUntilHumanTurnOrRoundEnd();
        }

        public void RespondDeclineWard()
        {
            if (!HumanHasPendingResponse)
            {
                return;
            }

            State = ClashReducer.DeclineWard(State, HumanPlayerIndex);
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
        // When State.Clash is null every new branch below is unreachable (the
        // `State.Clash is not null` / `pending is not null` guards short-circuit
        // first), so this collapses to exactly the pre-Clash loop for every
        // non-Clash game - the same gated-no-op discipline C0-C4 already proved out.
        private void AdvanceAiTurnsUntilHumanTurnOrRoundEnd()
        {
            while (!State.IsGameOver)
            {
                var pending = State.Clash?.Pending;
                if (pending is not null)
                {
                    if (pending.Target == HumanPlayerIndex)
                    {
                        break; // wait for the human to answer via ClashPromptView.
                    }

                    State = _clashAiPolicy.ShouldWard(State, pending.Target)
                        ? ClashReducer.Ward(State, pending.Target)
                        : ClashReducer.DeclineWard(State, pending.Target);
                    continue;
                }

                if (State.CurrentPhase is null || GameReducer.CurrentPlayer(State) == HumanPlayerIndex)
                {
                    break;
                }

                int currentPlayer = GameReducer.CurrentPlayer(State);

                // Mirrors ClashSelfPlay's pattern: occasionally declare before
                // drafting, rather than always doing so the instant it's affordable.
                if (State.Clash is not null && _rng.NextInt(4) == 0)
                {
                    var declaration = _clashAiPolicy.ChooseIntervention(State, currentPlayer, _rng);
                    if (declaration is not null)
                    {
                        State = ClashReducer.DeclareIntervention(State, currentPlayer, declaration.Value.Kind, declaration.Value.Params, _rng);
                        continue;
                    }
                }

                // AI turns resolve instantly (GDD SS4): no input, no delay.
                var move = _aiPolicy.ChooseMove(State, _rng);
                State = move is null ? GameReducer.ApplyForfeit(State) : GameReducer.ApplyDraft(State, move, _rng);
            }

            StateChanged?.Invoke();
        }
    }
}
