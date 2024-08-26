using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using WriteEverywhere.Layout;
using WriteEverywhere.Sprites;

namespace BelzontWE
{
    public static class WERenderingHelper
    {
        public static readonly int ControlMask = Shader.PropertyToID("_ControlMask");
        public static readonly int MaskMap = Shader.PropertyToID("_MaskMap");
        public static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
        public static readonly int EmissionMap = Shader.PropertyToID("_EmissiveColorMap");
        public static readonly int DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
        public static readonly int UV0Offset = Shader.PropertyToID("_UV0Offset");
        public static readonly int TileOffset = Shader.PropertyToID("_TileOffset");
        public static readonly int Transmittance = -1;
        public static readonly int IOR = -1;

        public const string defaultShaderName = "BH/SG_DefaultShader";
        public const string defaultGlassShaderName = "BH/GlsShader";

        static WERenderingHelper()
        {
            var glassShader = Shader.Find(defaultGlassShaderName);
            var propertyCount = glassShader.GetPropertyCount();
            for (int i = 0; i < propertyCount && (Transmittance == -1 || IOR == -1); i++)
            {
                switch (glassShader.GetPropertyDescription(i))
                {
                    case "IOR":
                        IOR = glassShader.GetPropertyNameId(i);
                        break;
                    case "TransmittanceColor":
                        Transmittance = glassShader.GetPropertyNameId(i);
                        break;
                }
            }
        }


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
            return GenerateBri(refName, imageInfo.Main, imageInfo.Normal, imageInfo.ControlMask, imageInfo.Emissive, imageInfo.MaskMap);
        }

        public static BasicRenderInformation GenerateBri(FixedString32Bytes refName, Texture main, Texture normal, Texture control, Texture emissive, Texture mask)
        {
            var proportion = main.width / (float)main.height;
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
                 main: main,
                 normal: normal,
                 control: control,
                 emissive: emissive,
                 mask: mask
                )
            {
                m_sizeMetersUnscaled = new Vector2(proportion, 1),
            };
            return bri;
        }

        public static Material GenerateMaterial(BasicRenderInformation bri, WEShader shader)
        {
            var material = CreateDefaultFontMaterial(shader);
            material.SetTexture(FontAtlas._BaseColorMap, bri.Main);
            if (bri.Mask && material.HasTexture(MaskMap)) material.SetTexture(MaskMap, bri.Mask);
            if (bri.Control && material.HasTexture(ControlMask)) material.SetTexture(ControlMask, bri.Control);
            if (bri.Normal && material.HasTexture(NormalMap)) material.SetTexture(NormalMap, bri.Normal);
            if (bri.Emissive && material.HasTexture(EmissionMap)) material.SetTexture(EmissionMap, bri.Emissive);
            return material;
        }
        private static Material CreateDefaultFontMaterial(WEShader type)
        {
            Material material = null;
            switch (type)
            {
                case WEShader.Default:
                    material = new Material(Shader.Find(defaultShaderName));
                    material.EnableKeyword("_GPU_ANIMATION_OFF");
                    HDMaterial.SetAlphaClipping(material, true);
                    HDMaterial.SetAlphaCutoff(material, .7f);
                    HDMaterial.SetUseEmissiveIntensity(material, true);
                    HDMaterial.SetEmissiveColor(material, UnityEngine.Color.white);
                    HDMaterial.SetEmissiveIntensity(material, 0, UnityEditor.Rendering.HighDefinition.EmissiveIntensityUnit.Nits);
                    material.SetFloat("_DoubleSidedEnable", 1);
                    material.SetVector("_DoubleSidedConstants", new Vector4(1, 1, -1, 0));
                    material.SetFloat("_Smoothness", .5f);
                    material.SetFloat("_ZTestGBuffer", 7);
                    material.SetFloat(DecalLayerMask, 8.ToFloatBitFlags());
                    material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
                    break;
                case WEShader.Glass:
                    material = new Material(Shader.Find(defaultGlassShaderName));
                    material.SetFloat("_DoubleSidedEnable", 1);
                    material.SetVector("_DoubleSidedConstants", new Vector4(1, 1, -1, 0));
                    material.SetFloat(DecalLayerMask, 8.ToFloatBitFlags());
                    material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
                    break;
            }
            HDMaterial.ValidateMaterial(material);
            return material;
        }

        internal static BasicRenderInformation GenerateBri(string spriteName, WETextureAtlas textureAtlas, WESpriteInfo spriteInfo)
        {
            var proportion = spriteInfo.Region.size.x / spriteInfo.Region.size.y;
            var min = new Vector2(spriteInfo.Region.position.x / textureAtlas.Width, spriteInfo.Region.position.y / textureAtlas.Height);
            var max = min + new Vector2(spriteInfo.Region.size.x / textureAtlas.Width, spriteInfo.Region.size.y / textureAtlas.Height);
            var bri = new BasicRenderInformation(spriteName,
                new[]
                    {
                        new Vector3(-.5f * proportion, -.5f, 0f),
                        new Vector3(-.5f * proportion, .5f, 0f),
                        new Vector3(.5f * proportion, .5f, 0f),
                        new Vector3(.5f * proportion, -.5f, 0f),
                    },
                uv: new[]
                    {
                        new Vector2(max.x, min.y),
                        max,
                        new Vector2(min.x, max.y),
                        min
                    },
                triangles: kTriangleIndices,
                 main: textureAtlas.Main,
                 normal: spriteInfo.HasNormal ? textureAtlas.Normal : null,
                 control: spriteInfo.HasControl ? textureAtlas.Control : null,
                 emissive: spriteInfo.HasEmissive ? textureAtlas.Emissive : null,
                 mask: spriteInfo.HasMask ? textureAtlas.Mask : null
                )
            {
                m_sizeMetersUnscaled = new Vector2(proportion, 1),
            };
            return bri;
        }
    }
}