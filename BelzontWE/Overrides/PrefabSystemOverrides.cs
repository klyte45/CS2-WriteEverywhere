using Belzont.Utils;
using Colossal.IO.AssetDatabase;
using Game.AssetPipeline;
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
            AddRedirect(typeof(AssetImportPipeline).GetMethod("GetTextureReferenceCount", RedirectorUtils.allFlags), GetType().GetMethod(nameof(GetTextureReferenceCount), RedirectorUtils.allFlags));
        }
        private static void BeforeUpdatePrefab(PrefabSystem __instance, bool __result, ref int __state)
        {
            m_templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>();
            var updateList = UpdateMapList(__instance);
            __state = (updateList.Count > 0 ? 0 : 1) | (updateList.Any(x => x.Value != m_templateManager.PrefabUpdateSource) ? 0 : 2);
        }
        private static void AfterUpdatePrefabs(bool __result, int __state)
        {
            if (__result)
            {
                m_templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>();
                if (__state == 3)
                    m_templateManager.MarkPrefabsDirty();
                else if (__state == 1)
                    m_templateManager.UpdateEligiblePropsDict();
            }
        }

        private static bool GetTextureReferenceCount(IEnumerable<SurfaceAsset> surfaces, ref int surfaceCount, ref Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> __result)
        {
            __result = new Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>>();
            surfaceCount = 0;
            foreach (SurfaceAsset surfaceAsset in surfaces)
            {
                var shallDispose = surfaceAsset.textures == null;
                if (shallDispose)
                {
                    surfaceAsset.LoadProperties(false);
                }
                foreach (KeyValuePair<string, Colossal.IO.AssetDatabase.TextureAsset> keyValuePair in surfaceAsset.textures)
                {
                    List<SurfaceAsset> list;
                    if (!__result.TryGetValue(keyValuePair.Value, out list))
                    {
                        list = new List<SurfaceAsset>();
                        __result.Add(keyValuePair.Value, list);
                    }
                    list.Add(surfaceAsset);
                }
                surfaceCount++;
                if (shallDispose)
                {
                    surfaceAsset.Dispose();
                }

            }
            return false;
        }
        private static bool get_nameAssetData(ref AssetData __instance, ref string __result)
        {
            if (__instance?.database is null)
            {
                __result = "<NULL!!!!>";
                return false;
            }
            return true;
        }
    }
}
