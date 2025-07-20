using Game.Common;
using Game.Net;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Jobs;
using Game;
using Game.Tools;
using Belzont.Utils;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using System;
using static Game.Rendering.Debug.RenderPrefabRenderer;





#if BURST
using Unity.Burst;
#endif
namespace BelzontWE
{
    public partial class WENodeExtraDataUpdater : GameSystemBase
    {
        private EntityQuery m_toBeCalculatedQuery;
        private EndFrameBarrier m_endFrameBarrier;
        private readonly Queue<Action> m_actionsToRun = new();
        private static WENodeExtraDataUpdater Instance { get; set; }

        public static void EnqueueToRun(Action action)
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("WENodeExtraDataUpdater is not initialized.");
            }
            Instance.m_actionsToRun.Enqueue(action);
        }

        public struct WENetNodeInformation : IBufferElementData, IComparer<WENetNodeInformation>
        {
            public Entity m_refEdge;
            public ushort m_azimuthDirection16bits;
            public float3 m_centerPoint;
            public float3 m_refPoint;
            public Colossal.Hash128 m_versionIdentifier;

            public int Compare(WENetNodeInformation x, WENetNodeInformation y) => x.m_azimuthDirection16bits.CompareTo(y.m_azimuthDirection16bits);

            public readonly (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) ToTuple()
                => (m_refEdge, m_azimuthDirection16bits, m_centerPoint, m_refPoint, m_versionIdentifier);
        }


        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            m_toBeCalculatedQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Node>(),
                        ComponentType.ReadOnly<ConnectedEdge>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WENetNodeInformation>(),
                        ComponentType.ReadOnly<Created>(),
                        ComponentType.ReadOnly<Updated>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
        }

        protected override void OnUpdate()
        {
            while (m_actionsToRun.TryDequeue(out var action))
            {
                action();
            }
            if (!m_toBeCalculatedQuery.IsEmpty)
            {
                new NodeCacheCalculation
                {
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_EntityTypeHandle = GetEntityTypeHandle(),
                    m_connectedEdgeLookup = GetBufferLookup<ConnectedEdge>(true),
                    m_curveLookup = GetComponentLookup<Curve>(true),
                    m_edgeLookup = GetComponentLookup<Edge>(true)
                }.ScheduleParallel(m_toBeCalculatedQuery, Dependency).Complete();
            }
        }
#if BURST
        [BurstCompile]
#endif
        private struct NodeCacheCalculation : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public EntityTypeHandle m_EntityTypeHandle;
            public BufferLookup<ConnectedEdge> m_connectedEdgeLookup;
            public ComponentLookup<Curve> m_curveLookup;
            public ComponentLookup<Edge> m_edgeLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {

                var entities = chunk.GetNativeArray(m_EntityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    var nodeEntity = entities[i];
                    var connectedEdges = m_connectedEdgeLookup[nodeEntity];
                    var tempArray = new NativeArray<WENetNodeInformation>(connectedEdges.Length, Allocator.Temp);
                    for (int j = 0; j < connectedEdges.Length; j++)
                    {
                        var edge = connectedEdges[j].m_Edge;
                        if (m_edgeLookup.TryGetComponent(edge, out var edgeData) && m_curveLookup.TryGetComponent(edge, out var curve))
                        {
                            var angle = 0f;
                            var refPoint = float3.zero;
                            var centerPoint = float3.zero;
                            if (edgeData.m_Start == nodeEntity)
                            {
                                angle = curve.m_Bezier.a.xz.GetAngleToPoint(curve.m_Bezier.b.xz);
                                centerPoint = curve.m_Bezier.a;
                                refPoint = curve.m_Bezier.b;
                            }
                            else if (edgeData.m_End == nodeEntity)
                            {
                                angle = curve.m_Bezier.d.xz.GetAngleToPoint(curve.m_Bezier.c.xz);
                                centerPoint = curve.m_Bezier.d;
                                refPoint = curve.m_Bezier.c;
                            }
                            else
                            {
                                continue; // This edge does not connect to this node
                            }

                            var azimuthDirection16bits = (ushort)((angle + 360) % 360f / 360f * 65536f);
                            tempArray[j] = new WENetNodeInformation
                            {
                                m_refEdge = edge,
                                m_azimuthDirection16bits = azimuthDirection16bits,
                                m_centerPoint = centerPoint,
                                m_refPoint = refPoint,
                                m_versionIdentifier = Guid.NewGuid()
                            };
                        }
                    }

                    tempArray.Sort(new WENetNodeInformation());
                    m_CommandBuffer.AddBuffer<WENetNodeInformation>(unfilteredChunkIndex, nodeEntity).Clear();
                    for (int j = 0; j < tempArray.Length; j++)
                    {
                        var info = tempArray[j];
                        if (info.m_refEdge != Entity.Null)
                        {
                            m_CommandBuffer.AppendToBuffer(unfilteredChunkIndex, nodeEntity, info);
                        }
                    }
                    tempArray.Dispose();
                }
            }
        }
    }
}
