using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;


#if BURST
#endif


namespace BelzontWE
{
    //System that will prepare the next frame meshes
    public partial class WEPostRendererSystem : SystemBase
    {
        private EntityQuery m_pendingQueueEntities;
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
                        ComponentType.ReadOnly<WEPlaceholderToBeProcessedInMain>(),
                    }
                }
            });

            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            RequireAnyForUpdate(m_pendingQueueEntities);
        }
        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            var cmdBuff = m_endFrameBarrier.CreateCommandBuffer();
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                var layoutsAvailable = new NativeArray<FixedString128Bytes>(m_templateManager.GetTemplateAvailableKeys(), Allocator.TempJob);
                Dependency = new WETextImageDataUpdateJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetBufferLookup<WETemplateUpdater>(true),
                    FontDictPtr = FontServer.Instance.DictPtr,
                    m_CommandBuffer = cmdBuff.AsParallelWriter(),
                    m_dataMainHdl = GetComponentTypeHandle<WETextDataMain>(),
                    m_dataMeshHdl = GetComponentTypeHandle<WETextDataMesh>(),
                    m_WeMeshLkp = GetComponentLookup<WETextDataMesh>(true),
                    m_WeMainLkp = GetComponentLookup<WETextDataMain>(true),
                    m_WeIsPlaceholderLkp = GetComponentLookup<WEIsPlaceholder>(true),
                    m_templateManagerEntries = layoutsAvailable
                }.Schedule(m_pendingQueueEntities, Dependency);

                layoutsAvailable.Dispose(Dependency);
            }
            Dependency.Complete();
        }


        private unsafe struct WETextImageDataUpdateJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextDataMain> m_dataMainHdl;
            public ComponentTypeHandle<WETextDataMesh> m_dataMeshHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public ComponentLookup<WETextDataMain> m_WeMainLkp;
            public ComponentLookup<WETextDataMesh> m_WeMeshLkp;
            public ComponentLookup<WEIsPlaceholder> m_WeIsPlaceholderLkp;
            public GCHandle FontDictPtr;
            public EntityStorageInfoLookup m_entityLookup;
            public BufferLookup<WETemplateUpdater> m_templateUpdaterLkp;
            public NativeArray<FixedString128Bytes> m_templateManagerEntries;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_dataMainHdl);
                var weMeshDatas = chunk.GetNativeArray(ref m_dataMeshHdl);
                var fontDict = FontDictPtr.Target as Dictionary<FixedString64Bytes, FontSystemData>;

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
                                if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.RemoveComponent<WEIsPlaceholder>(unfilteredChunkIndex, entity);
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        case WESimulationTextType.Image:
                            if (UpdateImageMesh(entity, ref weMeshData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.RemoveComponent<WEIsPlaceholder>(unfilteredChunkIndex, entity);
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        case WESimulationTextType.Placeholder:
                            if (UpdatePlaceholder(entity, ref weCustomData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                if (!m_WeIsPlaceholderLkp.HasComponent(entity))
                                {
                                    m_CommandBuffer.AddComponent<WEIsPlaceholder>(unfilteredChunkIndex, entity);
                                    m_CommandBuffer.AddComponent<WETemplateDirtyInstancing>(unfilteredChunkIndex, entity);
                                }
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                                m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            }
                            break;
                        default:
                            if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.RemoveComponent<WEIsPlaceholder>(unfilteredChunkIndex, entity);
                            m_CommandBuffer.RemoveComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                            break;

                    }
                }
            }
            private bool UpdatePlaceholder(Entity e, ref WETextDataMain weCustomData, string templateName, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (!m_entityLookup.Exists(weCustomData.TargetEntity)
                      || (m_WeMeshLkp.TryGetComponent(weCustomData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                      || (m_WeMeshLkp.TryGetComponent(weCustomData.TargetEntity, out weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                      || (weCustomData.TargetEntity == Entity.Null))
                {
#if !BURST
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {e} - Target doesntExists");
#endif
                    cmd.DestroyEntity(unfilteredChunkIndex, e);
                }
                else
                {
                    if (!m_templateUpdaterLkp.TryGetBuffer(e, out var templateUpdatedBuff))
                    {
                        templateUpdatedBuff = m_CommandBuffer.AddBuffer<WETemplateUpdater>(unfilteredChunkIndex, e);
                    }
                    var templateIsValid = m_templateManagerEntries.Contains(templateName);

                    if (templateIsValid)
                    {
                        m_CommandBuffer.AddComponent<WEPlaceholderToBeProcessedInMain>(unfilteredChunkIndex, e, new() { layoutName = templateName });
                    }
                    else
                    {
                        for (int i = 0; i < templateUpdatedBuff.Length; i++)
                        {
                            var templateUpdated = templateUpdatedBuff[i];
                            if (templateUpdated.childEntity != Entity.Null)
                            {
#if !BURST
                                if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
#endif 
                                cmd.DestroyEntity(unfilteredChunkIndex, templateUpdated.childEntity);
                            }
                        }
                        templateUpdatedBuff.Clear();
                    }

                }

                return true;
            }
            private bool UpdateImageMesh(Entity e, ref WETextDataMesh weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateUpdaterLkp.HasBuffer(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                IBasicRenderInformation bri = null;
                if (!weCustomData.CustomMeshName.IsEmpty)
                {
                    bri = WECustomMeshLibrary.Instance.GetMesh(weCustomData.CustomMeshName.ToString(), weCustomData.Atlas.ToString(), text);
                }
                bri ??= WEAtlasesLibrary.Instance.GetFromAvailableAtlases(weCustomData.Atlas.ToString(), text, true);
                if (bri == null)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog("IMAGE BRI STILL NULL!!!");
                    return false;
                }
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Image returned: {bri} {text} (a={weCustomData.Atlas}, m={weCustomData.CustomMeshName})");
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;
            }


            private bool UpdateTextMesh(Entity e, ref WETextDataMesh weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, Dictionary<FixedString64Bytes, FontSystemData> fontDict)
            {
                if (m_templateUpdaterLkp.HasBuffer(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                if (text.Trim() == "")
                {
                    weCustomData = weCustomData.UpdateBRI(new PrimitiveRenderInformation("", new UnityEngine.Vector3[0], new int[0], new UnityEngine.Vector2[0],
                        null), "");
                    return true;
                }
                var font = fontDict.TryGetValue(weCustomData.FontName, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
                if (font.Font == null)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Font not initialized!!!");
                    return false;
                }
                var bri = font.FontSystem.DrawText(text);
                if (bri == null || bri == PrimitiveRenderInformation.LOADING_PLACEHOLDER)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"BRI STILL NULL!!! ({text})");
                    return false;
                }
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;

            }
        }
    }
}
