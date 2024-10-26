using Game.City;
using Game.Simulation;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WECityFn
    {
        private static CityConfigurationSystem cityConfSystem;
        private static CitySystem citySystem;

        public static CityConfigurationSystem GetCityConfSystem(Entity e) => cityConfSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CityConfigurationSystem>();

        public static CitySystem GetCitySystem(Entity e) => citySystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CitySystem>();
    }
}