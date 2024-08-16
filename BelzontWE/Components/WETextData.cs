

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Colossal.Entities;
using Colossal.Mathematics;
using Kwytto.Utils;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextData : IDisposable, IComponentData
    {
        public const int DEFAULT_DECAL_FLAGS = 8;
        //8 = Exclude surface areas
        //4 = Accept decals
        public unsafe static int Size => sizeof(WETextData);
        private static int shader_colossal_LodDistanceFactor = UnityEngine.Shader.PropertyToID("colossal_LodDistanceFactor");


        private GCHandle basicRenderInformation;
        private GCHandle materialBlockPtr;
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

        public int DecalFlags
        {
            readonly get => decalFlags; set
            {
                decalFlags = value;
                dirty = true;
            }
        }
        public readonly bool IsGlass => Shader == WEShader.Glass;
        public bool InitializedEffectiveText { get; private set; }
        public bool useAbsoluteSizeEditing;

        public FixedString512Bytes LastErrorStr { get; private set; }
        public Entity TargetEntity { readonly get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { readonly get => parentEntity; private set => parentEntity = value; }

        public Entity Font
        {
            readonly get => FontServer.Instance.TryGetFontEntity(fontName, out var data) ? data : default;

            set
            {
                if (value == Entity.Null)
                {
                    fontName = "";
                    if (basicRenderInformation.IsAllocated)
                    {
                        basicRenderInformation.Free();
                        basicRenderInformation = default;
                    }
                }
                else if (FontServer.Instance.EntityManager.TryGetComponent(value, out FontSystemData fsd))
                {
                    fontName = fsd.Name;
                    if (basicRenderInformation.IsAllocated)
                    {
                        basicRenderInformation.Free();
                        basicRenderInformation = default;
                    }
                }
            }
        }
        public BasicRenderInformation RenderInformation
        {
            get
            {
                if (basicRenderInformation.IsAllocated && basicRenderInformation.Target is BasicRenderInformation bri && bri.Mesh != null)
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
        public MaterialPropertyBlock MaterialProperties
        {
            get
            {
                MaterialPropertyBlock block;
                if (!materialBlockPtr.IsAllocated)
                {
                    block = new MaterialPropertyBlock();
                    materialBlockPtr = GCHandle.Alloc(block, GCHandleType.Normal);
                    dirty = true;
                }
                else
                {
                    block = materialBlockPtr.Target as MaterialPropertyBlock;
                }
                if (dirty)
                {
                    block.SetColor("_BaseColor", color);
                    block.SetFloat("_Metallic", metallic);
                    block.SetColor("_EmissiveColor", emissiveColor);
                    block.SetFloat("_EmissiveIntensity", emissiveIntensity);
                    block.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight);
                    block.SetFloat("_CoatStrength", coatStrength);
                    block.SetFloat("_Smoothness", smoothness);
                    block.SetFloat(FontServer.DecalLayerMask, decalFlags.ToFloatBitFlags());
                    block.SetFloat(FontServer.IOR, glassRefraction);
                    block.SetColor(FontServer.Transmittance, glassColor);
                    //"COLOSSAL_GEOMETRY_TILING"
                    dirty = false;
                }
                return block;
            }
        }


        public string Text
        {
            readonly get => m_text.ToString();
            set
            {
                if (m_text != value && TextType == WESimulationTextType.Placeholder)
                {
                    templateDirty = true;
                }
                m_text = value ?? "";
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
                EffectiveText = value;
            }
        }
        public FixedString32Bytes Atlas
        {
            readonly get => atlas;
            set
            {
                atlas = value;
                if (type == WESimulationTextType.Image && basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
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
                    if (basicRenderInformation.IsAllocated)
                    {
                        basicRenderInformation.Free();
                        basicRenderInformation = default;
                    }
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
        public string Formulae
        {
            get => formulaeHandlerStr.ToString();
            private set => formulaeHandlerStr = value ?? "";
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
                TargetEntity = target,
                ParentEntity = parent ?? target,
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
                ItemName = "New item",
                Shader = WEShader.Default,
                decalFlags = DEFAULT_DECAL_FLAGS,
                glassRefraction = .5f
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
            basicRenderInformation = GCHandle.Alloc(bri, bri.m_refText == "" ? GCHandleType.Normal : GCHandleType.Weak);
            Bounds = bri.m_bounds;
            BriWidthMetersUnscaled = bri.m_sizeMetersUnscaled.x;
            if (bri.m_isError)
            {
                LastErrorStr = text;
            }
            return this;
        }

        public void Dispose()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            if (materialBlockPtr.IsAllocated) materialBlockPtr.Free();
            basicRenderInformation = default;
            materialBlockPtr = default;
        }

        public WETextData OnPostInstantiate()
        {
            basicRenderInformation = default;
            materialBlockPtr = default;
            return this;
        }

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs) => WEFormulaeHelper.SetFormulae(newFormulae ?? "", out errorFmtArgs, out formulaeHandlerStr, out var resultFormulaeFn);

        public bool UpdateEffectiveText(EntityManager em, Entity geometryEntity)
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
            return EffectiveText.ToString() != oldEffText;
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
        public WEShader Shader
        {
            readonly get => shader;
            set
            {
                shader = value;
                dirty = true;
            }
        }




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
                formulae = Formulae,
                text = Text,
                textType = TextType,
                maxWidthMeters = maxWidthMeters,
                decalFlags = DecalFlags,
                fontName = fontName.ToString(),
                style = new WETextDataXml.WETextDataStyleXml
                {
                    coatStrength = CoatStrength,
                    color = Color,
                    emissiveColor = EmissiveColor,
                    emissiveExposureWeight = EmissiveExposureWeight,
                    emissiveIntensity = EmissiveIntensity,
                    glassColor = GlassColor,
                    glassRefraction = GlassRefraction,
                    metallic = Metallic,
                    smoothness = Smoothness
                }
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
                Shader = xml.shader,
                Atlas = xml.atlas ?? "",
                Formulae = xml.formulae ?? "",
                Text = xml.text ?? "",
                TextType = xml.textType,
                CoatStrength = xml.style.coatStrength,
                Color = xml.style.color,
                EmissiveColor = xml.style.emissiveColor,
                GlassColor = xml.style.glassColor,
                GlassRefraction = xml.style.glassRefraction,
                EmissiveExposureWeight = xml.style.emissiveExposureWeight,
                EmissiveIntensity = xml.style.emissiveIntensity,
                Metallic = xml.style.metallic,
                Smoothness = xml.style.smoothness,
                maxWidthMeters = xml.maxWidthMeters,
                decalFlags = xml.decalFlags,
                fontName = xml.fontName?.Trim() ?? ""
            };
            FontServer.Instance.EnsureFont(weData.fontName);
            weData.UpdateEffectiveText(em, target);
            return weData;
        }

        #endregion
    }
}