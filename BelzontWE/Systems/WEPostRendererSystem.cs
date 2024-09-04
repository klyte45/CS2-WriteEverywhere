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
using BelzontWE.Sprites;
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
                        ComponentType.ReadWrite<WETextDataMain>(),
                        ComponentType.ReadWrite<WETextDataMesh>(),
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
                        ComponentType.ReadWrite<WETextDataMain>(),
                        ComponentType.ReadWrite<WETextDataMesh>(),
                        ComponentType.ReadOnly<WEWaitingRenderingPlaceholder>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WEToBeProcessedInMain>(),
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
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    FontDictPtr = FontServer.Instance.DictPtr,
                    m_CommandBuffer = cmdBuff.AsParallelWriter(),
                    m_dataMainHdl = GetComponentTypeHandle<WETextDataMain>(),
                    m_dataMeshHdl = GetComponentTypeHandle<WETextDataMesh>(),
                }.Schedule(m_pendingQueueEntities, Dependency);
            }
            if (!m_pendingQueuePlaceholders.IsEmptyIgnoreFilter)
            {
                var layoutsAvailable = new NativeArray<FixedString128Bytes>(m_templateManager.GetTemplateAvailableKeys(), Allocator.TempJob);
                Dependency = JobHandle.CombineDependencies(Dependency, new WEPlaceholderTemplateFilterJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                    m_CommandBuffer = cmdBuff.AsParallelWriter(),
                    m_WeMeshLkp = GetComponentLookup<WETextDataMesh>(true),
                    m_WeMainHdl = GetComponentTypeHandle<WETextDataMain>(),
                    m_WeMeshHdl = GetComponentTypeHandle<WETextDataMesh>(),
                    m_WeMainLkp = GetComponentLookup<WETextDataMain>(true),
                    m_templateManagerEntries = layoutsAvailable
                }.Schedule(m_pendingQueuePlaceholders, Dependency));
                layoutsAvailable.Dispose(Dependency);
            }
        }


        private unsafe struct WETextImageDataUpdateJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextDataMain> m_dataMainHdl;
            public ComponentTypeHandle<WETextDataMesh> m_dataMeshHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public GCHandle FontDictPtr;
            public EntityStorageInfoLookup m_entityLookup;
            public ComponentLookup<WETemplateUpdater> m_templateUpdaterLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_dataMainHdl);
                var weMeshDatas = chunk.GetNativeArray(ref m_dataMeshHdl);
                var fontDict = FontDictPtr.Target as Dictionary<FixedString32Bytes, FontSystemData>;

                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    var weMeshData = weMeshDatas[i];
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) || weCustomData.TargetEntity == Entity.Null)
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
                        m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, entity);
                        return;
                    }

                    switch (weMeshData.TextType)
                    {
                        case WESimulationTextType.Text:
                            if (UpdateTextMesh(entity, ref weMeshData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer, fontDict))
                            {
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        case WESimulationTextType.Image:
                            if (UpdateImageMesh(entity, ref weMeshData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        case WESimulationTextType.WhiteTexture:
                            var bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                            weMeshData.UpdateBRI(bri, bri.m_refText);
                            m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                            m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            break;
                        default:
                            m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            break;

                    }
                }
            }
            private bool UpdateImageMesh(Entity e, ref WETextDataMesh weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
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


            private bool UpdateTextMesh(Entity e, ref WETextDataMesh weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, Dictionary<FixedString32Bytes, FontSystemData> fontDict)
            {
                if (m_templateUpdaterLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                if (text.Trim() == "")
                {
                    weCustomData = weCustomData.UpdateBRI(new BasicRenderInformation("", new UnityEngine.Vector3[0], new int[0], new UnityEngine.Vector2[0], null), "");
                    return true;
                }
                var font = fontDict.TryGetValue(weCustomData.FontName, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
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
            public ComponentTypeHandle<WETextDataMain> m_WeMainHdl;
            public ComponentTypeHandle<WETextDataMesh> m_WeMeshHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public ComponentLookup<WETextDataMain> m_WeMainLkp;
            public ComponentLookup<WETextDataMesh> m_WeMeshLkp;
            public BufferLookup<WESubTextRef> m_subRefLkp;
            public EntityStorageInfoLookup m_entityLookup;
            public NativeArray<FixedString128Bytes> m_templateManagerEntries;
            public ComponentLookup<WETemplateUpdater> m_templateUpdaterLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_WeMainHdl);
                var weMeshDatas = chunk.GetNativeArray(ref m_WeMeshHdl);
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    var meshData = weMeshDatas[i];
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity)
                        || (m_WeMeshLkp.TryGetComponent(weCustomData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                        || (m_WeMeshLkp.TryGetComponent(weCustomData.TargetEntity, out weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                        || (weCustomData.TargetEntity == Entity.Null))
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
#endif
                        m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, entity);
                        continue;
                    }
                    UpdatePlaceholder(entity, ref meshData, unfilteredChunkIndex, m_CommandBuffer);
                    m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                    m_CommandBuffer.RemoveComponent<WEWaitingRenderingPlaceholder>(unfilteredChunkIndex, entity);
                }
            }
            private void UpdatePlaceholder(Entity e, ref WETextDataMesh weCustomData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                var targetTemplate = m_templateManagerEntries.Contains(new FixedString128Bytes(weCustomData.ValueData.EffectiveValue));
                if (m_templateUpdaterLkp.TryGetComponent(e, out var templateUpdated) && templateUpdated.childEntity != Entity.Null)
                {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
#endif
                    cmd.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, templateUpdated.childEntity);
                }
                m_CommandBuffer.AddComponent<WEToBeProcessedInMain>(unfilteredChunkIndex, e);
            }
        }
    }
}
