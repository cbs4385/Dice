using NUnit.Framework;
using UnityEngine;
using Quintessence.UI.DiceRoll;

namespace Quintessence.UI.Tests
{
    // Plain NUnit tests (no scene/Play mode needed - Mesh/Vector3 work fine in
    // EditMode) verifying the procedural Platonic solid geometry itself, since a
    // visual glance can't confirm "is this actually a regular solid with outward
    // normals." The dodecahedron in particular is derived as the icosahedron's
    // dual rather than from a hand-transcribed face list (see
    // PlatonicSolidMeshFactory's own comment) - the all-vertices-equidistant
    // check here is what actually proves that derivation is geometrically sound.
    public class PlatonicSolidMeshFactoryTests
    {
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        [TestCase(12)]
        [TestCase(20)]
        public void Build_ProducesExactlyTheRequestedFaceCount(int sides)
        {
            var result = PlatonicSolidMeshFactory.Build(sides);
            Assert.That(result.UpDirections, Has.Count.EqualTo(sides));
            Assert.That(result.LabelAnchors, Has.Count.EqualTo(sides));
        }

        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        [TestCase(12)]
        [TestCase(20)]
        public void Build_AllVerticesAreEquidistantFromOrigin(int sides)
        {
            var result = PlatonicSolidMeshFactory.Build(sides);
            Vector3[] verts = result.Mesh.vertices;

            float first = verts[0].magnitude;
            foreach (var v in verts)
            {
                Assert.That(v.magnitude, Is.EqualTo(first).Within(0.001f), "a regular solid's vertices must all be equidistant from its center");
            }
        }

        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        [TestCase(12)]
        [TestCase(20)]
        public void Build_EveryTriangleNormalPointsOutward(int sides)
        {
            // This checks the mesh's own physical surface, unaffected by the
            // tetrahedron's UpDirections convention below - the die's actual
            // geometry must always be outward-facing regardless of which
            // direction "shows" which value.
            var result = PlatonicSolidMeshFactory.Build(sides);
            Vector3[] verts = result.Mesh.vertices;
            int[] tris = result.Mesh.triangles;

            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
                Vector3 centroid = (a + b + c) / 3f;
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                Assert.That(Vector3.Dot(normal, centroid.normalized), Is.GreaterThan(0.5f), $"triangle at index {t} has an inward-facing normal");
            }
        }

        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        [TestCase(12)]
        [TestCase(20)]
        public void Build_UpDirectionsAreUnitLength(int sides)
        {
            var result = PlatonicSolidMeshFactory.Build(sides);
            foreach (var up in result.UpDirections)
            {
                Assert.That(up.magnitude, Is.EqualTo(1f).Within(0.001f));
            }
        }

        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        [TestCase(12)]
        [TestCase(20)]
        public void Build_LabelAnchorsSitInTheirUpDirection(int sides)
        {
            var result = PlatonicSolidMeshFactory.Build(sides);
            for (int i = 0; i < result.UpDirections.Count; i++)
            {
                Assert.That(Vector3.Dot(result.LabelAnchors[i].normalized, result.UpDirections[i]), Is.GreaterThan(0.5f),
                    $"face {i}'s label anchor should sit in the direction that points toward the viewer once settled");
            }
        }

        // A real d4 rests on a FACE with the OPPOSITE VERTEX pointing up (see
        // docs/progress.md) - unlike every other Platonic solid, which rests with
        // a face pointing up. This confirms that convention is actually applied:
        // each face's UpDirection must be the *negation* of that same face's own
        // physical surface normal, not equal to it.
        [Test]
        public void Build_Tetrahedron_UpDirectionIsOppositeItsOwnFaceNormal()
        {
            var result = PlatonicSolidMeshFactory.Build(4);
            Vector3[] verts = result.Mesh.vertices;
            int[] tris = result.Mesh.triangles;

            for (int face = 0; face < result.UpDirections.Count; face++)
            {
                int t = face * 3;
                Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
                Vector3 physicalNormal = Vector3.Cross(b - a, c - a).normalized;

                Assert.That(Vector3.Dot(result.UpDirections[face], physicalNormal), Is.LessThan(-0.99f),
                    $"face {face}'s UpDirection should point opposite its own physical normal (toward the opposite vertex)");
            }
        }

        [Test]
        public void Build_UnsupportedSideCount_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => PlatonicSolidMeshFactory.Build(10));
        }
    }
}
