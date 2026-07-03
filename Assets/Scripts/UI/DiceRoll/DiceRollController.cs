using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI.DiceRoll
{
    // Owns the physics dice-roll visual: subscribes to GameSessionController's
    // RoundStarted event, spawns one physics die per pool die near the arena's
    // bottom-left corner, launches them toward the upper-right with real
    // Rigidbody physics (gravity, bouncing off walls and each other under real
    // Unity physics), then snaps each to its predetermined face and hands control
    // back via GameSessionController.NotifyRollComplete().
    //
    // Timings/arena size below are placeholder-reasonable defaults, not tuned
    // "feel" (AGENTS.md) - same status as RollAnimationSeconds's original value.
    public sealed class DiceRollController : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private DiceRollDie _diePrefab;
        [SerializeField] private Transform _dieContainer;

        // Matches the DiceRollCamera's orthographic frustum at floor height
        // (orthographicSize=5, RenderTexture aspect 1024/768) - see the scene's
        // DiceRollArena walls, sized identically.
        private const float ArenaHalfWidth = 6.667f;
        private const float ArenaHalfDepth = 5f;
        private const float RestHeight = 0.6f;
        private const float TumbleSeconds = 1.3f;
        private const float SnapSeconds = 0.3f;
        private const float HoldSeconds = 0.2f;

        // Total = TumbleSeconds + SnapSeconds + HoldSeconds; kept in sync with
        // GameSessionController.RollAnimationSeconds by the scene wiring, not
        // referenced directly, to avoid a UI-view-to-view dependency.

        private readonly List<DiceRollDie> _spawned = new();
        private readonly Dictionary<Element, Material> _materialCache = new();

        private void OnEnable() => _controller.RoundStarted += OnRoundStarted;

        private void OnDisable() => _controller.RoundStarted -= OnRoundStarted;

        private void OnRoundStarted(IReadOnlyList<Die> pool) => StartCoroutine(PlayRoll(pool));

        private IEnumerator PlayRoll(IReadOnlyList<Die> pool)
        {
            _overlayRoot.SetActive(true);
            SpawnAndLaunch(pool);

            yield return new WaitForSeconds(TumbleSeconds);

            var snapCoroutines = new List<Coroutine>();
            for (int i = 0; i < _spawned.Count; i++)
            {
                snapCoroutines.Add(StartCoroutine(_spawned[i].SnapToFace(pool[i].Face, RestHeight, SnapSeconds)));
            }

            foreach (var c in snapCoroutines)
            {
                yield return c;
            }

            yield return new WaitForSeconds(HoldSeconds);

            _overlayRoot.SetActive(false);
            foreach (var die in _spawned)
            {
                Destroy(die.gameObject);
            }

            _spawned.Clear();
            _controller.NotifyRollComplete();
        }

        private void SpawnAndLaunch(IReadOnlyList<Die> pool)
        {
            Vector3 spawnBase = new Vector3(-ArenaHalfWidth * 0.7f, RestHeight + 1.5f, -ArenaHalfDepth * 0.7f);

            for (int i = 0; i < pool.Count; i++)
            {
                DiceRollDie die = Instantiate(_diePrefab, _dieContainer);
                var mesh = PlatonicSolidMeshFactory.Build(Sides.Of(pool[i].Element));
                die.Configure(mesh, MaterialFor(pool[i].Element));

                Vector3 spawnPos = spawnBase + new Vector3((i % 3) * 0.9f, (i / 3) * 0.4f, (i % 2) * 0.7f);
                Vector3 velocity = new Vector3(
                    Random.Range(6f, 9f),
                    0f,
                    Random.Range(6f, 9f));
                Vector3 angularVelocityDegrees = new Vector3(
                    Random.Range(180f, 420f),
                    Random.Range(180f, 420f),
                    Random.Range(180f, 420f));

                die.Launch(spawnPos, velocity, angularVelocityDegrees);
                _spawned.Add(die);
            }
        }

        private Material MaterialFor(Element element)
        {
            if (_materialCache.TryGetValue(element, out var cached))
            {
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = DieColors.ForElement(element) };
            _materialCache[element] = material;
            return material;
        }
    }
}
