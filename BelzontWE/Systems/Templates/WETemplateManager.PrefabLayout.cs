using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Utils;
using Colossal.Entities;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        #region Prefab Layout
        public void MarkPrefabsDirty() => isPrefabListDirty = true;
        private readonly Dictionary<string, HashSet<long>> PrefabNameToIndex = new();
        private Dictionary<(string prefabName, int meshIdx), ObjectState> MeshesHidden = new();
        private bool isPrefabListDirty = true;

        public int CanBePrefabLayout(WETextDataXmlTree data) => World.GetExistingSystemManaged<WETemplateQuerySystem>().CanBePrefabLayout(data);
        public int CanBePrefabLayout(WETextDataXmlTree data, bool isRoot) => World.GetExistingSystemManaged<WETemplateQuerySystem>().CanBePrefabLayout(data, isRoot);
        public int CanBePrefabLayout(WESelflessTextDataTree data) => World.GetExistingSystemManaged<WETemplateQuerySystem>().CanBePrefabLayout(data);
        public bool IsLoadingLayouts => LoadingPrefabLayoutsCoroutine != null;
        private Coroutine LoadingPrefabLayoutsCoroutine;
        private const string LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID = "loadingPrefabTemplates";
        private const string LOADING_SUBTEMPLATES_NOTIFICATION_ID = "loadingModSubtemplates";
        private const string ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID = "errorLoadingPrefabTemplates";
        private const string ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID = "errorLoadingModSubtemplates";
        private unsafe void UpdatePrefabIndexDictionary()
        {
            if (!isPrefabListDirty) return;
            if (LoadingPrefabLayoutsCoroutine != null) GameManager.instance.StopCoroutine(LoadingPrefabLayoutsCoroutine);
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"UpdatePrefabIndexDictionary!!!");

            LoadingPrefabLayoutsCoroutine = GameManager.instance.StartCoroutine(UpdatePrefabIndexDictionary_Coroutine());
        }
        private IEnumerator UpdatePrefabIndexDictionary_Coroutine()
        {
            isPrefabListDirty = false;
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 0);
            yield return 0;
            PrefabNameToIndex.Clear();
            var prefabs = PrefabSystemOverrides.LoadedPrefabBaseList(m_prefabSystem);
            var entities = PrefabSystemOverrides.LoadedPrefabEntitiesList(m_prefabSystem);

            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 1, textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.gettingPrefabIndexes");
            yield return 0;
            foreach (var prefab in prefabs)
            {
                var data = EntityManager.GetComponentData<PrefabData>(entities[prefab]);
                if (!PrefabNameToIndex.ContainsKey(prefab.name)) PrefabNameToIndex[prefab.name] = new();
                PrefabNameToIndex[prefab.name].Add(data.m_Index);
                if (EntityManager.TryGetBuffer<PlaceholderObjectElement>(entities[prefab], true, out var buff))
                {
                    for (int j = 0; j < buff.Length; j++)
                    {
                        var otherData = EntityManager.GetComponentData<PrefabData>(buff[j].m_Object);
                        PrefabNameToIndex[prefab.name].Add(otherData.m_Index);
                    }
                }
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 20, textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.prefabIndexesLoaded");
            yield return LoadTemplatesFromFolder(20, 80f);
            LoadingPrefabLayoutsCoroutine = null;
        }

        private IEnumerator LoadTemplatesFromFolder(int offsetPercentage, float totalStepFull, string modName = null)
        {
            var prefabs = PrefabSystemOverrides.LoadedPrefabBaseList(m_prefabSystem);
            var prefabsToUpdate = new List<PrefabBase>();
            var totalStepPrefabTemplates = modName is null ? totalStepFull : totalStepFull * .7f;
            KFileUtils.EnsureFolderCreation(SAVED_PREFABS_FOLDER);
            var currentValues = PrefabTemplates.Keys.ToArray();
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.01f * totalStepPrefabTemplates)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.erasingCachedLayouts");
            yield return 0;
            if (modName is null)
            {
                for (int i = 0; i < currentValues.Length; i++)
                {
                    PrefabTemplates.Remove(currentValues[i]);
                    if (i % 3 == 0)
                    {
                        NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.01f + (.09f * ((i + 1f) / currentValues.Length))) * totalStepPrefabTemplates)),
                            textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.disposedOldLayout", argsText: new()
                            {
                                ["progress"] = LocalizedString.Value($"{i}/{currentValues.Length}")
                            });
                        yield return 0;
                    }
                }
                PrefabTemplates.Clear();
                foreach (var x in MeshesHidden)
                {
                    foreach (var y in PrefabNameToIndex[x.Key.prefabName])
                    {
                        switch (prefabs[(int)y])
                        {
                            case ObjectGeometryPrefab ogp:
                                if (x.Key.meshIdx < ogp.m_Meshes.Length)
                                {
                                    ogp.m_Meshes[x.Key.meshIdx].m_RequireState = x.Value;
                                }
                                prefabsToUpdate.Add(ogp);
                                break;
                        }
                    }
                }
                MeshesHidden.Clear();
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.11f * totalStepPrefabTemplates)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.searchingForFiles");
            yield return 0;
            var files = modName != null
                ? Directory.GetFiles(m_modsTemplatesFolder[modName].rootFolder, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(y => (y, m_modsTemplatesFolder[modName].id, $"{m_modsTemplatesFolder[modName].name}: {y[m_modsTemplatesFolder[modName].rootFolder.Length..]}")).ToArray()
                : Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(x => (x, (string)null, x[SAVED_PREFABS_FOLDER.Length..]))
                    .Union(m_modsTemplatesFolder.Values.SelectMany(x => Directory.GetFiles(x.rootFolder, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(y => (y, x.id, $"{x.name}: {y[x.rootFolder.Length..]}"))))
                    .ToArray();
            var errorsList = new Dictionary<string, LocalizedString>();
            for (int i = 0; i < files.Length; i++)
            {
                yield return LoadPrefabFileTemplate(offsetPercentage, totalStepPrefabTemplates, files, errorsList, i);
            }
            if (errorsList.Count > 0)
            {
                NotificationHelper.NotifyWithCallback(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Colossal.PSI.Common.ProgressState.Warning, () =>
                {
                    var dialog2 = new MessageDialog(
                        LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID)),
                        LocalizedString.Id("K45::WE.TEMPLATE_MANAGER[errorDialogHeader]"),
                        LocalizedString.Value(string.Join("\n", errorsList.Select(x => $"{x.Key}: {x.Value.Translate()}"))),
                        true,
                        LocalizedString.Id("Common.OK"),
                        LocalizedString.Id(BasicIMod.ModData.FixLocaleId(BasicIMod.ModData.GetOptionLabelLocaleID(nameof(BasicModData.GoToLogFolder))))
                        );
                    GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog2, (x) =>
                    {
                        switch (x)
                        {
                            case 2:
                                BasicIMod.ModData.GoToLogFolder = true;
                                break;
                        }
                        NotificationHelper.RemoveNotification(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID);
                    });
                });
            }
            var meshesToHide = MeshesHidden.Keys.ToArray();
            foreach (var key in meshesToHide)
            {
                if (MeshesHidden[key] != 0) continue;
                foreach (var prefabIdx in PrefabNameToIndex[key.prefabName])
                {
                    switch (prefabs[(int)prefabIdx])
                    {
                        case ObjectGeometryPrefab ogp:
                            if (key.meshIdx < ogp.m_Meshes.Length)
                            {
                                MeshesHidden[key] = ogp.m_Meshes[key.meshIdx].m_RequireState;
                                ogp.m_Meshes[key.meshIdx].m_RequireState = ObjectState.Outline;
                            }

                            prefabsToUpdate.Add(ogp);
                            break;
                    }
                }
            }

            foreach (var prefab in prefabsToUpdate)
            {
                m_prefabSystem.UpdatePrefab(prefab, PrefabUpdateSource);
            }

            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + totalStepPrefabTemplates), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingComplete");
            new WEAddAndEnableComponentJob<WETemplateForPrefabDirty>
            {
                m_componentLkp = GetComponentLookup<WETemplateForPrefabDirty>(true),
                m_EntityType = GetEntityTypeHandle(),
                m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            }.ScheduleParallel(m_prefabsToMarkDirty, Dependency).Complete();
        }


        private IEnumerator LoadPrefabFileTemplate(int offsetPercentage, float totalStep, (string, string, string)[] files, Dictionary<string, LocalizedString> errorsList, int i)
        {
            var fileItemFull = files[i];
            var fileItem = fileItemFull.Item1;
            var modId = fileItemFull.Item2;
            var displayName = fileItemFull.Item3;
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.11f + (.89f * ((i + 1f) / files.Length))) * totalStep)),
                    textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingPrefabLayoutFile", argsText: new()
                    {
                        ["fileName"] = LocalizedString.Value(displayName),
                        ["progress"] = LocalizedString.Value($"{i}/{files.Length}")
                    });
            yield return 0;
            var prefabName = Path.GetFileName(fileItem)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];
            if (!PrefabNameToIndex.TryGetValue(prefabName, out var idxArray))
            {
                if (modId is null)
                {
                    LogUtils.DoInfoLog($"No prefab loaded with name: {prefabName}, from custom folder: {fileItem.Replace(SAVED_PREFABS_FOLDER, "")}. This is harmless. Skipping...");
                    errorsList.Add(displayName, new LocalizedString("K45::WE.TEMPLATE_MANAGER[invalidPrefabName]", null, new Dictionary<string, ILocElement>()
                    {
                        ["fileName"] = LocalizedString.Value(displayName.Replace("\\", "\\\\")),
                        ["prefabName"] = LocalizedString.Value(prefabName)
                    }));
                }
                else
                {

                    if (BasicIMod.DebugMode) LogUtils.DoLog($"No prefab loaded with name: {prefabName}, but it's from a mod. This is harmless. Skipping...");
                }
                yield break;
            }
            var tree = WETextDataXmlTree.FromXML(File.ReadAllText(fileItem));
            if (tree is null) yield break;
            tree.self = new WETextDataXml { };

            var validationResults = CanBePrefabLayout(tree);
            if (validationResults == 0)
            {
                if (modId is string effectiveModId)
                {
                    ExtractReplaceableContent(tree, effectiveModId);
                }
                foreach (var idx in idxArray)
                {
                    if (PrefabTemplates.ContainsKey(idx))
                    {
                        PrefabTemplates[idx].MergeChildren(tree);
                    }
                    else
                    {
                        PrefabTemplates[idx] = tree;
                    }
                }
                if (tree.MeshesToHide?.Length > 0)
                {
                    foreach (var x in tree.MeshesToHide)
                    {
                        if (!MeshesHidden.ContainsKey((prefabName, x)))
                        {
                            MeshesHidden[(prefabName, x)] = default;
                        }
                    }
                }
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded template for prefab: //{prefabName}// => {tree} from {fileItem[SAVED_PREFABS_FOLDER.Length..]}");
            }
            else if (modId != null)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"An integration mod have a broken template for prefab '{prefabName}' (File at: '{fileItem}'). Contact the developer of the corrupted file to get assistance ({validationResults})");
            }
            else
            {
                if (fileItem.StartsWith(SAVED_PREFABS_FOLDER))
                {
                    errorsList.Add(displayName, new LocalizedString("K45::WE.TEMPLATE_MANAGER[invalidFileContent]", null, new Dictionary<string, ILocElement>()
                    {
                        ["fileName"] = LocalizedString.Value(displayName),
                        ["prefabName"] = LocalizedString.Value(prefabName)
                    }));
                    LogUtils.DoWarnLog($"Failed loding default template for prefab '{prefabName}', from custom folder: {fileItem.Replace(SAVED_PREFABS_FOLDER, "")}. Check previous lines at mod log to more information ({validationResults})");
                }
                else
                {
                    LogUtils.DoWarnLog($"Failed loding default template for prefab '{prefabName}', from a mod located at: {fileItem}. Check previous lines at mod log to more information and contact author from that mod for support ({validationResults})");
                }
            }
        }

        private void ExtractReplaceableContent(WETextDataXmlTree tree, string effectiveModId)
        {
            if (!m_atlasesMapped.TryGetValue(effectiveModId, out var dictAtlases))
            {
                dictAtlases = m_atlasesMapped[effectiveModId] = new();
            }
            if (!m_fontsMapped.TryGetValue(effectiveModId, out var dictFonts))
            {
                dictFonts = m_fontsMapped[effectiveModId] = new();
            }
            if (!m_subtemplatesMapped.TryGetValue(effectiveModId, out var dictSubTemplates))
            {
                dictSubTemplates = m_subtemplatesMapped[effectiveModId] = new();
            }
            if (!m_meshesMapped.TryGetValue(effectiveModId, out var dictMeshes))
            {
                dictMeshes = m_meshesMapped[effectiveModId] = new();
            }
            tree.MapFontAtlasesTemplates(effectiveModId, dictAtlases, dictFonts, dictSubTemplates, dictMeshes);
            tree.ModSource = effectiveModId.TrimToNull();
        }
        #endregion
    }
}
