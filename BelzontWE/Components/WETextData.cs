

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
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    public struct WETextData : IDisposable, IComponentData
    {
        private struct WETextDataMaterial
        {
            public Color32 color;
            public Color32 emissiveColor;
            public Color32 glassColor;
            public float normalStrength;
            public bool dirty;
            public float glassRefraction;
            public float metallic;
            public float smoothness;
            public float emissiveIntensity;
            public float emissiveExposureWeight;
            public float coatStrength;
            public float glassThickness;
            public Color colorMask1;
            public Color colorMask2;
            public Color colorMask3;
            public GCHandle ownMaterial;
            public Colossal.Hash128 ownMaterialGuid;

            public readonly void UpdateDefaultMaterial(Material material, WESimulationTextType textType)
            {
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", metallic);
                material.SetColor("_EmissiveColor", emissiveColor);
                material.SetFloat("_EmissiveIntensity", emissiveIntensity);
                material.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight);
                material.SetFloat("_CoatStrength", coatStrength);
                material.SetFloat("_Smoothness", smoothness);
                if (textType == WESimulationTextType.Image)
                {
                    material.SetColor("colossal_ColorMask0", colorMask1);
                    material.SetColor("colossal_ColorMask1", colorMask2);
                    material.SetColor("colossal_ColorMask2", colorMask3);
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
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat(WERenderingHelper.IOR, glassRefraction);
                material.SetColor(WERenderingHelper.Transmittance, glassColor);
                material.SetFloat("_NormalStrength", normalStrength);
                material.SetFloat("_Thickness", glassThickness);
            }
            public void ResetMaterial()
            {
                if (ownMaterial.IsAllocated)
                {
                    GameObject.Destroy(ownMaterial.Target as Material);
                    ownMaterial.Free();
                }
            }
        }

        private struct WETextDataShader
        {
            public WEShader shader;
            public int decalFlags;
        }





        public const int DEFAULT_DECAL_FLAGS = 8;
        public unsafe static int Size => sizeof(WETextData);

        private WETextDataMaterial materialData;
        private WETextDataShader shaderData;
        public bool templateDirty;

        private GCHandle basicRenderInformation;
        private WESimulationTextType type;
        private FixedString32Bytes atlas;
        private FixedString512Bytes formulaeHandlerStr;
        private bool loadingFnDone;
        private FixedString32Bytes fontName;
        private Entity targetEntity;
        private Entity parentEntity;
        private FixedString32Bytes itemName;
        private FixedString512Bytes m_text;
        private bool dirtyBRI;
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        public int lastLodValue;
        public float maxWidthMeters;
        public bool useAbsoluteSizeEditing;
        public bool InitializedEffectiveText { get; private set; }
        public readonly FixedString512Bytes Text512 => m_text;

        public bool DirtyBRI => dirtyBRI || !basicRenderInformation.IsAllocated || basicRenderInformation.Target is null;

        public int DecalFlags
        {
            readonly get => shaderData.decalFlags; set
            {
                shaderData.decalFlags = value;
                materialData.dirty = true;
            }
        }

        public FixedString512Bytes LastErrorStr { get; private set; }
        public Entity TargetEntity { readonly get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { readonly get => parentEntity; private set => parentEntity = value; }

        public FixedString32Bytes Font
        {
            readonly get => fontName;

            set
            {
                fontName = value;
                dirtyBRI = true;
            }
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

        public float BriWidthMetersUnscaled { get; private set; }


        public Material OwnMaterial
        {
            get
            {

                if (!HasBRI || RenderInformation is null) return null;
                if (RenderInformation.Guid != materialData.ownMaterialGuid)
                {
                    materialData.ResetMaterial();
                    materialData.ownMaterialGuid = RenderInformation.Guid;
                    materialData.dirty = true;
                }
                ;
                if (!materialData.ownMaterial.IsAllocated || materialData.ownMaterial.Target is not Material material || !material)
                {
                    if (!RenderInformation.IsValid())
                    {
                        ResetBri();
                        return null;
                    }
                    materialData.ResetMaterial();
                    material = WERenderingHelper.GenerateMaterial(RenderInformation, shaderData.shader);
                    materialData.ownMaterial = GCHandle.Alloc(material);
                    materialData.dirty = true;
                }
                if (materialData.dirty)
                {
                    switch (shaderData.shader)
                    {
                        case WEShader.Default:
                            if (!RenderInformation.m_isError)
                            {
                                materialData.UpdateDefaultMaterial(material, TextType);
                            }
                            break;
                        case WEShader.Glass:
                            if (!RenderInformation.m_isError)
                            {
                                materialData.UpdateGlassMaterial(material);
                            }
                            break;
                        default:
                            return null;
                    }
                    material.SetFloat(WERenderingHelper.DecalLayerMask, shaderData.decalFlags.ToFloatBitFlags());
                    HDMaterial.ValidateMaterial(material);
                    materialData.dirty = false;
                }
                return material;
            }
        }

        private void ResetBri()
        {
            materialData.ResetMaterial();
            basicRenderInformation.Free();
        }


        public FixedString512Bytes Text
        {
            readonly get => m_text;
            set
            {
                if (m_text != value && TextType == WESimulationTextType.Placeholder)
                {
                    templateDirty = true;
                }
                m_text = value;
                EffectiveText = value;
                dirtyBRI = true;
            }
        }
        public FixedString32Bytes Atlas
        {
            readonly get => atlas;
            set
            {
                atlas = value;
                if (type == WESimulationTextType.Image)
                {
                    dirtyBRI = true;
                }
            }
        }
        public WESimulationTextType TextType
        {
            readonly get => type; set
            {
                if (type != value)
                {
                    type = value;
                    dirtyBRI = true;
                    if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
                }
            }
        }

        public Color32 Color
        {
            readonly get => materialData.color; set
            {
                materialData.color = value;
                materialData.dirty = true;
            }
        }
        public Color32 EmissiveColor
        {
            readonly get => materialData.emissiveColor; set
            {
                materialData.emissiveColor = value;
                materialData.dirty = true;
            }
        }
        public float Metallic
        {
            readonly get => materialData.metallic; set
            {
                materialData.metallic = Mathf.Clamp01(value);
                materialData.dirty = true;
            }
        }
        public float Smoothness
        {
            readonly get => materialData.smoothness; set
            {
                materialData.smoothness = Mathf.Clamp01(value);
                materialData.dirty = true;
            }
        }
        public float EmissiveIntensity
        {
            readonly get => materialData.emissiveIntensity; set
            {
                materialData.emissiveIntensity = Mathf.Clamp01(value);
                materialData.dirty = true;
            }
        }
        public float EmissiveExposureWeight
        {
            readonly get => materialData.emissiveExposureWeight; set
            {
                materialData.emissiveExposureWeight = Mathf.Clamp01(value);
                materialData.dirty = true;
            }
        }
        public float CoatStrength
        {
            readonly get => materialData.coatStrength; set
            {
                materialData.coatStrength = Mathf.Clamp01(value);
                materialData.dirty = true;
            }
        }
        public float GlassRefraction
        {
            readonly get => materialData.glassRefraction; set
            {
                materialData.glassRefraction = Mathf.Clamp(value, 1, 1000);
                materialData.dirty = true;
            }
        }
        public Color GlassColor
        {
            readonly get => materialData.glassColor; set
            {
                materialData.glassColor = value;
                materialData.dirty = true;
            }
        }
        public Color ColorMask1
        {
            readonly get => materialData.colorMask1; set
            {
                materialData.colorMask1 = value;
                materialData.dirty = true;
            }
        }
        public Color ColorMask2
        {
            readonly get => materialData.colorMask2; set
            {
                materialData.colorMask2 = value;
                materialData.dirty = true;
            }
        }
        public Color ColorMask3
        {
            readonly get => materialData.colorMask3; set
            {
                materialData.colorMask3 = value;
                materialData.dirty = true;
            }
        }
        public float NormalStrength
        {
            readonly get => materialData.normalStrength; set
            {
                materialData.normalStrength = Mathf.Clamp(value, 0, 10);
                materialData.dirty = true;
            }
        }
        public float GlassThickness
        {
            readonly get => materialData.glassThickness; set
            {
                materialData.glassThickness = Mathf.Clamp(value, 0.01f, 100);
                materialData.dirty = true;
            }
        }
        public WEShader Shader
        {
            readonly get => shaderData.shader;
            set
            {
                if (shaderData.shader != value)
                {
                    shaderData.shader = value;
                    materialData.ResetMaterial();
                    materialData.dirty = true;
                }
            }
        }
        public FixedString512Bytes Formulae
        {
            get => formulaeHandlerStr;
            private set => formulaeHandlerStr = value;
        }
        public bool HasBRI => basicRenderInformation.IsAllocated;
        public Bounds3 Bounds { get; private set; }
        private readonly Func<EntityManager, Entity, string> FormulaeFn => WEFormulaeHelper.GetCached(formulaeHandlerStr);

        public FixedString32Bytes ItemName
        {
            readonly get => itemName; set => itemName = value;
        }

        public static WETextData CreateDefault(Entity target, Entity? parent = null)
        {
            return new WETextData
            {
                targetEntity = target,
                parentEntity = parent ?? target,
                offsetPosition = new(0, 0, 0),
                offsetRotation = new(),
                scale = new(1, 1, 1),
                shaderData = new()
                {
                    shader = WEShader.Default,
                    decalFlags = DEFAULT_DECAL_FLAGS,
                },
                materialData = new()
                {
                    dirty = true,
                    color = new(0xff, 0xff, 0xff, 0xff),
                    emissiveColor = new(0xff, 0xff, 0xff, 0xff),
                    metallic = 0,
                    smoothness = 0,
                    emissiveIntensity = 0,
                    glassRefraction = 1f,
                    colorMask1 = UnityEngine.Color.white,
                    colorMask2 = UnityEngine.Color.white,
                    colorMask3 = UnityEngine.Color.white,
                    glassColor = UnityEngine.Color.white,
                    glassThickness = .5f,
                    coatStrength = 0f,
                },
                m_text = "NEW TEXT",
                itemName = "New item",
            };
        }

        public bool SetNewParent(Entity e, EntityManager em)
        {
            if ((e != targetEntity && e != Entity.Null && (!em.TryGetComponent<WETextData>(e, out var weData) || weData.TextType == WESimulationTextType.Placeholder || (weData.targetEntity != Entity.Null && weData.targetEntity != targetEntity))))
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"NOPE: e = {e}; weData = {weData}; targetEntity = {targetEntity}; weData.targetEntity = {weData.targetEntity}");
                return false;
            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"YEP: e = {e};  targetEntity = {targetEntity}");
            parentEntity = e;
            return true;
        }
        public void SetNewParentForced(Entity e)
        {
            parentEntity = e;
        }

        public readonly bool IsDirty() => materialData.dirty;
        public readonly bool IsTemplateDirty() => templateDirty;
        public void ClearTemplateDirty() => templateDirty = false;
        public WETextData UpdateBRI(BasicRenderInformation bri, string text)
        {
            materialData.dirty = true;
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            basicRenderInformation = default;
            basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
            Bounds = bri.m_bounds;
            BriWidthMetersUnscaled = bri.m_sizeMetersUnscaled.x;
            if (bri.m_isError)
            {
                LastErrorStr = text;
            }
            dirtyBRI = false;
            return this;
        }

        public void Dispose()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            if (materialData.ownMaterial.IsAllocated)
            {
                GameObject.Destroy(materialData.ownMaterial.Target as Material);
                materialData.ownMaterial.Free();
            }
            basicRenderInformation = default;
            materialData.ownMaterial = default;
        }

        public WETextData OnPostInstantiate(EntityManager em)
        {
            FontServer.Instance.EnsureFont(fontName);
            UpdateEffectiveText(em, targetEntity);
            return this;
        }

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            var result = WEFormulaeHelper.SetFormulae(newFormulae ?? "", out errorFmtArgs, out formulaeHandlerStr, out var resultFormulaeFn);
            if (result == 0)
            {
                dirtyBRI = true;
                if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            }
            return result;
        }

        public void UpdateEffectiveText(EntityManager em, Entity geometryEntity)
        {
            InitializedEffectiveText = true;
            if (!loadingFnDone)
            {
                if (formulaeHandlerStr.Length > 0)
                {
                    SetFormulae(formulaeHandlerStr.ToString(), out _);
                }
                loadingFnDone = true;
            }
            string oldEffText = (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : RenderInformation?.m_refText;
            EffectiveText = FormulaeFn is Func<EntityManager, Entity, string> fn
                ? fn(em, geometryEntity)?.ToString().Trim().Truncate(500) ?? "<InvlidFn>"
                : formulaeHandlerStr.Length > 0 ? "<InvalidFn>" : Text;
            var result = EffectiveText.ToString() != oldEffText;
            if (result) dirtyBRI = true;
        }

        public static Entity GetTargetEntityEffective(Entity target, EntityManager em, bool fullRecursive = false)
        {
            return em.TryGetComponent<WETextData>(target, out var weData)
                ? (fullRecursive || weData.TextType == WESimulationTextType.Archetype) && weData.TargetEntity != target
                    ? GetTargetEntityEffective(weData.TargetEntity, em)
                    : em.TryGetComponent<WETextData>(weData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder
                    ? GetTargetEntityEffective(weDataParent.targetEntity, em)
                    : weData.TargetEntity
                : target;
        }

        public FixedString512Bytes EffectiveText { get; private set; }





        #region Serialize
        public WETextDataXml ToDataXml(EntityManager em)
        {
            return new WETextDataXml
            {
                offsetPosition = (Vector3Xml)offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)offsetRotation).eulerAngles,
                scale = (Vector3Xml)scale,
                itemName = ItemName.ToString(),
                shader = Shader,
                atlas = Atlas.ToString(),
                formulae = Formulae.ToString(),
                text = Text.ToString(),
                textType = TextType,
                maxWidthMeters = maxWidthMeters,
                decalFlags = DecalFlags,
                fontName = fontName.ToString(),
                defaultStyle = new WETextDataXml.WETextDataDefaultStyleXml
                {
                    coatStrength = CoatStrength,
                    color = Color,
                    emissiveColor = EmissiveColor,
                    emissiveExposureWeight = EmissiveExposureWeight,
                    emissiveIntensity = EmissiveIntensity,
                    metallic = Metallic,
                    smoothness = Smoothness,
                    colorMask1 = ColorMask1,
                    colorMask2 = ColorMask2,
                    colorMask3 = ColorMask3,
                },
                glassStyle = new WETextDataXml.WETextDataGlassStyleXml
                {
                    color = Color,
                    glassColor = GlassColor,
                    glassRefraction = GlassRefraction,
                    metallic = Metallic,
                    smoothness = Smoothness,
                    normalStrength = NormalStrength,
                    thickness = GlassThickness
                }
            };
        }
        public WETextDataStruct ToDataStruct(EntityManager em)
        {
            return new WETextDataStruct
            {
                offsetPosition = (Vector3Xml)offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)offsetRotation).eulerAngles,
                scale = (Vector3Xml)scale,
                itemName = ItemName.ToString(),
                shader = Shader,
                atlas = Atlas.ToString(),
                formulae = Formulae,
                text = Text,
                textType = TextType,
                maxWidthMeters = maxWidthMeters,
                decalFlags = DecalFlags,
                fontName = fontName.ToString(),
                defaultStyle = new()
                {
                    coatStrength = CoatStrength,
                    color = Color,
                    emissiveColor = EmissiveColor,
                    emissiveExposureWeight = EmissiveExposureWeight,
                    emissiveIntensity = EmissiveIntensity,
                    metallic = Metallic,
                    smoothness = Smoothness,
                    colorMask1 = ColorMask1,
                    colorMask2 = ColorMask2,
                    colorMask3 = ColorMask3
                },
                glassStyle = new()
                {
                    color = Color,
                    glassColor = GlassColor,
                    glassRefraction = GlassRefraction,
                    metallic = Metallic,
                    smoothness = Smoothness,
                    normalStrength = NormalStrength,
                    thickness = GlassThickness
                },
            };
        }

        public static WETextData FromDataXml(WETextDataXml xml, Entity parent, EntityManager em)
        {
            Entity target;
            if (em.TryGetComponent(parent, out WETextData parentData))
            {
                target = parentData.targetEntity;
            }
            else
            {
                target = parent;
            }

            var weData = new WETextData
            {
                targetEntity = target,
                parentEntity = parent,
                offsetPosition = (float3)xml.offsetPosition,
                offsetRotation = Quaternion.Euler(xml.offsetRotation),
                scale = (float3)xml.scale,
                ItemName = xml.itemName ?? "",
                shaderData = new()
                {
                    shader = xml.shader,
                    decalFlags = xml.decalFlags,
                },
                Atlas = xml.atlas ?? "",
                Formulae = xml.formulae ?? "",
                Text = xml.text ?? "",
                TextType = xml.textType,
                maxWidthMeters = xml.maxWidthMeters,
                fontName = xml.fontName?.Trim() ?? "",
            };
            switch (xml.shader)
            {
                case WEShader.Glass:
                    weData.materialData = new()
                    {
                        color = xml.glassStyle.color,
                        glassColor = xml.glassStyle.glassColor,
                        glassRefraction = xml.glassStyle.glassRefraction,
                        metallic = xml.glassStyle.metallic,
                        smoothness = xml.glassStyle.smoothness,
                        normalStrength = xml.glassStyle.normalStrength,
                        glassThickness = xml.glassStyle.thickness,
                    };
                    break;
                default:
                    weData.materialData = new()
                    {
                        coatStrength = xml.defaultStyle.coatStrength,
                        color = xml.defaultStyle.color,
                        emissiveColor = xml.defaultStyle.emissiveColor,
                        emissiveExposureWeight = xml.defaultStyle.emissiveExposureWeight,
                        emissiveIntensity = xml.defaultStyle.emissiveIntensity,
                        metallic = xml.defaultStyle.metallic,
                        smoothness = xml.defaultStyle.smoothness,
                        colorMask1 = xml.defaultStyle.colorMask1,
                        colorMask2 = xml.defaultStyle.colorMask2,
                        colorMask3 = xml.defaultStyle.colorMask3,
                    };
                    break;
            }
            FontServer.Instance.EnsureFont(weData.fontName);
            weData.UpdateEffectiveText(em, target);
            return weData;
        }

        public static WETextData FromDataStruct(WETextDataStruct xml, Entity parent, EntityManager em)
        {
            Entity target;
            if (em.TryGetComponent(parent, out WETextData parentData))
            {
                target = parentData.targetEntity;
            }
            else
            {
                target = parent;
            }
            var weData = FromDataStruct(xml, parent, target);
            FontServer.Instance.EnsureFont(weData.fontName);
            weData.UpdateEffectiveText(em, target);
            return weData;
        }
        public static WETextData FromDataStruct(WETextDataStruct xml, Entity parent, Entity target)
        {
            var weData = CreateDefault(default, default);

            weData.targetEntity = target;
            weData.parentEntity = parent;
            weData.offsetPosition = xml.offsetPosition;
            weData.offsetRotation = Quaternion.Euler(xml.offsetRotation);
            weData.scale = xml.scale;
            weData.itemName = xml.itemName;
            weData.shaderData = new()
            {
                shader = xml.shader,
                decalFlags = xml.decalFlags,
            };
            weData.atlas = xml.atlas;
            weData.Formulae = xml.formulae;
            weData.Text = xml.text;
            weData.TextType = xml.textType;
            weData.maxWidthMeters = xml.maxWidthMeters;
            weData.fontName = xml.fontName.Trim();

            switch (xml.shader)
            {
                case WEShader.Glass:
                    weData.materialData = new()
                    {
                        color = xml.glassStyle.color,
                        glassColor = xml.glassStyle.glassColor,
                        glassRefraction = xml.glassStyle.glassRefraction,
                        metallic = xml.glassStyle.metallic,
                        smoothness = xml.glassStyle.smoothness,
                        normalStrength = xml.glassStyle.normalStrength,
                        glassThickness = xml.glassStyle.thickness,
                    };
                    break;
                default:
                    weData.materialData = new()
                    {
                        coatStrength = xml.defaultStyle.coatStrength,
                        color = xml.defaultStyle.color,
                        emissiveColor = xml.defaultStyle.emissiveColor,
                        emissiveExposureWeight = xml.defaultStyle.emissiveExposureWeight,
                        emissiveIntensity = xml.defaultStyle.emissiveIntensity,
                        metallic = xml.defaultStyle.metallic,
                        smoothness = xml.defaultStyle.smoothness,
                        colorMask1 = xml.defaultStyle.colorMask1,
                        colorMask2 = xml.defaultStyle.colorMask2,
                        colorMask3 = xml.defaultStyle.colorMask3,
                    };
                    break;
            }
            return weData;
        }

        #endregion
    }
}