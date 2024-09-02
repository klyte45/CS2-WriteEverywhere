

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
    public partial struct WETextData : IDisposable, IComponentData
    {
        public const int DEFAULT_DECAL_FLAGS = 8;
        public unsafe static int Size => sizeof(WETextData);

        private WETextDataMaterial materialData;
        private WETextDataShader shaderData;
        private WETextDataTransform transformData;
        private WETextDataValueString valueData;

        public bool templateDirty;

        private GCHandle basicRenderInformation;
        private WESimulationTextType type;
        private FixedString32Bytes atlas;
        private FixedString32Bytes fontName;

        private FixedString32Bytes itemName;
        private Entity targetEntity;
        private Entity parentEntity;

        private bool dirtyBRI;
        public int lastLodValue;
        public float maxWidthMeters;

        #region Public getters & setters
        public readonly bool InitializedEffectiveText => valueData.InitializedEffectiveText;

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

        public FixedString512Bytes Text
        {
            readonly get => valueData.m_text;
            set
            {
                if (valueData.m_text != value && TextType == WESimulationTextType.Placeholder)
                {
                    templateDirty = true;
                }
                valueData.m_text = value;
                valueData.EffectiveText = value;
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

        public float3 OffsetPosition { readonly get => transformData.offsetPosition; set => transformData.offsetPosition = value; }
        public quaternion OffsetRotation { readonly get => transformData.offsetRotation; set => transformData.offsetRotation = value; }
        public float3 Scale { readonly get => transformData.scale; set => transformData.scale = value; }
        public bool UseAbsoluteSizeEditing { get => transformData.useAbsoluteSizeEditing; set => transformData.useAbsoluteSizeEditing = value; }
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
            get => valueData.formulaeHandlerStr;
            private set => valueData.formulaeHandlerStr = value;
        }
        public readonly FixedString512Bytes EffectiveText => valueData.EffectiveText;
        public bool HasBRI => basicRenderInformation.IsAllocated;
        public Bounds3 Bounds { get; private set; }

        public FixedString32Bytes ItemName
        {
            readonly get => itemName; set => itemName = value;
        }
#endregion

        private void ResetBri()
        {
            materialData.ResetMaterial();
            basicRenderInformation.Free();
        }


        public int SetFormulae(string value, out string[] cmpErr) => valueData.SetFormulae(value, out cmpErr);
        public static WETextData CreateDefault(Entity target, Entity? parent = null)
        {
            return new WETextData
            {
                targetEntity = target,
                parentEntity = parent ?? target,
                transformData = new()
                {
                    offsetPosition = new(0, 0, 0),
                    offsetRotation = new(),
                    scale = new(1, 1, 1),
                },
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
                valueData = new()
                {
                    m_text = "NEW TEXT"
                },
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


        public void UpdateEffectiveText(EntityManager em, Entity geometryEntity)
        {
            var result = valueData.UpdateEffectiveText(em, geometryEntity, (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : RenderInformation?.m_refText);
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


        #region Serialize
        public WETextDataXml ToDataXml(EntityManager em)
        {
            return new WETextDataXml
            {
                offsetPosition = (Vector3Xml)transformData.offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)transformData.offsetRotation).eulerAngles,
                scale = (Vector3Xml)transformData.scale,
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
                offsetPosition = (Vector3Xml)transformData.offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)transformData.offsetRotation).eulerAngles,
                scale = (Vector3Xml)transformData.scale,
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
                transformData = new()
                {
                    offsetPosition = (float3)xml.offsetPosition,
                    offsetRotation = Quaternion.Euler(xml.offsetRotation),
                    scale = (float3)xml.scale
                },
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
            Entity target = em.TryGetComponent(parent, out WETextData parentData) ? parentData.targetEntity : parent;
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
            weData.itemName = xml.itemName;
            weData.transformData = new()
            {
                offsetPosition = xml.offsetPosition,
                offsetRotation = Quaternion.Euler(xml.offsetRotation),
                scale = xml.scale
            };
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