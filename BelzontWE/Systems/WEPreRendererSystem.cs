using Game;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEPreRendererSystem : SystemBase
    {
        private EntityQuery m_pendingPostInstantiate;
        private EndFrameBarrier m_endFrameBarrier;


        protected override void OnCreate()
        {
            base.OnCreate();
            m_pendingPostInstantiate = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextDataMesh>(),
                        ComponentType.ReadWrite<WETextDataMain>(),
                        ComponentType.ReadOnly<WEWaitingPostInstantiation>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            RequireAnyForUpdate(m_pendingPostInstantiate);
        }
        protected override void OnUpdate()
        {
            var commandBuffer = m_endFrameBarrier.CreateCommandBuffer();
            if (!m_pendingPostInstantiate.IsEmpty)
            {
                var entities = m_pendingPostInstantiate.ToEntityArray(Allocator.Temp);
                var meshData = m_pendingPostInstantiate.ToComponentDataArray<WETextDataMesh>(Allocator.Temp);
                var mainData = m_pendingPostInstantiate.ToComponentDataArray<WETextDataMain>(Allocator.Temp);
                try
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        meshData[i].OnPostInstantiate(EntityManager, mainData[i].TargetEntity);
                        commandBuffer.SetComponent(entities[i], meshData[i]);
                        commandBuffer.RemoveComponent<WEWaitingPostInstantiation>(entities[i]);
                    }
                }
                finally
                {
                    meshData.Dispose();
                    entities.Dispose();
                }
            }
        }
    }

}