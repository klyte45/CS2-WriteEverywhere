using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Mathematics;
using System;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using UnityEngine;

namespace BelzontWE.Font.Utility
{
    public class BasicRenderInformation : IDisposable
    {
        public const string PLACEHOLDER_REFTEXT = "\0Placeholder\0";
        public static readonly BasicRenderInformation LOADING_PLACEHOLDER = new(PLACEHOLDER_REFTEXT, null, null, null, null, null, null, Texture2D.whiteTexture);
        public BasicRenderInformation(string refText,
            Vector3[] vertices, int[] triangles, Vector2[] uv,
            Vector3[] verticesCube, int[] trianglesCube, Vector2[] uvCube,
            Texture main, Texture normal = null, Texture control = null, Texture emissive = null, Texture mask = null)
        {
            m_refText = refText ?? throw new ArgumentNullException("refText");
            if (vertices != null && (triangles?.All(x => x < vertices.Length) ?? false))
            {
                m_vertices = vertices;
                m_triangles = triangles;
                m_uv = uv;
                var minUv = new Vector2(float.MaxValue, float.MaxValue);
                var maxUv = new Vector2(float.MinValue, float.MinValue);
                foreach (var uvI in uv)
                {
                    minUv = Vector2.Min(minUv, uvI);
                    maxUv = Vector2.Max(maxUv, uvI);
                }
                BoundsUV = new Bounds2(minUv, maxUv);
                m_bounds = vertices.Length == 0 ? default : new Bounds3(vertices.Aggregate((x, y) => Vector3.Min(x, y)), vertices.Aggregate((x, y) => Vector3.Max(x, y)));
            }
            else if (triangles != null && vertices != null)
            {
                LogUtils.DoWarnLog($"m_vertices.Length = {m_vertices?.Length} | m_triangles: [{string.Join(",", m_triangles ?? new int[0])}]");
            }
            if (verticesCube != null && (trianglesCube?.All(x => x < verticesCube.Length) ?? false))
            {
                m_verticesCube = verticesCube;
                m_trianglesCube = trianglesCube;
                m_uvCube = uvCube;
                m_boundsCube = vertices.Length == 0 ? default : new Bounds3(vertices.Aggregate((x, y) => Vector3.Min(x, y)), vertices.Aggregate((x, y) => Vector3.Max(x, y)));
            }
            else if (triangles != null && vertices != null)
            {
                LogUtils.DoWarnLog($"m_verticesCube.Length = {m_verticesCube?.Length} | m_trianglesCube: [{string.Join(",", m_trianglesCube ?? new int[0])}]");
            }
            Main = main;
            Normal = normal;
            Emissive = emissive;
            Control = control;
            Mask = mask;
            Guid = System.Guid.NewGuid();
        }
        public static BasicRenderInformation Fill(BasicRenderInformationJob brij, Texture main)
        {
            if (brij.Invalid)
            {                
                return null;
            }
            var bri = new BasicRenderInformation(brij.originalText.ToString(), brij.vertices.ToArray(), brij.triangles.ToArray(), brij.uv1.ToArray(),
                brij.verticesCube.ToArray(), brij.trianglesCube.ToArray(), brij.uv1Cube.ToArray(), main);
            if (bri.Mesh == null) return null;

            bri.m_colors32 = brij.colors.ToArray();
            bri.m_colors32Cube = brij.colorsCube.ToArray();

            bri.m_sizeMetersUnscaled = bri.Mesh.bounds.size;
            //   if (BasicIMod.DebugMode) LogUtils.DoLog($"MESH: {m_mesh} {m_mesh.vertices[0]} {m_mesh.vertices[1]}...  {m_mesh.tangents[0]} {m_mesh.tangents[1]}...  {m_mesh.normals[0]} {m_mesh.normals[1]}... {m_mesh.vertices.Length} {m_mesh.triangles.Length} {m_sizeMetersUnscaled}m");
            brij.Dispose();
            return bri;
        }

        private readonly Vector3[] m_vertices;
        private readonly int[] m_triangles;
        private Color32[] m_colors32;
        private readonly Vector2[] m_uv;
        public readonly Bounds3 m_bounds;


        private readonly Vector3[] m_verticesCube;
        private readonly int[] m_trianglesCube;
        private Color32[] m_colors32Cube;
        private readonly Vector2[] m_uvCube;
        public readonly Bounds3 m_boundsCube;

        private Mesh m_mesh;
        private Mesh m_meshCube;

        [XmlIgnore]
        public Texture Main { get; private set; }
        public Texture Normal { get; private set; }
        public Texture Emissive { get; private set; }
        public Texture Control { get; private set; }
        public Texture Mask { get; private set; }

        public Bounds2 BoundsUV { get; }

        public Mesh GetMesh(WEShader shader) => shader == WEShader.Decal ? MeshCube : Mesh;

        [XmlIgnore]
        private Mesh Mesh
        {
            get
            {
                if (m_mesh is null && m_vertices?.Length > 0)
                {
                    m_mesh = new Mesh
                    {
                        vertices = m_vertices,
                        triangles = m_triangles,
                        colors32 = m_colors32,
                        uv = m_uv,
                    };
                    m_mesh.RecalculateBounds();
                    m_mesh.RecalculateNormals();
                    m_mesh.RecalculateTangents();
                }
                return m_mesh;
            }
        }
        [XmlIgnore]
        private Mesh MeshCube
        {
            get
            {
                if (m_meshCube is null && m_verticesCube?.Length > 0)
                {
                    m_meshCube = new Mesh
                    {
                        vertices = m_verticesCube,
                        triangles = m_trianglesCube,
                        colors32 = m_colors32Cube,
                        uv = m_uvCube,
                    };
                    m_meshCube.RecalculateBounds();
                    m_meshCube.RecalculateNormals();
                    m_meshCube.RecalculateTangents();
                }
                return m_meshCube;
            }
        }
        public Colossal.Hash128 Guid { get; private set; }


        public Vector2 m_sizeMetersUnscaled;
        public readonly string m_refText;
        public bool m_isError = false;


        public override string ToString() => $"BRI [r={m_refText};v={m_vertices?.Length};sz={m_sizeMetersUnscaled}]";

        internal long GetSize() => GetMeshSize();

        public bool IsValid() => Main || m_refText.TrimToNull() == null;

        private long GetMeshSize()
        {
            unsafe
            {
                return
                    sizeof(Color32) * (Mesh.colors32?.Length ?? 0)
                    + sizeof(int) * 4
                    + sizeof(Bounds)
                    + sizeof(BoneWeight) * (Mesh.boneWeights?.Length ?? 0)
                    + sizeof(Matrix4x4) * (Mesh.bindposes?.Length ?? 0)
                    + sizeof(Vector3) * (Mesh.vertices?.Length ?? 0)
                    + sizeof(Vector3) * (Mesh.normals?.Length ?? 0)
                    + sizeof(Vector4) * (Mesh.tangents?.Length ?? 0)
                    + sizeof(Vector2) * (Mesh.uv?.Length ?? 0)
                    + sizeof(Vector2) * (Mesh.uv2?.Length ?? 0)
                    + sizeof(Vector2) * (Mesh.uv3?.Length ?? 0)
                    + sizeof(Vector2) * (Mesh.uv4?.Length ?? 0)
                    + sizeof(Color) * (Mesh.colors?.Length ?? 0)
                    + sizeof(int) * (Mesh.triangles?.Length ?? 0)
                    + sizeof(bool)
                    ;
            }
        }

        public void Dispose()
        {
            if (Main) GameObject.Destroy(Main);
            if (Normal) GameObject.Destroy(Normal);
            if (Emissive) GameObject.Destroy(Emissive);
            if (Control) GameObject.Destroy(Control);
            if (Mask) GameObject.Destroy(Mask);
        }
    }

    public unsafe struct BasicRenderInformationJob : IDisposable
    {
        public NativeArray<Color32> colors;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uv1;


        public NativeArray<Color32> colorsCube;
        public NativeArray<Vector3> verticesCube;
        public NativeArray<int> trianglesCube;
        public NativeArray<Vector2> uv1Cube;

        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;
        public uint AtlasVersion;
        public FixedString512Bytes originalText;
        public bool Invalid;

        public void Dispose()
        {
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog("DISPOSING BRIJ");
            colors.Dispose();
            vertices.Dispose();
            triangles.Dispose();
            uv1.Dispose();
        }
    }

    public struct RangeVector
    {
        public float min;
        public float max;

        public readonly float Offset => max - min;
        public readonly float Center => max + (min / 2);

        public override readonly string ToString() => $"[min = {min}, max = {max}]";


    }
}
