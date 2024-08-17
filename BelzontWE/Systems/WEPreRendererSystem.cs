using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEPreRendererSystem : SystemBase
    {
        private EntityQuery m_pendingPostInstantiate;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_pendingPostInstantiate = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextData>(),
                        ComponentType.ReadOnly<WEWaitingPostInstantiation>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            }); ;

            RequireForUpdate(m_pendingPostInstantiate);
        }
        protected override void OnUpdate()
        {
            var entities = m_pendingPostInstantiate.ToEntityArray(Allocator.Temp);
            var weData = m_pendingPostInstantiate.ToComponentDataArray<WETextData>(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    weData[i].OnPostInstantiate(EntityManager);
                    EntityManager.SetComponentData(entities[i], weData[i]);
                    EntityManager.RemoveComponent<WEWaitingPostInstantiation>(entities[i]);
                }
            }
            finally
            {
                weData.Dispose();
                entities.Dispose();
            }
        }
    }

}