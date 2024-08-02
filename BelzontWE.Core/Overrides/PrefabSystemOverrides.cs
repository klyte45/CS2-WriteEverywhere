using Belzont.Utils;
using Game.Prefabs;
using HarmonyLib;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE
{
    public class PrefabSystemOverrides : Redirector, IRedirectableWorldless
    {
        private static WETemplateManager m_templateManager;
        public static readonly AccessTools.FieldRef<PrefabSystem, List<PrefabBase>> LoadedPrefabBaseList = AccessTools.FieldRefAccess<PrefabSystem, List<PrefabBase>>("m_Prefabs");
        public static readonly AccessTools.FieldRef<PrefabSystem, Dictionary<PrefabBase, Entity>> LoadedPrefabEntitiesList = AccessTools.FieldRefAccess<PrefabSystem, Dictionary<PrefabBase, Entity>>("m_Entities");
        public void Awake()
        {
            AddRedirect(typeof(PrefabSystem).GetMethod("UpdatePrefabs", RedirectorUtils.allFlags), null, GetType().GetMethod(nameof(AfterUpdatePrefabs), RedirectorUtils.allFlags));
        }
        private static void AfterUpdatePrefabs(bool __result)
        {
            if (__result)
            {
                m_templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>();
                m_templateManager.MarkPrefabsDirty();
            }
        }
    }
}
