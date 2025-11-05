using Belzont.Interfaces;
using Belzont.Utils;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        #region Mod SubTemplates

        private Coroutine reloadingSubtemplatesCoroutine;
        public void ReloadSubtemplates()
        {
            if (reloadingSubtemplatesCoroutine != null) return;
            reloadingSubtemplatesCoroutine = GameManager.instance.StartCoroutine(LoadModSubtemplates_Coroutine());
        }


        private IEnumerator LoadModSubtemplates_Coroutine()
        {
            yield return 0;
            var mods = ModsSubTemplates.Keys.ToArray();
            if (mods.Length == 0) yield break;
            var eachItemPart = 1f / mods.Length;
            for (int i = 0; i < mods.Length; i++)
            {
                string modId = mods[i];
                GameManager.instance.StartCoroutine(LoadModSubtemplates_Item(0, 100, modId, true));
            }
            m_templatesDirty = true;
            reloadingSubtemplatesCoroutine = null;
        }


        private IEnumerator LoadModSubtemplates_Item(float offsetPercentage, float totalStep, string modId, bool isStandalone = false)
        {
            var groupId = isStandalone ? $"{LOADING_SUBTEMPLATES_NOTIFICATION_ID}:{modId}" : LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID;

            NotificationHelper.NotifyProgress(groupId, Mathf.RoundToInt(offsetPercentage + (.11f * totalStep)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.searchingForFiles");
            yield return 0;
            var files = Directory.GetFiles(m_modsTemplatesFolder[modId].rootFolder, $"*.{SIMPLE_LAYOUT_EXTENSION}", SearchOption.TopDirectoryOnly)
                .Select(y => (y, $"{m_modsTemplatesFolder[modId].name}: {y[m_modsTemplatesFolder[modId].rootFolder.Length..]}"))
                .ToArray();

            if (!ModsSubTemplates.TryGetValue(modId, out var list))
            {
                ModsSubTemplates[modId] = list = new Dictionary<string, WETextDataXmlTree>();
            }
            else
            {
                list.Clear();
            }

            if (files.Length > 0)
            {
                var errorsList = new Dictionary<string, LocalizedString>();
                for (int i = 0; i < files.Length; i++)
                {
                    var fileItemFull = files[i];
                    var fileItem = fileItemFull.Item1;
                    var displayName = fileItemFull.Item2;
                    NotificationHelper.NotifyProgress(groupId, Mathf.RoundToInt(offsetPercentage + ((.11f + (.89f * ((i + 1f) / files.Length))) * totalStep)),
                            textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingLayoutFile", argsText: new()
                            {
                                ["fileName"] = LocalizedString.Value(displayName),
                                ["progress"] = LocalizedString.Value($"{i}/{files.Length}")
                            });
                    yield return 0;
                    try
                    {
                        var tree = WETextDataXmlTree.FromXML(File.ReadAllText(fileItem));
                        if (tree is null) yield break;
                        ExtractReplaceableContent(tree, modId);


                        var templateName = Path.GetFileName(fileItem)[..^(SIMPLE_LAYOUT_EXTENSION.Length + 1)];
                        list[templateName] = tree;

                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded subtemplate \"{displayName}\"");
                    }
                    catch (Exception e)
                    {
                        LogUtils.DoWarnLog($"Failed loading subtemplate \"{displayName}\": {e}");
                        yield break;
                    }

                }
                if (errorsList.Count > 0)
                {
                    NotificationHelper.NotifyWithCallback(ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID, Colossal.PSI.Common.ProgressState.Warning, () =>
                    {
                        var dialog2 = new MessageDialog(
                            LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID)),
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
                            NotificationHelper.RemoveNotification(ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID);
                        });
                    });
                }
                m_templatesDirty = true;
            }
            NotificationHelper.NotifyProgress(groupId, Mathf.RoundToInt(offsetPercentage + totalStep), textI18n: $"{LOADING_SUBTEMPLATES_NOTIFICATION_ID}.loadingComplete");

        }
        internal record struct ModSubtemplateRegistry(string ModId, string ModName, string[] Subtemplates) { }

        internal ModSubtemplateRegistry[] ListModSubtemplates() => m_modsTemplatesFolder
            .Where(x => ModsSubTemplates.ContainsKey(x.Key))
            .Select(x => new ModSubtemplateRegistry(x.Key, x.Value.name, ModsSubTemplates[x.Key].Select(y => $"{x.Key}:{y.Key}").ToArray()))
            .ToArray();

        #endregion
    }
}
