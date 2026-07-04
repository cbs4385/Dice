using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using Quintessence.Game;
using Quintessence.UI;

namespace Quintessence.UI.Tests
{
    // Loads ClashPlayTest.unity - a PlayMode-only test fixture (not in Build
    // Settings) that is a duplicate of the real MainPlay.unity with one
    // GameSessionController field (_enableClashForTesting) flipped to true and a
    // lowered test-only InterventionCost, so these tests exercise the real Clash
    // UI wiring in a live scene without making Clash reachable in the shipped
    // game (see MainPlaySceneInteractionTests' own comment for the same
    // real-scene-over-synthetic-rebuild rationale).
    public class ClashPlaySceneInteractionTests
    {
        private const string ScenePath = "Assets/Scenes/ClashPlayTest.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            GameSessionController controller = null;
            for (int i = 0; i < 60 && controller?.State == null; i++)
            {
                yield return null;
                GameObject sessionGo = GameObject.Find("GameSession");
                controller = sessionGo != null ? sessionGo.GetComponent<GameSessionController>() : null;
            }

            Assert.That(controller, Is.Not.Null, "GameSession not found after scene load");
            Assert.That(controller.State, Is.Not.Null, "GameSessionController.State not initialized in time");
            Assert.That(controller.State.Clash, Is.Not.Null, "ClashPlayTest.unity must have Clash enabled");
        }

        // A fixed WaitForSeconds(RollAnimationSeconds + buffer) was flaky on a
        // slower CI runner (found live: MainPlaySceneInteractionTests' own copy
        // of this helper hit "Transform child out of bounds" on PoolContainer in
        // CI, never reproduced locally) - a slow enough frame can push the
        // coroutine's actual finish past a fixed buffer regardless of its size.
        // Polling the controller's own completion flag instead has no timing
        // assumption to get wrong.
        private static IEnumerator StartTurnAndWaitForPool()
        {
            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            GameObject.Find("StartTurnButton").GetComponent<Button>().onClick.Invoke();
            int guard = 0;
            while (controller.IsRollInProgress && guard < 1200)
            {
                guard++;
                yield return null;
            }
        }

        // The AI can also declare an intervention against the human during its own
        // turn (likely here given the test scene's lowered InterventionCost) - when
        // that happens, control returns to the human as a pending prompt rather
        // than a normal turn. Declining it is a legitimate, real response (covered
        // on its own by other assertions in this file), used here purely to reach
        // a normal IsHumanTurn state so picker/turn-flow tests aren't flaky.
        private static IEnumerator ResolveAnyPromptsAgainstHuman(GameSessionController controller)
        {
            int guard = 0;
            while (controller.HumanHasPendingResponse && guard < 20)
            {
                guard++;
                controller.RespondDeclineWard();
                yield return null;
            }
        }

        // Finds the human's own band cell whose band the given die's face already
        // satisfies, so a single placement is guaranteed to charge Storm.
        private static bool TryFindInBandCell(GameSessionController controller, Quintessence.Engine.Die die, out int row, out int col)
        {
            var board = controller.State.Players[0].Board;
            for (int r = 0; r < Quintessence.Engine.Board.Rows; r++)
            {
                for (int c = 0; c < Quintessence.Engine.Board.Columns; c++)
                {
                    if (board.CellAt(r, c) is Quintessence.Engine.Cell.BandCell band
                        && Quintessence.Engine.Bands.Of(die.Face) == band.Band
                        && Quintessence.Engine.Legality.IsLegalPlacement(board, new Quintessence.Engine.Placement(r, c, die)).IsLegal)
                    {
                        row = r;
                        col = c;
                        return true;
                    }
                }
            }

            row = col = -1;
            return false;
        }

        [UnityTest]
        public IEnumerator StormMeter_ReflectsAnInBandPlacement()
        {
            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            yield return StartTurnAndWaitForPool();

            var pool = controller.State.CurrentPhase.Pool;
            int poolIndex = -1;
            int row = -1, col = -1;
            for (int i = 0; i < pool.Count && poolIndex < 0; i++)
            {
                if (TryFindInBandCell(controller, pool[i], out row, out col))
                {
                    poolIndex = i;
                }
            }

            Assert.That(poolIndex, Is.GreaterThanOrEqualTo(0), "expected at least one pool die to fit some band cell");

            GameObject.Find("PoolContainer").transform.GetChild(poolIndex).GetComponent<Button>().onClick.Invoke();
            GameObject.Find("BoardContainer").transform.GetChild(row * Quintessence.Engine.Board.Columns + col).GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(controller.State.Clash.Storm[0], Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InterventionPicker_HiddenUntilAffordable_ThenListsRealCandidates()
        {
            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            var clashSection = GameObject.Find("ClashSection");
            var pickerRoot = clashSection.transform.Find("InterventionPickerRoot").gameObject;

            yield return StartTurnAndWaitForPool();
            Assert.That(pickerRoot.activeSelf, Is.False, "storm starts at 0, nothing should be affordable yet");

            var pool = controller.State.CurrentPhase.Pool;
            int poolIndex = -1;
            int row = -1, col = -1;
            for (int i = 0; i < pool.Count && poolIndex < 0; i++)
            {
                if (TryFindInBandCell(controller, pool[i], out row, out col))
                {
                    poolIndex = i;
                }
            }

            GameObject.Find("PoolContainer").transform.GetChild(poolIndex).GetComponent<Button>().onClick.Invoke();
            GameObject.Find("BoardContainer").transform.GetChild(row * Quintessence.Engine.Board.Columns + col).GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return ResolveAnyPromptsAgainstHuman(controller);

            Assert.That(controller.IsHumanTurn, Is.True, "test scene's snake draft should return to the human within the same round");
            Assert.That(pickerRoot.activeSelf, Is.True, "storm now meets the test scene's lowered InterventionCost");

            pickerRoot.transform.Find("OpenButton").GetComponent<Button>().onClick.Invoke();
            var candidateListRoot = pickerRoot.transform.Find("CandidateListRoot");
            Assert.That(candidateListRoot.gameObject.activeSelf, Is.True);
            Assert.That(candidateListRoot.childCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DeclaringAgainstTheAi_AutoResolves_AndPlayContinues()
        {
            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            var clashSection = GameObject.Find("ClashSection");
            var pickerRoot = clashSection.transform.Find("InterventionPickerRoot").gameObject;

            yield return StartTurnAndWaitForPool();
            var pool = controller.State.CurrentPhase.Pool;
            int poolIndex = -1;
            int row = -1, col = -1;
            for (int i = 0; i < pool.Count && poolIndex < 0; i++)
            {
                if (TryFindInBandCell(controller, pool[i], out row, out col))
                {
                    poolIndex = i;
                }
            }

            GameObject.Find("PoolContainer").transform.GetChild(poolIndex).GetComponent<Button>().onClick.Invoke();
            GameObject.Find("BoardContainer").transform.GetChild(row * Quintessence.Engine.Board.Columns + col).GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return ResolveAnyPromptsAgainstHuman(controller);

            // Scorch/Petrify/Eclipse always target an opponent (ClashLegalMoves
            // never enumerates p == actor for these); Gust/Riptide instead target
            // whoever currently holds draft priority, which - on the human's own
            // turn - is the human themselves (a documented, harmless quirk, see
            // the plan). Pick a guaranteed-opponent-targeting candidate so this
            // test isn't flaky depending on which kinds were dealt/ordered.
            var candidates = Quintessence.Game.Clash.ClashLegalMoves.EnumerateDeclarations(controller.State, 0);
            int opponentTargetingIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Kind is Quintessence.Game.Clash.InterventionKind.Scorch
                    or Quintessence.Game.Clash.InterventionKind.Petrify
                    or Quintessence.Game.Clash.InterventionKind.Eclipse)
                {
                    opponentTargetingIndex = i;
                    break;
                }
            }

            Assert.That(opponentTargetingIndex, Is.GreaterThanOrEqualTo(0), "expected at least one opponent-targeting candidate");

            pickerRoot.transform.Find("OpenButton").GetComponent<Button>().onClick.Invoke();
            var candidateListRoot = pickerRoot.transform.Find("CandidateListRoot");
            candidateListRoot.GetChild(opponentTargetingIndex).GetComponent<Button>().onClick.Invoke();
            yield return null;

            // The AI must have either Warded or had the effect Applied - either way
            // it resolves on its own, with no pending intervention left over, and
            // play returns to a normal state (human's turn or an AI-turn advance).
            Assert.That(controller.State.Clash.Pending, Is.Null);
            Assert.That(controller.State.Clash.InterventionLog, Has.Count.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator ScorePanel_ShowsAtGameOver()
        {
            // Regression check for a real bug found and fixed this session: a view
            // that calls _root.SetActive(false) on the same GameObject its own
            // script lives on unsubscribes itself from StateChanged the moment it
            // hides (Unity fires OnDisable synchronously), so it could never show
            // itself again. ScorePanel now lives on Canvas, not ScorePanelRoot.
            var scoreRoot = GameObject.Find("Canvas").transform.Find("ScorePanelRoot");
            Assert.That(scoreRoot.gameObject.activeSelf, Is.False, "hidden at game start");

            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            int guard = 0;
            // The test scene's lowered InterventionCost (test convenience only)
            // makes interventions fire often; each Declare/Ward/Decline cycle
            // consumes a loop iteration without being one of the game's 12 real
            // picks, so this needs a little headroom beyond just "a dozen turns."
            while (!controller.State.IsGameOver && guard < 500)
            {
                guard++;
                if (controller.AwaitingTurnStart)
                {
                    controller.StartTurn();
                    // Polls the same completion flag as StartTurnAndWaitForPool,
                    // not a fixed wait - see that helper's comment.
                    while (controller.IsRollInProgress)
                    {
                        yield return null;
                    }

                    continue;
                }

                if (controller.HumanHasPendingResponse)
                {
                    controller.RespondDeclineWard();
                    yield return null;
                    continue;
                }

                if (!controller.IsHumanTurn)
                {
                    yield return null;
                    continue;
                }

                IReadOnlyList<DraftChoice> choices = LegalDrafts.EnumerateSimple(controller.State);
                if (choices.Count == 0)
                {
                    controller.ForfeitHumanTurn();
                }
                else
                {
                    // ConfirmPlacement acts on whatever the controller currently has
                    // armed (mirroring the real click-pool-then-click-cell flow) -
                    // it is not a self-contained "place this die" call.
                    DraftChoice choice = choices[0];
                    controller.ArmDie(choice.Source, choice.Index, LegalDrafts.ResolveDie(controller.State, choice));
                    controller.ConfirmPlacement(choice.Row, choice.Col);
                }

                yield return null;
            }

            Assert.That(controller.State.IsGameOver, Is.True, "game should finish within the guard budget");
            Assert.That(scoreRoot.gameObject.activeSelf, Is.True, "ScorePanel must show itself once the game ends");
        }
    }
}
