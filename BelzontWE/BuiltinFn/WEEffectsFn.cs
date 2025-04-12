using Game.Simulation;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WEEffectsFn
    {
        private static PlanetarySystem planetarySystem;

        public static float GetNightLight01(Entity _)
        {
            planetarySystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlanetarySystem>();
            return planetarySystem.NightLight.isValid && planetarySystem.NightLight.additionalData?.intensity > .5f ? 1 : 0;
        }
    }
}
