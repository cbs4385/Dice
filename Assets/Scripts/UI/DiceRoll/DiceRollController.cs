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
        // Long enough to read as a deliberate pause, not part of the roll or
        // the fly-to-tray motion - found live: with a short hold, the tail end
        // of the tumble (naturally slowing as it nears rest) and the fly-to-
        // tray lerp read as one continuous motion instead of three distinct
        // beats (roll, stop, then move to the tray).
        private const float HoldSeconds = 2f;
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

            yield return PlaybackRecordings(dice, recordings);

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
        // resting pose and throwing it - craps-style - straight at the near
        // corner it's already resting close to, guaranteeing a real wall
        // bounce almost immediately, which then redirects it back across the
        // arena toward the opposite ("entry") corner for the rest of the
        // simulation. Recording that and reversing it produces a trajectory
        // that starts chaotic (thrown in from the entry corner) and ends -
        // guaranteed, by construction - exactly at rest on the correct face,
        // since that's the literal pose this started from. This replaces an
        // earlier "roll randomly, then snap to the correct face" approach,
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
                recordings.Add(new List<DieFrame>(stepCount + 1));
            }

            for (int i = 0; i < dice.Count; i++)
            {
                DiceRollDie die = dice[i];
                Vector3 restPosition = restPoses[i].position;
                Quaternion restRotation = restPoses[i].rotation;

                // Like a real craps throw: launched *at* the near corner's
                // walls, not away from them - guaranteeing real contact almost
                // immediately. Aimed at whichever of the two walls (X or Z) is
                // actually closer to *this specific* die, not generically at
                // the corner point - ComputeRestPosition's grid necessarily
                // puts different dice at different distances from each wall
                // (needed for spacing, so dice don't overlap at spawn), and
                // aiming at the corner point diagonally left the two dice
                // furthest from it travelling the longest path and frequently
                // not arriving in time (measured live: those two consistently
                // missed). Aiming squarely at the nearer wall instead gives
                // every die the shortest possible path to guaranteed contact,
                // regardless of where it sits in the grid.
                float marginToXWall = ArenaHalfWidth - restPosition.x;
                float marginToZWall = ArenaHalfDepth - restPosition.z;
                Vector3 intoNearestWall = marginToXWall < marginToZWall
                    ? new Vector3(1f, 0f, 0.3f).normalized
                    : new Vector3(0.3f, 0f, 1f).normalized;
                Vector3 velocity = intoNearestWall * Random.Range(9f, 12f) + Vector3.up * Random.Range(1f, 2f);

                // The random "chaos" component here was found (via isolated,
                // single-die testing - no other dice present to blame)
                // to be strong enough to meaningfully curve the die's actual
                // path away from a straight line toward the wall, on top of
                // the correlated rolling component - a spinning die's
                // friction against the floor genuinely does curve its path,
                // same as a spinning ball in real life, so a bigger chaos
                // spin means a less predictable line. Scaled down from the
                // tumble's own chaos multiplier (0.6) so this specific throw
                // reaches the wall it's aimed at reliably, while still
                // tumbling visibly rather than sliding like a puck.
                Vector3 rollAngularVelocity = Vector3.Cross(Vector3.up, velocity) / radii[i];
                Vector3 chaosAngularVelocity = Random.insideUnitSphere * (velocity.magnitude * 0.25f);
                Vector3 angularVelocityDegrees = (rollAngularVelocity + chaosAngularVelocity) * Mathf.Rad2Deg;

                die.Launch(restPosition, restRotation, velocity, angularVelocityDegrees);
            }

            // Record the exact launch pose *before* simulating anything - this
            // becomes the final settle frame after reversal in the caller, and
            // skipping it was a real bug: without it, that role fell to
            // whatever the pose was after the first Physics.Simulate step
            // instead of the launch pose itself, usually a negligible
            // difference, but not once the rest position sits close enough to
            // a wall for an immediate collision in that very first step to
            // meaningfully rotate the die away from its guaranteed-correct
            // RotationForFace pose before it was ever recorded - found live as
            // a tetrahedron settling on a point instead of a face.
            for (int i = 0; i < dice.Count; i++)
            {
                Transform t = dice[i].transform;
                recordings[i].Add(new DieFrame { Position = t.position, Rotation = t.rotation });
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
        private IEnumerator PlaybackRecordings(List<DiceRollDie> dice, List<List<DieFrame>> recordings)
        {
            int frameCount = recordings[0].Count;
            for (int i = 0; i < dice.Count; i++)
            {
                DieFrame first = recordings[i][0];
                dice[i].transform.SetPositionAndRotation(first.Position, first.Rotation);
            }

            float elapsed = 0f;
            float duration = (frameCount - 1) * PrecomputeStep;
            while (elapsed < duration)
            {
                int frameIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / PrecomputeStep), 0, frameCount - 1);
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
                DieFrame last = recordings[i][frameCount - 1];
                dice[i].transform.SetPositionAndRotation(last.Position, last.Rotation);
            }
        }

        // Where each die comes to rest - and since PrecomputeReversedTrajectories
        // throws every die *away* from this exact spot toward the entry corner
        // before reversing, this is also where the visible roll ends up
        // settling. Placed near the far ("rear") wall, diagonally opposite the
        // entry corner, so the reversed playback actually crosses the arena and
        // settles against that wall - found live: resting near the arena's
        // center instead made the tumble look like it stopped halfway, with the
        // separate fly-to-tray motion after it reading as an unrelated,
        // disconnected "slerp" to a different spot.
        private Vector3 ComputeRestPosition(int index)
        {
            // An "L" along both walls near the corner, not a filled 2D grid -
            // a grid (even pushed close to the corner) necessarily has some
            // dice closer to one wall and farther from the other, and the
            // farthest ones measured live as unreliably reaching a wall at
            // all within the tumble - a die's own random spin curves its path
            // enough (like a spinning ball's friction against a floor) that
            // "close, but with distance still to cover" isn't good enough for
            // a guarantee. Along an L, every die's *rest* position already
            // sits within the wall-contact margin of one wall or the other, so
            // the guarantee no longer depends on the tumble's path at all -
            // three dice hug the X wall spread along Z, two hug the Z wall
            // spread along X, with 2-unit spacing (comfortably more than twice
            // a die's own half-extent of ~0.85) so no two ever start close
            // enough to overlap.
            const float wallMargin = 0.9f;
            if (index < 3)
            {
                return new Vector3(ArenaHalfWidth - wallMargin, RestHeight, index * 2f - 1f);
            }

            return new Vector3((index - 3) * 2f + 1.5f, RestHeight, ArenaHalfDepth - wallMargin);
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

            // Lit, not Unlit: switched to Unlit earlier while chasing a "ghost"
            // rendering bug on the theory that lighting gradients were
            // involved (confirmed live at the time: no change). The ghost's
            // real cause turned out to be unrelated (Main Camera's culling
            // mask rendering the dice a second time - see docs/progress.md),
            // so lighting was never the problem - but Unlit was: with zero
            // shading, a correctly vertex-up tetrahedron (verified via
            // Build_Tetrahedron_UpDirectionIsOppositeItsOwnFaceNormal, which
            // still passes) has no visible shading difference between its
            // three angled side faces, so it reads as a single flat triangle
            // indistinguishable from resting on a point instead of a face.
            // Lit restores the depth cues that make the actual 3D shape legible.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = DieColors.ForElement(element) };
            _materialCache[element] = material;
            return material;
        }
    }
}
