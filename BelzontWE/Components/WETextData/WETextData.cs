using Belzont.Utils;
using BelzontWE.Font.Utility;
using BelzontWE.Utils;
using Colossal.Entities;
using Colossal.Mathematics;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    public struct WETextDataMesh : IComponentData, IDisposable
    {
        private WESimulationTextType textType;

        private GCHandle basicRenderInformation;
        private FixedString32Bytes atlas;
        private FixedString32Bytes fontName;
        private WETextDataValueString valueData;
        private bool dirty;
        private bool templateDirty;

        public WESimulationTextType TextType
        {
            get => textType; set
            {
                textType = value;
                dirty = true;
            }
        }
        public FixedString32Bytes Atlas { readonly get => atlas; set => atlas = value; }
        public FixedString32Bytes FontName { readonly get => fontName; set => fontName = value; }
        public WETextDataValueString ValueData { readonly get => valueData; set => valueData = value; }
        public int LastLodValue { get; set; }
        public float MaxWidthMeters { get; set; }

        public Bounds3 Bounds { get; private set; }
        public bool HasBRI => basicRenderInformation.IsAllocated;
        public float BriWidthMetersUnscaled { get; private set; }
        public FixedString512Bytes LastErrorStr { get; private set; }

        public int SetFormulae(string value, out string[] cmpErr) => valueData.SetFormulae(value, out cmpErr);

        public void ResetBri()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
        }
        public static WETextDataMesh CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                ValueData = new()
                {
                    defaultValue = "NEW TEXT"
                },
            };

        public bool IsDirty() => dirty;
        public bool IsTemplateDirty() => templateDirty;
        public void ClearTemplateDirty() => templateDirty = false;

        public WETextDataMesh UpdateBRI(BasicRenderInformation bri, string text)
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            basicRenderInformation = default;
            basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
            Bounds = bri.m_bounds;
            BriWidthMetersUnscaled = bri.m_sizeMetersUnscaled.x;
            if (bri.m_isError)
            {
                LastErrorStr = text;
            }
            dirty = false;
            return this;
        }
        public void Dispose()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            basicRenderInformation = default;
        }

        public WETextDataMesh OnPostInstantiate(EntityManager em, Entity targetEntity)
        {
            FontServer.Instance.EnsureFont(fontName);
            UpdateFormulaes(em, targetEntity);
            return this;
        }
        public void UpdateFormulaes(EntityManager em, Entity geometryEntity)
        {
            if (textType != WESimulationTextType.Text && textType != WESimulationTextType.Image) return;
            var result = valueData.UpdateEffectiveValue(em, geometryEntity, (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : RenderInformation?.m_refText);
            if (result) templateDirty = dirty = true;
        }
        public static Entity GetTargetEntityEffective(Entity target, EntityManager em, bool fullRecursive = false)
        {
            return em.TryGetComponent<WETextDataMain>(target, out var weDataMain) && em.TryGetComponent<WETextDataMesh>(target, out var weDataMesh)
                ? (fullRecursive || weDataMesh.textType == WESimulationTextType.Archetype) && weDataMain.TargetEntity != target
                    ? GetTargetEntityEffective(weDataMain.TargetEntity, em)
                    : em.TryGetComponent<WETextDataMain>(weDataMain.ParentEntity, out var weDataParent) && weDataMesh.textType == WESimulationTextType.Placeholder
                    ? GetTargetEntityEffective(weDataParent.TargetEntity, em)
                    : weDataMain.TargetEntity
                : target;
        }

        public BasicRenderInformation RenderInformation
        {
            get
            {
                if (basicRenderInformation.IsAllocated && basicRenderInformation.Target is BasicRenderInformation bri && bri.IsValid())
                {
                    return bri;
                }
                else
                {
                    if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
                    return null;
                }
            }
        }

        public string Text { readonly get => valueData.defaultValue.ToString(); set { valueData.defaultValue = value; dirty = true; } }
    }

    public struct WETextDataMain : IComponentData
    {

        private FixedString32Bytes itemName;
        private Entity targetEntity;
        private Entity parentEntity;

        public FixedString32Bytes ItemName { get => itemName; set => itemName = value; }
        public Entity TargetEntity { get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { get => parentEntity; set => parentEntity = value; }

        public static WETextDataMain CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                targetEntity = target,
                parentEntity = parent ?? target,
                itemName = "New item",
            };
        public bool SetNewParent(Entity e, EntityManager em)
        {
            if ((e != TargetEntity && e != Entity.Null && (!em.TryGetComponent<WETextDataMesh>(e, out var mesh) || !em.TryGetComponent<WETextDataMain>(e, out var mainData) || mesh.TextType == WESimulationTextType.Placeholder || (mainData.TargetEntity != Entity.Null && mainData.TargetEntity != TargetEntity))))
            {
                return false;
            }
            ParentEntity = e;
            return true;
        }
        public void SetNewParentForced(Entity e)
        {
            ParentEntity = e;
        }
    }

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
        public WEShader shader;
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
        public float EmissiveIntensity { readonly get => emissiveIntensity.defaultValue; set { emissiveIntensity.defaultValue = math.clamp(value, 0, 100); } }
        public float EmissiveExposureWeight { readonly get => emissiveExposureWeight.defaultValue; set { emissiveExposureWeight.defaultValue = math.clamp(value, 0, 1); } }
        public float CoatStrength { readonly get => coatStrength.defaultValue; set { coatStrength.defaultValue = math.clamp(value, 0, 1); } }
        public float GlassThickness { readonly get => glassThickness.defaultValue; set { glassThickness.defaultValue = math.clamp(value, 0, 10); } }
        public Color ColorMask1 { readonly get => colorMask1.defaultValue; set { colorMask1.defaultValue = value; } }
        public Color ColorMask2 { readonly get => colorMask2.defaultValue; set { colorMask2.defaultValue = value; } }
        public Color ColorMask3 { readonly get => colorMask3.defaultValue; set { colorMask3.defaultValue = value; } }

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
            var bri = mesh.RenderInformation;
            result = null;
            bool requireUpdate = false;
            if (!mesh.HasBRI || bri is null) return false;
            if (bri.Guid != ownMaterialGuid)
            {
                ResetMaterial();
                ownMaterialGuid = bri.Guid;
                dirty = true;
            }
            if (!ownMaterial.IsAllocated || ownMaterial.Target is not Material material || !material)
            {
                if (!bri.IsValid())
                {
                    mesh.ResetBri();
                    return true;
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
            };
    }
    public struct WETextDataTransform : IComponentData
    {
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        public bool useAbsoluteSizeEditing;
        public static WETextDataTransform CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                offsetPosition = new(0, 0, 0),
                offsetRotation = new(),
                scale = new(1, 1, 1),
            };
    }
}