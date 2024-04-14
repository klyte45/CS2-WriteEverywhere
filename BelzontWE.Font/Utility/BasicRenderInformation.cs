using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

namespace BelzontWE.Font.Utility
{
    public class BasicRenderInformation
    {
        public void Fill(BasicRenderInformationJob brij, Material targetAtlas)
        {
            m_YAxisOverflows = brij.m_YAxisOverflows;
            m_fontBaseLimits = brij.m_fontBaseLimits;
            m_generatedMaterial = targetAtlas;
            m_pixelDensityMeters = 1000f;


            m_mesh = new Mesh
            {
                vertices = AlignVertices(brij.vertices.ToList()),
                triangles = brij.triangles.ToArray(),
                colors32 = brij.colors.ToArray(),
                uv = brij.uv1.ToArray(),
                //normals = brij.vertices.Select(_ => new Vector3(x, y, z)).ToArray()
            };
            //m_mesh.tangents = m_mesh.vertices.Select(x => new Vector4(1, 0, 0, 1)).ToArray();
            //m_mesh.RecalculateUVDistributionMetrics();
            //m_mesh.Optimize();
            m_mesh.RecalculateBounds();
            m_mesh.RecalculateNormals();
            m_mesh.RecalculateTangents();

            m_sizeMetersUnscaled = m_mesh.bounds.size;
            LogUtils.DoLog($"MESH: {m_mesh} {m_mesh.vertices[0]} {m_mesh.vertices[1]}...  {m_mesh.tangents[0]} {m_mesh.tangents[1]}...  {m_mesh.normals[0]} {m_mesh.normals[1]}... {m_mesh.vertices.Length} {m_mesh.triangles.Length} {m_sizeMetersUnscaled}m");
            brij.Dispose();
        }
        [XmlIgnore]
        public Mesh m_mesh;
        public int MeshSize { get => m_mesh.vertices.Length; set { } }
        public Vector2 m_sizeMetersUnscaled;
        public long m_materialGeneratedTick;
        [XmlIgnore]
        public Material m_generatedMaterial;
        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;
        public float m_refY = 1f;
        public string m_refText;
        public float m_baselineOffset = 0;
        public Vector4 m_borders = default;
        public float m_pixelDensityMeters;
        public float m_lineOffset = .5f;
        public bool m_expandXIfAlone;
        public float m_offsetScaleX = 1f;

        private static Vector3[] AlignVertices(List<Vector3> points)
        {
            if (points.Count == 0)
            {
                return points.ToArray();
            }
            //var valueUI = WETestController.Overlay;
            //var x = (valueUI & 0xff) - 128f;
            //var y = ((valueUI >> 8) & 0xff) - 128f;
            //var z = ((valueUI >> 16) & 0xff) / 128f;
            var max = new Vector3(points.Select(x => x.x).Max(), points.Select(x => x.y).Max(), points.Select(x => x.z).Max());
            var min = new Vector3(points.Select(x => x.x).Min(), points.Select(x => x.y).Min(), points.Select(x => x.z).Min());
            Vector3 offset = (max + min) / 2;
            return points.Select(k => k - offset).ToArray();
        }

        public override string ToString() => $"BRI [m={m_mesh?.bounds};sz={m_sizeMetersUnscaled}]";

        internal long GetSize() => GetMeshSize();

        private long GetMeshSize()
        {
            unsafe
            {
                return
                    sizeof(Color32) * (m_mesh.colors32?.Length ?? 0)
                    + sizeof(int) * 4
                    + sizeof(Bounds)
                    + sizeof(BoneWeight) * (m_mesh.boneWeights?.Length ?? 0)
                    + sizeof(Matrix4x4) * (m_mesh.bindposes?.Length ?? 0)
                    + sizeof(Vector3) * (m_mesh.vertices?.Length ?? 0)
                    + sizeof(Vector3) * (m_mesh.normals?.Length ?? 0)
                    + sizeof(Vector4) * (m_mesh.tangents?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv2?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv3?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv4?.Length ?? 0)
                    + sizeof(Color) * (m_mesh.colors?.Length ?? 0)
                    + sizeof(int) * (m_mesh.triangles?.Length ?? 0)
                    + sizeof(bool)
                    ;
            }
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

        public void Dispose()
        {
            LogUtils.DoInfoLog("DISPOSING BRIJ");
            colors.Dispose();
            vertices.Dispose();
            triangles.Dispose();
            uv1.Dispose();
        }

        internal long GetSize() => GetMeshSize();

        private unsafe long GetMeshSize() => sizeof(BasicRenderInformationJob);
    }

    public struct RangeVector
    {
        public float min;
        public float max;

        public float Offset => max - min;
        public float Center => max + (min / 2);

        public override string ToString() => $"[min = {min}, max = {max}]";


    }
}
