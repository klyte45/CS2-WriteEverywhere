

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
        public const int DEFAULT_DECAL_FLAGS = 8;
        public unsafe static int Size => sizeof(WETextData);


        private GCHandle basicRenderInformation;
        private float normalStrength;
        private bool dirty;
        private bool templateDirty;
        private Color32 color;
        private Color32 emissiveColor;
        private Color32 glassColor;
        private float glassRefraction;
        private float metallic;
        private float smoothness;
        private float emissiveIntensity;
        private float emissiveExposureWeight;
        private float coatStrength;
        private WESimulationTextType type;
        private FixedString32Bytes atlas;
        public readonly FixedString512Bytes Text512 => m_text;
        private FixedString512Bytes formulaeHandlerStr;
        private bool loadingFnDone;
        private FixedString32Bytes fontName;
        private Entity targetEntity;
        private Entity parentEntity;
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        private FixedString32Bytes itemName;
        private WEShader shader;
        public float maxWidthMeters;
        private FixedString512Bytes m_text;
        private int decalFlags;
        public int lastLodValue;
        private Colossal.Hash128 ownMaterialGuid;
        private GCHandle ownMaterial;
        private bool dirtyBRI;
        private Color colorMask1;
        private Color colorMask2;
        private Color colorMask3;

        public bool DirtyBRI => dirtyBRI || !basicRenderInformation.IsAllocated || basicRenderInformation.Target is null;

        public int DecalFlags
        {
            readonly get => decalFlags; set
            {
                decalFlags = value;
                dirty = true;
            }
        }
        public bool InitializedEffectiveText { get; private set; }
        public bool useAbsoluteSizeEditing;
        private float glassThickness;

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
                if (RenderInformation.Guid != ownMaterialGuid)
                {
                    ResetMaterial();
                    ownMaterialGuid = RenderInformation.Guid;
                    dirty = true;
                }
                ;
                if (!ownMaterial.IsAllocated || ownMaterial.Target is not Material material || !material)
                {
                    if (!RenderInformation.IsValid())
                    {
                        ResetBri();
                        return null;
                    }
                    ResetMaterial();
                    material = WERenderingHelper.GenerateMaterial(RenderInformation, shader);
                    ownMaterial = GCHandle.Alloc(material);
                    dirty = true;
                }
                if (dirty)
                {
                    switch (shader)
                    {
                        case WEShader.Default:
                            if (!RenderInformation.m_isError)
                            {
                                material.SetColor("_BaseColor", color);
                                material.SetFloat("_Metallic", metallic);
                                material.SetColor("_EmissiveColor", emissiveColor);
                                material.SetFloat("_EmissiveIntensity", emissiveIntensity);
                                material.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight);
                                material.SetFloat("_CoatStrength", coatStrength);
                                material.SetFloat("_Smoothness", smoothness);
                                if (TextType == WESimulationTextType.Image)
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
                            break;
                        case WEShader.Glass:
                            if (!RenderInformation.m_isError)
                            {
                                material.SetColor("_BaseColor", color);
                                material.SetFloat("_Metallic", metallic);
                                material.SetFloat("_Smoothness", smoothness);
                                material.SetFloat(WERenderingHelper.IOR, glassRefraction);
                                material.SetColor(WERenderingHelper.Transmittance, glassColor);
                                material.SetFloat("_NormalStrength", normalStrength);
                                material.SetFloat("_Thickness", glassThickness);
                            }
                            break;
                        default:
                            return null;
                    }
                    material.SetFloat(WERenderingHelper.DecalLayerMask, decalFlags.ToFloatBitFlags());
                    HDMaterial.ValidateMaterial(material);
                    dirty = false;
                }
                return material;
            }
        }

        private void ResetBri()
        {
            ResetMaterial();
            basicRenderInformation.Free();
        }
        private void ResetMaterial()
        {
            if (ownMaterial.IsAllocated)
            {
                GameObject.Destroy(ownMaterial.Target as Material);
                ownMaterial.Free();
            }
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
            readonly get => color; set
            {
                color = value;
                dirty = true;
            }
        }
        public Color32 EmissiveColor
        {
            readonly get => emissiveColor; set
            {
                emissiveColor = value;
                dirty = true;
            }
        }
        public float Metallic
        {
            readonly get => metallic; set
            {
                metallic = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float Smoothness
        {
            readonly get => smoothness; set
            {
                smoothness = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float EmissiveIntensity
        {
            readonly get => emissiveIntensity; set
            {
                emissiveIntensity = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float EmissiveExposureWeight
        {
            readonly get => emissiveExposureWeight; set
            {
                emissiveExposureWeight = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float CoatStrength
        {
            readonly get => coatStrength; set
            {
                coatStrength = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float GlassRefraction
        {
            readonly get => glassRefraction; set
            {
                glassRefraction = Mathf.Clamp(value, 1, 1000);
                dirty = true;
            }
        }
        public Color GlassColor
        {
            readonly get => glassColor; set
            {
                glassColor = value;
                dirty = true;
            }
        }
        public Color ColorMask1
        {
            readonly get => colorMask1; set
            {
                colorMask1 = value;
                dirty = true;
            }
        }
        public Color ColorMask2
        {
            readonly get => colorMask2; set
            {
                colorMask2 = value;
                dirty = true;
            }
        }
        public Color ColorMask3
        {
            readonly get => colorMask3; set
            {
                colorMask3 = value;
                dirty = true;
            }
        }
        public float NormalStrength
        {
            readonly get => normalStrength; set
            {
                normalStrength = Mathf.Clamp(value, 0, 10);
                dirty = true;
            }
        }
        public float GlassThickness
        {
            readonly get => glassThickness; set
            {
                glassThickness = Mathf.Clamp(value, 0.01f, 100);
                dirty = true;
            }
        }
        public WEShader Shader
        {
            readonly get => shader;
            set
            {
                if (shader != value)
                {
                    shader = value;
                    ResetMaterial();
                    dirty = true;
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
            get => itemName; set => itemName = value;
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
                dirty = true,
                color = new(0xff, 0xff, 0xff, 0xff),
                emissiveColor = new(0xff, 0xff, 0xff, 0xff),
                metallic = 0,
                smoothness = 0,
                emissiveIntensity = 0,
                coatStrength = 0f,
                m_text = "NEW TEXT",
                itemName = "New item",
                shader = WEShader.Default,
                decalFlags = DEFAULT_DECAL_FLAGS,
                glassRefraction = 1f,
                colorMask1 = UnityEngine.Color.white,
                colorMask2 = UnityEngine.Color.white,
                colorMask3 = UnityEngine.Color.white,
                glassColor = UnityEngine.Color.white,
                glassThickness = .5f
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

        public readonly bool IsDirty() => dirty;
        public readonly bool IsTemplateDirty() => templateDirty;
        public void ClearTemplateDirty() => templateDirty = false;
        public WETextData UpdateBRI(BasicRenderInformation bri, string text)
        {
            dirty = true;
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
            if (ownMaterial.IsAllocated)
            {
                GameObject.Destroy(ownMaterial.Target as Material);
                ownMaterial.Free();
            }
            basicRenderInformation = default;
            ownMaterial = default;
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
                    colorMask1 = colorMask1,
                    colorMask2 = colorMask2,
                    colorMask3 = colorMask3,
                },
                glassStyle = new WETextDataXml.WETextDataGlassStyleXml
                {
                    color = Color,
                    glassColor = GlassColor,
                    glassRefraction = GlassRefraction,
                    metallic = Metallic,
                    smoothness = Smoothness,
                    normalStrength = normalStrength,
                    thickness = glassThickness
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
                    colorMask1 = colorMask1,
                    colorMask2 = colorMask2,
                    colorMask3 = colorMask3
                },
                glassStyle = new()
                {
                    color = Color,
                    glassColor = GlassColor,
                    glassRefraction = GlassRefraction,
                    metallic = Metallic,
                    smoothness = Smoothness,
                    normalStrength = normalStrength,
                    thickness = glassThickness
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
                shader = xml.shader,
                Atlas = xml.atlas ?? "",
                Formulae = xml.formulae ?? "",
                Text = xml.text ?? "",
                TextType = xml.textType,
                maxWidthMeters = xml.maxWidthMeters,
                decalFlags = xml.decalFlags,
                fontName = xml.fontName?.Trim() ?? "",
            };
            switch (xml.shader)
            {
                case WEShader.Glass:
                    weData.color = xml.glassStyle.color;
                    weData.glassColor = xml.glassStyle.glassColor;
                    weData.glassRefraction = xml.glassStyle.glassRefraction;
                    weData.metallic = xml.glassStyle.metallic;
                    weData.smoothness = xml.glassStyle.smoothness;
                    weData.normalStrength = xml.glassStyle.normalStrength;
                    weData.glassThickness = xml.glassStyle.thickness;
                    break;
                default:
                    weData.coatStrength = xml.defaultStyle.coatStrength;
                    weData.color = xml.defaultStyle.color;
                    weData.emissiveColor = xml.defaultStyle.emissiveColor;
                    weData.emissiveExposureWeight = xml.defaultStyle.emissiveExposureWeight;
                    weData.emissiveIntensity = xml.defaultStyle.emissiveIntensity;
                    weData.metallic = xml.defaultStyle.metallic;
                    weData.smoothness = xml.defaultStyle.smoothness;
                    weData.colorMask1 = xml.defaultStyle.colorMask1;
                    weData.colorMask2 = xml.defaultStyle.colorMask2;
                    weData.colorMask2 = xml.defaultStyle.colorMask3;
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
            weData.shader = xml.shader;
            weData.atlas = xml.atlas;
            weData.Formulae = xml.formulae;
            weData.Text = xml.text;
            weData.TextType = xml.textType;
            weData.maxWidthMeters = xml.maxWidthMeters;
            weData.decalFlags = xml.decalFlags;
            weData.fontName = xml.fontName.Trim();

            switch (xml.shader)
            {
                case WEShader.Glass:
                    weData.color = xml.glassStyle.color;
                    weData.glassColor = xml.glassStyle.glassColor;
                    weData.glassRefraction = xml.glassStyle.glassRefraction;
                    weData.metallic = xml.glassStyle.metallic;
                    weData.smoothness = xml.glassStyle.smoothness;
                    weData.normalStrength = xml.glassStyle.normalStrength;
                    weData.glassThickness = xml.glassStyle.thickness;
                    break;
                default:
                    weData.coatStrength = xml.defaultStyle.coatStrength;
                    weData.color = xml.defaultStyle.color;
                    weData.emissiveColor = xml.defaultStyle.emissiveColor;
                    weData.emissiveExposureWeight = xml.defaultStyle.emissiveExposureWeight;
                    weData.emissiveIntensity = xml.defaultStyle.emissiveIntensity;
                    weData.metallic = xml.defaultStyle.metallic;
                    weData.smoothness = xml.defaultStyle.smoothness;
                    weData.colorMask1 = xml.defaultStyle.colorMask1;
                    weData.colorMask2 = xml.defaultStyle.colorMask2;
                    weData.colorMask2 = xml.defaultStyle.colorMask3;
                    break;
            }
            return weData;
        }

        #endregion
    }
}