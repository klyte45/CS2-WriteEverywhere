using BelzontWE;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Unity.Collections;
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

        public static BasicRenderInformation GenerateBri(FixedString32Bytes refName, Texture2D tex)
        {
            var proportion = tex.width / (float)tex.height;
            var materialArray = new Material[2];
            for (int i = 0; i < materialArray.Length; i++)
            {
                materialArray[i] = FontServer.CreateDefaultFontMaterial(i);
                materialArray[i].mainTexture = tex;
                materialArray[i].SetTexture(FontAtlas._BaseColorMap, tex);
            }
            var bri = new BasicRenderInformation(refName.ToString(),
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
                triangles: kTriangleIndices,
                material: materialArray[0],
                glassMaterial: materialArray[1]
                )
            {
                m_sizeMetersUnscaled = new Vector2(proportion, 1),
            };
            return bri;

        }


    }
}