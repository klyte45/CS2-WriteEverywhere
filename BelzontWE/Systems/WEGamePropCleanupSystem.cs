using Belzont.Interfaces;
using Belzont.Utils;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    /// <summary>
    /// Cleans up stale entries in WESubObject buffers on GameProp text nodes:
    ///   - Iterates only entities that have a WESubObject buffer (text nodes that spawned props)
    ///   - For each buffer entry, if the referenced sub-entity no longer exists or lacks WEChild, destroys it and removes it from the buffer
    /// This is O(spawned-text-nodes), not O(all-entities).
    /// </summary>
    public partial class WEGamePropCleanupSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.ModificationEnd;

        private EntityQuery m_textNodesWithSubObjectsQuery;

        protected override void OnCreateWithBarrier()
        {
            m_textNodesWithSubObjectsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<WESubObject>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                }
            });
        }

        protected override void OnUpdate()
        {
            if (m_textNodesWithSubObjectsQuery.IsEmpty) return;

            var cmd = Barrier.CreateCommandBuffer();
            var childLkp = GetComponentLookup<WEChild>(true);

            var textNodes = m_textNodesWithSubObjectsQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < textNodes.Length; i++)
            {
                var textNode = textNodes[i];
                var subBuf = EntityManager.GetBuffer<WESubObject>(textNode);

                for (int j = subBuf.Length - 1; j >= 0; j--)
                {
                    var subEntity = subBuf[j].m_SubObject;
                    if (EntityManager.Exists(subEntity) && childLkp.HasComponent(subEntity)) continue;

                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[WEGamePropCleanup] Removing stale WESubObject entry {subEntity} from text node {textNode}");

                    if (EntityManager.Exists(subEntity))
                        cmd.AddComponent<Deleted>(subEntity);

                    subBuf.RemoveAtSwapBack(j);
                }
            }
            textNodes.Dispose();
        }

        private static void RemoveFromSubObjectBuffer(ref DynamicBuffer<WESubObject> buf, Entity toRemove)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].m_SubObject == toRemove)
                {
                    buf.RemoveAtSwapBack(i);
                    return;
                }
            }
        }
    }
}
