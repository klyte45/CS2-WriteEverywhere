using Colossal.Entities;
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
        public static string GetTransportLineNumber(Entity reference) => GetTransportLineNumber_binding?.Invoke(reference) ?? "<!>";
    }

}
//&BelzontWE.Builtin.WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0
//&Color32;get_cyan

//&WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0