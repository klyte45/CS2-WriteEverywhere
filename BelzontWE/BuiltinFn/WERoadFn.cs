using Belzont.Utils;
using Colossal.Entities;
using Game.Common;
using Game.Net;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Road")]
    public static class WERoadFn
    {
        public struct WENodeElementCache : IComponentData
        {
            public byte ExtraDataBelongingIdx { get; set; }
            public Colossal.Hash128 VersionHash { get; set; }
            public byte ExtraDataBelongingSideIdx { get; set; }
        }

        [WEFormula(typeof(WENodeElementCache))]
        public static WENodeElementCache GetNodePropData(Entity reference) => GetRoadCache(reference, Identity);
        [WEFormula(typeof(WENodeExtraDataUpdater.WENetNodeInformation))]
        public static WENodeExtraDataUpdater.WENetNodeInformation GetRoadSideSegmentForProp(Entity reference) => GetRoadCache(reference, GetSideSegment);
        [WEFormula(typeof(WENodeExtraDataUpdater.WENetNodeInformation))]
        public static WENodeExtraDataUpdater.WENetNodeInformation GetRoadOwnSegmentForProp(Entity reference) => GetRoadCache(reference, GetRoadOwnSegment);
        [WEFormula(typeof(WENodeExtraDataUpdater.WENetNodeInformation))]
        public static WENodeExtraDataUpdater.WENetNodeInformation GetFromPropByTargetVar(Entity reference, Dictionary<string, string> vars)
        {
            return vars.GetValueOrDefault("target") switch
            {
                "side" => GetRoadSideSegmentForProp(reference),
                _ => GetRoadOwnSegmentForProp(reference),
            };
        }

        private static WENodeElementCache Identity(WENodeElementCache cache, ref DynamicBuffer<WENodeExtraDataUpdater.WENetNodeInformation> nodeBuffer) => cache;
        private static WENodeExtraDataUpdater.WENetNodeInformation GetSideSegment(WENodeElementCache cache, ref DynamicBuffer<WENodeExtraDataUpdater.WENetNodeInformation> nodeInfo) => nodeInfo[cache.ExtraDataBelongingSideIdx];
        private static WENodeExtraDataUpdater.WENetNodeInformation GetRoadOwnSegment(WENodeElementCache cache, ref DynamicBuffer<WENodeExtraDataUpdater.WENetNodeInformation> nodeInfo) => nodeInfo[cache.ExtraDataBelongingIdx];

        private delegate T DoWithCacheDelegate<T>(WENodeElementCache cache, ref DynamicBuffer<WENodeExtraDataUpdater.WENetNodeInformation> nodeBuffer);

        private static T GetRoadCache<T>(Entity reference, DoWithCacheDelegate<T> doWithCache)
        {

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (!em.TryGetComponent<Owner>(reference, out var owner) || !em.TryGetBuffer<WENodeExtraDataUpdater.WENetNodeInformation>(owner.m_Owner, true, out var nodeInfo) || nodeInfo.Length == 0)
            {
                return default;
            }
            if (em.TryGetComponent<WENodeElementCache>(reference, out var elementCache))
            {
                if (elementCache.VersionHash == nodeInfo[elementCache.ExtraDataBelongingIdx].m_versionIdentifier)
                {
                    return doWithCache(elementCache, ref nodeInfo);
                }
            }

            if (em.TryGetComponent<Transform>(reference, out var transform))
            {
                WENodeElementCache result = default;
                var angle = nodeInfo[0].m_centerPoint.xz.GetAngleToPoint(transform.m_Position.xz);
                var azimuthDirection16bits = (ushort)((angle + 360) % 360f / 360f * 65536f);

                var nextRefIdx = 0;
                for (; nextRefIdx < nodeInfo.Length; nextRefIdx++)
                {
                    if (nodeInfo[nextRefIdx].m_azimuthDirection16bits >= azimuthDirection16bits)
                    {
                        break;
                    }
                }
                var prevRefIdx = (nextRefIdx + nodeInfo.Length - 1) % nodeInfo.Length;
                nextRefIdx %= nodeInfo.Length;

                var belongsToPrev = false;
                unchecked
                {
                    belongsToPrev = math.abs((short)(nodeInfo[prevRefIdx].m_azimuthDirection16bits - azimuthDirection16bits)) < math.abs((short)(nodeInfo[nextRefIdx].m_azimuthDirection16bits - azimuthDirection16bits));
                }
                if (belongsToPrev)
                {
                    result.VersionHash = nodeInfo[prevRefIdx].m_versionIdentifier;
                    result.ExtraDataBelongingIdx = (byte)prevRefIdx;
                    result.ExtraDataBelongingSideIdx = (byte)nextRefIdx;
                }
                else
                {
                    result.VersionHash = nodeInfo[nextRefIdx].m_versionIdentifier;
                    result.ExtraDataBelongingIdx = (byte)nextRefIdx;
                    result.ExtraDataBelongingSideIdx = (byte)prevRefIdx;
                }
                WENodeExtraDataUpdater.EnqueueToRun((B) => B.CreateCommandBuffer().AddComponent(reference, result));
                return doWithCache(result, ref nodeInfo);
            }
            return default;
        }

        [WEFormula(typeof(Entity))]
        public static Entity GetRoadAggregation(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetComponent<Aggregated>(reference, out var agg) ? agg.m_Aggregate : Entity.Null;
    }
}