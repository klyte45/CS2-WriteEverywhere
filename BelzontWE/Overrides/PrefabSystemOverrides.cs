using Belzont.Utils;
using Game.Prefabs;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace BelzontWE
{
    public class PrefabSystemOverrides : Redirector, IRedirectableWorldless
    {
        private static WETemplateManager m_templateManager;
        public static readonly AccessTools.FieldRef<PrefabSystem, List<PrefabBase>> LoadedPrefabBaseList = AccessTools.FieldRefAccess<PrefabSystem, List<PrefabBase>>("m_Prefabs");
        public static readonly AccessTools.FieldRef<PrefabSystem, Dictionary<PrefabBase, Entity>> LoadedPrefabEntitiesList = AccessTools.FieldRefAccess<PrefabSystem, Dictionary<PrefabBase, Entity>>("m_Entities");
        private static readonly AccessTools.FieldRef<PrefabSystem, Dictionary<PrefabBase, Entity>> UpdateMapList = AccessTools.FieldRefAccess<PrefabSystem, Dictionary<PrefabBase, Entity>>("m_UpdateMap");
        public void Awake()
        {
            AddRedirect(typeof(PrefabSystem).GetMethod("UpdatePrefabs", RedirectorUtils.allFlags), GetType().GetMethod(nameof(BeforeUpdatePrefab)), GetType().GetMethod(nameof(AfterUpdatePrefabs), RedirectorUtils.allFlags));
        }
        private static void BeforeUpdatePrefab(PrefabSystem __instance, bool __result, ref bool __state)
        {
            m_templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>();
            var updateList = UpdateMapList(__instance);
            __state = updateList.Count > 0 && updateList.Any(x => x.Value != m_templateManager.PrefabUpdateSource);
        }
        private static void AfterUpdatePrefabs(bool __result, bool __state)
        {
            if (__result && __state)
            {
                m_templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>();
                m_templateManager.MarkPrefabsDirty();
            }
        }
    }
}
