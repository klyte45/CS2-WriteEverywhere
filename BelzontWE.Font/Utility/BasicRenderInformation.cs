using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace WriteEverywhere.Font.Utility
{
    public class BasicRenderInformation 
    {
        public void Fill(BasicRenderInformationJob bri, Material targetAtlas)
        {
            m_sizeMetersUnscaled = bri.m_sizeMetersUnscaled;
            m_materialGeneratedTick = bri.m_materialGeneratedTick;
            m_YAxisOverflows = bri.m_YAxisOverflows;
            m_fontBaseLimits = bri.m_fontBaseLimits;
            m_refY = bri.m_refY;
            m_generatedMaterial = targetAtlas;
            m_baselineOffset = bri.m_baselineOffset;
            m_borders = bri.m_borders;
            m_pixelDensityMeters = bri.m_pixelDensityMeters;
            m_lineOffset = bri.m_lineOffset;
            m_expandXIfAlone = bri.m_expandXIfAlone;
            m_offsetScaleX = bri.m_offsetScaleX;
            m_mesh = new Mesh
            {
                colors32 = bri.colors.ToArray(),
                triangles = bri.triangles.ToArray(),
                uv = bri.uv1.ToArray(),
                vertices = bri.vertices.ToArray()
            };
            m_mesh.RecalculateBounds();
            m_mesh.RecalculateNormals();
            m_mesh.RecalculateTangents();

        }

        public Mesh m_mesh;
        public Vector2 m_sizeMetersUnscaled;
        public long m_materialGeneratedTick;
        public Material m_generatedMaterial;
        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;
        public float m_refY = 1f;
        public string m_refText;
        public float m_baselineOffset = 0;
        public Vector4 m_borders;
        public float m_pixelDensityMeters;
        public float m_lineOffset;
        public bool m_expandXIfAlone;
        public float m_offsetScaleX = 1f;

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
        public Vector2 m_sizeMetersUnscaled;
        public Bounds m_bounds;
        public NativeArray<Color32> colors;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uv1;
        public long m_materialGeneratedTick;
        public Entity m_refFont;
        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;
        public float m_refY;
        public float m_baselineOffset;
        public Vector4 m_borders;
        public float m_pixelDensityMeters;
        public float m_lineOffset;
        public bool m_expandXIfAlone;
        public float m_offsetScaleX;

        public void Dispose()
        {
            colors.Dispose();
            vertices.Dispose();
            triangles.Dispose();
            uv1.Dispose();
        }

        public override string ToString() => $"BRI [m={m_bounds};sz={m_sizeMetersUnscaled}]";

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
