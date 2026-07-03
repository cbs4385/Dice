using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Quintessence.UI.DiceRoll
{
    // A single physics-simulated die for the roll visual (see DiceRollController).
    // The final face is never left to chance - DiceRollController precomputes
    // this die's whole trajectory *starting from* the predetermined resting face
    // and plays it back in reverse, so real physics drives the full tumble/bounce
    // and the roll still always ends exactly on the correct result, since the
    // actual value comes from the pure, tested GameReducer/Bag, never from
    // Unity's physics or RNG (Quintessence.Engine/Game must stay untouched by
    // either - see AGENTS.md).
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class DiceRollDie : MonoBehaviour
    {
        private const float LabelSize = 0.35f;
        private const float LabelOffset = 0.03f;

        private Rigidbody _rigidbody;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private IReadOnlyList<Vector3> _upDirections;

        private readonly struct LabelEntry
        {
            public readonly GameObject GameObject;
            public readonly Vector3 LocalUp;

            public LabelEntry(GameObject gameObject, Vector3 localUp)
            {
                GameObject = gameObject;
                LocalUp = localUp;
            }
        }

        private readonly List<LabelEntry> _labels = new();

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            // Discrete collision detection let fast-moving dice tunnel through each
            // other on the frame they'd otherwise first touch (found via live
            // verification - dice visibly clipping through one another mid-roll).
            // Continuous detection checks the whole swept path, not just the two
            // endpoint positions. Interpolation smooths the visual between fixed
            // physics steps, which otherwise looks like stutter at a variable
            // render framerate.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Configure(PlatonicSolidMeshFactory.Result meshResult, Material material)
        {
            _upDirections = meshResult.UpDirections;
            _meshFilter.sharedMesh = meshResult.Mesh;
            _meshRenderer.sharedMaterial = material;
            _meshCollider.sharedMesh = meshResult.Mesh;
            _meshCollider.convex = true;

            foreach (var label in _labels)
            {
                Destroy(label.GameObject);
            }

            _labels.Clear();

            for (int i = 0; i < meshResult.LabelAnchors.Count; i++)
            {
                Vector3 localUp = meshResult.UpDirections[i];
                _labels.Add(new LabelEntry(BuildLabel(i + 1, meshResult.LabelAnchors[i], localUp), localUp));
            }

            UpdateLabelVisibility();
        }

        // TextMeshPro's default SDF material renders in the Transparent queue
        // and, under URP, wasn't reliably depth-tested against this die's own
        // opaque mesh - found live: every face's label rendered simultaneously,
        // including ones on the far side of the die from the camera, creating a
        // jumble of "ghost" numbers instead of only the near-facing ones. Rather
        // than fight shader/render-queue compatibility, this hides any label
        // whose face currently points away from the camera (world-down, since
        // DiceRollCamera always looks straight down) - a label is only ever
        // shown when its face is the one actually readable from above.
        private void LateUpdate()
        {
            UpdateLabelVisibility();
        }

        private void UpdateLabelVisibility()
        {
            for (int i = 0; i < _labels.Count; i++)
            {
                Vector3 worldUp = transform.rotation * _labels[i].LocalUp;
                _labels[i].GameObject.SetActive(Vector3.Dot(worldUp, Vector3.up) > 0f);
            }
        }

        // The world rotation that shows `face` (1-indexed, matching
        // Quintessence.Engine.Die.Face) - the direction that should read "up"
        // once the die is at rest, aligned to world up.
        public Quaternion RotationForFace(int face)
        {
            Vector3 localUp = _upDirections[(face - 1) % _upDirections.Count];
            return Quaternion.FromToRotation(localUp, Vector3.up);
        }

        // Launches this die under live Rigidbody physics from an explicit pose -
        // used only during DiceRollController's offscreen precompute pass (see
        // its own comment): starts from a known rest pose and gets thrown away
        // from it, so the recorded trajectory can be played back in reverse.
        public void Launch(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocityDegrees)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = angularVelocityDegrees * Mathf.Deg2Rad;
        }

        // Switches between live Rigidbody simulation (during precompute) and a
        // kinematic pose driven directly by DiceRollController each frame
        // (during the recorded, reversed playback and the fly-to-tray phase).
        public void SetKinematic(bool kinematic)
        {
            _rigidbody.isKinematic = kinematic;

            // Interpolation is meant to smooth a *physics-driven* Rigidbody
            // between fixed timesteps - it has no business staying on once this
            // die is kinematic and being repositioned every frame via direct
            // Transform writes (PlaybackRecordings/FlyToTray), which don't go
            // through the Rigidbody API at all. Leaving it enabled here was
            // suspected as the cause of a live-reported "ghost" - a large,
            // stretched, translucent silhouette appearing near a die during the
            // reversed-playback/fly-to-tray phases - since Unity's interpolation
            // system is documented to behave unpredictably when a rigidbody's
            // transform is written directly instead of through
            // MovePosition/MoveRotation.
            if (kinematic)
            {
                _rigidbody.interpolation = RigidbodyInterpolation.None;
            }
        }

        // The second half of one continuous roll animation: once settled on its
        // final face (via SnapToFace), the die flies from its resting spot to
        // `targetPosition` - the arena-space point that lines up with the 2D pool
        // tray on screen - shrinking away as it arrives, so the physics roll
        // visually hands off into the real pool button appearing in the same
        // place, rather than the die vanishing and the button popping in
        // separately.
        public IEnumerator FlyToTray(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = transform.position;
            Vector3 startScale = transform.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = t * t; // ease-in: lingers, then snaps up quickly
                transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
        }

        // A small 3D TextMeshPro label sitting on the die's surface at the given
        // local anchor, facing outward along localUp, showing the face's number -
        // reuses the project's existing TMP dependency rather than baking a custom
        // texture atlas.
        private GameObject BuildLabel(int number, Vector3 localAnchor, Vector3 localUp)
        {
            var labelGo = new GameObject($"Face{number}Label");
            labelGo.layer = gameObject.layer; // must match the die's own layer, or DiceRollCamera's culling mask hides it entirely
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = localAnchor + localUp.normalized * LabelOffset;
            // TextMeshPro's readable side faces AWAY from its local forward (+Z)
            // axis - live-verified: LookRotation(localUp, ...) rendered a
            // mirrored digit, confirming the viewer was seeing the back of the
            // text quad, so forward must point back toward the die's center.
            Vector3 upHint = Mathf.Abs(Vector3.Dot(localUp, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            labelGo.transform.localRotation = Quaternion.LookRotation(-localUp.normalized, upHint);

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = number.ToString();
            tmp.fontSize = LabelSize * 10f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);

            return labelGo;
        }
    }
}
