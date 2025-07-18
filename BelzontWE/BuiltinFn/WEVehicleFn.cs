﻿using Colossal.Entities;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using System;
using Unity.Entities;
using Target = Game.Common.Target;

namespace BelzontWE.Builtin
{
    public static class WEVehicleFn
    {
        public const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string NUMBERS = "0123456789";
        public readonly static string[] DIGITS_ORDER = { NUMBERS, NUMBERS, LETTERS, NUMBERS, LETTERS, LETTERS, LETTERS };


        public static Func<Entity, string> GetTargetDestinationStatic_binding = (entity) =>
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return !em.TryGetComponent<Target>(entity, out var target) ?
                        !em.TryGetComponent<Owner>(entity, out var owner) ? "<?NO TARGET?>"
                        : WEUtitlitiesFn.GetEntityName(owner.m_Owner)
                : target.m_Target == Entity.Null ? "Cities Skylines II"
                : WERouteFn.GetWaypointStaticDestinationName(entity) is string destinationName ? destinationName
                : !em.TryGetComponent(entity, out owner) ? WEUtitlitiesFn.GetEntityName(target.m_Target)
                : WEUtitlitiesFn.GetEntityName(owner.m_Owner);
        };

        private static CitySystem citySys;

        public static Func<Entity, string> GetTargetDestinationDynamic_binding = GetTargetDestinationStatic_binding;
        public static Func<Entity, string> GetVehiclePlate_binding = (Entity refNum) =>
        {
            citySys ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CitySystem>();
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var refControl = em.TryGetComponent<Controller>(refNum, out var controller) ? controller.m_Controller : refNum;
            var output = "";
            var idx = refNum.Index + (em.TryGetComponent<Owner>(refControl, out var owner) && em.HasComponent<OutsideConnection>(owner.m_Owner) ? owner.m_Owner.Index : citySys.City.Index << 4);
            for (int i = 0; i < DIGITS_ORDER.Length; i++)
            {
                output = DIGITS_ORDER[i][idx % DIGITS_ORDER[i].Length] + output;
                idx /= DIGITS_ORDER[i].Length;
            }

            return output.PadRight(7, '0');
        };
        public static Func<Entity, string> GetTargetTransportLineNumber_binding = (entity) =>
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            return !em.TryGetComponent<Target>(entity, out var target) ? "<T>"
                : target.m_Target == Entity.Null ? "CS2"
                : !em.TryGetComponent<Owner>(target.m_Target, out var ownerRoute) ? "---"
                : WERouteFn.GetTransportLineNumber(ownerRoute.m_Owner);
        };
        public static Func<Entity, string> GetSerialNumber_binding = (entity) => (entity.Index % 100000).ToString().PadLeft(5, '0');
        public static Func<Entity, string> GetConvoyId_binding = (entity) => GetVehiclePlate_binding(World.DefaultGameObjectInjectionWorld.EntityManager.TryGetComponent(entity, out Controller c) ? c.m_Controller : entity);
        public static Func<Entity, string> GetVehiclePlateLine1_binding = (entity) => { var plate = GetVehiclePlate(entity); return plate[..(plate.Length / 2)]; };
        public static Func<Entity, string> GetVehiclePlateLine2_binding = (entity) => { var plate = GetVehiclePlate(entity); return plate[(plate.Length / 2)..]; };

        public static string GetTargetDestinationStatic(Entity reference) => GetTargetDestinationStatic_binding?.Invoke(reference) ?? "<???>";
        public static string GetTargetDestinationDynamic(Entity reference) => GetTargetDestinationDynamic_binding?.Invoke(reference) ?? "<???>";
        public static string GetVehiclePlate(Entity vehicleRef) => GetVehiclePlate_binding?.Invoke(vehicleRef) ?? "<???>";
        public static string GetVehiclePlateLine1(Entity vehicleRef) => GetVehiclePlateLine1_binding?.Invoke(vehicleRef) ?? "<???>";
        public static string GetVehiclePlateLine2(Entity vehicleRef) => GetVehiclePlateLine2_binding?.Invoke(vehicleRef) ?? "<???>";
        public static string GetTransportLineNumber(Entity reference) => GetTargetTransportLineNumber_binding?.Invoke(reference) ?? "<!>";
        public static string GetSerialNumber(Entity reference) => GetSerialNumber_binding?.Invoke(reference) ?? "<???>";
        public static string GetConvoyId(Entity vehicleRef) => GetConvoyId_binding?.Invoke(vehicleRef) ?? "<???>";
    }

}