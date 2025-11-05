using Belzont.Utils;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        #region Entity Processing
        // Methods for processing entities in the main thread

        private Coroutine m_updatingEntitiesOnMain;

        /// <summary>
        /// Updates prefab archetypes by applying templates to prefab entities.
        /// Processes up to 10,000 entities per call to avoid frame drops.
        /// </summary>
        internal void UpdatePrefabArchetypes(NativeArray<ArchetypeChunk> chunks)
        {
            var m_TextDataLkp = GetComponentLookup<WETextDataMain>();
            var m_prefabDataLkp = GetComponentLookup<PrefabData>();
            var m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>();
            var globalCounter = 0;
            for (int h = 0; h < chunks.Length; h++)
            {
                var chunk = chunks[h];
                using var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                using var prefabRefs = chunk.GetNativeArray(ref m_prefabRefHdl);
                for (int i = 0; i < entities.Length; i++, globalCounter++)
                {
                    if (globalCounter >= 10_000)
                    {
                        m_updatingEntitiesOnMain = null;
                        return;
                    }
                    var e = entities[i];
                    var prefabRef = prefabRefs[i];

                    EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
                    cmd.SetComponentEnabled<WETemplateForPrefabToRunOnMain>(e, false);
                    if (m_prefabDataLkp.TryGetComponent(prefabRef.m_Prefab, out var prefabData))
                    {
                        if (PrefabTemplates.TryGetValue(prefabData.m_Index, out var newTemplate))
                        {
                            var guid = newTemplate.Guid;
                            var childEntity = WELayoutUtility.DoCreateLayoutItemCmdBuffer(true, newTemplate.ModSource, newTemplate, e, Entity.Null, ref m_TextDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_FOR_PARENT);
                            if (m_prefabEmptyLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateForPrefabEmpty>(e);
                            cmd.AddComponent<WETemplateForPrefab>(entities[i], new()
                            {
                                templateRef = guid,
                                childEntity = childEntity
                            });

                            continue;
                        }
                    }

                    cmd.AddComponent<WETemplateForPrefab>(entities[i], new()
                    {
                        templateRef = default,
                        childEntity = Entity.Null
                    });
                    if (!m_prefabEmptyLkp.HasComponent(e)) cmd.AddComponent<WETemplateForPrefabEmpty>(e);
                }
            }
        }

        /// <summary>
        /// Updates placeholder layouts by instantiating template children.
        /// Processes up to 10,000 entities per call to avoid frame drops.
        /// Handles array instancing with proper spacing and alignment.
        /// </summary>
        internal void UpdateLayouts(NativeArray<ArchetypeChunk> chunks)
        {
            var m_MainDataLkp = GetComponentLookup<WETextDataMain>();
            var m_DataTransformLkp = GetComponentLookup<WETextDataTransform>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var toBeProcessedDataHdl = GetComponentTypeHandle<WEPlaceholderToBeProcessedInMain>();
            var globalCounter = 0;
            for (int h = 0; h < chunks.Length; h++)
            {
                var chunk = chunks[h];
                using var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                using var dataToBeProcessedArray = chunk.GetNativeArray(ref toBeProcessedDataHdl);
                for (int i = 0; i < entities.Length; i++, globalCounter++)
                {
                    if (globalCounter >= 10_000)
                    {
                        m_updatingEntitiesOnMain = null;
                        return;
                    }
                    var e = entities[i];
                    EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
                    var buff = cmd.SetBuffer<WETemplateUpdater>(e);

                    var dataToBeProcessed = dataToBeProcessedArray[i];
                    m_DataTransformLkp.TryGetComponent(e, out var transformData);
                    bool hasTemplate = TryGetTargetTemplate(dataToBeProcessed.layoutName, out WETextDataXmlTree targetTemplate);

                    for (int j = 0; j < buff.Length; j++)
                    {
                        cmd.DestroyEntity(buff[j].childEntity);
                    }
                    buff.Clear();
                    if (hasTemplate)
                    {
                        targetTemplate = targetTemplate.Clone();

                        var targetSize = transformData.InstanceCountFn.EffectiveValue < 0 ? math.clamp(transformData.ArrayInstancing.x * transformData.ArrayInstancing.y * transformData.ArrayInstancing.z, 1, 256) : math.min(256, (uint)transformData.InstanceCountFn.EffectiveValue);
                        if (targetSize == 0) goto end;
                        var instancingCount = (uint3)math.min(transformData.InstanceCountByAxisOrder, math.ceil(targetSize / new float3(1, transformData.InstanceCountByAxisOrder[0], transformData.InstanceCountByAxisOrder[0] * transformData.InstanceCountByAxisOrder[1])));
                        var spacingOffsets = transformData.SpacingByAxisOrder;
                        var totalArea = (transformData.ArrayInstancing - 1) * transformData.arrayInstancingGapMeters;

                        var effectivePivot = transformData.PivotAsFloat3 - (math.sign(totalArea.xyz) / 2) - .5f;

                        var pivotOffset = effectivePivot * math.abs(totalArea);
                        var alignmentByAxisOrder = transformData.AlignmentByAxisOrder;

                        var spacingO = spacingOffsets[2];
                        GetSpacingAndOffset(targetSize, instancingCount.z, instancingCount.y * instancingCount.x, alignmentByAxisOrder.o, ref spacingO, out float3 offsetO);
                        for (int o = 0; o < instancingCount.z; o++)
                        {
                            var spacingN = spacingOffsets[1];
                            GetSpacingAndOffset(targetSize - (uint)buff.Length, instancingCount.y, instancingCount.x, alignmentByAxisOrder.n, ref spacingN, out float3 offsetN);
                            for (int n = 0; n < instancingCount.y; n++)
                            {
                                var spacingM = spacingOffsets[0];
                                GetSpacingAndOffset(targetSize - (uint)buff.Length, instancingCount.x, 1, alignmentByAxisOrder.m, ref spacingM, out float3 offsetM);
                                var totalOffset = pivotOffset + offsetM + offsetN + offsetO;
                                for (int m = 0; m < instancingCount.x; m++)
                                {
                                    targetTemplate.self.transform.offsetPosition = (Vector3Xml)(Vector3)(totalOffset + (m * spacingM) + (n * spacingN) + (o * spacingO));
                                    targetTemplate.self.transform.pivot = transformData.pivot;

                                    var updater = new WETemplateUpdater()
                                    {
                                        templateEntity = targetTemplate.Guid,
                                        childEntity = WELayoutUtility.DoCreateLayoutItemCmdBuffer(true, targetTemplate.ModSource, targetTemplate, e, Entity.Null, ref m_MainDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET)
                                    };

                                    buff.Add(updater);
                                    globalCounter++;
                                    if (buff.Length >= targetSize) goto end;
                                }
                            }
                        }
                    }
                end:
                    cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                }
            }
        }

        /// <summary>
        /// Calculates spacing and offset for array instancing based on alignment settings.
        /// Used for justified, centered, and right-aligned layout patterns.
        /// </summary>
        private static void GetSpacingAndOffset(uint remaining, uint rowCount, uint rowCapacity, WEPlacementAlignment axisAlignment, ref float3 spacing, out float3 offset)
        {
            offset = float3.zero;
            uint capacity = rowCapacity * (rowCount - 1);
            if (remaining <= capacity && axisAlignment != WEPlacementAlignment.Left)
            {
                var totalWidth = (rowCount - 1) * spacing;
                var effectiveRowsCount = math.ceil(remaining / rowCapacity);
                var effectiveWidth = (effectiveRowsCount - 1) * spacing;
                switch (axisAlignment)
                {
                    case WEPlacementAlignment.Center:
                        offset = (totalWidth - effectiveWidth) / 2;
                        break;
                    case WEPlacementAlignment.Right:
                        offset = totalWidth - effectiveWidth;
                        break;
                    case WEPlacementAlignment.Justified:
                        spacing = effectiveRowsCount == 1 ? 0 : totalWidth / (effectiveRowsCount - 1);
                        break;
                }
            }
        }

        /// <summary>
        /// Enqueues a material to be destroyed on the next frame.
        /// Used for cleanup of dynamically created materials.
        /// </summary>
        public void EnqueueToBeDestructed(Material m)
        {
            m_executionQueue.Enqueue((x) => GameObject.Destroy(m));
        }

        #endregion
    }
}
