using Belzont.Utils;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        #region Entity Processing
        private const int kMaxLayoutArrayInstancesPerFrame = 32;
        // Methods for processing entities in the main thread


        /// <summary>
        /// Updates prefab archetypes by applying templates to prefab entities.
        /// Processes up to 10,000 entities per call to avoid frame drops.
        /// </summary>
        internal void UpdatePrefabArchetypes(NativeArray<Entity> entities)
        {
            var m_TextDataLkp = GetComponentLookup<WETextDataMain>();
            var m_prefabDataLkp = GetComponentLookup<PrefabData>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>();
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == Entity.Null) continue;
                var prefabRef = EntityManager.GetComponentData<PrefabRef>(e);

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

        /// <summary>
        /// Updates placeholder layouts by instantiating template children.
        /// Handles array instancing incrementally so large saves do not flood EndFrameBarrier playback.
        /// </summary>
        internal void UpdateLayouts(NativeArray<Entity> entities)
        {
            var m_MainDataLkp = GetComponentLookup<WETextDataMain>();
            var m_DataTransformLkp = GetComponentLookup<WETextDataTransform>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
            var remainingFrameInstances = kMaxLayoutArrayInstancesPerFrame;

            for (int i = 0; i < entities.Length && remainingFrameInstances > 0; i++)
            {
                var e = entities[i];
                if (e == Entity.Null) continue;

                var dataToBeProcessed = EntityManager.GetComponentData<WEPlaceholderToBeProcessedInMain>(e);
                m_DataTransformLkp.TryGetComponent(e, out var transformData);
                bool hasTemplate = TryGetTargetTemplate(dataToBeProcessed.layoutName, out WETextDataXmlTree targetTemplate);
                if (!hasTemplate)
                {
                    ClearTemplateUpdaterBuffer(e, cmd);
                    cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                    continue;
                }

                var registeredGuid = targetTemplate.Guid;
                var targetSize = transformData.InstanceCountFn.EffectiveValue < 0
                    ? math.clamp(transformData.ArrayInstancing.x * transformData.ArrayInstancing.y * transformData.ArrayInstancing.z, 1, 256)
                    : math.clamp(transformData.InstanceCountFn.EffectiveValue, 0, 256);

                if (targetSize == 0)
                {
                    ClearTemplateUpdaterBuffer(e, cmd);
                    cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                    continue;
                }

                var hasExistingBuffer = EntityManager.HasBuffer<WETemplateUpdater>(e);
                var existingBuffer = hasExistingBuffer ? EntityManager.GetBuffer<WETemplateUpdater>(e, true) : default;
                var existingCount = hasExistingBuffer ? existingBuffer.Length : 0;
                var resetExisting = !hasExistingBuffer || existingCount > targetSize;
                for (int j = 0; !resetExisting && j < existingCount; j++)
                {
                    var item = existingBuffer[j];
                    resetExisting = item.templateEntity != registeredGuid || item.childEntity == Entity.Null || !EntityManager.Exists(item.childEntity);
                }

                DynamicBuffer<WETemplateUpdater> buff;
                if (resetExisting)
                {
                    buff = hasExistingBuffer ? cmd.SetBuffer<WETemplateUpdater>(e) : cmd.AddBuffer<WETemplateUpdater>(e);
                    for (int j = 0; hasExistingBuffer && j < existingCount; j++)
                    {
                        var childEntity = existingBuffer[j].childEntity;
                        if (childEntity != Entity.Null && EntityManager.Exists(childEntity)) cmd.DestroyEntity(childEntity);
                    }
                    existingCount = 0;
                }
                else
                {
                    if (existingCount >= targetSize)
                    {
                        cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                        continue;
                    }
                    buff = cmd.SetBuffer<WETemplateUpdater>(e);
                    for (int j = 0; j < existingCount; j++) buff.Add(existingBuffer[j]);
                }

                targetTemplate = targetTemplate.Clone();
                var instancingCount = (uint3)math.min(transformData.InstanceCountByAxisOrder, math.ceil(targetSize / new float3(1, transformData.InstanceCountByAxisOrder[0], transformData.InstanceCountByAxisOrder[0] * transformData.InstanceCountByAxisOrder[1])));
                var spacingOffsets = transformData.SpacingByAxisOrder;
                var totalArea = (transformData.ArrayInstancing - 1) * transformData.arrayInstancingGapMeters.EffectiveValue;
                var effectivePivot = transformData.PivotAsFloat3 - (math.sign(totalArea.xyz) / 2) - .5f;
                var pivotOffset = effectivePivot * math.abs(totalArea);
                var alignmentByAxisOrder = transformData.AlignmentByAxisOrder;

                var spacingO = spacingOffsets[2];
                GetSpacingAndOffset(instancingCount.z, transformData.InstanceCountByAxisOrder.z, alignmentByAxisOrder.o, ref spacingO, out float3 offsetO);
                var currentIndex = 0;

                for (int o = 0; o < instancingCount.z && buff.Length < targetSize && remainingFrameInstances > 0; o++)
                {
                    var spacingN = spacingOffsets[1];
                    var remainingInLayer = math.max(0, targetSize - (o * transformData.InstanceCountByAxisOrder.x * transformData.InstanceCountByAxisOrder.y));
                    var effectiveCountN = (uint)math.min(math.ceil(remainingInLayer / (float)instancingCount.x), instancingCount.y);
                    GetSpacingAndOffset(effectiveCountN, transformData.InstanceCountByAxisOrder.y, alignmentByAxisOrder.n, ref spacingN, out float3 offsetN);

                    for (int n = 0; n < effectiveCountN && buff.Length < targetSize && remainingFrameInstances > 0; n++)
                    {
                        var spacingM = spacingOffsets[0];
                        var rowStartIndex = (o * transformData.InstanceCountByAxisOrder.x * transformData.InstanceCountByAxisOrder.y) + (n * transformData.InstanceCountByAxisOrder.x);
                        var effectiveCountM = (uint)math.min(targetSize - rowStartIndex, instancingCount.x);
                        GetSpacingAndOffset(effectiveCountM, transformData.InstanceCountByAxisOrder.x, alignmentByAxisOrder.m, ref spacingM, out float3 offsetM);
                        var totalOffset = pivotOffset + offsetM + offsetN + offsetO;

                        for (int m = 0; m < effectiveCountM && buff.Length < targetSize && remainingFrameInstances > 0; m++)
                        {
                            currentIndex = rowStartIndex + m;
                            if (currentIndex < existingCount) continue;

                            targetTemplate.self.transform.offsetPosition = (Vector3Xml)(Vector3)(totalOffset + (m * spacingM) + (n * spacingN) + (o * spacingO));
                            targetTemplate.self.transform.pivot = transformData.pivot;

                            var updater = new WETemplateUpdater()
                            {
                                templateEntity = registeredGuid,
                                childEntity = WELayoutUtility.DoCreateLayoutItemCmdBuffer(true, targetTemplate.ModSource, targetTemplate, e, Entity.Null, ref m_MainDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET)
                            };

                            buff.Add(updater);
                            remainingFrameInstances--;
                        }
                    }
                }

                if (buff.Length >= targetSize)
                {
                    cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                }
            }
        }

        private void ClearTemplateUpdaterBuffer(Entity e, EntityCommandBuffer cmd)
        {
            if (EntityManager.HasBuffer<WETemplateUpdater>(e))
            {
                var existingBuffer = EntityManager.GetBuffer<WETemplateUpdater>(e, true);
                for (int j = 0; j < existingBuffer.Length; j++)
                {
                    var childEntity = existingBuffer[j].childEntity;
                    if (childEntity != Entity.Null && EntityManager.Exists(childEntity)) cmd.DestroyEntity(childEntity);
                }
                cmd.SetBuffer<WETemplateUpdater>(e).Clear();
            }
        }

        /// <summary>
        /// Calculates spacing and offset for array instancing based on alignment settings.
        /// Used for justified, centered, and right-aligned layout patterns.
        /// </summary>
        internal static void GetSpacingAndOffset(uint totalSlotsRow, int originalTotalSlots, WEPlacementAlignment axisAlignment, ref float3 spacing, out float3 offset)
        {
            offset = float3.zero;
            if (totalSlotsRow <= originalTotalSlots && axisAlignment != WEPlacementAlignment.Left)
            {
                var lineWidth = (totalSlotsRow - 1) * spacing;
                var originalWidth = (originalTotalSlots - 1) * spacing;
                switch (axisAlignment)
                {
                    case WEPlacementAlignment.Center:
                        offset = (originalWidth - lineWidth) / 2;
                        break;
                    case WEPlacementAlignment.Right:
                        offset = originalWidth - lineWidth;
                        break;
                    case WEPlacementAlignment.Justified:
                        spacing = totalSlotsRow == 1 ? 0 : originalWidth / (totalSlotsRow - 1);
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
