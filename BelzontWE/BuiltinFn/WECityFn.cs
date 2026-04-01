using Game.City;
using Game.Simulation;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("City")]
    public static class WECityFn
    {
        private static CityConfigurationSystem cityConfSystem;
        private static CitySystem citySystem;

        [WEFormula(typeof(CityConfigurationSystem))]
        public static CityConfigurationSystem GetCityConfSystem(Entity e) => cityConfSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CityConfigurationSystem>();

        [WEFormula(typeof(CitySystem))]
        public static CitySystem GetCitySystem(Entity e) => citySystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CitySystem>();
    }
}