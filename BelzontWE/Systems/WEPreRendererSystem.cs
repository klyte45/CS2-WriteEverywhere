using Belzont.Utils;
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
            }); ;

            RequireForUpdate(m_pendingPostInstantiate);
        }
        protected override void OnUpdate()
        {
            var entities = m_pendingPostInstantiate.ToEntityArray(Allocator.Temp);
            var meshData = m_pendingPostInstantiate.ToComponentDataArray<WETextDataMesh>(Allocator.Temp);
            var mainData = m_pendingPostInstantiate.ToComponentDataArray<WETextDataMain>(Allocator.Temp);
            var sourceData = m_pendingPostInstantiate.ToComponentDataArray<WETextDataSourceMod>(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    meshData[i].OnPostInstantiate(EntityManager, mainData[i].TargetEntity, sourceData[i].ToString());
                    EntityManager.SetComponentData(entities[i], meshData[i]);
                    EntityManager.RemoveComponent<WEWaitingPostInstantiation>(entities[i]);
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