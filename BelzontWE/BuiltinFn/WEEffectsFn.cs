using Game.Simulation;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Effects")]
    public static class WEEffectsFn
    {
        private static PlanetarySystem planetarySystem;

        [WEFormula(typeof(float))]
        public static float GetNightLight01(Entity _)
        {
            planetarySystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlanetarySystem>();
            return planetarySystem.NightLight.isValid && planetarySystem.NightLight.additionalData?.intensity > .5f ? 1 : 0;
        }
    }
}
