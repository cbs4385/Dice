using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using Quintessence.UI;

namespace Quintessence.UI.Tests
{
    // These load the real MainPlay scene and drive its actual Button components,
    // rather than rebuilding a synthetic scene in-test - the point is to catch
    // real wiring breakage (a dropped serialized reference, a renamed GameObject),
    // not to duplicate the engine/reducer coverage already in the EditMode suite.
    // Uses EditorSceneManager (editor-only) - these tests are meant to run inside
    // the Editor's Test Runner, not in a standalone player test build.
    public class MainPlaySceneInteractionTests
    {
        private const string ScenePath = "Assets/Scenes/MainPlay.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            // A single fixed-frame yield was flaky across repeated reloads within one
            // PlayMode session (Unity does not guarantee the reload settles in exactly
            // one frame); wait until GameSessionController has actually initialized.
            GameSessionController controller = null;
            for (int i = 0; i < 60 && controller?.State == null; i++)
            {
                yield return null;
                GameObject sessionGo = GameObject.Find("GameSession");
                controller = sessionGo != null ? sessionGo.GetComponent<GameSessionController>() : null;
            }

            Assert.That(controller, Is.Not.Null, "GameSession not found after scene load");
            Assert.That(controller.State, Is.Not.Null, "GameSessionController.State not initialized in time");
        }

        [UnityTest]
        public IEnumerator ArmingPoolDieThenClickingLegalCell_PlacesDieOnBoard()
        {
            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            GameObject.Find("PoolContainer").transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            yield return null;

            Transform boardContainer = GameObject.Find("BoardContainer").transform;
            Button legalCell = null;
            for (int i = 0; i < boardContainer.childCount; i++)
            {
                var candidate = boardContainer.GetChild(i).GetComponent<Button>();
                if (candidate.interactable)
                {
                    legalCell = candidate;
                    break;
                }
            }

            Assert.That(legalCell, Is.Not.Null, "expected at least one legal first placement");
            legalCell.onClick.Invoke();
            yield return null;

            Assert.That(controller.State.Players[0].Board.HasAnyDie(), Is.True);
        }

        [UnityTest]
        public IEnumerator ArmedDie_LeavesAtLeastOneIncompatibleElementCellNonInteractable()
        {
            GameObject.Find("PoolContainer").transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            yield return null;

            Transform boardContainer = GameObject.Find("BoardContainer").transform;
            bool anyDisabled = false;
            for (int i = 0; i < boardContainer.childCount; i++)
            {
                if (!boardContainer.GetChild(i).GetComponent<Button>().interactable)
                {
                    anyDisabled = true;
                    break;
                }
            }

            // Every board has 4 distinct element cells; a single die's element can
            // match at most one of them, so at least one is always incompatible.
            Assert.That(anyDisabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ScorePanel_IsHiddenAtGameStart()
        {
            yield return null;

            // GameObject.Find cannot find inactive objects, and ScorePanel.Render()
            // deactivates the root as soon as the (non-game-over) state is known -
            // look it up via the active Canvas parent's Transform.Find instead.
            Transform scoreRoot = GameObject.Find("Canvas").transform.Find("ScorePanelRoot");
            Assert.That(scoreRoot, Is.Not.Null);
            Assert.That(scoreRoot.gameObject.activeSelf, Is.False);
        }
    }
}
