using Belzont.Interfaces;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    /// <summary>
    /// Per-frame transform sync for GameProp sub-entities spawned by WEGamePropSpawnSystem.
    /// Reads WEOwner → WETextDataMain.TargetEntity (geometry) → compose transforms → write Transform.
    /// Runs at ModificationEnd, after SubObjectSystem (Modification2B).
    /// </summary>
    [UpdateAfter(typeof(WEGamePropCleanupSystem))]
    public partial class WEGamePropTransformSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.ModificationEnd;

        private EntityQuery m_propQuery;

        protected override void OnCreateWithBarrier()
        {
            m_propQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<WEChild>(),
                    ComponentType.ReadOnly<WEOwner>(),
                    ComponentType.ReadWrite<Game.Objects.Transform>(),
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
            if (m_propQuery.IsEmpty) return;

            new TransformSyncJob
            {
                m_OwnerHdl = GetComponentTypeHandle<WEOwner>(true),
                m_GameTransformHdl = GetComponentTypeHandle<Game.Objects.Transform>(),
                m_MainLkp = GetComponentLookup<WETextDataMain>(true),
                m_TransformLkp = GetComponentLookup<Game.Objects.Transform>(true),
                m_WeTransformLkp = GetComponentLookup<WETextDataTransform>(true),
            }.ScheduleParallel(m_propQuery, Dependency).Complete();
        }

#if BURST
        [BurstCompile]
#endif
        private struct TransformSyncJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<WEOwner> m_OwnerHdl;
            public ComponentTypeHandle<Game.Objects.Transform> m_GameTransformHdl;
            [ReadOnly] public ComponentLookup<WETextDataMain> m_MainLkp;
            [ReadOnly] public ComponentLookup<Game.Objects.Transform> m_TransformLkp;
            [ReadOnly] public ComponentLookup<WETextDataTransform> m_WeTransformLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var owners = chunk.GetNativeArray(ref m_OwnerHdl);
                var gameTransforms = chunk.GetNativeArray(ref m_GameTransformHdl);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var ownerTextNode = owners[i].m_weOwnerEntity;
                    if (!m_MainLkp.TryGetComponent(ownerTextNode, out var main)) continue;

                    var geoEntity = main.TargetEntity;
                    if (!m_TransformLkp.TryGetComponent(geoEntity, out var geomTransform)) continue;

                    float3 worldPos;
                    quaternion worldRot;

                    if (m_WeTransformLkp.TryGetComponent(ownerTextNode, out var weTransform))
                    {
                        var geomMatrix = float4x4.TRS(geomTransform.m_Position, geomTransform.m_Rotation, new float3(1f));
                        worldPos = math.transform(geomMatrix, weTransform.offsetPosition);
                        worldRot = math.mul(geomTransform.m_Rotation, weTransform.offsetRotation);
                    }
                    else
                    {
                        worldPos = geomTransform.m_Position;
                        worldRot = geomTransform.m_Rotation;
                    }

                    gameTransforms[i] = new Game.Objects.Transform(worldPos, worldRot);
                }
            }
        }
    }
}
