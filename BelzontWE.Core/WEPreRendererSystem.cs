using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game;
using Game.Common;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using WriteEverywhere.Sprites;

namespace BelzontWE
{
    public partial class WEPreRendererSystem : SystemBase
    {
        private EntityQuery m_noWaitQueueEntities;
        private EntityQuery m_pendingQueueEntities;
        private EndFrameBarrier m_endFrameBarrier;
        private WEAtlasesLibrary m_atlasesLibrary;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_atlasesLibrary = World.GetOrCreateSystemManaged<WEAtlasesLibrary>();

            m_noWaitQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadWrite<WESimulationTextComponent>(),
                    },
                    Any =new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
            m_pendingQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                    },
                    Any =new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            }); ;

            RequireAnyForUpdate(m_pendingQueueEntities, m_noWaitQueueEntities);
        }
        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            CheckPendingQueue();
            CheckNoWaitQueue();

        }

        private void CheckNoWaitQueue()
        {
            if (!m_noWaitQueueEntities.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_noWaitQueueEntities.ToEntityArray(Allocator.TempJob);
                var barrier = m_endFrameBarrier.CreateCommandBuffer();
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    barrier.AddBuffer<WEWaitingRenderingComponent>(entity);
                }
                entities.Dispose();
            }
        }

        private void CheckPendingQueue()
        {
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_pendingQueueEntities.ToEntityArray(Allocator.TempJob);
                var barrier = m_endFrameBarrier.CreateCommandBuffer();
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomDataPending = EntityManager.GetBuffer<WEWaitingRenderingComponent>(entity);
                    if (!EntityManager.TryGetBuffer<WESimulationTextComponent>(entity, true, out var wePersistentData))
                    {
                        wePersistentData = barrier.AddBuffer<WESimulationTextComponent>(entity);
                    }

                    for (var j = 0; j < weCustomDataPending.Length; j++)
                    {
                        var weCustomData = weCustomDataPending[j];
                        var text = weCustomData.src.GetEffectiveText(EntityManager);
                        if (weCustomData.src.TextType == WESimulationTextType.Text)
                        {
                            UpdateTextMesh(ref weCustomDataPending, ref wePersistentData, ref j, weCustomData, text);
                        }
                        else if (weCustomData.src.TextType == WESimulationTextType.Image)
                        {
                            UpdateImageMesh(ref weCustomDataPending, ref wePersistentData, ref j, weCustomData, text);
                        }
                    }
                }
                entities.Dispose();
            }
        }

        private void UpdateImageMesh(ref DynamicBuffer<WEWaitingRenderingComponent> weCustomDataPending, ref DynamicBuffer<WESimulationTextComponent> wePersistentData, ref int j, WEWaitingRenderingComponent weCustomData, string text)
        {
            var bri = m_atlasesLibrary.GetFromLocalAtlases(weCustomData.src.Atlas, text, true);
            if (bri == null)
            {
                 LogUtils.DoWarnLog("IMAGE BRI STILL NULL!!!");
                return;
            }
            wePersistentData.Add(WESimulationTextComponent.From(weCustomData, bri, text));
            weCustomDataPending.RemoveAt(j);
            j--;
        }

        private void UpdateTextMesh(ref DynamicBuffer<WEWaitingRenderingComponent> weCustomDataPending, ref DynamicBuffer<WESimulationTextComponent> wePersistentData, ref int j, WEWaitingRenderingComponent weCustomData, string text)
        {
            var font = EntityManager.TryGetComponent<FontSystemData>(weCustomData.src.Font, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
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
            wePersistentData.Add(WESimulationTextComponent.From(weCustomData, bri, text));
            weCustomDataPending.RemoveAt(j);
            j--;
        }
    }

}