using Belzont.Interfaces;
using Belzont.Utils;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    /// <summary>
    /// Removes orphaned GameProp sub-entities spawned by WEGamePropSpawnSystem:
    ///   1. WEChild without WEOwner (post-load stale — owner was not serialized across save)
    ///   2. WEChild + WEOwner where the owner entity no longer has WETextDataMesh
    /// Also keeps WESubObject buffers on text nodes consistent.
    /// </summary>
    public partial class WEGamePropCleanupSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.ModificationEnd;

        private EntityQuery m_staleOrphanQuery;   // WEChild, no WEOwner
        private EntityQuery m_orphanWeSubObjects; // WESubObject without data entity

        protected override void OnCreateWithBarrier()
        {
            m_staleOrphanQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<WEChild>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<WEOwner>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                }
            });

            m_orphanWeSubObjects = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<WESubObject>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<WETextDataMain>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                }
            });

            RequireAnyForUpdate(m_staleOrphanQuery, m_orphanWeSubObjects);
        }

        protected override void OnUpdate()
        {
            var cmd = Barrier.CreateCommandBuffer();

            // Pass 1: Post-load stale — WEChild but no WEOwner
            if (!m_staleOrphanQuery.IsEmpty)
            {
                var stale = m_staleOrphanQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < stale.Length; i++)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[WEGamePropCleanup] Destroying stale orphan (no owner): {stale[i]}");
                    cmd.AddComponent<Deleted>(stale[i]);
                }
                stale.Dispose();
            }

            if (m_orphanWeSubObjects.IsEmpty) return;
            var childLkp = GetComponentLookup<WEChild>(true);

            var textNodes = m_orphanWeSubObjects.ToEntityArray(Allocator.Temp);
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

                }
                subBuf.Clear();
            }
            textNodes.Dispose();
        }
    }
}
