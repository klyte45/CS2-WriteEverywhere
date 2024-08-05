using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using WriteEverywhere.Sprites;
using Unity.Jobs;
using System;
using BelzontWE.Font.Utility;





#if BURST
using Unity.Burst;
#endif


namespace BelzontWE
{
    //System that will prepare the next frame meshes
    public partial class WEPostRendererSystem : SystemBase
    {
        private EntityQuery m_pendingQueueEntities;
        private EntityQuery m_pendingQueuePlaceholders;
        private EndFrameBarrier m_endFrameBarrier;
        private WEAtlasesLibrary m_atlasesLibrary;
        private WETemplateManager m_templateManager;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_atlasesLibrary = World.GetOrCreateSystemManaged<WEAtlasesLibrary>();
            m_pendingQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextData>(),
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WEWaitingRenderingPlaceholder>(),
                    }
                }
            });

            m_pendingQueuePlaceholders = GetEntityQuery(new EntityQueryDesc[]
          {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextData>(),
                        ComponentType.ReadOnly<WEWaitingRenderingPlaceholder>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
          });
            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            RequireAnyForUpdate(m_pendingQueueEntities, m_pendingQueuePlaceholders);
        }
        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                Dependency = new WETextImageDataUpdateJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_WETextDataHdl = GetComponentTypeHandle<WETextData>(true),
                    m_templateDataLkp = GetComponentLookup<WETemplateData>(true),
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    m_FontDataLkp = GetComponentLookup<FontSystemData>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter()
                }.Schedule(m_pendingQueueEntities, Dependency);
            }
            if (!m_pendingQueuePlaceholders.IsEmptyIgnoreFilter)
            {
                Dependency = JobHandle.CombineDependencies(Dependency, new WEPlaceholderTemplateFilterJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_WETextDataHdl = GetComponentTypeHandle<WETextData>(true),
                    m_templateDataLkp = GetComponentLookup<WETemplateData>(true),
                    m_templateManager = m_templateManager.RegisteredTemplatesRef,
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                    m_TextDataLkp = GetComponentLookup<WETextData>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter()
                }.Schedule(m_pendingQueuePlaceholders, Dependency));
            }
        }


        private unsafe struct WETextImageDataUpdateJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextData> m_WETextDataHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public ComponentLookup<FontSystemData> m_FontDataLkp;
            public EntityStorageInfoLookup m_entityLookup;
            public ComponentLookup<WETemplateData> m_templateDataLkp;
            public ComponentLookup<WETemplateUpdater> m_templateUpdaterLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_WETextDataHdl);

                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) || (weCustomData.TargetEntity == Entity.Null && !m_templateDataLkp.HasComponent(entity)))
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        return;
                    }

                    switch (weCustomData.TextType)
                    {
                        case WESimulationTextType.Text:
                            if (UpdateTextMesh(entity, ref weCustomData, weCustomData.EffectiveText.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        case WESimulationTextType.Image:
                            if (UpdateImageMesh(entity, ref weCustomData, weCustomData.EffectiveText.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        case WESimulationTextType.Placeholder:
                            throw new Exception("INVALID PLACEHOLDER TYPE!");

                    }
                }
            }
            private bool UpdateImageMesh(Entity e, ref WETextData weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateUpdaterLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                SetupTemplateComponent(e, ref weCustomData, unfilteredChunkIndex, cmd);
                var bri = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WEAtlasesLibrary>().GetFromLocalAtlases(weCustomData.Atlas, text, true);
                if (bri == null)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog("IMAGE BRI STILL NULL!!!");
                    return false;
                }
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Image returned: {bri} {text} (a={weCustomData.Atlas})");
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;
            }


            private bool UpdateTextMesh(Entity e, ref WETextData weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateUpdaterLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                SetupTemplateComponent(e, ref weCustomData, unfilteredChunkIndex, cmd);
                if (text == "")
                {
                    weCustomData = weCustomData.UpdateBRI(new BasicRenderInformation(null, null, null) { m_refText = "" }, "");
                    return true;
                }
                var font = m_FontDataLkp.TryGetComponent(weCustomData.Font, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
                if (font.Font == null)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Font not initialized!!!");
                    return false;
                }
                var bri = font.FontSystem.DrawText(text, FontServer.Instance.ScaleEffective);
                if (bri == null)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"BRI STILL NULL!!! ({text})");
                    return false;
                }
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;

            }

            private bool SetupTemplateComponent(Entity e, ref WETextData weCustomData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateDataLkp.HasComponent(weCustomData.TargetEntity))
                {
                    if (!m_templateDataLkp.HasComponent(e)) cmd.AddComponent<WETemplateData>(unfilteredChunkIndex, e);
                    return true;
                }
                if (m_templateDataLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateData>(unfilteredChunkIndex, e);
                return false;
            }
        }

#if BURST
        [BurstCompile]
#endif
        private unsafe struct WEPlaceholderTemplateFilterJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextData> m_WETextDataHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public ComponentLookup<WETextData> m_TextDataLkp;
            public BufferLookup<WESubTextRef> m_subRefLkp;
            public EntityStorageInfoLookup m_entityLookup;
            public ComponentLookup<WETemplateData> m_templateDataLkp;
            public UnsafeParallelHashMap<FixedString32Bytes, Entity> m_templateManager;
            public ComponentLookup<WETemplateUpdater> m_templateUpdaterLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_WETextDataHdl);
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) 
                        || (m_TextDataLkp.TryGetComponent(weCustomData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                        || (m_TextDataLkp.TryGetComponent(weCustomData.TargetEntity, out weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder) 
                        || (weCustomData.TargetEntity == Entity.Null && !m_templateDataLkp.HasComponent(entity)))
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
#endif
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        continue;
                    }
                    UpdatePlaceholder(entity, ref weCustomData, unfilteredChunkIndex, m_CommandBuffer);
                    m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                    m_CommandBuffer.RemoveComponent<WEWaitingRenderingPlaceholder>(unfilteredChunkIndex, entity);
                }
            }
            private void UpdatePlaceholder(Entity e, ref WETextData weCustomData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (!SetupTemplateComponent(e, ref weCustomData, unfilteredChunkIndex, cmd))
                {
                    var targetTemplate = m_templateManager[weCustomData.ItemName];
                    if (m_templateUpdaterLkp.TryGetComponent(e, out var templateUpdated) && templateUpdated.childEntity != Entity.Null)
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
#endif
                        cmd.DestroyEntity(unfilteredChunkIndex, templateUpdated.childEntity);
                    }

                    var newData = new WETemplateUpdater()
                    {
                        templateEntity = targetTemplate,
                        childEntity = targetTemplate == Entity.Null ? Entity.Null : WELayoutUtility.DoCloneTextItemReferenceSelf(targetTemplate, e, ref m_TextDataLkp, ref m_subRefLkp, unfilteredChunkIndex, cmd)
                    };
#if !BURST
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Cloned info! {weCustomData.ItemName} => {targetTemplate}");
#endif
                    if (m_templateUpdaterLkp.HasComponent(e))
                    {
                        cmd.SetComponent(unfilteredChunkIndex, e, newData);
                    }
                    else
                    {
                        cmd.AddComponent(unfilteredChunkIndex, e, newData);
                    }
                }
            }
            private bool SetupTemplateComponent(Entity e, ref WETextData weCustomData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateDataLkp.HasComponent(weCustomData.TargetEntity))
                {
                    if (!m_templateDataLkp.HasComponent(e)) cmd.AddComponent<WETemplateData>(unfilteredChunkIndex, e);
                    return true;
                }
                if (m_templateDataLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateData>(unfilteredChunkIndex, e);
                return false;
            }
        }
    }
}
