using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Quintessence.UI.DiceRoll
{
    // A single physics-simulated die for the roll visual (see DiceRollController).
    // Real Rigidbody physics drives the tumble/bounce for the full effect, but the
    // final face is never left to chance - SnapToFace always forces the
    // predetermined result, since the actual value comes from the pure, tested
    // GameReducer/Bag, never from Unity's physics or RNG (Quintessence.Engine/Game
    // must stay untouched by either - see AGENTS.md).
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
        private readonly List<GameObject> _labels = new();

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
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
                Destroy(label);
            }

            _labels.Clear();

            for (int i = 0; i < meshResult.LabelAnchors.Count; i++)
            {
                _labels.Add(BuildLabel(i + 1, meshResult.LabelAnchors[i], meshResult.UpDirections[i]));
            }
        }

        public void Launch(Vector3 position, Vector3 velocity, Vector3 angularVelocityDegrees)
        {
            transform.position = position;
            transform.rotation = Random.rotation;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = angularVelocityDegrees * Mathf.Deg2Rad;
        }

        // Smoothly forces this die to display `face` (1-indexed, matching
        // Quintessence.Engine.Die.Face) over `duration` seconds, settling its
        // position at `restHeight` at the same time - a settle, not a pop.
        public IEnumerator SnapToFace(int face, float restHeight, float duration)
        {
            int index = (face - 1) % _upDirections.Count;
            Vector3 localUp = _upDirections[index];
            Quaternion targetRotation = Quaternion.FromToRotation(localUp, Vector3.up);

            _rigidbody.isKinematic = true;
            Quaternion startRotation = transform.rotation;
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = new Vector3(startPosition.x, restHeight, startPosition.z);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;
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
