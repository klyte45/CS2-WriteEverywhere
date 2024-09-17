using Colossal.Entities;
using Game.Buildings;
using System;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WEBuildingFn
    {
        public static Func<Entity, Entity> GetBuildingRoad_binding = (entity)
            => BuildingUtils.GetAddress(World.DefaultGameObjectInjectionWorld.EntityManager, entity, out var road, out _)
                ? road
                : Entity.Null;


        public static Func<Entity, string> GetBuildingRoadNumber_binding = (entity)
            => BuildingUtils.GetAddress(World.DefaultGameObjectInjectionWorld.EntityManager, entity, out _, out var number)
                ? number.ToString()
                : "N/A";
        public static Func<Entity, Entity> GetBuildingMainRenter_binding = (entity)
            => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<Renter>(entity, true, out var renters) && renters.Length > 0
                ? renters[0].m_Renter
                : Entity.Null;
        public static Entity GetBuildingRoad(Entity reference) => GetBuildingRoad_binding?.Invoke(reference) ?? Entity.Null;
        public static string GetBuildingRoadNumber(Entity reference) => GetBuildingRoadNumber_binding?.Invoke(reference) ?? "N/A";
        public static Entity GetBuildingMainRenter(Entity reference) => GetBuildingMainRenter_binding?.Invoke(reference) ?? Entity.Null;
    }
}