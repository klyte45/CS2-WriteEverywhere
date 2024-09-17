using Colossal.Entities;
using Game;
using Game.City;
using Game.Common;
using Game.Net;
using Kwytto.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;

namespace BelzontWE.Builtin
{
    public static class WERoadFn
    {
        public interface ICacheComponent
        {
            Entity CachedValue { set; get; }
            int RefChecksum { get; set; }
            byte NodeIdx { get; set; }
            Entity PrevValue { get; set; }
            Entity NextValue { get; set; }
        }
        public struct WECachedGetRoadSegmentOfSubProp : IComponentData, ICacheComponent
        {
            public Entity CachedValue { get; set; }
            public int RefChecksum { get; set; }
            public byte NodeIdx { get; set; }
            public Entity PrevValue { get; set; }
            public Entity NextValue { get; set; }
        }
        public struct WECachedGetRoadSegmentOfSubProp180 : IComponentData, ICacheComponent
        {
            public Entity CachedValue { get; set; }
            public int RefChecksum { get; set; }
            public byte NodeIdx { get; set; }
            public Entity PrevValue { get; set; }
            public Entity NextValue { get; set; }
        }
        //&BelzontWE.Builtin.WERoadFn;GetRoadSegmentCrossingDataByPropAngle180.CachedValue/&BelzontWE.Builtin.WERoadFn;GetRoadAggregation/&BelzontWE.Builtin.WEUtitlitiesFn;GetEntityName
        private static EndFrameBarrier m_barrier;
        private static EndFrameBarrier Barrier => m_barrier ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EndFrameBarrier>();
        private static CityConfigurationSystem m_cityConf;
        private static CityConfigurationSystem CityConfiguration => m_cityConf ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CityConfigurationSystem>();

        public static ICacheComponent GetRoadSegmentCrossingDataByPropAngle(Entity reference) => GetReferenceRoadSegmentByAngle<WECachedGetRoadSegmentOfSubProp>(reference, false);
        public static ICacheComponent GetRoadSegmentCrossingDataByPropAngle180(Entity reference) => GetReferenceRoadSegmentByAngle<WECachedGetRoadSegmentOfSubProp180>(reference, true);
        public static Entity GetNextRoadSegmentOfSubPropByAngle(ICacheComponent cache) => CityConfiguration.leftHandTraffic ? cache.PrevValue : cache.NextValue;
        public static Entity GetPrevRoadSegmentOfSubPropByAngle(ICacheComponent cache) => CityConfiguration.leftHandTraffic ? cache.NextValue : cache.PrevValue;
        public static Entity GetCrossingRoad(ICacheComponent cache)
        {
            var currentAggregation = GetRoadAggregation(cache.CachedValue);
            return (CityConfiguration.leftHandTraffic && GetRoadAggregation(cache.PrevValue) != currentAggregation) || GetRoadAggregation(cache.NextValue) == currentAggregation ? cache.PrevValue : cache.NextValue;
        }
        //&BelzontWE.Builtin.WERoadFn;GetCrossingRoadAggregation/&BelzontWE.Builtin.WEUtitlitiesFn;GetEntityName
        private static TCacheComponent GetReferenceRoadSegmentByAngle<TCacheComponent>(Entity reference, bool rotated180) where TCacheComponent : unmanaged, ICacheComponent, IComponentData
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (em.TryGetComponent<Transform>(reference, out var transform)
                && em.TryGetComponent<Owner>(reference, out var owner)
                && em.TryGetBuffer<ConnectedEdge>(owner.m_Owner, true, out var edges))
            {
                var refChecksum = 0;
                unchecked
                {
                    for (int i = 0; i < edges.Length; i++)
                    {
                        refChecksum += edges[i].m_Edge.Index * edges[i].m_Edge.Version;
                    }
                }
                if (em.TryGetComponent<TCacheComponent>(reference, out var cache))
                {
                    if (cache.RefChecksum == refChecksum)
                    {
                        return cache;
                    }
                    else
                    {
                        em.RemoveComponent<TCacheComponent>(reference);
                    }
                }
                var vectorAngle = math.abs(((Quaternion)transform.m_Rotation).eulerAngles.y + 360) % 360;
                for (int i = 0; i < edges.Length; i++)
                {
                    var edge = edges[i];
                    em.TryGetComponent<Edge>(edge.m_Edge, out var edgeNodes);
                    em.TryGetComponent<Node>(edgeNodes.m_Start, out var startTransform);
                    em.TryGetComponent<Node>(edgeNodes.m_End, out var endTransform);
                    var angle = startTransform.m_Position.xz.GetAngleToPoint(endTransform.m_Position.xz);
                    if (edgeNodes.m_Start != owner.m_Owner) angle = (angle + 180) % 360;
                    var diff = math.abs(angle - vectorAngle);
                    if (rotated180 ? diff > 160 && diff < 200 : diff < 20 || diff > 340)
                    {
                        var cacheObj = new TCacheComponent
                        {
                            CachedValue = edge.m_Edge,
                            RefChecksum = edges.Length,
                            NodeIdx = (byte)i,
                            PrevValue = edges[(i + edges.Length - 1) % edges.Length].m_Edge,
                            NextValue = edges[(i + 1) % edges.Length].m_Edge
                        };
                        Barrier.CreateCommandBuffer().AddComponent(reference, cacheObj);
                        return cacheObj;
                    }
                }

            }
            return default;
        }

        public static Entity GetRoadAggregation(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetComponent<Aggregated>(reference, out var agg) ? agg.m_Aggregate : Entity.Null;
    }
}