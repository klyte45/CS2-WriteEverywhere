using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using System;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE.Font.Utility
{
    public class PrimitiveRenderInformation : IBasicRenderInformation
    {
        public const string PLACEHOLDER_REFTEXT = "\0Placeholder\0";
        public static readonly PrimitiveRenderInformation LOADING_PLACEHOLDER = new(PLACEHOLDER_REFTEXT, null, null, null, default, null, null, null, Texture2D.whiteTexture);



        public PrimitiveRenderInformation(string refText,
            Vector3[] vertices, int[] triangles, Vector2[] uv, bool2 invertUv,
            Texture main, Texture normal = null, Texture control = null, Texture emissive = null, Texture mask = null)
        {
            m_refText = refText ?? throw new ArgumentNullException("refText");
            m_invertUv = invertUv;
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
                Bounds = vertices.Length == 0 ? default : new Bounds3(vertices.Aggregate((x, y) => Vector3.Min(x, y)), vertices.Aggregate((x, y) => Vector3.Max(x, y)));
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
        public static PrimitiveRenderInformation Fill(BasicRenderInformationJob brij, Texture main)
        {
            if (brij.Invalid)
            {
                return null;
            }
            var bri = new PrimitiveRenderInformation(brij.originalText.ToString(), brij.vertices.ToArray(), brij.triangles.ToArray(), brij.uv1.ToArray(),
                brij.invertUv,  main);
            if (bri.Mesh == null) return null;

            bri.m_colors32 = brij.colors.ToArray();

            bri.m_sizeMetersUnscaled = bri.Mesh.bounds.size;
            brij.Dispose();
            return bri;
        }

        private readonly Vector3[] m_vertices;
        private readonly int[] m_triangles;
        private Color32[] m_colors32;
        private readonly Vector2[] m_uv;
        public Bounds3 Bounds { get; private set; }
        public readonly Bounds3 m_boundsCube;

        private Mesh m_mesh;
        private Mesh[] m_meshCube;
        private Vector3[] m_meshCubeOffsets;

        [XmlIgnore]
        public Texture Main { get; private set; }
        public Texture Normal { get; private set; }
        public Texture Emissive { get; private set; }
        public Texture Control { get; private set; }
        public Texture Mask { get; private set; }

        public Bounds2 BoundsUV { get; }

        //  public Mesh GetMesh(WEShader shader) => shader == WEShader.Decal ? MeshCube : Mesh;
        public int MeshCount(WEShader shader) => shader == WEShader.Decal ? MeshCube.Length : 1;
        public Mesh GetMesh(WEShader shader, int idx = 0) => shader == WEShader.Decal ? MeshCube[idx] : Mesh;
        public MaterialPropertyBlock GetPropertyBlock(WEShader shader, int idx = 0) => shader == WEShader.Decal ? CubeDecalBlocks[idx] : null;
        public Vector3 GetMeshTranslation(WEShader shader, int idx = 0) => shader == WEShader.Decal ? m_meshCubeOffsets[idx] : default;

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
                    m_mesh.tangents = m_vertices.Select(x => Vector4.zero).ToArray();
                }
                return m_mesh;
            }
        }
        [XmlIgnore]
        internal Mesh[] MeshCube
        {
            get
            {
                if (m_meshCube is null && m_vertices?.Length > 0)
                {
                    WERenderingHelper.DecalCubeFromPlanes(m_vertices, m_uv, out var m_verticesCube, out var m_trianglesCube, out var m_uvCube, out m_meshCubeOffsets);
                    m_meshCube = m_verticesCube.Select((x, i) =>
                    {
                        var mesh = new Mesh
                        {
                            vertices = x,
                            triangles = m_trianglesCube[i],
                            uv = m_uvCube[i],
                        };
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();
                        mesh.tangents = x.Select(_ => Vector4.zero).ToArray();
                        return mesh;
                    }).ToArray();
                }
                return m_meshCube;
            }
        }

        [XmlIgnore]
        private MaterialPropertyBlock[] m_cubeDecalBlocks;

        [XmlIgnore]
        public MaterialPropertyBlock[] CubeDecalBlocks
        {
            get
            {
                if (m_cubeDecalBlocks is null)
                {
                    m_cubeDecalBlocks = MeshCube.Select(x => new MaterialPropertyBlock()).ToArray();
                    for (int i = 0; i < m_cubeDecalBlocks.Length; i++)
                    {
                        var uvBounds = (min: new float2(MeshCube[i].uv.Min(x => x.x), MeshCube[i].uv.Min(x => x.y)),
                                       max: new float2(MeshCube[i].uv.Max(x => x.x), MeshCube[i].uv.Max(x => x.y)));
                        var valueArea = new float4(uvBounds.min, uvBounds.max);
                        if (m_invertUv[0])
                        {
                            valueArea = valueArea.zyxw;
                        }
                        if (m_invertUv[1])
                        {
                            valueArea = valueArea.xwzy;
                        }

                        m_cubeDecalBlocks[i].SetVector("colossal_TextureArea", valueArea);
                        m_cubeDecalBlocks[i].SetVector("colossal_MeshSize", new float4(MeshCube[i].bounds.size, 0f));
                    }
                }
                return m_cubeDecalBlocks;
            }
        }
        public Colossal.Hash128 Guid { get; private set; }

        public bool IsError { get; set; } = false;


        public Vector2 m_sizeMetersUnscaled;
        public readonly string m_refText;
        private readonly bool2 m_invertUv;
        public bool m_isError = false;

        public override string ToString() => $"BRI [r={m_refText};v={m_vertices?.Length};sz={m_sizeMetersUnscaled};{(IsError ? "ERR" : "")}]";

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
            if (Mesh) GameObject.Destroy(Mesh);
            MeshCube?.ForEach(x => GameObject.Destroy(x));
        }
    }

    public unsafe struct BasicRenderInformationJob : IDisposable
    {
        public NativeArray<Color32> colors;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uv1;
        public bool2 invertUv;

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
