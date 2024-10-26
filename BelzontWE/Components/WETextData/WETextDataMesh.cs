using BelzontWE.Font.Utility;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.SceneFlow;
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
        private WESimulationTextType textType;

        private GCHandle basicRenderInformation;
        private FixedString32Bytes atlas;
        private FixedString32Bytes fontName;
        private WETextDataValueString valueData;
        private bool dirty;
        private bool templateDirty;
        private int nextUpdateFrame;

        public WESimulationTextType TextType
        {
            get => textType; set
            {
                textType = value;
                dirty = true;
                templateDirty = true;
                if (value == WESimulationTextType.Placeholder || value == WESimulationTextType.WhiteTexture)
                {
                    ResetBri();
                }
            }
        }
        public FixedString32Bytes Atlas { readonly get => atlas; set { atlas = value; templateDirty = dirty = true; } }
        public FixedString32Bytes FontName { readonly get => fontName; set { fontName = value; templateDirty = dirty = true; } }
        public WETextDataValueString ValueData { readonly get => valueData; set => valueData = value; }
        public int MinLod { get; set; }
        public float3 LodReferenceScale { get; set; }
        public int LastLod { get; set; }
        public float MaxWidthMeters { get; set; }

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
            else
            {
                LastErrorStr = default;
            }
            dirty = false;
            MinLod = 0;
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
            UpdateFormulaes(em, targetEntity, true);
            return this;
        }
        public bool UpdateFormulaes(EntityManager em, Entity geometryEntity, bool force = false)
        {
            if (!force && nextUpdateFrame > Time.frameCount)
            {
                return false;
            }
            nextUpdateFrame = Time.frameCount + WEModData.InstanceWE.FramesCheckUpdateVal;
            bool result;
            switch (textType)
            {
                case WESimulationTextType.Text:
                    result = valueData.UpdateEffectiveValue(em, geometryEntity);
                    break;
                case WESimulationTextType.Image:
                    result = valueData.UpdateEffectiveValue(em, geometryEntity, (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : valueData.EffectiveValue.ToString());
                    break;
                case WESimulationTextType.Placeholder:
                    result = valueData.UpdateEffectiveValue(em, geometryEntity);
                    break;
                case WESimulationTextType.WhiteTexture:
                    templateDirty = dirty = false;
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
}