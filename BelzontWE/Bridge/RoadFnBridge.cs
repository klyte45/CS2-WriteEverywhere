using BelzontWE.Builtin;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class RoadFnBridge
    {
        public static (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) GetRoadSideSegmentForProp(Entity reference) => WERoadFn.GetRoadSideSegmentForProp(reference).ToTuple();
        public static (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) GetRoadOwnSegmentForProp(Entity reference) => WERoadFn.GetRoadOwnSegmentForProp(reference).ToTuple();
        public static (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) GetFromPropByTargetVar(Entity reference, Dictionary<string, string> vars) => WERoadFn.GetFromPropByTargetVar(reference, vars).ToTuple();
    }
}
