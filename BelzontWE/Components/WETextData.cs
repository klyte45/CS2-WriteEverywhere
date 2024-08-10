

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextData : ISerializable, IDisposable, IComponentData
    {
        public const uint CURRENT_VERSION = 3;
        public unsafe static int Size => sizeof(WETextData);


        private GCHandle basicRenderInformation;
        private GCHandle materialBlockPtr;
        private bool dirty;
        private bool templateDirty;
        private Color32 color;
        private Color32 emissiveColor;
        private float metallic;
        private float smoothness;
        private float emissiveIntensity;
        private float emissiveExposureWeight;
        private float coatStrength;
        private WESimulationTextType type;
        private GCHandle atlas;
        public readonly FixedString512Bytes Text512 => m_text;
        private FixedString512Bytes formulaeHandlerStr;
        private GCHandle formulaeHandlerFn;
        private bool loadingFnDone;
        private Entity font;
        private FixedString32Bytes fontName;
        private Entity targetEntity;
        private Entity parentEntity;
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        private FixedString32Bytes itemName;
        public WEShader shader;
        public float maxWidthMeters;
        private FixedString512Bytes m_text;

        public bool InitializedEffectiveText { get; private set; }

        public FixedString512Bytes LastErrorStr { get; private set; }
        public Entity TargetEntity { readonly get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { readonly get => parentEntity; private set => parentEntity = value; }

        public Entity Font
        {
            get
            {
                if (fontName != "" && !FontServer.Instance.EntityManager.Exists(font) && (font = FontServer.Instance[fontName.ToString()]) == Entity.Null)
                {
                    fontName = "";
                }
                return font;
            }

            set
            {
                if (value == Entity.Null)
                {
                    font = default;
                    fontName = "";
                    if (basicRenderInformation.IsAllocated)
                    {
                        basicRenderInformation.Free();
                        basicRenderInformation = default;
                    }
                }
                else if (FontServer.Instance.EntityManager.TryGetComponent(value, out FontSystemData fsd))
                {
                    font = value;
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
        public string Atlas
        {
            readonly get => atlas.IsAllocated ? atlas.Target as string ?? "" : "";
            set
            {
                if (atlas.IsAllocated) atlas.Free();
                atlas = GCHandle.Alloc(value);
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
        public string Formulae
        {
            get => formulaeHandlerStr.ToString();
            private set => formulaeHandlerStr = value ?? "";
        }
        public bool HasFormulae => formulaeHandlerFn.IsAllocated;
        public bool HasBRI => basicRenderInformation.IsAllocated;
        public Bounds3 Bounds { get; private set; }
        private Func<EntityManager, Entity, string> FormulaeFn
        {
            get => formulaeHandlerFn.IsAllocated ? formulaeHandlerFn.Target as Func<EntityManager, Entity, string> : null;
            set
            {
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                if (value != null) formulaeHandlerFn = GCHandle.Alloc(value);
            }
        }

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
                coatStrength = 0.5f,
                m_text = "NEW TEXT",
                ItemName = "New item",
                shader = WEShader.Default
            };
        }

        public bool SetNewParent(Entity e, EntityManager em)
        {
            if (e != targetEntity && e != Entity.Null && (!em.TryGetComponent<WETextData>(e, out var weData) || (weData.targetEntity != Entity.Null && weData.targetEntity != targetEntity)))
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
            basicRenderInformation.Free();
            materialBlockPtr.Free();
            formulaeHandlerFn.Free();
            atlas.Free();
        }


        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            var result = WEFormulaeHelper.SetFormulae(newFormulae, out errorFmtArgs, out var formulaeStr, out var resultFormulaeFn);
            if (result == 0)
            {
                Formulae = formulaeStr;
                FormulaeFn = resultFormulaeFn;
            }
            return result;
        }

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

        //public static Entity GetTargetEntityEffective2(Entity target, EntityManager em, bool fullRecursive = false)
        //{
        //    var result = em.TryGetComponent<WETextData>(target, out var weData)
        //        ? weData.TargetEntity == target && weData.ParentEntity != target
        //            ? GetTargetEntityEffective(weData.ParentEntity, em)
        //            : weData.TargetEntity != target && weData.TargetEntity != Entity.Null
        //            ? GetTargetEntityEffective(weData.TargetEntity, em)
        //            : target
        //        : target;

        //    LogUtils.DoLog($"PARENT FOR {target} => {result}");
        //    return result;
        //}
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



        public void OnPostInstantiate()
        {
            formulaeHandlerFn = formulaeHandlerFn.IsAllocated ? GCHandle.Alloc(formulaeHandlerFn.Target) : default;
            atlas = atlas.IsAllocated ? GCHandle.Alloc(atlas.Target) : default;
            basicRenderInformation = default;
            materialBlockPtr = default;
        }

        #region Serialize
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(TargetEntity);
            writer.Write((byte)shader);
            writer.Write(offsetPosition);
            writer.Write(offsetRotation);
            writer.Write(scale);
            writer.Write(color);
            writer.Write(emissiveColor);
            writer.Write(emissiveIntensity);
            writer.Write(metallic);
            writer.Write(smoothness);
            writer.Write(m_text);
            writer.Write(font);
            writer.Write(ItemName);
            writer.Write(coatStrength);
            writer.Write(Formulae ?? "");
            writer.Write((ushort)TextType);
            writer.Write(Atlas);
            writer.Write(parentEntity);
            writer.Write(fontName);
            writer.Write(maxWidthMeters);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out targetEntity);
            reader.Read(out byte shader);
            this.shader = (WEShader)shader;
            reader.Read(out offsetPosition);
            reader.Read(out offsetRotation);
            reader.Read(out scale);
            reader.Read(out color);
            reader.Read(out emissiveColor);
            reader.Read(out emissiveIntensity);
            reader.Read(out metallic);
            reader.Read(out smoothness);
            reader.Read(out m_text);
            reader.Read(out font);
            reader.Read(out itemName);
            reader.Read(out coatStrength);
            reader.Read(out string formulae);
            Formulae = formulae.TrimToNull();
            reader.Read(out short type);
            this.type = (WESimulationTextType)type;
            reader.Read(out string atlas);
            Atlas = atlas;
            if (version >= 1)
            {
                reader.Read(out parentEntity);
            }
            else
            {
                parentEntity = targetEntity;
            }
            if (version >= 2)
            {
                reader.Read(out fontName);
            }
            if (version >= 3)
            {
                reader.Read(out maxWidthMeters);
            }
        }

        public WETextDataXml ToDataXml(EntityManager em)
        {
            return new WETextDataXml
            {
                offsetPosition = (Vector3Xml)offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)offsetRotation).eulerAngles,
                scale = (Vector3Xml)scale,
                itemName = ItemName.ToString(),
                shader = shader,
                atlas = Atlas,
                formulae = Formulae,
                text = Text,
                textType = TextType,
                maxWidthMeters = maxWidthMeters,
                fontName = fontName == "" && font != Entity.Null ? em.TryGetComponent(font, out FontSystemData data) ? data.Name : "" : fontName.ToString(),
                style = new WETextDataXml.WETextDataStyleXml
                {
                    coatStrength = CoatStrength,
                    color = Color,
                    emissiveColor = EmissiveColor,
                    emissiveExposureWeight = EmissiveExposureWeight,
                    emissiveIntensity = EmissiveIntensity,
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
            var font = FontServer.Instance.GetOrCreateFontAsDefault(xml.fontName);

            return new WETextData
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
                font = font,
                CoatStrength = xml.style.coatStrength,
                Color = xml.style.color,
                EmissiveColor = xml.style.emissiveColor,
                EmissiveExposureWeight = xml.style.emissiveExposureWeight,
                EmissiveIntensity = xml.style.emissiveIntensity,
                Metallic = xml.style.metallic,
                Smoothness = xml.style.smoothness,
                maxWidthMeters = xml.maxWidthMeters,
            };
        }

        #endregion
    }
}