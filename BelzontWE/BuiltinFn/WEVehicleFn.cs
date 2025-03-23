using Colossal.Entities;
using Game.Common;
using Game.Objects;
using Game.Routes;
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
                : !em.TryGetComponent<Connected>(target.m_Target, out var connected)
                    ? !em.TryGetComponent(entity, out owner)
                        ? WEUtitlitiesFn.GetEntityName(target.m_Target)
                        : WEUtitlitiesFn.GetEntityName(owner.m_Owner)
                : WEUtitlitiesFn.GetEntityName(connected.m_Connected);
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
                : !em.TryGetComponent<RouteNumber>(ownerRoute.m_Owner, out var number) ? "<?>"
                : number.m_Number.ToString();
        };
        public static Func<Entity, string> GetSerialNumber_binding = (entity) => (entity.Index % 100000).ToString().PadLeft(5, '0');
        public static Func<Entity, string> GetVehiclePlateLine1_binding = (entity) => { var plate = GetVehiclePlate(entity); return plate[..(plate.Length / 2)]; };
        public static Func<Entity, string> GetVehiclePlateLine2_binding = (entity) => { var plate = GetVehiclePlate(entity); return plate[(plate.Length / 2)..]; };

        public static string GetTargetDestinationStatic(Entity reference) => GetTargetDestinationStatic_binding?.Invoke(reference) ?? "<???>";
        public static string GetTargetDestinationDynamic(Entity reference) => GetTargetDestinationDynamic_binding?.Invoke(reference) ?? "<???>";
        public static string GetVehiclePlate(Entity vehicleRef) => GetVehiclePlate_binding?.Invoke(vehicleRef) ?? "<???>";
        public static string GetVehiclePlateLine1(Entity vehicleRef) => GetVehiclePlateLine1_binding?.Invoke(vehicleRef) ?? "<???>";
        public static string GetVehiclePlateLine2(Entity vehicleRef) => GetVehiclePlateLine2_binding?.Invoke(vehicleRef) ?? "<???>";
        public static string GetTransportLineNumber(Entity reference) => GetTargetTransportLineNumber_binding?.Invoke(reference) ?? "<!>";
        public static string GetSerialNumber(Entity reference) => GetSerialNumber_binding?.Invoke(reference) ?? "<???>";
    }
}
//&BelzontWE.Builtin.WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0
//&Color32;get_cyan

//&WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0