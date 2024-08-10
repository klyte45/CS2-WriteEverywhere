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

        public static BasicRenderInformation GenerateBri(string refName, Texture2D tex)
        {
            var proportion = tex.width / (float)tex.height;
            var bri = new BasicRenderInformation(refName,
                new[]
                    {
                        new Vector3(-.5f * proportion, -.5f, 0f),
                        new Vector3(-.5f * proportion, .5f, 0f),
                        new Vector3(.5f * proportion, .5f, 0f),
                        new Vector3(.5f * proportion, -.5f, 0f),
                    },
                uv: new[]
                    {
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1),
                        new Vector2(0, 0),
                    },
                triangles: kTriangleIndices)
            {
                m_sizeMetersUnscaled = new Vector2(proportion, 1),
                m_generatedMaterial = FontServer.CreateDefaultFontMaterial()
            };
            bri.m_generatedMaterial.mainTexture = tex;
            bri.m_generatedMaterial.SetTexture(FontAtlas._BaseColorMap, tex);
            return bri;

        }


    }
}