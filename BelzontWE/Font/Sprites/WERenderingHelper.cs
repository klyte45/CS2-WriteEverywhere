using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using BelzontWE.Layout;
using BelzontWE.Sprites;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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
        public const string defaultDecalShaderName = "BH/Decals/DefaultDecalShader";

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
            0,3,1,
            3,2,1
        };
        public static
#if !DEBUG
    readonly
#endif
            int[] kTriangleIndicesCube = new int[]    {
                   0,                      1,   3,
                   1,                      0,   2,
                   4,                      5,   7,
                   5,                      4,   6,
                   8,                       9,   11,
                   9,                       8,   10,
                    12,                       13,    15,
                    13,                       12,    14,
                    16,                       17,    19,
                    17,                       16,    18,
                    20,                       21,    23,
                    21,                       20,    22,
            };

        public static readonly Vector3[] kVerticesPositionsCube =
        {
            new(- 1,+ 1,+ 1),            new(+ 1,+ 1,- 1),            new(- 1,+ 1,- 1),            new(+ 1,+ 1,+ 1),
            new(+ 1,+ 1,+ 1),            new(- 1,- 1,+ 1),            new(+ 1,- 1,+ 1),            new(- 1,+ 1,+ 1),
            new(- 1,+ 1,- 1),            new(- 1,- 1,+ 1),            new(- 1,+ 1,+ 1),            new(- 1,- 1,- 1),
            new(+ 1,- 1,- 1),            new(- 1,+ 1,- 1),            new(+ 1,+ 1,- 1),            new(- 1,- 1,- 1),
            new(+ 1,- 1,- 1),            new(+ 1,+ 1,+ 1),            new(+ 1,- 1,+ 1),            new(+ 1,+ 1,- 1),
            new(- 1,- 1,+ 1),            new(+ 1,- 1,- 1),            new(+ 1,- 1,+ 1),            new(- 1,- 1,- 1),
        };
        public static readonly Vector2[] kUvCube = kVerticesPositionsCube.Select(x => new Vector2(x.x <= 0 ? 0 : 1, x.z <= 0 ? 0 : 1)).ToArray();

        public static BasicRenderInformation GenerateBri(FixedString32Bytes refName, WEImageInfo imageInfo)
        {
            return GenerateBri(refName, imageInfo.Main, imageInfo.Normal, imageInfo.ControlMask, imageInfo.Emissive, imageInfo.MaskMap);
        }

        public static void DecalCubeFromPlanes(Vector3[] originalVertices, Vector2[] originalUv, out Vector3[] cubeVertices, out int[] cubeTris, out Vector2[] uvCube, float xDivider)
        {
            var verticesGroup = originalVertices.Select((x, i) => (x, i)).GroupBy(x => x.i / 4);
            cubeVertices = verticesGroup.Select(x =>
            {
                var list = x.Select(x => x.x).ToList();
                return (minx: list.Min(x => x.x) / xDivider, maxx: list.Max(x => x.x) / xDivider, miny: list.Min(x => x.y), maxy: list.Max(x => x.y));
            })
                .SelectMany(x =>
                    kVerticesPositionsCube.Select((y, j) => new Vector3(y.x < 0 ? x.minx : x.maxx, y.y * -.5f, y.z < 0 ? x.miny : x.maxy)))
                .ToArray();
            cubeTris = verticesGroup.SelectMany((_, i) => kTriangleIndicesCube.Select(x => x + (i * 24))).ToArray();

            uvCube = originalUv.Select((x, i) => (x, i)).GroupBy(x => x.i / 4).Select(x =>
            {
                var list = x.Select(x => x.x).ToList();
                return (minx: list.Min(x => x.x), maxx: list.Max(x => x.x), miny: list.Min(x => x.y), maxy: list.Max(x => x.y));
            })
                .SelectMany(x => kUvCube.Select((y, j) => new Vector2(y.x < 0 ? x.minx : x.maxx, y.y < 0 ? x.miny : x.maxy)))
                .ToArray();
        }

        public static BasicRenderInformation GenerateBri(FixedString32Bytes refName, Texture main, Texture normal, Texture control, Texture emissive, Texture mask)
        {
            var proportion = main.width / (float)main.height;
            var bri = new BasicRenderInformation(refName.ToString(),
                new[]
                    {
                        new Vector3(-.5f * proportion, -.5f, 0f),
                        new Vector3(-.5f * proportion,  .5f ,0f),
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
            if (material is null) return null;
            material.SetTexture(FontAtlas._BaseColorMap, bri.Main);
            if (bri.Mask && material.HasTexture(MaskMap)) material.SetTexture(MaskMap, bri.Mask);
            if (bri.Control && material.HasTexture(ControlMask)) material.SetTexture(ControlMask, bri.Control);
            if (bri.Normal && material.HasTexture(NormalMap)) material.SetTexture(NormalMap, bri.Normal);
            if (bri.Emissive && material.HasTexture(EmissionMap)) material.SetTexture(EmissionMap, bri.Emissive);
            return material;
        }
        private static Material CreateDefaultFontMaterial(WEShader type)
        {
            Material material;
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
                    material.SetFloat(DecalLayerMask, math.asfloat(8));
                    material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
                    break;
                case WEShader.Glass:
                    material = new Material(Shader.Find(defaultGlassShaderName));
                    material.SetFloat("_DoubleSidedEnable", 1);
                    material.SetVector("_DoubleSidedConstants", new Vector4(1, 1, -1, 0));
                    material.SetFloat(DecalLayerMask, math.asfloat(8));
                    material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
                    break;
                case WEShader.Decal:
                    material = new Material(Shader.Find(defaultDecalShaderName));
                    material.SetFloat(DecalLayerMask, math.asfloat(8));
                    material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
                    material.SetFloat("_AffectAlbedo", 1);
                    material.SetFloat("_AffectNormal", 1);
                    material.SetFloat("_AffectMetal", 1);
                    material.SetFloat("_AffectAO", 1);
                    material.SetFloat("_AffectSmoothness", 1);
                    break;
                default:
                    return null;
            }
            HDMaterial.ValidateMaterial(material);
            return material;
        }

        internal static BasicRenderInformation GenerateBri(WETextureAtlas textureAtlas, WESpriteInfo spriteInfo)
        {
            var proportion = spriteInfo.Region.size.x / spriteInfo.Region.size.y;
            var min = new Vector2(spriteInfo.Region.position.x / textureAtlas.Width, spriteInfo.Region.position.y / textureAtlas.Height);
            var max = min + new Vector2(spriteInfo.Region.size.x / textureAtlas.Width, spriteInfo.Region.size.y / textureAtlas.Height);
            var bri = new BasicRenderInformation(spriteInfo.Name,
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
                        //verticesCube: kVerticesPositionsCube.Select(x => new Vector3(.5f * x.x, .5f * x.y, .5f * x.z)).ToArray(),
                        //uvCube: kUvCube,
                        //trianglesCube: kTriangleIndicesCube,
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