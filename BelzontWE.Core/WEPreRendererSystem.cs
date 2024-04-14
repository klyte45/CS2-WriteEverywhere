using Colossal.Entities;
using Game;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEPreRendererSystem : SystemBase
    {
        private FontServer m_FontServer;
        private EntityQuery m_noWaitQueueEntities;
        private EntityQuery m_pendingQueueEntities;
        private EndFrameBarrier m_endFrameBarrier;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();

            m_noWaitQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
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
                new EntityQueryDesc
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
                        var font = m_FontServer[weCustomData.fontName.ToString()];
                        if (font is null)
                        {
                            continue;
                        }
                        var bri = m_FontServer[weCustomData.fontName.ToString()].DrawString(weCustomData.text.ToString(), default);
                        if (bri == null)
                        {
                            continue;
                        }
                        wePersistentData.Add(WESimulationTextComponent.From(weCustomData, font, bri));
                        weCustomDataPending.RemoveAt(j);
                        j--;
                    }
                }
                entities.Dispose();
            }
        }

    }

}