using BelzontWE.Font.Utility;
using Colossal.Entities;
using Colossal.Mathematics;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public struct WETextDataMesh : IComponentData, IDisposable, ICleanupComponentData
    {
        private WESimulationTextType textType;

        private GCHandle basicRenderInformation;
        private FixedString64Bytes atlas;
        public FixedString128Bytes originalName;
        private FixedString64Bytes fontName;
        private FixedString64Bytes customMeshName;
        internal ushort lastUpdateModReplacements;
        private WETextDataValueString valueData;
        private bool dirty;
        private bool templateDirty;
        public bool childrenRefersToFrontFace;

        public WESimulationTextType TextType
        {
            get => textType; set
            {
                textType = value;
                dirty = true;
                templateDirty = true;
                if (value == WESimulationTextType.Placeholder || value == WESimulationTextType.WhiteTexture || value == WESimulationTextType.WhiteCube)
                {
                    ResetBri();
                }
            }
        }
        public WETextDataValueFloat3 OffsetPositionFormulae;
        public WETextDataValueFloat3 OffsetRotationFormulae;
        public WETextDataValueFloat3 ScaleFormulae;
        public FixedString64Bytes Atlas { readonly get => atlas; set { atlas = value; templateDirty = dirty = true; } }
        public FixedString64Bytes FontName { readonly get => fontName; set { fontName = value; templateDirty = dirty = true; } }
        public FixedString64Bytes CustomMeshName { readonly get => customMeshName; set { customMeshName = value; templateDirty = dirty = true; } }
        public WETextDataValueString ValueData { readonly get => valueData; set => valueData = value; }
        public int MinLod { get; set; }
        public float3 LodReferenceScale { get; set; }
        public int LastLod { get; set; }
        public WETextDataValueFloat MaxWidthMeters;
        public bool RescaleHeightOnTextOverflow { get; set; }

        public Bounds3 Bounds { get; private set; }
        public bool HasBRI => basicRenderInformation.IsAllocated;
        public float BriWidthMetersUnscaled { get; private set; }
        public FixedString512Bytes LastErrorStr { get; private set; }
        public int SetFormulae(string value, out string[] cmpErr) => valueData.SetFormulae(value, out cmpErr);

        public void ResetBri()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            basicRenderInformation = default;
            MinLod = 0;
            Bounds = new Bounds3(new float3(-.5f, -.5f, 0), new float3(.5f, .5f, 0));
        }
        public static WETextDataMesh CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                ValueData = new()
                {
                    DefaultValue = "NEW TEXT"
                },
                ScaleFormulae = new()
                {
                    defaultValue = new float3(1, 1, 1)
                }
            };

        public readonly bool IsDirty() => dirty;
        public readonly bool IsTemplateDirty() => templateDirty;
        public void ClearTemplateDirty() => templateDirty = false;

        public WETextDataMesh UpdateBRI(IBasicRenderInformation ibri, string text)
        {
            if (ValueData.IsEmpty)
            {
                ResetBri();
                dirty = false;
                return this;
            }
            if (ibri is PrimitiveRenderInformation bri)
            {
                if (bri.m_sizeMetersUnscaled.x < 0 && !bri.IsError && bri.m_refText != "") return this;
                if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
                basicRenderInformation = default;
                basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
                Bounds = bri.Bounds;
                BriWidthMetersUnscaled = bri.m_sizeMetersUnscaled.x;
                LastErrorStr = bri.IsError ? (FixedString512Bytes)text : default;
                dirty = false;
                MinLod = 0;
                return this;
            }
            else
            {
                if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
                basicRenderInformation = GCHandle.Alloc(ibri, GCHandleType.Weak);
                Bounds = ibri.Bounds;
                BriWidthMetersUnscaled = 1;
                LastErrorStr = default;
                dirty = false;
                return this;
            }
        }

        public void Dispose()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            basicRenderInformation = default;

        }

        public WETextDataMesh OnPostInstantiate(EntityManager em, Entity targetEntity)
        {
            UpdateFormulaes(em, targetEntity, default, true);
            FontServer.Instance.EnsureFont(fontName);
            return this;
        }

        public bool UpdateFormulaes(EntityManager em, Entity geometryEntity, FixedString512Bytes varsStr, bool force = false)
        {
            if (HasBRI && (basicRenderInformation.Target is not IBasicRenderInformation))
            {
                basicRenderInformation.Free();
                basicRenderInformation = default;
                dirty = true;
                return true;
            }
            bool result = false;
            var vars = WEVarsCacheBank.Instance[WEVarsCacheBank.Instance[varsStr]];
            switch (textType)
            {
                case WESimulationTextType.Text:
                    if (originalName.Length > 0 && lastUpdateModReplacements != WETemplateManager.Instance.SpritesAndLayoutsDataVersion)
                    {
                        lastUpdateModReplacements = WETemplateManager.Instance.SpritesAndLayoutsDataVersion;
                        fontName = WETemplateManager.Instance.GetFontFor(originalName.ToString(), fontName, ref result);
                    }
                    result |= valueData.UpdateEffectiveValue(em, geometryEntity, vars) | MaxWidthMeters.UpdateEffectiveValue(em, geometryEntity, vars);
                    break;
                case WESimulationTextType.Image:
                    if (originalName.Length > 0 && lastUpdateModReplacements != WETemplateManager.Instance.SpritesAndLayoutsDataVersion)
                    {
                        lastUpdateModReplacements = WETemplateManager.Instance.SpritesAndLayoutsDataVersion;
                        atlas = WETemplateManager.Instance.GetAtlasFor(originalName.ToString(), atlas, ref result);
                        if (HasBRI && (RenderInformation?.IsError ?? false))
                        {
                            result = true;
                        }
                    }
                    result |= valueData.UpdateEffectiveValue(em, geometryEntity, (RenderInformation?.IsError ?? false) ? LastErrorStr.ToString() : valueData.EffectiveValue.ToString(), vars);
                    break;
                case WESimulationTextType.Placeholder:
                    if (originalName.Length > 0 && lastUpdateModReplacements != WETemplateManager.Instance.SpritesAndLayoutsDataVersion)
                    {
                        lastUpdateModReplacements = WETemplateManager.Instance.SpritesAndLayoutsDataVersion;
                        valueData.DefaultValue = WETemplateManager.Instance.GetTemplateFor(originalName.ToString(), valueData.DefaultValue, ref result).ToString();
                    }
                    result |= valueData.UpdateEffectiveValue(em, geometryEntity, vars);
                    break;
                case WESimulationTextType.WhiteTexture:
                case WESimulationTextType.WhiteCube:
                    templateDirty = dirty = false;
                    return true;
                case WESimulationTextType.MatrixTransform:
                    OffsetPositionFormulae.UpdateEffectiveValue(em, geometryEntity, vars);
                    OffsetRotationFormulae.UpdateEffectiveValue(em, geometryEntity, vars);
                    ScaleFormulae.UpdateEffectiveValue(em, geometryEntity, vars);
                    return true;
                default:
                    return true;
            }
            if (result) templateDirty = dirty = true;
            return result;
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


        public IBasicRenderInformation RenderInformation
        {
            get
            {
                if (basicRenderInformation.IsAllocated && basicRenderInformation.Target is IBasicRenderInformation bri && bri.IsValid())
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

        public string Text { readonly get => valueData.DefaultValue.ToString(); set { valueData.DefaultValue = value; dirty = true; } }
        public readonly FixedString512Bytes EffectiveText => valueData.EffectiveValue;
    }
}