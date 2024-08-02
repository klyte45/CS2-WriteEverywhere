using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using WriteEverywhere.Sprites;
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
            CheckPendingQueue();
            if (!m_pendingQueuePlaceholders.IsEmpty)
            {
                Dependency = new WEPlaceholderTemplateFilterJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_WETextDataHdl = GetComponentTypeHandle<WETextData>(true),
                    m_templateDataLkp = GetComponentLookup<WETemplateData>(true),
                    m_templateManager = m_templateManager.RegisteredTemplatesRef,
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                    m_TextDataLkp = GetComponentLookup<WETextData>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer(),
                }.Schedule(m_pendingQueuePlaceholders, Dependency);
            }

        }

        private void CheckPendingQueue()
        {
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                var cmd = m_endFrameBarrier.CreateCommandBuffer();
                NativeArray<Entity> entities = m_pendingQueueEntities.ToEntityArray(Allocator.Temp);
                for (var i = 0; i < entities.Length && i < 100; i++)
                {
                    var entity = entities[i];
                    var weCustomData = EntityManager.GetComponentData<WETextData>(entity);
                    if (!EntityManager.Exists(weCustomData.TargetEntity) || (weCustomData.TargetEntity == Entity.Null && !EntityManager.HasComponent<WETemplateData>(entity)))
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
                        cmd.DestroyEntity(entity);
                        continue;
                    }
                    if (weCustomData.TextType == WESimulationTextType.Text)
                    {
                        UpdateTextMesh(entity, ref weCustomData, weCustomData.EffectiveText.ToString(), cmd);
                    }
                    else if (weCustomData.TextType == WESimulationTextType.Image)
                    {
                        UpdateImageMesh(entity, ref weCustomData, weCustomData.EffectiveText.ToString(), cmd);
                    }
                    else if (weCustomData.TextType == WESimulationTextType.Placeholder)
                    {
                        cmd.AddComponent<WEWaitingRenderingPlaceholder>(entity);
                        cmd.RemoveComponent<WEWaitingRendering>(entity);
                        continue;
                    }
                    cmd.SetComponent(entity, weCustomData);
                    cmd.RemoveComponent<WEWaitingRendering>(entity);
                }
                entities.Dispose();
            }
        }

        private void UpdateImageMesh(Entity e, ref WETextData weCustomData, string text, EntityCommandBuffer cmd)
        {
            if (EntityManager.HasComponent<WETemplateUpdater>(e)) cmd.RemoveComponent<WETemplateUpdater>(e);
            SetupTemplateComponent(e, ref weCustomData, cmd);
            var bri = m_atlasesLibrary.GetFromLocalAtlases(weCustomData.Atlas, text, true);
            if (bri == null)
            {
                LogUtils.DoWarnLog("IMAGE BRI STILL NULL!!!");
                return;
            }
            weCustomData.UpdateBRI(bri, text);
        }


        private void UpdateTextMesh(Entity e, ref WETextData weCustomData, string text, EntityCommandBuffer cmd)
        {
            if (EntityManager.HasComponent<WETemplateUpdater>(e)) cmd.RemoveComponent<WETemplateUpdater>(e);
            SetupTemplateComponent(e, ref weCustomData, cmd);
            var font = EntityManager.TryGetComponent<FontSystemData>(weCustomData.Font, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
            if (font.Font == null)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog("Font not initialized!!!");
                return;
            }

            var bri = font.FontSystem.DrawText(text, FontServer.Instance.ScaleEffective);
            if (bri == null)
            {
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog("BRI STILL NULL!!!");
                return;
            }
            weCustomData.UpdateBRI(bri, text);

        }

        private bool SetupTemplateComponent(Entity e, ref WETextData weCustomData, EntityCommandBuffer cmd)
        {
            if (EntityManager.HasComponent<WETemplateData>(weCustomData.TargetEntity))
            {
                if (!EntityManager.HasComponent<WETemplateData>(e)) cmd.AddComponent<WETemplateData>(e);
                return true;
            }
            if (EntityManager.HasComponent<WETemplateData>(e)) cmd.RemoveComponent<WETemplateData>(e);
            return false;
        }

#if BURST
        [BurstCompile]
#endif
        private unsafe struct WEPlaceholderTemplateFilterJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextData> m_WETextDataHdl;
            public EntityCommandBuffer m_CommandBuffer;
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
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) || (weCustomData.TargetEntity == Entity.Null && !m_templateDataLkp.HasComponent(entity)))
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
#endif
                        m_CommandBuffer.DestroyEntity(entity);
                        continue;
                    }
                    UpdatePlaceholder(entity, ref weCustomData, m_CommandBuffer);
                    m_CommandBuffer.SetComponent(entity, weCustomData);
                    m_CommandBuffer.RemoveComponent<WEWaitingRenderingPlaceholder>(entity);
                }
            }
            private void UpdatePlaceholder(Entity e, ref WETextData weCustomData, EntityCommandBuffer cmd)
            {
                if (!SetupTemplateComponent(e, ref weCustomData, cmd))
                {
                    var targetTemplate = m_templateManager[weCustomData.ItemName];
                    if (m_templateUpdaterLkp.TryGetComponent(e, out var templateUpdated) && templateUpdated.childEntity != Entity.Null)
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
#endif
                        cmd.DestroyEntity(templateUpdated.childEntity);
                    }

                    var newData = new WETemplateUpdater()
                    {
                        templateEntity = targetTemplate,
                        childEntity = targetTemplate == Entity.Null ? Entity.Null : WELayoutUtility.DoCloneTextItemReferenceSelf(targetTemplate, e, ref m_TextDataLkp, ref m_subRefLkp, cmd)
                    };
#if !BURST
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Cloned info! {weCustomData.ItemName} => {targetTemplate}");
#endif
                    if (m_templateUpdaterLkp.HasComponent(e))
                    {
                        cmd.SetComponent(e, newData);
                    }
                    else
                    {
                        cmd.AddComponent(e, newData);
                    }
                }
            }
            private bool SetupTemplateComponent(Entity e, ref WETextData weCustomData, EntityCommandBuffer cmd)
            {
                if (m_templateDataLkp.HasComponent(weCustomData.TargetEntity))
                {
                    if (!m_templateDataLkp.HasComponent(e)) cmd.AddComponent<WETemplateData>(e);
                    return true;
                }
                if (m_templateDataLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateData>(e);
                return false;
            }
        }
    }
}