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
using BelzontWE.Font.Utility;
using System.Runtime.InteropServices;
using System.Collections.Generic;


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
        private WETemplateManager m_templateManager;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
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
            var cmdBuff = m_endFrameBarrier.CreateCommandBuffer();
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                Dependency = new WETextImageDataUpdateJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_WETextDataHdl = GetComponentTypeHandle<WETextData>(true),
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    FontDictPtr = FontServer.Instance.DictPtr,
                    m_CommandBuffer = cmdBuff.AsParallelWriter()
                }.Schedule(m_pendingQueueEntities, Dependency);
            }
            if (!m_pendingQueuePlaceholders.IsEmptyIgnoreFilter)
            {
                Dependency = JobHandle.CombineDependencies(Dependency, new WEPlaceholderTemplateFilterJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_WETextDataHdl = GetComponentTypeHandle<WETextData>(true),
                    m_templateManager = m_templateManager.RegisteredTemplatesRef,
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                    m_TextDataLkp = GetComponentLookup<WETextData>(true),
                    m_CommandBuffer = cmdBuff.AsParallelWriter()
                }.Schedule(m_pendingQueuePlaceholders, Dependency));
            }
        }


        private unsafe struct WETextImageDataUpdateJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextData> m_WETextDataHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public GCHandle FontDictPtr;
            public EntityStorageInfoLookup m_entityLookup;
            public ComponentLookup<WETemplateUpdater> m_templateUpdaterLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_WETextDataHdl);
                var fontDict = FontDictPtr.Target as Dictionary<FixedString32Bytes, FontSystemData>;

                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) || weCustomData.TargetEntity == Entity.Null)
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
                        m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, entity);
                        return;
                    }

                    switch (weCustomData.TextType)
                    {
                        case WESimulationTextType.Text:
                            if (UpdateTextMesh(entity, ref weCustomData, weCustomData.EffectiveText.ToString(), unfilteredChunkIndex, m_CommandBuffer, fontDict))
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
                        default:
                            m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            break;

                    }
                }
            }
            private bool UpdateImageMesh(Entity e, ref WETextData weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateUpdaterLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                var bri = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WEAtlasesLibrary>().GetFromAvailableAtlases(weCustomData.Atlas, text, true);
                if (bri == null)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog("IMAGE BRI STILL NULL!!!");
                    return false;
                }
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Image returned: {bri} {text} (a={weCustomData.Atlas})");
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;
            }


            private bool UpdateTextMesh(Entity e, ref WETextData weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, Dictionary<FixedString32Bytes, FontSystemData> fontDict)
            {
                if (m_templateUpdaterLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                if (text.Trim() == "")
                {
                    weCustomData = weCustomData.UpdateBRI(new BasicRenderInformation("", new UnityEngine.Vector3[0], new int[0], new UnityEngine.Vector2[0], null), "");
                    return true;
                }
                var font = fontDict.TryGetValue(weCustomData.Font, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
                if (font.Font == null)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Font not initialized!!!");
                    return false;
                }
                var bri = font.FontSystem.DrawText(text);
                if (bri == null || bri == BasicRenderInformation.LOADING_PLACEHOLDER)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"BRI STILL NULL!!! ({text})");
                    return false;
                }
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;

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
            public UnsafeParallelHashMap<FixedString128Bytes, WETextDataTreeStruct> m_templateManager;
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
                        || (weCustomData.TargetEntity == Entity.Null))
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
#endif
                        m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, entity);
                        continue;
                    }
                    UpdatePlaceholder(entity, ref weCustomData, unfilteredChunkIndex, m_CommandBuffer);
                    m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                    m_CommandBuffer.RemoveComponent<WEWaitingRenderingPlaceholder>(unfilteredChunkIndex, entity);
                }
            }
            private void UpdatePlaceholder(Entity e, ref WETextData weCustomData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                var targetTemplate = m_templateManager[new FixedString128Bytes(weCustomData.Text512)];
                if (m_templateUpdaterLkp.TryGetComponent(e, out var templateUpdated) && templateUpdated.childEntity != Entity.Null)
                {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
#endif
                    cmd.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, templateUpdated.childEntity);
                }

                var newData = new WETemplateUpdater()
                {
                    templateEntity = targetTemplate.Guid,
                    childEntity = !targetTemplate.IsInitialized ? Entity.Null : WELayoutUtility.DoCreateLayoutItem(targetTemplate, e, Entity.Null, ref m_TextDataLkp, ref m_subRefLkp, unfilteredChunkIndex, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET)
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
    }
}
