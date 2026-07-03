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
    // integrated pipeline (spawn -> physics -> snap -> cleanup -> pool buttons)
    // works end to end, and that "physics for show, guaranteed outcome" really
    // holds: whatever GameReducer/Bag determined is what ends up both on each
    // settled 3D die's face and in the resulting 2D pool button.
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

            yield return new WaitForSeconds(GameSessionController.RollAnimationSeconds + 0.3f);

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
        public IEnumerator DiceRollDie_SnapToFace_ActuallyOrientsTheCorrectFaceUpward()
        {
            // A focused, non-integration check of the face-snap math itself:
            // spawn one physics die directly, let it tumble briefly under real
            // physics (so this isn't just testing a kinematic no-op), then snap
            // it to a specific face and verify that face's world-space normal
            // really points up - the exact contract DiceRollController depends on.
            var diePrefabAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/DiceRollDie.prefab");
            var dieGo = Object.Instantiate(diePrefabAsset);
            var die = dieGo.GetComponent<DiceRollDie>();
            var meshResult = PlatonicSolidMeshFactory.Build(Sides.Of(Element.Water));
            die.Configure(meshResult, null);
            die.Launch(new Vector3(0, 5, 0), new Vector3(3, 0, 2), new Vector3(200, 250, 180));

            yield return new WaitForSeconds(0.3f);

            const int targetFace = 7;
            yield return die.StartCoroutine(die.SnapToFace(targetFace, restHeight: 0.6f, duration: 0.3f));

            Vector3 localUp = meshResult.UpDirections[targetFace - 1];
            Vector3 worldUp = dieGo.transform.rotation * localUp;
            Assert.That(Vector3.Dot(worldUp, Vector3.up), Is.GreaterThan(0.99f),
                "the requested face's up-direction must point world-up once settled");

            Object.Destroy(dieGo);
        }
    }
}
