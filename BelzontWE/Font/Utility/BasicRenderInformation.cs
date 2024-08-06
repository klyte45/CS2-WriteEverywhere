﻿using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE.Font.Utility
{
    public class BasicRenderInformation
    {
        public BasicRenderInformation(Vector3[] vertices, int[] triangles, Vector2[] uv)
        {
            if (vertices != null && (triangles?.All(x => x < vertices.Length) ?? false))
            {
                m_vertices = vertices;
                m_triangles = triangles;
                m_uv = uv;
                m_bounds = vertices.Length == 0 ? default : new Bounds3(vertices.Aggregate((x, y) => Vector3.Min(x, y)), vertices.Aggregate((x, y) => Vector3.Max(x, y)));
            }
            else if (triangles != null && vertices != null)
            {
                LogUtils.DoLog($"m_vertices.Length = {m_vertices?.Length} | m_triangles: [{string.Join(",", m_triangles ?? new int[0])}]");
            }
        }
        public static BasicRenderInformation Fill(BasicRenderInformationJob brij, Material targetAtlas)
        {
            var bri = new BasicRenderInformation(AlignVertices(brij.vertices.ToList()), brij.triangles.ToArray(), brij.uv1.ToArray());
            if (bri.Mesh == null) return null;
            bri.m_YAxisOverflows = brij.m_YAxisOverflows;
            bri.m_fontBaseLimits = brij.m_fontBaseLimits;
            bri.m_generatedMaterial = targetAtlas;
            bri.m_pixelDensityMeters = 1000f;

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
            if (BasicIMod.DebugMode) LogUtils.DoLog("DISPOSING BRIJ");
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
