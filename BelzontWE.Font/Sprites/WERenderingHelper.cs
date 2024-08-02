using BelzontWE;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using UnityEngine;

namespace WriteEverywhere.Sprites
{
    public static class WERenderingHelper
    {
        public static readonly int[] kTriangleIndices = new int[]    {
            0,
            3,
            1,
            3,
            2,
            1,
        };

        public static readonly Mesh basicMesh = new()
        {
            vertices = new[]
            {
                new Vector3(-.5f, -.5f, 0f),
                new Vector3(0.5f, -.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-.5f, 0.5f, 0f),
            },
            uv = new[]
            {
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            },
            triangles = kTriangleIndices
        };

        static WERenderingHelper()
        {
            basicMesh.RecalculateNormals();
            basicMesh.RecalculateTangents();
            basicMesh.RecalculateBounds();
        }

        public static BasicRenderInformation GenerateBri(Texture2D tex, Vector4 borders = default, float pixelDensity = 1000)
        {
            var proportion = tex.width / (float)tex.height;
            var bri = new BasicRenderInformation
            {
                Mesh = new()
                {
                    vertices = new[]
                    {
                        new Vector3(50f * proportion, -50f, 0f),
                        new Vector3(50f * proportion, 50f, 0f),
                        new Vector3(-50f * proportion, 50f, 0f),
                        new Vector3(-50f * proportion, -50f, 0f),
                    },
                    uv = new[]
                    {
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1),
                        new Vector2(0, 0),
                    },
                    triangles = kTriangleIndices
                },
                m_fontBaseLimits = new RangeVector { min = 0, max = 1 },
                m_YAxisOverflows = new RangeVector { min = -.5f, max = .5f },
                m_sizeMetersUnscaled = new Vector2(proportion, 1),
                m_offsetScaleX = 1,
                m_generatedMaterial = FontServer.CreateDefaultFontMaterial(),
                m_borders = borders,
                m_pixelDensityMeters = pixelDensity,
                m_lineOffset = .5f,
                m_expandXIfAlone = true
            };

            bri.Mesh.RecalculateNormals();
            bri.Mesh.RecalculateTangents();
            bri.Mesh.RecalculateBounds();

            bri.m_generatedMaterial.mainTexture = tex;
            bri.m_generatedMaterial.SetTexture(FontAtlas._BaseColorMap, tex);
            return bri;

        }


    }
}