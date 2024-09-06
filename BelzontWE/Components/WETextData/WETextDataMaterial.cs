using Belzont.Utils;
using BelzontWE.Sprites;
using BelzontWE.Utils;
using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    public struct WETextDataMaterial : IComponentData, IDisposable
    {
        public const int DEFAULT_DECAL_FLAGS = 8;

        private WETextDataValueColor color;
        private WETextDataValueColor emissiveColor;
        private WETextDataValueColor glassColor;
        private WETextDataValueFloat normalStrength;
        private WETextDataValueFloat glassRefraction;
        private WETextDataValueFloat metallic;
        private WETextDataValueFloat smoothness;
        private WETextDataValueFloat emissiveIntensity;
        private WETextDataValueFloat emissiveExposureWeight;
        private WETextDataValueFloat coatStrength;
        private WETextDataValueFloat glassThickness;
        private WETextDataValueColor colorMask1;
        private WETextDataValueColor colorMask2;
        private WETextDataValueColor colorMask3;
        private WEShader shader;
        public WEShader Shader { readonly get => shader; set { shader = value; ResetMaterial(); } }
        public int decalFlags;

        private bool dirty;
        private GCHandle ownMaterial;
        private Colossal.Hash128 ownMaterialGuid;

        public Color Color { readonly get => color.defaultValue; set { color.defaultValue = value; } }
        public Color EmissiveColor { readonly get => emissiveColor.defaultValue; set { emissiveColor.defaultValue = value; } }
        public Color GlassColor { readonly get => glassColor.defaultValue; set { glassColor.defaultValue = value; } }
        public float NormalStrength { readonly get => normalStrength.defaultValue; set { normalStrength.defaultValue = math.clamp(value, 0, 1); } }
        public float GlassRefraction { readonly get => glassRefraction.defaultValue; set { glassRefraction.defaultValue = math.clamp(value, 1, 1000); } }
        public float Metallic { readonly get => metallic.defaultValue; set { metallic.defaultValue = math.clamp(value, 0, 1); } }
        public float Smoothness { readonly get => smoothness.defaultValue; set { smoothness.defaultValue = math.clamp(value, 0, 1); } }
        public float EmissiveIntensity { readonly get => emissiveIntensity.defaultValue; set { emissiveIntensity.defaultValue = math.clamp(value, 0, 1000); } }
        public float EmissiveExposureWeight { readonly get => emissiveExposureWeight.defaultValue; set { emissiveExposureWeight.defaultValue = math.clamp(value, 0, 1); } }
        public float CoatStrength { readonly get => coatStrength.defaultValue; set { coatStrength.defaultValue = math.clamp(value, 0, 1); } }
        public float GlassThickness { readonly get => glassThickness.defaultValue; set { glassThickness.defaultValue = math.clamp(value, 0, 10); } }
        public Color ColorMask1 { readonly get => colorMask1.defaultValue; set { colorMask1.defaultValue = value; } }
        public Color ColorMask2 { readonly get => colorMask2.defaultValue; set { colorMask2.defaultValue = value; } }
        public Color ColorMask3 { readonly get => colorMask3.defaultValue; set { colorMask3.defaultValue = value; } }


        public string ColorFormulae => color.formulaeStr.ToString();
        public string EmissiveColorFormulae => emissiveColor.formulaeStr.ToString();
        public string GlassColorFormulae => glassColor.formulaeStr.ToString();
        public string NormalStrengthFormulae => normalStrength.formulaeStr.ToString();
        public string GlassRefractionFormulae => glassRefraction.formulaeStr.ToString();
        public string MetallicFormulae => metallic.formulaeStr.ToString();
        public string SmoothnessFormulae => smoothness.formulaeStr.ToString();
        public string EmissiveIntensityFormulae => emissiveIntensity.formulaeStr.ToString();
        public string EmissiveExposureWeightFormulae => emissiveExposureWeight.formulaeStr.ToString();
        public string CoatStrengthFormulae => coatStrength.formulaeStr.ToString();
        public string GlassThicknessFormulae => glassThickness.formulaeStr.ToString();
        public string ColorMask1Formulae => colorMask1.formulaeStr.ToString();
        public string ColorMask2Formulae => colorMask2.formulaeStr.ToString();
        public string ColorMask3Formulae => colorMask3.formulaeStr.ToString();


        public int SetFormulaeColor(string value, out string[] cmpErr) => color.SetFormulae(value, out cmpErr);
        public int SetFormulaeEmissiveColor(string value, out string[] cmpErr) => emissiveColor.SetFormulae(value, out cmpErr);
        public int SetFormulaeGlassColor(string value, out string[] cmpErr) => glassColor.SetFormulae(value, out cmpErr);
        public int SetFormulaeNormalStrength(string value, out string[] cmpErr) => normalStrength.SetFormulae(value, out cmpErr);
        public int SetFormulaeGlassRefraction(string value, out string[] cmpErr) => glassRefraction.SetFormulae(value, out cmpErr);
        public int SetFormulaeMetallic(string value, out string[] cmpErr) => metallic.SetFormulae(value, out cmpErr);
        public int SetFormulaeSmoothness(string value, out string[] cmpErr) => smoothness.SetFormulae(value, out cmpErr);
        public int SetFormulaeEmissiveIntensity(string value, out string[] cmpErr) => emissiveIntensity.SetFormulae(value, out cmpErr);
        public int SetFormulaeEmissiveExposureWeight(string value, out string[] cmpErr) => emissiveExposureWeight.SetFormulae(value, out cmpErr);
        public int SetFormulaeCoatStrength(string value, out string[] cmpErr) => coatStrength.SetFormulae(value, out cmpErr);
        public int SetFormulaeGlassThickness(string value, out string[] cmpErr) => glassThickness.SetFormulae(value, out cmpErr);
        public int SetFormulaeColorMask1(string value, out string[] cmpErr) => colorMask1.SetFormulae(value, out cmpErr);
        public int SetFormulaeColorMask2(string value, out string[] cmpErr) => colorMask2.SetFormulae(value, out cmpErr);
        public int SetFormulaeColorMask3(string value, out string[] cmpErr) => colorMask3.SetFormulae(value, out cmpErr);

        public bool UpdateFormulaes(EntityManager em, Entity geometryEntity)
        {
            return dirty |= color.UpdateEffectiveValue(em, geometryEntity)
              | emissiveColor.UpdateEffectiveValue(em, geometryEntity)
              | glassColor.UpdateEffectiveValue(em, geometryEntity)
              | normalStrength.UpdateEffectiveValue(em, geometryEntity)
              | glassRefraction.UpdateEffectiveValue(em, geometryEntity)
              | metallic.UpdateEffectiveValue(em, geometryEntity)
              | smoothness.UpdateEffectiveValue(em, geometryEntity)
              | emissiveIntensity.UpdateEffectiveValue(em, geometryEntity)
              | emissiveExposureWeight.UpdateEffectiveValue(em, geometryEntity)
              | coatStrength.UpdateEffectiveValue(em, geometryEntity)
              | glassThickness.UpdateEffectiveValue(em, geometryEntity)
              | colorMask1.UpdateEffectiveValue(em, geometryEntity)
              | colorMask2.UpdateEffectiveValue(em, geometryEntity)
              | colorMask3.UpdateEffectiveValue(em, geometryEntity);
        }

        public readonly void UpdateDefaultMaterial(Material material, WESimulationTextType textType)
        {
            material.SetColor("_BaseColor", color.EffectiveValue);
            material.SetFloat("_Metallic", metallic.EffectiveValue);
            material.SetColor("_EmissiveColor", emissiveColor.EffectiveValue);
            material.SetFloat("_EmissiveIntensity", emissiveIntensity.EffectiveValue);
            material.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight.EffectiveValue);
            material.SetFloat("_CoatStrength", coatStrength.EffectiveValue);
            material.SetFloat("_Smoothness", smoothness.EffectiveValue);
            if (textType == WESimulationTextType.Image)
            {
                material.SetColor("colossal_ColorMask0", colorMask1.EffectiveValue);
                material.SetColor("colossal_ColorMask1", colorMask2.EffectiveValue);
                material.SetColor("colossal_ColorMask2", colorMask3.EffectiveValue);
            }
            else
            {
                material.SetColor("colossal_ColorMask0", UnityEngine.Color.white);
                material.SetColor("colossal_ColorMask1", UnityEngine.Color.white);
                material.SetColor("colossal_ColorMask2", UnityEngine.Color.white);
            }
        }

        public readonly void UpdateGlassMaterial(Material material)
        {
            material.SetColor("_BaseColor", color.EffectiveValue);
            material.SetFloat("_Metallic", metallic.EffectiveValue);
            material.SetFloat("_Smoothness", smoothness.EffectiveValue);
            material.SetFloat(WERenderingHelper.IOR, glassRefraction.EffectiveValue);
            material.SetColor(WERenderingHelper.Transmittance, glassColor.EffectiveValue);
            material.SetFloat("_NormalStrength", normalStrength.EffectiveValue);
            material.SetFloat("_Thickness", glassThickness.EffectiveValue);
        }
        public void ResetMaterial()
        {
            if (ownMaterial.IsAllocated)
            {
                GameObject.Destroy(ownMaterial.Target as Material);
                ownMaterial.Free();
            }
        }
        public static WETextDataMaterial CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                shader = WEShader.Default,
                decalFlags = DEFAULT_DECAL_FLAGS,
                dirty = true,
                color = new() { defaultValue = Color.white },
                emissiveColor = new() { defaultValue = Color.white },
                metallic = new() { defaultValue = 0 },
                smoothness = new() { defaultValue = 0 },
                emissiveIntensity = new() { defaultValue = 0 },
                glassRefraction = new() { defaultValue = 1f },
                colorMask1 = new() { defaultValue = UnityEngine.Color.white },
                colorMask2 = new() { defaultValue = UnityEngine.Color.white },
                colorMask3 = new() { defaultValue = UnityEngine.Color.white },
                glassColor = new() { defaultValue = UnityEngine.Color.white },
                glassThickness = new() { defaultValue = .5f },
                coatStrength = new() { defaultValue = 0f },
            };

        public void Dispose()
        {
            if (ownMaterial.IsAllocated)
            {
                GameObject.Destroy(ownMaterial.Target as Material);
                ownMaterial.Free();
            }
            ownMaterial = default;
        }

        public bool GetOwnMaterial(ref WETextDataMesh mesh, out Material result)
        {
            var bri = (mesh.TextType) switch
            {
                WESimulationTextType.Text or WESimulationTextType.Image => mesh.RenderInformation,
                _ => WEAtlasesLibrary.GetWhiteTextureBRI()
            };
            result = null;
            bool requireUpdate = false;
            if (bri is null) return false;
            if (bri.Guid != ownMaterialGuid)
            {
                ResetMaterial();
                ownMaterialGuid = bri.Guid;
                dirty = true;
            }
            if (!ownMaterial.IsAllocated || ownMaterial.Target is not Material material || !material)
            {
                switch (mesh.TextType)
                {
                    case WESimulationTextType.Text:
                    case WESimulationTextType.Image:
                        if (!bri.IsValid())
                        {
                            mesh.ResetBri();
                            return true;
                        }
                        break;
                }
                ResetMaterial();
                material = WERenderingHelper.GenerateMaterial(bri, shader);
                ownMaterial = GCHandle.Alloc(material);
                dirty = true;
            }
            if (dirty)
            {
                switch (shader)
                {
                    case WEShader.Default:
                        if (!bri.m_isError)
                        {
                            UpdateDefaultMaterial(material, mesh.TextType);
                        }
                        break;
                    case WEShader.Glass:
                        if (!bri.m_isError)
                        {
                            UpdateGlassMaterial(material);
                        }
                        break;
                    default:
                        return false;
                }
                material.SetFloat(WERenderingHelper.DecalLayerMask, decalFlags.ToFloatBitFlags());
                HDMaterial.ValidateMaterial(material);
                dirty = false;
                requireUpdate = true;
            }
            result = material;
            return requireUpdate;
        }


        public WETextDataXml.DefaultStyleXml ToDefaultXml()
            => new()
            {
                color = color.ToRgbaXml(),
                emissiveColor = emissiveColor.ToRgbaXml(),
                metallic = metallic.ToXml(),
                smoothness = smoothness.ToXml(),
                emissiveIntensity = emissiveIntensity.ToXml(),
                emissiveExposureWeight = emissiveExposureWeight.ToXml(),
                coatStrength = coatStrength.ToXml(),
                colorMask1 = colorMask1.ToRgbXml(),
                colorMask2 = colorMask2.ToRgbXml(),
                colorMask3 = colorMask3.ToRgbXml(),

            };
        public WETextDataXml.GlassStyleXml ToGlassXml()
            => new()
            {
                color = color.ToRgbaXml(),
                glassColor = glassColor.ToRgbXml(),
                glassRefraction = glassRefraction.ToXml(),
                metallic = metallic.ToXml(),
                smoothness = smoothness.ToXml(),
                normalStrength = normalStrength.ToXml(),
                glassThickness = glassThickness.ToXml(),
            };
        public static WETextDataMaterial ToComponent(WETextDataXml.DefaultStyleXml value)
            => new()
            {
                color = value.color.ToComponent(),
                emissiveColor = value.emissiveColor.ToComponent(),
                metallic = value.metallic.ToComponent(),
                smoothness = value.smoothness.ToComponent(),
                emissiveIntensity = value.emissiveIntensity.ToComponent(),
                emissiveExposureWeight = value.emissiveExposureWeight.ToComponent(),
                coatStrength = value.coatStrength.ToComponent(),
                colorMask1 = value.colorMask1.ToComponent(),
                colorMask2 = value.colorMask2.ToComponent(),
                colorMask3 = value.colorMask3.ToComponent(),
                shader = value.shader,
                decalFlags = value.decalFlags
            };
        public static WETextDataMaterial ToComponent(WETextDataXml.GlassStyleXml value)
            => new()
            {
                color = value.color.ToComponent(),
                glassColor = value.glassColor.ToComponent(),
                glassRefraction = value.glassRefraction.ToComponent(),
                metallic = value.metallic.ToComponent(),
                smoothness = value.smoothness.ToComponent(),
                normalStrength = value.normalStrength.ToComponent(),
                glassThickness = value.glassThickness.ToComponent(),
                shader = value.shader,
                decalFlags = value.decalFlags
            };
    }
}