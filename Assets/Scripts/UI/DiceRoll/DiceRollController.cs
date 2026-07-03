using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI.DiceRoll
{
    // Owns the physics dice-roll visual: subscribes to GameSessionController's
    // RoundStarted event, then for each pool die:
    //   1. Precomputes a real physics trajectory *starting from that die's
    //      already-determined resting face* and thrown away from rest (see
    //      PrecomputeReversedTrajectories) - offscreen, faster than real time.
    //   2. Plays that trajectory back in reverse as a kinematic animation, so
    //      what the player sees is a natural-looking tumble/bounce that always
    //      ends *exactly* on the correct face with no artificial correction,
    //      because the last frame played is the literal pose precompute started
    //      from.
    //   3. Flies each settled die up into the on-screen pool tray as one
    //      continuous motion, then hands control back via
    //      GameSessionController.NotifyRollComplete().
    //
    // Timings/arena size below are placeholder-reasonable defaults, not tuned
    // "feel" (AGENTS.md) - same status as RollAnimationSeconds's original value.
    public sealed class DiceRollController : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private DiceRollDie _diePrefab;
        [SerializeField] private Transform _dieContainer;
        [SerializeField] private Camera _arenaCamera;

        // The 2D pool tray's RectTransform - the die "flies" here (in arena world
        // space, via ComputeTrayWorldPosition) so the roll visually hands off into
        // the real pool buttons appearing in the same screen location, instead of
        // popping in separately.
        [SerializeField] private RectTransform _trayTarget;

        // Matches the DiceRollCamera's orthographic frustum at floor height
        // (orthographicSize=5, RenderTexture aspect 1024/768) - see the scene's
        // DiceRollArena walls, sized identically.
        private const float ArenaHalfWidth = 6.667f;
        private const float ArenaHalfDepth = 5f;
        private const float RestHeight = 0.6f;
        private const float TumbleSeconds = 1.3f;
        private const float HoldSeconds = 0.2f;
        private const float FlySeconds = 0.35f;

        // Finer than Unity's default fixed timestep (0.02s) purely for the
        // offscreen precompute pass, which isn't tied to real render time -
        // extra resolution costs nothing here and reduces the chance of a
        // fast-moving die tunnelling through a wall between steps.
        private const float PrecomputeStep = 0.01f;

        // Total = TumbleSeconds + HoldSeconds + FlySeconds; kept in sync with
        // GameSessionController.RollAnimationSeconds by the scene wiring, not
        // referenced directly, to avoid a UI-view-to-view dependency.

        private readonly Dictionary<Element, Material> _materialCache = new();

        private struct DieFrame
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private void OnEnable() => _controller.RoundStarted += OnRoundStarted;

        private void OnDisable() => _controller.RoundStarted -= OnRoundStarted;

        private void OnRoundStarted(IReadOnlyList<Die> pool) => StartCoroutine(PlayRoll(pool));

        private IEnumerator PlayRoll(IReadOnlyList<Die> pool)
        {
            var dice = new List<DiceRollDie>();
            var restPoses = new List<(Vector3 position, Quaternion rotation)>();
            var radii = new List<float>();

            for (int i = 0; i < pool.Count; i++)
            {
                DiceRollDie die = Instantiate(_diePrefab, _dieContainer);
                var mesh = PlatonicSolidMeshFactory.Build(Sides.Of(pool[i].Element));
                die.Configure(mesh, MaterialFor(pool[i].Element));
                dice.Add(die);
                restPoses.Add((ComputeRestPosition(i), die.RotationForFace(pool[i].Face)));
                radii.Add(Mathf.Max(0.15f, mesh.Mesh.bounds.extents.magnitude * die.transform.localScale.x));
            }

            int stepCount = Mathf.CeilToInt(TumbleSeconds / PrecomputeStep);
            var recordings = PrecomputeReversedTrajectories(dice, restPoses, radii, stepCount);

            _overlayRoot.SetActive(true);

            yield return PlaybackRecordings(dice, recordings, stepCount);

            yield return new WaitForSeconds(HoldSeconds);

            Vector3 trayTarget = ComputeTrayWorldPosition();
            var flyCoroutines = new List<Coroutine>();
            foreach (var die in dice)
            {
                Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
                flyCoroutines.Add(StartCoroutine(die.FlyToTray(trayTarget + jitter, FlySeconds)));
            }

            foreach (var c in flyCoroutines)
            {
                yield return c;
            }

            _overlayRoot.SetActive(false);
            foreach (var die in dice)
            {
                Destroy(die.gameObject);
            }

            _controller.NotifyRollComplete();
        }

        // Runs real Rigidbody physics *forward* in time, offscreen and faster
        // than real time, starting each die at its own already-determined
        // resting pose and throwing it away from rest toward the arena's
        // bottom-left "entry" corner. Recording that and reversing it produces a
        // trajectory that starts chaotic (thrown in from the bottom-left) and
        // ends - guaranteed, by construction - exactly at rest on the correct
        // face, since that's the literal pose this started from. This replaces
        // an earlier "roll randomly, then snap to the correct face" approach,
        // which looked like a visible correction rather than a natural stop.
        private List<List<DieFrame>> PrecomputeReversedTrajectories(
            List<DiceRollDie> dice,
            List<(Vector3 position, Quaternion rotation)> restPoses,
            List<float> radii,
            int stepCount)
        {
            var previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            var recordings = new List<List<DieFrame>>(dice.Count);
            for (int i = 0; i < dice.Count; i++)
            {
                recordings.Add(new List<DieFrame>(stepCount));
            }

            Vector3 entryPoint = new Vector3(-ArenaHalfWidth * 0.7f, 0f, -ArenaHalfDepth * 0.7f);

            for (int i = 0; i < dice.Count; i++)
            {
                DiceRollDie die = dice[i];
                Vector3 restPosition = restPoses[i].position;
                Quaternion restRotation = restPoses[i].rotation;

                Vector3 towardEntry = entryPoint - new Vector3(restPosition.x, 0f, restPosition.z);
                Vector3 velocity = towardEntry.normalized * Random.Range(6f, 9f) + Vector3.up * Random.Range(1f, 2f);

                Vector3 rollAngularVelocity = Vector3.Cross(Vector3.up, velocity) / radii[i];
                Vector3 chaosAngularVelocity = Random.insideUnitSphere * (velocity.magnitude * 0.6f);
                Vector3 angularVelocityDegrees = (rollAngularVelocity + chaosAngularVelocity) * Mathf.Rad2Deg;

                die.Launch(restPosition, restRotation, velocity, angularVelocityDegrees);
            }

            for (int s = 0; s < stepCount; s++)
            {
                Physics.Simulate(PrecomputeStep);
                for (int i = 0; i < dice.Count; i++)
                {
                    Transform t = dice[i].transform;
                    recordings[i].Add(new DieFrame { Position = t.position, Rotation = t.rotation });
                }
            }

            Physics.simulationMode = previousMode;

            foreach (var recording in recordings)
            {
                recording.Reverse();
            }

            foreach (var die in dice)
            {
                die.SetKinematic(true);
            }

            return recordings;
        }

        // Kinematically drives each die through its precomputed, reversed
        // trajectory - a scripted replay of a real physics simulation, not live
        // physics, so what plays back is exactly what was recorded (including
        // the guaranteed-correct final frame).
        private IEnumerator PlaybackRecordings(List<DiceRollDie> dice, List<List<DieFrame>> recordings, int stepCount)
        {
            for (int i = 0; i < dice.Count; i++)
            {
                DieFrame first = recordings[i][0];
                dice[i].transform.SetPositionAndRotation(first.Position, first.Rotation);
            }

            float elapsed = 0f;
            float duration = stepCount * PrecomputeStep;
            while (elapsed < duration)
            {
                int frameIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / PrecomputeStep), 0, stepCount - 1);
                for (int i = 0; i < dice.Count; i++)
                {
                    DieFrame frame = recordings[i][frameIndex];
                    dice[i].transform.SetPositionAndRotation(frame.Position, frame.Rotation);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < dice.Count; i++)
            {
                DieFrame last = recordings[i][stepCount - 1];
                dice[i].transform.SetPositionAndRotation(last.Position, last.Rotation);
            }
        }

        // Where each die comes to rest before its precomputed "thrown from
        // rest" trajectory begins - spread across the arena so five resting
        // dice don't overlap.
        private Vector3 ComputeRestPosition(int index)
        {
            Vector3 center = new Vector3(ArenaHalfWidth * 0.1f, RestHeight, ArenaHalfDepth * 0.1f);
            return center + new Vector3((index % 3) * 2.2f - 2.0f, 0f, (index / 3) * 1.8f - 0.7f);
        }

        // Maps the 2D pool tray's on-screen position into the arena's world
        // space, so the physics dice can fly to the exact spot the real pool
        // buttons will appear - the RenderTexture overlay covers the full screen
        // 1:1, so a screen point maps to a viewport fraction *of the screen*,
        // and from there to a world point on the floor plane via the arena
        // camera (orthographic projection keeps X/Z independent of the chosen
        // depth, so any in-range depth gives the same X/Z result).
        //
        // Deliberately NOT _arenaCamera.ScreenToViewportPoint: that divides by
        // the arena camera's own pixelWidth/pixelHeight, which is the
        // *RenderTexture's* 1024x768, not the actual screen/canvas size the
        // pool tray's screen position was measured in - a real bug found live
        // (dice flew to screen center instead of the tray).
        private Vector3 ComputeTrayWorldPosition()
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, _trayTarget.position);
            Vector3 viewportPos = new Vector3(screenPos.x / Screen.width, screenPos.y / Screen.height, 0f);
            float depth = Mathf.Abs(_arenaCamera.transform.position.y - RestHeight);
            Vector3 worldPos = _arenaCamera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, depth));
            return new Vector3(worldPos.x, RestHeight, worldPos.z);
        }

        private Material MaterialFor(Element element)
        {
            if (_materialCache.TryGetValue(element, out var cached))
            {
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { color = DieColors.ForElement(element) };
            _materialCache[element] = material;
            return material;
        }
    }
}
