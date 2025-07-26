using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using BelzontWE.Utils;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    public struct WETextDataMaterial : IComponentData, IDisposable, ICleanupComponentData
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
        private int decalFlags;

        private bool dirty;
        private GCHandle ownMaterial;
        private Colossal.Hash128 ownMaterialGuid;

        private bool affectSmoothness;
        private float drawOrder;

        public WEShader Shader { readonly get => shader; set { shader = value; ResetMaterial(); } }
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


        public string ColorFormulae => color.Formulae;
        public string EmissiveColorFormulae => emissiveColor.Formulae;
        public string GlassColorFormulae => glassColor.Formulae;
        public string NormalStrengthFormulae => normalStrength.Formulae;
        public string GlassRefractionFormulae => glassRefraction.Formulae;
        public string MetallicFormulae => metallic.Formulae;
        public string SmoothnessFormulae => smoothness.Formulae;
        public string EmissiveIntensityFormulae => emissiveIntensity.Formulae;
        public string EmissiveExposureWeightFormulae => emissiveExposureWeight.Formulae;
        public string CoatStrengthFormulae => coatStrength.Formulae;
        public string GlassThicknessFormulae => glassThickness.Formulae;
        public string ColorMask1Formulae => colorMask1.Formulae;
        public string ColorMask2Formulae => colorMask2.Formulae;
        public string ColorMask3Formulae => colorMask3.Formulae;
        public readonly Color ColorEffective => color.EffectiveValue;
        public readonly Color EmissiveColorEffective => emissiveColor.EffectiveValue;
        public readonly Color GlassColorEffective => glassColor.EffectiveValue;
        public readonly float NormalStrengthEffective => normalStrength.EffectiveValue;
        public readonly float GlassRefractionEffective => glassRefraction.EffectiveValue;
        public readonly float MetallicEffective => metallic.EffectiveValue;
        public readonly float SmoothnessEffective => smoothness.EffectiveValue;
        public readonly float EmissiveIntensityEffective => emissiveIntensity.EffectiveValue;
        public readonly float EmissiveExposureWeightEffective => emissiveExposureWeight.EffectiveValue;
        public readonly float CoatStrengthEffective => coatStrength.EffectiveValue;
        public readonly float GlassThicknessEffective => glassThickness.EffectiveValue;
        public readonly Color ColorMask1Effective => colorMask1.EffectiveValue;
        public readonly Color ColorMask2Effective => colorMask2.EffectiveValue;
        public readonly Color ColorMask3Effective => colorMask3.EffectiveValue;

        public int DecalFlags { readonly get => decalFlags; set { decalFlags = value; dirty = true; } }

        public bool AffectSmoothness { readonly get => affectSmoothness; set { affectSmoothness = value; dirty = true; } }

        public float DrawOrder { readonly get => drawOrder; set { drawOrder = value; dirty = true; } }

        public int SetFormulaeMainColor(string value, out string[] cmpErr) => color.SetFormulae(value, out cmpErr);
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

        public bool UpdateFormulaes(EntityManager em, Entity geometryEntity, FixedString512Bytes varsStr)
        {
            var vars = WEVarsCacheBank.Instance[WEVarsCacheBank.Instance[varsStr]];
            return dirty |= color.UpdateEffectiveValue(em, geometryEntity, vars)
              | emissiveColor.UpdateEffectiveValue(em, geometryEntity, vars)
              | glassColor.UpdateEffectiveValue(em, geometryEntity, vars)
              | normalStrength.UpdateEffectiveValue(em, geometryEntity, vars)
              | glassRefraction.UpdateEffectiveValue(em, geometryEntity, vars)
              | metallic.UpdateEffectiveValue(em, geometryEntity, vars)
              | smoothness.UpdateEffectiveValue(em, geometryEntity, vars)
              | emissiveIntensity.UpdateEffectiveValue(em, geometryEntity, vars)
              | emissiveExposureWeight.UpdateEffectiveValue(em, geometryEntity, vars)
              | coatStrength.UpdateEffectiveValue(em, geometryEntity, vars)
              | glassThickness.UpdateEffectiveValue(em, geometryEntity, vars)
              | colorMask1.UpdateEffectiveValue(em, geometryEntity, vars)
              | colorMask2.UpdateEffectiveValue(em, geometryEntity, vars)
              | colorMask3.UpdateEffectiveValue(em, geometryEntity, vars);
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
            material.SetFloat(WERenderingHelper.DecalLayerMask, math.asfloat(DecalFlags));
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

        public readonly void UpdateDecalMaterial(Material material, WESimulationTextType textType, DecalCharCoordinates coordinates)
        {
            material.SetColor("_BaseColor", color.EffectiveValue);
            material.SetFloat("_Metallic", metallic.EffectiveValue);
            material.SetFloat("_Smoothness", smoothness.EffectiveValue);
            material.SetFloat("_AffectAlbedo", 1);
            material.SetFloat("_AffectMetal", 1);
            material.SetFloat("_AffectNormal", 1);
            material.SetFloat("_AffectSmoothness", AffectSmoothness ? 1 : 0);
            material.SetFloat("_MetallicOpacity", coatStrength.EffectiveValue);
            material.SetFloat("_NormalOpacity", normalStrength.EffectiveValue);
            material.SetFloat("_DecalColorMask0", colorMask1.EffectiveValue.r);
            material.SetFloat("_DecalColorMask1", colorMask1.EffectiveValue.g);
            material.SetFloat("_DecalColorMask2", colorMask1.EffectiveValue.b);
            material.SetFloat("_DecalColorMask3", colorMask1.EffectiveValue.a);
            material.SetFloat("_DrawOrder", DrawOrder);
            material.SetFloat(WERenderingHelper.DecalLayerMask, math.asfloat(DecalFlags));
            material.SetVector("colossal_TextureArea", coordinates.textureArea);
            material.SetVector("colossal_MeshSize", coordinates.meshSize);
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
            material.SetVector("colossal_TextureArea", new float4(Vector2.zero, Vector2.one));
            material.SetFloat(WERenderingHelper.DecalLayerMask, math.asfloat(DecalFlags));
        }
        public void ResetMaterial()
        {
            if (ownMaterial.IsAllocated)
            {
                if (ownMaterial.Target is Material[] matArray)
                {
                    foreach (var material in matArray)
                    {
                        GameObject.Destroy(material);
                    }
                }
                ownMaterial.Free();
            }
        }
        public static WETextDataMaterial CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                shader = WEShader.Default,
                DecalFlags = DEFAULT_DECAL_FLAGS,
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
                if (ownMaterial.Target is Material[] matArray)
                {
                    foreach (var material in matArray)
                    {
                        GameObject.Destroy(material);
                    }
                }
                ownMaterial.Free();
            }
            ownMaterial = default;
        }

        public bool GetOwnMaterial(ref WETextDataMesh mesh, DecalCharCoordinates[] coordinates, out Material[] result)
        {
            if (mesh.TextType == WESimulationTextType.MatrixTransform)
            {
                result = default;
                return true;
            }
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
            if (!ownMaterial.IsAllocated || ownMaterial.Target is not Material[] materialArray || materialArray.Length == 0 || !materialArray[0])
            {
                switch (mesh.TextType)
                {
                    case WESimulationTextType.WhiteCube:
                        if (shader == WEShader.Decal)
                        {
                            shader = WEShader.Default;
                            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WEWorldPickerController>().ForceReload();
                        }
                        break;
                    case WESimulationTextType.Image:
                    case WESimulationTextType.Text:
                        if (!bri.IsValid())
                        {
                            mesh.ResetBri();
                            return true;
                        }
                        break;
                }
                ResetMaterial();
                materialArray = new Material[coordinates?.Length ?? 1];
                ownMaterial = GCHandle.Alloc(materialArray);
                dirty = true;
            }
            if (dirty)
            {
                var baseMaterial = materialArray[0] ??= WERenderingHelper.GenerateMaterial(bri, shader);
                switch (shader)
                {
                    case WEShader.Default:
                        if (!bri.IsError)
                        {
                            UpdateDefaultMaterial(baseMaterial, mesh.TextType);
                            HDMaterial.ValidateMaterial(baseMaterial);
                        }
                        break;
                    case WEShader.Glass:
                        if (!bri.IsError)
                        {
                            UpdateGlassMaterial(baseMaterial);
                            HDMaterial.ValidateMaterial(baseMaterial);
                        }
                        break;
                    case WEShader.Decal:
                        if (!bri.IsError)
                        {
                            for (int i = 0; i < materialArray.Length; i++)
                            {
                                materialArray[i] ??= new Material(baseMaterial);
                                UpdateDecalMaterial(materialArray[i], mesh.TextType, coordinates?[i] ?? default);
                                HDMaterial.ValidateMaterial(materialArray[i]);
                            }
                        }
                        break;
                    default:
                        return false;
                }
                dirty = false;
                requireUpdate = true;
            }
            result = materialArray;
            return requireUpdate;
        }
        public readonly bool CheckIsDecal(WETextDataMesh mesh) => Shader == WEShader.Decal && (mesh.TextType) switch { WESimulationTextType.WhiteCube or WESimulationTextType.Placeholder => false, _ => true };

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
                decalFlags = decalFlags

            };
        public WETextDataXml.DecalStyleXml ToDecalXml()
            => new()
            {
                color = color.ToRgbaXml(),
                metallic = metallic.ToXml(),
                smoothness = smoothness.ToXml(),
                drawOrder = drawOrder,
                affectSmoothness = affectSmoothness,
                decalFlags = decalFlags,
                metallicOpacity = coatStrength.ToXml(),
                normalOpacity = normalStrength.ToXml(),
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
                decalFlags = decalFlags
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
                DecalFlags = value.decalFlags
            };
        public static WETextDataMaterial ToComponent(WETextDataXml.DecalStyleXml value)
            => new()
            {
                color = value.color.ToComponent(),
                metallic = value.metallic.ToComponent(),
                smoothness = value.smoothness.ToComponent(),
                drawOrder = value.drawOrder,
                affectSmoothness = value.affectSmoothness,
                decalFlags = value.decalFlags,
                shader = value.shader,
                DecalFlags = value.decalFlags,
                coatStrength = value.metallicOpacity.ToComponent(),
                normalStrength = value.normalOpacity.ToComponent(),
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
                DecalFlags = value.decalFlags
            };
    }
}