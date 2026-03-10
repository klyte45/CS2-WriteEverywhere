using Colossal.Entities;
using Game.Routes;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WERouteFn
    {
        public static Func<Entity, string> GetTransportLineNumber_binding = (entity) =>
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return !em.TryGetComponent<RouteNumber>(entity, out var number) ? "<?>"
                : number.m_Number.ToString();
        };
        public static Func<Entity, Entity> GetWaypointStaticDestinationEntity_binding = (entity) =>
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return em.TryGetComponent<Waypoint>(entity, out var waypoint)
                && em.TryGetComponent<Connected>(entity, out var connected)
                && em.TryGetBuffer(connected.m_Connected, true, out DynamicBuffer<RouteWaypoint> waypoints)
                && waypoints.Length > 0
                ? waypoints[(waypoint.m_Index + 1) % waypoints.Length].m_Waypoint
                : entity;
        };
        public static Func<Entity, string> GetWaypointStaticDestinationName_binding = (entity) =>
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return WEUtitlitiesFn.GetEntityName(GetWaypointStaticDestinationEntity_binding(entity));
        };
        public static string GetTransportLineNumber(Entity reference) => GetTransportLineNumber_binding?.Invoke(reference) ?? "<!>";
        public static string GetWaypointStaticDestinationName(Entity waypointEntity) => GetWaypointStaticDestinationName_binding?.Invoke(waypointEntity) ?? "???";
        public static Entity GetWaypointStaticDestinationEntity(Entity waypointEntity) => GetWaypointStaticDestinationEntity_binding?.Invoke(waypointEntity) ?? default;

        public static Entity GetNthWaypoint(Entity entity, Dictionary<string, string> vars) => !vars.TryGetValue("!wp#", out var idxStr)
                ? default
                : idxStr.StartsWith("~") && !vars.TryGetValue(idxStr[1..], out idxStr)
                ? default
                : !ushort.TryParse(idxStr, out var n)
                ? default
                : !World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<ConnectedRoute>(entity, true, out var connected)
                ? default
                : connected.Length <= n ? default : connected[n].m_Waypoint;
    }

}