using Belzont.Interfaces;
using Belzont.Utils;
using Game.Common;
using Game.Prefabs;
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
    public partial class WECleanupSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.Modification1;

        private EntityQuery m_staleOrphanQuery;   // WEChild, no WEOwner
        private EntityQuery m_orphanWeSubObjects; // WESubObject without data entity
        private EntityQuery m_orphanRefObjects; // WESubObject without data entity

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
                }
            });

            m_orphanRefObjects = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<WESubTextRef>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<WETextComponentValid>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                }
            });

            RequireAnyForUpdate(m_staleOrphanQuery, m_orphanRefObjects, m_orphanWeSubObjects);
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
                    cmd.RemoveComponent<WEChild>(stale[i]);
                }
                stale.Dispose();
            }

            if (!m_orphanWeSubObjects.IsEmpty)
            {
                var childLkp = GetComponentLookup<WEChild>(true);

                var textNodes = m_orphanWeSubObjects.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < textNodes.Length; i++)
                {
                    var textNode = textNodes[i];
                    var subBuf = EntityManager.GetBuffer<WESubObject>(textNode);

                    for (int j = subBuf.Length - 1; j >= 0; j--)
                    {
                        var subEntity = subBuf[j].m_SubObject;

                        if (BasicIMod.DebugMode) LogUtils.DoLog($"[WEGamePropCleanup] Removing stale WESubObject entry {subEntity} from text node {textNode}");

                        if (EntityManager.Exists(subEntity))
                            cmd.AddComponent<Deleted>(subEntity);

                    }
                    subBuf.Clear();
                    EntityManager.RemoveComponent<WESubObject>(textNode);
                }
                textNodes.Dispose();
            }

            if (m_orphanRefObjects.IsEmpty) return;

            var refNodes = m_orphanRefObjects.ToEntityArray(Allocator.Temp);
            for (int h = 0; h < refNodes.Length; h++)
            {
                var refNode = refNodes[h];
                var buff = EntityManager.GetBuffer<WESubTextRef>(refNode);
                for (int i = buff.Length - 1; i >= 0; i--)
                {
                    EntityManager.AddComponent<Game.Common.Deleted>(buff[i].m_weTextData);
                    if (EntityManager.HasComponent<WETextComponentValid>(buff[i].m_weTextData)) EntityManager.RemoveComponent<WETextComponentValid>(buff[i].m_weTextData);
                    buff.RemoveAt(i);
                }
                EntityManager.RemoveComponent<WESubTextRef>(refNode);
            }

        }
    }
}
