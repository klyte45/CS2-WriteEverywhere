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
        private EntityQuery m_orphanWithOwnerQuery; // WEChild + WEOwner

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

            m_orphanWithOwnerQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<WEChild>(),
                    ComponentType.ReadOnly<WEOwner>(),
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

            // Pass 2: Runtime orphans — WEOwner entity no longer has WETextDataMesh
            if (!m_orphanWithOwnerQuery.IsEmpty)
            {
                var meshLkp = GetComponentLookup<WETextDataMesh>(true);
                var subObjLkp = GetBufferLookup<WESubObject>(false);

                var candidates = m_orphanWithOwnerQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < candidates.Length; i++)
                {
                    var e = candidates[i];
                    var owner = EntityManager.GetComponentData<WEOwner>(e);
                    if (meshLkp.HasComponent(owner.m_weOwnerEntity)) continue;

                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[WEGamePropCleanup] Destroying orphan (owner lost mesh): {e}, owner={owner.m_weOwnerEntity}");

                    // Remove from WESubObject buffer on the owner text node (best-effort; owner may be gone)
                    if (EntityManager.Exists(owner.m_weOwnerEntity)
                        && subObjLkp.TryGetBuffer(owner.m_weOwnerEntity, out var subBuf))
                    {
                        RemoveFromSubObjectBuffer(ref subBuf, e);
                    }

                    cmd.AddComponent<Deleted>(e);
                }
                candidates.Dispose();
            }
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
