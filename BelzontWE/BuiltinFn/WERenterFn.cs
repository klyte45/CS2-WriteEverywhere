﻿using Colossal.Entities;
using Game.Companies;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public class WERenterFn
    {
        public static TradeCost GetTradeCost0(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<TradeCost>(reference, true, out var costs) && costs.Length > 0 ? costs[0] : default;
        public static TradeCost GetTradeCost1(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<TradeCost>(reference, true, out var costs) && costs.Length > 1 ? costs[1] : default;
        public static TradeCost GetTradeCost2(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<TradeCost>(reference, true, out var costs) && costs.Length > 2 ? costs[2] : default;
        public static TradeCost GetTradeCost3(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<TradeCost>(reference, true, out var costs) && costs.Length > 3 ? costs[3] : default;
        public static TradeCost GetTradeCost4(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<TradeCost>(reference, true, out var costs) && costs.Length > 4 ? costs[4] : default;
    }
}
//&BelzontWE.Builtin.WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0
//&Color32;get_cyan

//&WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0