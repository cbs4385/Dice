using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quintessence.UI.DiceRoll
{
    // Procedural Platonic solid meshes for the physics dice-roll visual. Face
    // numbering (index 0..sides-1) is arbitrary - nothing in the game logic reads
    // it; only each face's geometry matters, used to compute a "show this face"
    // rotation once physics settles (see DiceRollDie) and where to anchor that
    // face's printed number.
    //
    // The dodecahedron is derived as the icosahedron's dual (one dodecahedron
    // vertex per icosahedron face, one dodecahedron pentagon per icosahedron
    // vertex) rather than from a separately hand-transcribed face list, so its
    // correctness rests on the icosahedron data below, not an unverifiable
    // from-memory table.
    public static class PlatonicSolidMeshFactory
    {
        public readonly struct Result
        {
            public readonly Mesh Mesh;

            // Index i => the local direction that should point toward the viewer
            // (world up, once DiceRollDie.SnapToFace applies its rotation) to show
            // die face i+1, and the local position to anchor that face's printed
            // number label.
            //
            // For every solid except the tetrahedron, "showing face i+1" means
            // face i itself points up, and its own centroid is the natural label
            // anchor - same as a real d6/d8/d12/d20. The tetrahedron is the one
            // Platonic solid with no opposite-face pairing: a real d4 rests on a
            // face with the OPPOSITE VERTEX pointing up, and the matching number
            // is printed ringed around that vertex on the three surrounding
            // faces - so its UpDirections are negated per-face and its anchors
            // sit at that opposite vertex instead of the face itself.
            public readonly IReadOnlyList<Vector3> UpDirections;
            public readonly IReadOnlyList<Vector3> LabelAnchors;

            public Result(Mesh mesh, IReadOnlyList<Vector3> upDirections, IReadOnlyList<Vector3> labelAnchors)
            {
                Mesh = mesh;
                UpDirections = upDirections;
                LabelAnchors = labelAnchors;
            }
        }

        private static readonly Dictionary<int, Result> Cache = new();

        public static Result Build(int sides)
        {
            // Unity's native Mesh object is destroyed when Play Mode exits, but a
            // static C# field survives unless a domain reload also happens to run
            // in between - so a plain dictionary lookup can hand back a reference
            // to an already-destroyed mesh (MissingReferenceException on next
            // access, found via repeated PlayMode test runs). `!= null` here uses
            // Unity's overridden null-check, which is false for destroyed objects,
            // so staleness triggers a clean regenerate instead.
            if (Cache.TryGetValue(sides, out var cached) && cached.Mesh != null)
            {
                return cached;
            }

            Result result = sides switch
            {
                4 => BuildTetrahedron(),
                6 => BuildCube(),
                8 => BuildFromTriangleFaces(OctahedronVertices(), OctahedronFaces()),
                12 => BuildDodecahedron(),
                20 => BuildFromTriangleFaces(IcosahedronVertices(), IcosahedronFaces()),
                _ => throw new ArgumentOutOfRangeException(nameof(sides), sides, "Unsupported die side count."),
            };

            Cache[sides] = result;
            return result;
        }

        // A tetrahedron centered at the origin has a neat symmetry: each face's
        // outward normal points in exactly the opposite direction from the one
        // vertex not on that face (verified directly against the vertex data
        // below, not assumed) - so "negate the normal" is both the correct
        // rotation target *and* the correct label-anchor direction for the
        // opposite-vertex reading convention real d4s use.
        private static Result BuildTetrahedron()
        {
            Vector3[] verts = TetrahedronVertices();
            var baseResult = BuildFromTriangleFaces(verts, TetrahedronFaces());
            float circumradius = verts[0].magnitude;

            var upDirections = new List<Vector3>();
            var labelAnchors = new List<Vector3>();
            for (int i = 0; i < baseResult.UpDirections.Count; i++)
            {
                Vector3 up = -baseResult.UpDirections[i];
                upDirections.Add(up);
                labelAnchors.Add(up * circumradius);
            }

            return new Result(baseResult.Mesh, upDirections, labelAnchors);
        }

        // ---- Tetrahedron (4 vertices, alternating cube corners) ----
        private static Vector3[] TetrahedronVertices() => new[]
        {
            new Vector3(1, 1, 1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1),
        };

        private static int[][] TetrahedronFaces() => new[]
        {
            new[] { 0, 1, 2 }, new[] { 0, 3, 1 }, new[] { 0, 2, 3 }, new[] { 1, 3, 2 },
        };

        // ---- Octahedron (6 axis vertices, 8 sign-combination faces) ----
        private static Vector3[] OctahedronVertices() => new[]
        {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0), new Vector3(0, -1, 0),
            new Vector3(0, 0, 1), new Vector3(0, 0, -1),
        };

        private static int[][] OctahedronFaces()
        {
            const int xPos = 0, xNeg = 1, yPos = 2, yNeg = 3, zPos = 4, zNeg = 5;
            return new[]
            {
                new[] { xPos, yPos, zPos }, new[] { xPos, yPos, zNeg },
                new[] { xPos, yNeg, zPos }, new[] { xPos, yNeg, zNeg },
                new[] { xNeg, yPos, zPos }, new[] { xNeg, yPos, zNeg },
                new[] { xNeg, yNeg, zPos }, new[] { xNeg, yNeg, zNeg },
            };
        }

        // ---- Icosahedron (12 golden-ratio vertices, 20 standard triangular faces) ----
        private static Vector3[] IcosahedronVertices()
        {
            float phi = (1f + Mathf.Sqrt(5f)) / 2f;
            return new[]
            {
                new Vector3(-1, phi, 0), new Vector3(1, phi, 0), new Vector3(-1, -phi, 0), new Vector3(1, -phi, 0),
                new Vector3(0, -1, phi), new Vector3(0, 1, phi), new Vector3(0, -1, -phi), new Vector3(0, 1, -phi),
                new Vector3(phi, 0, -1), new Vector3(phi, 0, 1), new Vector3(-phi, 0, -1), new Vector3(-phi, 0, 1),
            };
        }

        private static int[][] IcosahedronFaces() => new[]
        {
            new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 },
            new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 },
            new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 },
            new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 },
        };

        // ---- Cube: reuse Unity's own primitive - it's already correct, no need
        // to rebuild it by hand. Its face normals are read directly off the mesh. ----
        private static Result BuildCube()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(temp);

            var normals = mesh.normals;
            var triangles = mesh.triangles;
            var upDirections = new List<Vector3>();
            var seen = new HashSet<Vector3>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 n = normals[triangles[i]];
                var rounded = new Vector3(Mathf.Round(n.x), Mathf.Round(n.y), Mathf.Round(n.z));
                if (seen.Add(rounded))
                {
                    upDirections.Add(rounded);
                }
            }

            // Unity's default cube is 1 unit per side, so each face plane sits at
            // distance 0.5 from the origin along its normal.
            var labelAnchors = upDirections.Select(n => n * 0.5f).ToList();
            return new Result(mesh, upDirections, labelAnchors);
        }

        // ---- Dodecahedron as the icosahedron's dual ----
        private static Result BuildDodecahedron()
        {
            Vector3[] icoVerts = IcosahedronVertices();
            int[][] icoFaces = IcosahedronFaces();

            // One dodecahedron vertex per icosahedron face (its centroid).
            var dodecaVerts = new Vector3[icoFaces.Length];
            for (int f = 0; f < icoFaces.Length; f++)
            {
                dodecaVerts[f] = (icoVerts[icoFaces[f][0]] + icoVerts[icoFaces[f][1]] + icoVerts[icoFaces[f][2]]) / 3f;
            }

            // One dodecahedron (pentagon) face per icosahedron vertex: the 5
            // icosahedron faces touching that vertex, ordered by angle around it
            // so the pentagon's vertices are wound consistently rather than
            // arbitrarily.
            var facesTouchingVertex = new List<int>[icoVerts.Length];
            for (int v = 0; v < icoVerts.Length; v++)
            {
                facesTouchingVertex[v] = new List<int>();
            }

            for (int f = 0; f < icoFaces.Length; f++)
            {
                foreach (int v in icoFaces[f])
                {
                    facesTouchingVertex[v].Add(f);
                }
            }

            var triangleFaces = new List<int[]>();
            for (int v = 0; v < icoVerts.Length; v++)
            {
                Vector3 axis = icoVerts[v].normalized;
                Vector3 arbitrary = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 tangentA = Vector3.Cross(axis, arbitrary).normalized;
                Vector3 tangentB = Vector3.Cross(axis, tangentA).normalized;

                int[] pentagon = facesTouchingVertex[v]
                    .OrderBy(fi =>
                    {
                        Vector3 d = dodecaVerts[fi] - Vector3.Dot(dodecaVerts[fi], axis) * axis;
                        return Mathf.Atan2(Vector3.Dot(d, tangentB), Vector3.Dot(d, tangentA));
                    })
                    .ToArray();

                // Fan-triangulate the pentagon: (0,1,2) (0,2,3) (0,3,4).
                for (int i = 1; i < pentagon.Length - 1; i++)
                {
                    triangleFaces.Add(new[] { pentagon[0], pentagon[i], pentagon[i + 1] });
                }
            }

            return BuildFromTriangleFaces(dodecaVerts, triangleFaces.ToArray(), trianglesPerFace: 3);
        }

        // Builds a faceted mesh (each triangle gets its own vertices, so normals
        // are flat/per-face, not smoothed) from a shared vertex pool + triangle
        // index list, auto-correcting winding so every face normal points away
        // from the origin - all these solids are centered at the origin by
        // construction, so this needs no hand-verified winding order per face.
        private static Result BuildFromTriangleFaces(Vector3[] sharedVerts, int[][] faces, int trianglesPerFace = 1)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var upDirections = new List<Vector3>();
            var labelAnchors = new List<Vector3>();

            for (int f = 0; f < faces.Length; f++)
            {
                Vector3 a = sharedVerts[faces[f][0]];
                Vector3 b = sharedVerts[faces[f][1]];
                Vector3 c = sharedVerts[faces[f][2]];
                Vector3 centroid = (a + b + c) / 3f;
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

                if (Vector3.Dot(normal, centroid) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                int baseIndex = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                if (f % trianglesPerFace == 0)
                {
                    upDirections.Add(normal);
                    // For multi-triangle faces (the dodecahedron's fan-triangulated
                    // pentagons), average all of this face's triangle centroids
                    // for a properly centered anchor, not just the first triangle's.
                    Vector3 anchor = Vector3.zero;
                    for (int t = f; t < f + trianglesPerFace && t < faces.Length; t++)
                    {
                        anchor += (sharedVerts[faces[t][0]] + sharedVerts[faces[t][1]] + sharedVerts[faces[t][2]]) / 3f;
                    }

                    labelAnchors.Add(anchor / trianglesPerFace);
                }
            }

            var mesh = new Mesh { name = $"PlatonicSolid_{upDirections.Count}face" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();

            return new Result(mesh, upDirections, labelAnchors);
        }
    }
}
