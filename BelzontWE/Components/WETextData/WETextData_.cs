

//using Belzont.Interfaces;
//using Belzont.Utils;
//using BelzontWE.Font.Utility;
//using Colossal.Entities;
//using Colossal.Mathematics;
//using System;
//using System.Runtime.InteropServices;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.Rendering.HighDefinition;

//namespace BelzontWE
//{
//    public partial struct WETextData_ : IDisposable, IComponentData
//    {
//        public const int DEFAULT_DECAL_FLAGS = 8;
//        public unsafe static int Size => sizeof(WETextData_);

//        private WETextDataMaterial materialData;
//        private WETextDataShader shaderData;
//        private WETextDataTransform transformData;
//        private WETextDataValueString valueData;

//        public bool templateDirty;

//        private GCHandle basicRenderInformation;
//        private WESimulationTextType type;
//        private FixedString32Bytes atlas;
//        private FixedString32Bytes fontName;

//        private FixedString32Bytes itemName;
//        private Entity targetEntity;
//        private Entity parentEntity;

//        private bool dirtyBRI;
//        public int lastLodValue;
//        public float maxWidthMeters;


//        private void ResetBri()
//        {
//            materialData.ResetMaterial();
//            basicRenderInformation.Free();
//        }

//        public int SetFormulae(string value, out string[] cmpErr) => valueData.SetFormulae(value, out cmpErr);

//        public static WETextData_ CreateDefault(Entity target, Entity? parent = null)
//        {
//            return new WETextData_
//            {
//                targetEntity = target,
//                parentEntity = parent ?? target,
//                transformData = new()
//                {
//                    offsetPosition = new(0, 0, 0),
//                    offsetRotation = new(),
//                    scale = new(1, 1, 1),
//                },
//                shaderData = new()
//                {
//                    shader = WEShader.Default,
//                    decalFlags = DEFAULT_DECAL_FLAGS,
//                },
//                materialData = new()
//                {
//                    dirty = true,
//                    color = new() { defaultValue = new(0xff, 0xff, 0xff, 0xff) },
//                    emissiveColor = new() { defaultValue = new(0xff, 0xff, 0xff, 0xff) },
//                    metallic = new() { defaultValue = 0 },
//                    smoothness = new() { defaultValue = 0 },
//                    emissiveIntensity = new() { defaultValue = 0 },
//                    glassRefraction = new() { defaultValue = 1f },
//                    colorMask1 = new() { defaultValue = UnityEngine.Color.white },
//                    colorMask2 = new() { defaultValue = UnityEngine.Color.white },
//                    colorMask3 = new() { defaultValue = UnityEngine.Color.white },
//                    glassColor = new() { defaultValue = UnityEngine.Color.white },
//                    glassThickness = new() { defaultValue = .5f },
//                    coatStrength = new() { defaultValue = 0f },
//                },
//                valueData = new()
//                {
//                    defaultValue = "NEW TEXT"
//                },
//                itemName = "New item",
//            };
//        }

//        public bool SetNewParent(Entity e, EntityManager em)
//        {
//            if ((e != targetEntity && e != Entity.Null && (!em.TryGetComponent<WETextData_>(e, out var weData) || weData.TextType == WESimulationTextType.Placeholder || (weData.targetEntity != Entity.Null && weData.targetEntity != targetEntity))))
//            {
//                if (BasicIMod.DebugMode) LogUtils.DoLog($"NOPE: e = {e}; weData = {weData}; targetEntity = {targetEntity}; weData.targetEntity = {weData.targetEntity}");
//                return false;
//            }
//            if (BasicIMod.DebugMode) LogUtils.DoLog($"YEP: e = {e};  targetEntity = {targetEntity}");
//            parentEntity = e;
//            return true;
//        }
//        public void SetNewParentForced(Entity e)
//        {
//            parentEntity = e;
//        }

//        public readonly bool IsTemplateDirty() => templateDirty;
//        public void ClearTemplateDirty() => templateDirty = false;
//        public WETextData_ UpdateBRI(BasicRenderInformation bri, string text)
//        {
//            materialData.dirty = true;
//            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
//            basicRenderInformation = default;
//            basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
//            Bounds = bri.m_bounds;
//            BriWidthMetersUnscaled = bri.m_sizeMetersUnscaled.x;
//            if (bri.m_isError)
//            {
//                LastErrorStr = text;
//            }
//            dirtyBRI = false;
//            return this;
//        }

//        public void Dispose()
//        {
//            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
//            if (materialData.ownMaterial.IsAllocated)
//            {
//                GameObject.Destroy(materialData.ownMaterial.Target as Material);
//                materialData.ownMaterial.Free();
//            }
//            basicRenderInformation = default;
//            materialData.ownMaterial = default;
//        }

//        public WETextData_ OnPostInstantiate(EntityManager em)
//        {
//            FontServer.Instance.EnsureFont(fontName);
//            UpdateEffectiveText(em, targetEntity);
//            return this;
//        }


//        public void UpdateEffectiveText(EntityManager em, Entity geometryEntity)
//        {
//            var result = valueData.UpdateEffectiveText(em, geometryEntity, (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : RenderInformation?.m_refText);
//            if (result) dirtyBRI = true;
//        }

//        public static Entity GetTargetEntityEffective(Entity target, EntityManager em, bool fullRecursive = false)
//        {
//            return em.TryGetComponent<WETextData_>(target, out var weData)
//                ? (fullRecursive || weData.TextType == WESimulationTextType.Archetype) && weData.TargetEntity != target
//                    ? GetTargetEntityEffective(weData.TargetEntity, em)
//                    : em.TryGetComponent<WETextData_>(weData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder
//                    ? GetTargetEntityEffective(weDataParent.targetEntity, em)
//                    : weData.TargetEntity
//                : target;
//        }

//        #region Public getters & setters
//        public readonly bool InitializedEffectiveText => valueData.InitializedEffectiveText;

//        public bool DirtyBRI => dirtyBRI || !basicRenderInformation.IsAllocated || basicRenderInformation.Target is null;

//        public int DecalFlags
//        {
//            readonly get => shaderData.decalFlags; set
//            {
//                shaderData.decalFlags = value;
//                materialData.dirty = true;
//            }
//        }

//        public FixedString512Bytes LastErrorStr { get; private set; }
//        public Entity TargetEntity { readonly get => targetEntity; set => targetEntity = value; }
//        public Entity ParentEntity { readonly get => parentEntity; private set => parentEntity = value; }

//        public FixedString32Bytes Font
//        {
//            readonly get => fontName;

//            set
//            {
//                fontName = value;
//                dirtyBRI = true;
//            }
//        }
//        public BasicRenderInformation RenderInformation
//        {
//            get
//            {
//                if (basicRenderInformation.IsAllocated && basicRenderInformation.Target is BasicRenderInformation bri && bri.IsValid())
//                {
//                    return bri;
//                }
//                else
//                {
//                    if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
//                    return null;
//                }
//            }
//        }

//        public float BriWidthMetersUnscaled { get; private set; }


//        public Material OwnMaterial
//        {
//            get
//            {

//                if (!HasBRI || RenderInformation is null) return null;
//                if (RenderInformation.Guid != materialData.ownMaterialGuid)
//                {
//                    materialData.ResetMaterial();
//                    materialData.ownMaterialGuid = RenderInformation.Guid;
//                    materialData.dirty = true;
//                }
//                ;
//                if (!materialData.ownMaterial.IsAllocated || materialData.ownMaterial.Target is not Material material || !material)
//                {
//                    if (!RenderInformation.IsValid())
//                    {
//                        ResetBri();
//                        return null;
//                    }
//                    materialData.ResetMaterial();
//                    material = WERenderingHelper.GenerateMaterial(RenderInformation, shaderData.shader);
//                    materialData.ownMaterial = GCHandle.Alloc(material);
//                    materialData.dirty = true;
//                }
//                if (materialData.dirty)
//                {
//                    switch (shaderData.shader)
//                    {
//                        case WEShader.Default:
//                            if (!RenderInformation.m_isError)
//                            {
//                                materialData.UpdateDefaultMaterial(material, TextType);
//                            }
//                            break;
//                        case WEShader.Glass:
//                            if (!RenderInformation.m_isError)
//                            {
//                                materialData.UpdateGlassMaterial(material);
//                            }
//                            break;
//                        default:
//                            return null;
//                    }
//                    material.SetFloat(WERenderingHelper.DecalLayerMask, shaderData.decalFlags.ToFloatBitFlags());
//                    HDMaterial.ValidateMaterial(material);
//                    materialData.dirty = false;
//                }
//                return material;
//            }
//        }

//        public FixedString512Bytes Text
//        {
//            readonly get => valueData.defaultValue;
//            set
//            {
//                if (valueData.defaultValue != value && TextType == WESimulationTextType.Placeholder)
//                {
//                    templateDirty = true;
//                }
//                valueData.defaultValue = value;
//                dirtyBRI = true;
//            }
//        }
//        public FixedString32Bytes Atlas
//        {
//            readonly get => atlas;
//            set
//            {
//                atlas = value;
//                if (type == WESimulationTextType.Image)
//                {
//                    dirtyBRI = true;
//                }
//            }
//        }
//        public WESimulationTextType TextType
//        {
//            readonly get => type; set
//            {
//                if (type != value)
//                {
//                    type = value;
//                    dirtyBRI = true;
//                    if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
//                }
//            }
//        }

//        public float3 OffsetPosition { readonly get => transformData.offsetPosition; set => transformData.offsetPosition = value; }
//        public quaternion OffsetRotation { readonly get => transformData.offsetRotation; set => transformData.offsetRotation = value; }
//        public float3 Scale { readonly get => transformData.scale; set => transformData.scale = value; }
//        public bool UseAbsoluteSizeEditing { get => transformData.useAbsoluteSizeEditing; set => transformData.useAbsoluteSizeEditing = value; }
//        public Color32 Color
//        {
//            readonly get => materialData.color.defaultValue; set
//            {
//                materialData.color.defaultValue = value;
//                materialData.dirty = true;
//            }
//        }
//        public Color32 EmissiveColor
//        {
//            readonly get => materialData.emissiveColor.defaultValue; set
//            {
//                materialData.emissiveColor.defaultValue = value;
//                materialData.dirty = true;
//            }
//        }
//        public float Metallic
//        {
//            readonly get => materialData.metallic.defaultValue; set
//            {
//                materialData.metallic.defaultValue = Mathf.Clamp01(value);
//                materialData.dirty = true;
//            }
//        }
//        public float Smoothness
//        {
//            readonly get => materialData.smoothness.defaultValue; set
//            {
//                materialData.smoothness.defaultValue = Mathf.Clamp01(value);
//                materialData.dirty = true;
//            }
//        }
//        public float EmissiveIntensity
//        {
//            readonly get => materialData.emissiveIntensity.defaultValue; set
//            {
//                materialData.emissiveIntensity.defaultValue = Mathf.Clamp01(value);
//                materialData.dirty = true;
//            }
//        }
//        public float EmissiveExposureWeight
//        {
//            readonly get => materialData.emissiveExposureWeight.defaultValue; set
//            {
//                materialData.emissiveExposureWeight.defaultValue = Mathf.Clamp01(value);
//                materialData.dirty = true;
//            }
//        }
//        public float CoatStrength
//        {
//            readonly get => materialData.coatStrength.defaultValue; set
//            {
//                materialData.coatStrength.defaultValue = Mathf.Clamp01(value);
//                materialData.dirty = true;
//            }
//        }
//        public float GlassRefraction
//        {
//            readonly get => materialData.glassRefraction.defaultValue; set
//            {
//                materialData.glassRefraction.defaultValue = Mathf.Clamp(value, 1, 1000);
//                materialData.dirty = true;
//            }
//        }
//        public Color GlassColor
//        {
//            readonly get => materialData.glassColor.defaultValue; set
//            {
//                materialData.glassColor.defaultValue = value;
//                materialData.dirty = true;
//            }
//        }
//        public Color ColorMask1
//        {
//            readonly get => materialData.colorMask1.defaultValue; set
//            {
//                materialData.colorMask1.defaultValue = value;
//                materialData.dirty = true;
//            }
//        }
//        public Color ColorMask2
//        {
//            readonly get => materialData.colorMask2.defaultValue; set
//            {
//                materialData.colorMask2.defaultValue = value;
//                materialData.dirty = true;
//            }
//        }
//        public Color ColorMask3
//        {
//            readonly get => materialData.colorMask3.defaultValue; set
//            {
//                materialData.colorMask3.defaultValue = value;
//                materialData.dirty = true;
//            }
//        }
//        public float NormalStrength
//        {
//            readonly get => materialData.normalStrength.defaultValue; set
//            {
//                materialData.normalStrength.defaultValue = Mathf.Clamp(value, 0, 10);
//                materialData.dirty = true;
//            }
//        }
//        public float GlassThickness
//        {
//            readonly get => materialData.glassThickness.defaultValue; set
//            {
//                materialData.glassThickness.defaultValue = Mathf.Clamp(value, 0.01f, 100);
//                materialData.dirty = true;
//            }
//        }
//        public WEShader Shader
//        {
//            readonly get => shaderData.shader;
//            set
//            {
//                if (shaderData.shader != value)
//                {
//                    shaderData.shader = value;
//                    materialData.ResetMaterial();
//                    materialData.dirty = true;
//                }
//            }
//        }
//        public FixedString512Bytes Formulae
//        {
//            get => valueData.formulaeStr;
//            private set => valueData.formulaeStr = value;
//        }
//        public readonly FixedString512Bytes EffectiveText => valueData.EffectiveValue;
//        public bool HasBRI => basicRenderInformation.IsAllocated;
//        public Bounds3 Bounds { get; private set; }

//        public FixedString32Bytes ItemName
//        {
//            readonly get => itemName; set => itemName = value;
//        }
//        #endregion

//        #region Serialize
//        public WETextDataXml ToDataXml(EntityManager em) => ToDataStruct(em).ToXml();
//        public WETextDataStruct ToDataStruct(EntityManager em)
//        {
//            return new WETextDataStruct
//            {
//                offsetPosition = (Vector3Xml)transformData.offsetPosition,
//                offsetRotation = (Vector3Xml)((Quaternion)transformData.offsetRotation).eulerAngles,
//                scale = (Vector3Xml)transformData.scale,
//                itemName = ItemName.ToString(),
//                shader = Shader,
//                atlas = Atlas.ToString(),
//                formulae = Formulae,
//                text = Text,
//                textType = TextType,
//                maxWidthMeters = maxWidthMeters,
//                decalFlags = DecalFlags,
//                fontName = fontName.ToString(),
//                defaultStyle = new()
//                {
//                    coatStrength = materialData.coatStrength.ToDataStruct(),
//                    color = materialData.color.ToDataStruct(),
//                    emissiveColor = materialData.emissiveColor.ToDataStruct(),
//                    emissiveExposureWeight = materialData.emissiveExposureWeight.ToDataStruct(),
//                    emissiveIntensity = materialData.emissiveIntensity.ToDataStruct(),
//                    metallic = materialData.metallic.ToDataStruct(),
//                    smoothness = materialData.smoothness.ToDataStruct(),
//                    colorMask1 = materialData.colorMask1.ToDataStruct(),
//                    colorMask2 = materialData.colorMask2.ToDataStruct(),
//                    colorMask3 = materialData.colorMask3.ToDataStruct(),
//                },
//                glassStyle = new()
//                {
//                    color = materialData.color.ToDataStruct(),
//                    glassColor = materialData.glassColor.ToDataStruct(),
//                    glassRefraction = materialData.glassRefraction.ToDataStruct(),
//                    metallic = materialData.metallic.ToDataStruct(),
//                    smoothness = materialData.smoothness.ToDataStruct(),
//                    normalStrength = materialData.normalStrength.ToDataStruct(),
//                    thickness = materialData.glassThickness.ToDataStruct()
//                },
//            };
//        }

//        public static WETextData_ FromDataStruct(WETextDataStruct xml, Entity parent, Entity target)
//        {
//            var weData = CreateDefault(default, default);

//            weData.targetEntity = target;
//            weData.parentEntity = parent;
//            weData.itemName = xml.itemName;
//            weData.transformData = new()
//            {
//                offsetPosition = xml.offsetPosition,
//                offsetRotation = Quaternion.Euler(xml.offsetRotation),
//                scale = xml.scale
//            };
//            weData.shaderData = new()
//            {
//                shader = xml.shader,
//                decalFlags = xml.decalFlags,
//            };
//            weData.atlas = xml.atlas;
//            weData.Formulae = xml.formulae;
//            weData.Text = xml.text;
//            weData.TextType = xml.textType;
//            weData.maxWidthMeters = xml.maxWidthMeters;
//            weData.fontName = xml.fontName.Trim();

//            switch (xml.shader)
//            {
//                case WEShader.Glass:
//                    weData.materialData = new()
//                    {
//                        color = WETextDataValueColor.FromStruct(xml.glassStyle.color),
//                        glassColor = WETextDataValueColor.FromStruct(xml.glassStyle.glassColor),
//                        glassRefraction = WETextDataValueFloat.FromStruct(xml.glassStyle.glassRefraction),
//                        metallic = WETextDataValueFloat.FromStruct(xml.glassStyle.metallic),
//                        smoothness = WETextDataValueFloat.FromStruct(xml.glassStyle.smoothness),
//                        normalStrength = WETextDataValueFloat.FromStruct(xml.glassStyle.normalStrength),
//                        glassThickness = WETextDataValueFloat.FromStruct(xml.glassStyle.thickness),
//                    };
//                    break;
//                default:
//                    weData.materialData = new()
//                    {
//                        color = WETextDataValueColor.FromStruct(xml.defaultStyle.color),
//                        emissiveColor = WETextDataValueColor.FromStruct(xml.defaultStyle.emissiveColor),
//                        colorMask1 = WETextDataValueColor.FromStruct(xml.defaultStyle.colorMask1),
//                        colorMask2 = WETextDataValueColor.FromStruct(xml.defaultStyle.colorMask2),
//                        colorMask3 = WETextDataValueColor.FromStruct(xml.defaultStyle.colorMask3),
//                        coatStrength = WETextDataValueFloat.FromStruct(xml.defaultStyle.coatStrength),
//                        emissiveExposureWeight = WETextDataValueFloat.FromStruct(xml.defaultStyle.emissiveExposureWeight),
//                        emissiveIntensity = WETextDataValueFloat.FromStruct(xml.defaultStyle.emissiveIntensity),
//                        metallic = WETextDataValueFloat.FromStruct(xml.defaultStyle.metallic),
//                        smoothness = WETextDataValueFloat.FromStruct(xml.defaultStyle.smoothness),
//                    };
//                    break;
//            }
//            return weData;
//        }


//        #endregion
//    }
//}