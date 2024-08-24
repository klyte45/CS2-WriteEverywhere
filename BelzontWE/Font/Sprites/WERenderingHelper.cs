using BelzontWE;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Unity.Collections;
using UnityEngine;
using WriteEverywhere.Layout;

namespace WriteEverywhere.Sprites
{
    public static class WERenderingHelper
    {
        public static readonly int ControlMask = Shader.PropertyToID("_ControlMask");
        public static readonly int MaskMap = Shader.PropertyToID("_MaskMap");
        public static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
        public static readonly int EmissionMap = Shader.PropertyToID("_EmissiveColorMap");


        public static readonly int[] kTriangleIndices = new int[]    {
            0,
            3,
            1,
            3,
            2,
            1,
        };

        public static BasicRenderInformation GenerateBri(FixedString32Bytes refName, WEImageInfo imageInfo)
        {
            var proportion = imageInfo.Texture.width / (float)imageInfo.Texture.height;
            var materialArray = new Material[2];
            for (int i = 0; i < materialArray.Length; i++)
            {
                materialArray[i] = FontServer.CreateDefaultFontMaterial(i);
                materialArray[i].SetTexture(FontAtlas._BaseColorMap, imageInfo.Texture);
                if (imageInfo.MaskMap && materialArray[i].HasTexture(MaskMap)) materialArray[i].SetTexture(MaskMap, imageInfo.MaskMap);
                if (imageInfo.ControlMask && materialArray[i].HasTexture(ControlMask)) materialArray[i].SetTexture(ControlMask, imageInfo.ControlMask);
                if (imageInfo.Normal && materialArray[i].HasTexture(NormalMap)) materialArray[i].SetTexture(NormalMap, imageInfo.Normal);
                if (imageInfo.Emissive && materialArray[i].HasTexture(EmissionMap)) materialArray[i].SetTexture(EmissionMap, imageInfo.Emissive);
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