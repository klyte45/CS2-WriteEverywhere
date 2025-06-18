using Colossal.Entities;
using Game.Common;
using Game.Routes;
using System;
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
        public static Func<Entity, string> GetWaypointStaticDestinationName_binding = (entity) =>
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return !em.TryGetComponent<Connected>(entity, out var connected)
                    ? null
                    : WEUtitlitiesFn.GetEntityName(connected.m_Connected);
        };
        public static string GetTransportLineNumber(Entity reference) => GetTransportLineNumber_binding?.Invoke(reference) ?? "<!>";
        public static string GetWaypointStaticDestinationName(Entity waypointEntity) => GetWaypointStaticDestinationName_binding?.Invoke(waypointEntity) ?? "???";
    }

}