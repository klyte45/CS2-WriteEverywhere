using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Entities;
using Colossal.Mathematics;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextDataMesh : IComponentData, IDisposable
    {

        private GCHandle basicRenderInformation;
        private FixedString32Bytes atlas;
        private FixedString32Bytes fontName;
        private WETextDataValueString valueData;
        private bool dirty;

        public FixedString32Bytes Atlas { readonly get => atlas; set => atlas = value; }
        public FixedString32Bytes FontName { readonly get => fontName; set => fontName = value; }
        public WETextDataValueString ValueData { readonly get => valueData; set => valueData = value; }
        public int LastLodValue { get; set; }
        public float MaxWidthMeters { get; set; }

        public Bounds3 Bounds { get; private set; }
        public bool HasBRI => basicRenderInformation.IsAllocated;
        public float BriWidthMetersUnscaled { get; private set; }
        public FixedString512Bytes LastErrorStr { get; private set; }

        public int SetFormulae(string value, out string[] cmpErr) => ValueData.SetFormulae(value, out cmpErr);

        public void ResetBri()
        {
            basicRenderInformation.Free();
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
            UpdateEffectiveText(em, targetEntity);
            return this;
        }
        public void UpdateEffectiveText(EntityManager em, Entity geometryEntity)
        {
            var result = ValueData.UpdateEffectiveText(em, geometryEntity, (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : RenderInformation?.m_refText);
            if (result) dirty = true;
        }
        public static Entity GetTargetEntityEffective(Entity target, EntityManager em, bool fullRecursive = false)
        {
            return em.TryGetComponent<WETextDataMain>(target, out var weDataMain)
                ? (fullRecursive || weDataMain.TextType == WESimulationTextType.Archetype) && weDataMain.TargetEntity != target
                    ? GetTargetEntityEffective(weDataMain.TargetEntity, em)
                    : em.TryGetComponent<WETextDataMain>(weDataMain.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder
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
    }

    public struct WETextDataMain : IComponentData
    {
        public const int DEFAULT_DECAL_FLAGS = 8;

        private WESimulationTextType textType;
        private FixedString32Bytes itemName;
        private Entity targetEntity;
        private Entity parentEntity;
        private bool dirty;
        public WEShader shader;
        public int decalFlags;

        public WESimulationTextType TextType
        {
            get => textType; set
            {
                textType = value;
                dirty = true;
            }
        }
        public FixedString32Bytes ItemName { get => itemName; set => itemName = value; }
        public Entity TargetEntity { get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { get => parentEntity; set => parentEntity = value; }

        public static WETextDataMain CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                shader = WEShader.Default,
                decalFlags = DEFAULT_DECAL_FLAGS,
                targetEntity = target,
                parentEntity = parent ?? target,
                itemName = "New item",
            };
        public bool IsDirty() => dirty;
        public bool SetNewParent(Entity e, EntityManager em)
        {
            if ((e != TargetEntity && e != Entity.Null && (!em.TryGetComponent<WETextDataMain>(e, out var weData) || weData.textType == WESimulationTextType.Placeholder || (weData.TargetEntity != Entity.Null && weData.TargetEntity != TargetEntity))))
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"NOPE: e = {e}; weData = {weData}; targetEntity = {TargetEntity}; weData.targetEntity = {weData.TargetEntity}");
                return false;
            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"YEP: e = {e};  targetEntity = {TargetEntity}");
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
        public WETextDataValueColor color;
        public WETextDataValueColor emissiveColor;
        public WETextDataValueColor glassColor;
        public WETextDataValueFloat normalStrength;
        public WETextDataValueFloat glassRefraction;
        public WETextDataValueFloat metallic;
        public WETextDataValueFloat smoothness;
        public WETextDataValueFloat emissiveIntensity;
        public WETextDataValueFloat emissiveExposureWeight;
        public WETextDataValueFloat coatStrength;
        public WETextDataValueFloat glassThickness;
        public WETextDataValueColor colorMask1;
        public WETextDataValueColor colorMask2;
        public WETextDataValueColor colorMask3;

        public bool dirty;
        public GCHandle ownMaterial;
        public Colossal.Hash128 ownMaterialGuid;

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
                dirty = true,
                color = new() { defaultValue = new(0xff, 0xff, 0xff, 0xff) },
                emissiveColor = new() { defaultValue = new(0xff, 0xff, 0xff, 0xff) },
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