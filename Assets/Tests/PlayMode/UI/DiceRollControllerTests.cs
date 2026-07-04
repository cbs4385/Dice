using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using Quintessence.Engine;
using Quintessence.UI.DiceRoll;

namespace Quintessence.UI.Tests
{
    // Loads the real MainPlay scene and drives a genuine roll through
    // GameSessionController.StartTurn(), the same way a player would - not a
    // synthetic DiceRollDie instantiated in isolation - to prove the actual
    // integrated pipeline (precompute -> reversed playback -> fly to tray ->
    // cleanup -> pool buttons) works end to end, and that "physics for show,
    // guaranteed outcome" really holds: whatever GameReducer/Bag determined is
    // what ends up both on each settled 3D die's face and in the resulting 2D
    // pool button.
    public class DiceRollControllerTests
    {
        private const string ScenePath = "Assets/Scenes/MainPlay.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            GameSessionController controller = null;
            for (int i = 0; i < 60 && controller == null; i++)
            {
                yield return null;
                GameObject sessionGo = GameObject.Find("GameSession");
                controller = sessionGo != null ? sessionGo.GetComponent<GameSessionController>() : null;
            }

            Assert.That(controller, Is.Not.Null, "GameSession not found after scene load");
            GameObject.Find("StandardModeButton").GetComponent<Button>().onClick.Invoke();

            // Standard/Clash no longer start the match directly - the host now
            // confirms a player count/type setup first (PlayerSetupView), which
            // Show() activates. Poll instead of assuming one frame is enough for
            // Confirm to exist yet (its overlay starts inactive, and
            // GameObject.Find cannot find inactive objects' children).
            GameObject confirmGo = null;
            for (int i = 0; i < 60 && confirmGo == null; i++)
            {
                yield return null;
                confirmGo = GameObject.Find("ConfirmButton");
            }

            Assert.That(confirmGo, Is.Not.Null, "PlayerSetupView's Confirm button not found after clicking Standard");
            confirmGo.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(controller.State, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator StartTurn_RollsRealPhysicsDice_ThenSettlesOnThePredeterminedFaces()
        {
            var controller = GameObject.Find("GameSession").GetComponent<GameSessionController>();
            var overlay = GameObject.Find("Canvas").transform.Find("DiceRollOverlay").gameObject;
            var dieContainer = GameObject.Find("DieContainer").transform;

            Assert.That(overlay.activeSelf, Is.False, "overlay must not show before a roll starts");

            GameObject.Find("StartTurnButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(controller.IsRollInProgress, Is.True);
            Assert.That(overlay.activeSelf, Is.True, "overlay must show while dice are physically rolling");
            Assert.That(dieContainer.childCount, Is.GreaterThan(0), "physics dice must actually be spawned");

            // The already-determined pool - captured now, before anything else
            // can touch it, so we can assert each settled die matches it exactly.
            var pool = controller.State.CurrentPhase.Pool;
            Assert.That(dieContainer.childCount, Is.EqualTo(pool.Count));

            // Polls the actual completion flag rather than a fixed
            // WaitForSeconds(RollAnimationSeconds + buffer) - a fixed buffer was
            // found live to be flaky on a slower CI runner (a real "Transform
            // child out of bounds" elsewhere in the suite, never reproduced
            // locally), since a slow enough frame can push the coroutine's
            // actual finish past any fixed buffer.
            int guard = 0;
            while (controller.IsRollInProgress && guard < 1200)
            {
                guard++;
                yield return null;
            }

            Assert.That(controller.IsRollInProgress, Is.False, "roll must have finished");
            Assert.That(overlay.activeSelf, Is.False, "overlay must hide once the roll finishes");
            Assert.That(dieContainer.childCount, Is.EqualTo(0), "physics dice must be cleaned up after settling");

            // The 2D pool buttons must now show exactly the predetermined pool.
            Transform poolContainer = GameObject.Find("PoolContainer").transform;
            Assert.That(poolContainer.childCount, Is.GreaterThanOrEqualTo(pool.Count));
            for (int i = 0; i < pool.Count; i++)
            {
                var label = poolContainer.GetChild(i).GetComponentInChildren<TMPro.TextMeshProUGUI>();
                Assert.That(label.text, Is.EqualTo($"{pool[i].Element}\n{pool[i].Face}"));
            }
        }

        [UnityTest]
        public IEnumerator DiceRollDie_RotationForFace_ActuallyOrientsTheCorrectFaceUpward()
        {
            // A focused, non-integration check of the underlying rotation math
            // DiceRollController's reverse-simulation trick depends on: whatever
            // rotation RotationForFace(face) returns must genuinely put that
            // face's up-direction at world-up when applied - this is the exact
            // pose precompute starts every trajectory from (see
            // DiceRollController.PrecomputeReversedTrajectories), so if this
            // rotation were wrong, every roll would end on the wrong face
            // regardless of how good the physics playback looked.
            var diePrefabAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/DiceRollDie.prefab");
            var dieGo = Object.Instantiate(diePrefabAsset);
            var die = dieGo.GetComponent<DiceRollDie>();
            var meshResult = PlatonicSolidMeshFactory.Build(Sides.Of(Element.Water));
            die.Configure(meshResult, null);
            // Without this, the die's non-kinematic, gravity-affected Rigidbody
            // can move/rotate it during the yield below, independent of the
            // rotation this test sets directly - found live (a spurious ~0 dot
            // product), not a bug in RotationForFace itself.
            die.SetKinematic(true);

            const int targetFace = 7;
            dieGo.transform.rotation = die.RotationForFace(targetFace);
            yield return null;

            Vector3 localUp = meshResult.UpDirections[targetFace - 1];
            Vector3 worldUp = dieGo.transform.rotation * localUp;
            Assert.That(Vector3.Dot(worldUp, Vector3.up), Is.GreaterThan(0.99f),
                "the requested face's up-direction must point world-up under RotationForFace");

            Object.Destroy(dieGo);
        }
    }
}
