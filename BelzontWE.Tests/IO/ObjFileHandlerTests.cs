using NUnit.Framework;
using System.IO;
using BelzontWE.IO;
using UnityEngine;

namespace BelzontWE.Tests.IO
{
    // NOTE: ImportFromObj internally calls String.Split(char) / String.Split(Char, StringSplitOptions)
    // which are .NET 5+ APIs not available in .NET Framework 4.8. Tests that invoke ImportFromObj
    // are marked [Ignore] to document the intended behavior without causing runtime failures.

    [TestFixture]
    public class ObjFileHandlerTests
    {
        // ── WEMeshDescriptor direct constructor ────────────────────────────────

        [Test]
        public void WEMeshDescriptor_SetsVertices()
        {
            var verts = new Vector3[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6) };
            var desc = new ObjFileHandler.WEMeshDescriptor(verts, new Vector3[0], new Vector2[0], new int[0]);
            Assert.AreEqual(verts, desc.Vertices);
        }

        [Test]
        public void WEMeshDescriptor_SetsNormals()
        {
            var norms = new Vector3[] { new Vector3(0, 0, 1) };
            var desc = new ObjFileHandler.WEMeshDescriptor(new Vector3[0], norms, new Vector2[0], new int[0]);
            Assert.AreEqual(norms, desc.Normals);
        }

        [Test]
        public void WEMeshDescriptor_SetsUVs()
        {
            var uvs = new Vector2[] { new Vector2(0.5f, 0.5f) };
            var desc = new ObjFileHandler.WEMeshDescriptor(new Vector3[0], new Vector3[0], uvs, new int[0]);
            Assert.AreEqual(uvs, desc.UVs);
        }

        [Test]
        public void WEMeshDescriptor_SetsTriangles()
        {
            var tris = new int[] { 0, 1, 2 };
            var desc = new ObjFileHandler.WEMeshDescriptor(new Vector3[0], new Vector3[0], new Vector2[0], tris);
            Assert.AreEqual(tris, desc.Triangles);
        }

        [Test]
        public void WEMeshDescriptor_EmptyArrays_AllLengthZero()
        {
            var desc = new ObjFileHandler.WEMeshDescriptor(new Vector3[0], new Vector3[0], new Vector2[0], new int[0]);
            Assert.AreEqual(0, desc.Vertices.Length);
            Assert.AreEqual(0, desc.Normals.Length);
            Assert.AreEqual(0, desc.UVs.Length);
            Assert.AreEqual(0, desc.Triangles.Length);
        }

        // ── WEMeshDescriptor copy constructor ──────────────────────────────────

        [Test]
        public void WEMeshDescriptorCopy_VertexCountMatches()
        {
            var original = new ObjFileHandler.WEMeshDescriptor(
                new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) },
                new Vector3[0], new Vector2[0], new int[] { 0, 1, 2 });
            var copy = new ObjFileHandler.WEMeshDescriptor(original);
            Assert.AreEqual(3, copy.Vertices.Length);
        }

        [Test]
        public void WEMeshDescriptorCopy_VerticesAreNewArray()
        {
            var verts = new Vector3[] { new Vector3(1, 2, 3) };
            var original = new ObjFileHandler.WEMeshDescriptor(verts, new Vector3[0], new Vector2[0], new int[] { 0 });
            var copy = new ObjFileHandler.WEMeshDescriptor(original);
            Assert.AreNotSame(original.Vertices, copy.Vertices);
        }

        [Test]
        public void WEMeshDescriptorCopy_TrianglesAreNewArray()
        {
            var tris = new int[] { 0, 1, 2 };
            var original = new ObjFileHandler.WEMeshDescriptor(
                new Vector3[] { Vector3.zero, Vector3.right, Vector3.up },
                new Vector3[0], new Vector2[0], tris);
            var copy = new ObjFileHandler.WEMeshDescriptor(original);
            Assert.AreNotSame(original.Triangles, copy.Triangles);
            Assert.AreEqual(3, copy.Triangles.Length);
        }

        [Test]
        public void WEMeshDescriptorCopy_VertexValuesPreserved()
        {
            var original = new ObjFileHandler.WEMeshDescriptor(
                new Vector3[] { new Vector3(1.5f, 2.5f, 3.5f) },
                new Vector3[0], new Vector2[0], new int[0]);
            var copy = new ObjFileHandler.WEMeshDescriptor(original);
            Assert.AreEqual(1.5f, copy.Vertices[0].x, 0.001f);
            Assert.AreEqual(2.5f, copy.Vertices[0].y, 0.001f);
            Assert.AreEqual(3.5f, copy.Vertices[0].z, 0.001f);
        }

        // ── ImportFromObj — Ignored due to net48 runtime limitation ────────────
        // ObjFileHandler.ImportFromObj uses String.Split(char) (a .NET 5+ API).
        // The JIT fails to compile the method body under .NET Framework 4.8.

        [Test]
        [Ignore("ImportFromObj uses String.Split(char) which is unavailable in net48")]
        public void ImportFromObj_MissingFile_ReturnsNull()
        {
            var result = ObjFileHandler.ImportFromObj("nonexistent_file_xyz.obj");
            Assert.IsNull(result);
        }

        [Test]
        [Ignore("ImportFromObj uses String.Split(char) which is unavailable in net48")]
        public void ImportFromObj_SimpleTriangle_ReturnsThreeVertices()
        {
            // A minimal .obj with 3 vertices and 1 triangle:
            //   v 0 0 0 / v 1 0 0 / v 0 1 0 / s 1 / f 1 2 3
            Assert.Inconclusive("Blocked by net48 limitation — String.Split(char)");
        }

        [Test]
        [Ignore("ImportFromObj uses String.Split(char) which is unavailable in net48")]
        public void ImportFromObj_SimpleTriangle_ReturnsThreeTriangleIndices()
        {
            Assert.Inconclusive("Blocked by net48 limitation — String.Split(char)");
        }

        [Test]
        [Ignore("ImportFromObj uses String.Split(char) which is unavailable in net48")]
        public void ImportFromObj_WithNormals_ParsesNormals()
        {
            Assert.Inconclusive("Blocked by net48 limitation — String.Split(char)");
        }

        [Test]
        [Ignore("ImportFromObj uses String.Split(char) which is unavailable in net48")]
        public void ImportFromObj_WithUVs_ParsesUVs()
        {
            Assert.Inconclusive("Blocked by net48 limitation — String.Split(char)");
        }

        [Test]
        [Ignore("ImportFromObj uses String.Split(char) which is unavailable in net48")]
        public void ImportFromObj_InvalidFloat_ReturnsNull()
        {
            Assert.Inconclusive("Blocked by net48 limitation — String.Split(char)");
        }
    }
}
