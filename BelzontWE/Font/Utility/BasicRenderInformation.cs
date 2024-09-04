using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE.Font.Utility
{
    public class BasicRenderInformation : IDisposable
    {
        public const string PLACEHOLDER_REFTEXT = "\0Placeholder\0";
        public static readonly BasicRenderInformation LOADING_PLACEHOLDER = new(PLACEHOLDER_REFTEXT, null, null, null, Texture2D.whiteTexture);
        public BasicRenderInformation(string refText, Vector3[] vertices, int[] triangles, Vector2[] uv, Texture main, Texture normal = null, Texture control = null, Texture emissive = null, Texture mask = null)
        {
            m_refText = refText ?? throw new ArgumentNullException("refText");
            if (vertices != null && (triangles?.All(x => x < vertices.Length) ?? false))
            {
                m_vertices = vertices;
                m_triangles = triangles;
                m_uv = uv;
                m_bounds = vertices.Length == 0 ? default : new Bounds3(vertices.Aggregate((x, y) => Vector3.Min(x, y)), vertices.Aggregate((x, y) => Vector3.Max(x, y)));
                if ((m_bounds.min - m_bounds.max).z == 0)
                {
                    m_bounds.min = new float3(m_bounds.min.xy, -((Vector3)m_bounds.min).magnitude * .5f);
                    m_bounds.max = new float3(m_bounds.max.xy, ((Vector3)m_bounds.max).magnitude * .5f);
                }
            }
            else if (triangles != null && vertices != null)
            {
                LogUtils.DoWarnLog($"m_vertices.Length = {m_vertices?.Length} | m_triangles: [{string.Join(",", m_triangles ?? new int[0])}]");
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
            var bri = new BasicRenderInformation(brij.originalText.ToString(), AlignVertices(brij.vertices.ToList()), brij.triangles.ToArray(), brij.uv1.ToArray(), main);
            if (bri.Mesh == null) return null;

            bri.m_colors32 = brij.colors.ToArray();

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
        private Mesh m_mesh;

        [XmlIgnore]
        public Texture Main { get; private set; }
        public Texture Normal { get; private set; }
        public Texture Emissive { get; private set; }
        public Texture Control { get; private set; }
        public Texture Mask { get; private set; }

        [XmlIgnore]
        public Mesh Mesh
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
        public int MeshSize { get => Mesh.vertices.Length; set { } }
        public Colossal.Hash128 Guid { get; private set; }


        public Vector2 m_sizeMetersUnscaled;
        public readonly string m_refText;
        public bool m_isError = false;

        private static Vector3[] AlignVertices(List<Vector3> points)
        {
            if (points.Count == 0)
            {
                return points.ToArray();
            }
            var max = new Vector3(points.Select(x => x.x).Max(), 0, points.Select(x => x.z).Max());
            var min = new Vector3(points.Select(x => x.x).Min(), 0, points.Select(x => x.z).Min());
            Vector3 offset = (max + min) / 2;
            return points.Select(k => k - offset).ToArray();
        }

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

    public unsafe struct BasicRenderInformationJob : IComponentData, IDisposable
    {
        public NativeArray<Color32> colors;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uv1;
        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;
        public uint AtlasVersion;
        public FixedString512Bytes originalText;

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
