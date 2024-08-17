using Belzont.Interfaces;
using Belzont.Utils;
using Colossal;
using Colossal.Entities;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WELayoutController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "layouts.";
        private WETemplateManager m_templateManager;
        private PrefabSystem m_prefabSystem;
        private WEWorldPickerController m_controller;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}exportComponentAsXml", ExportComponentAsXml);
            callBinder($"{PREFIX}loadAsChildFromXml", LoadAsChildFromXml);
            callBinder($"{PREFIX}saveAsCityTemplate", SaveAsCityTemplate);
            callBinder($"{PREFIX}exportComponentAsPrefabDefault", ExportComponentAsPrefabDefault);
            callBinder($"{PREFIX}checkCityTemplateExists", CheckCityTemplateExists);
            callBinder($"{PREFIX}listCityTemplates", ListCityTemplates);
            callBinder($"{PREFIX}getCityTemplateDetail", GetCityTemplateDetail);
            callBinder($"{PREFIX}renameCityTemplate", RenameCityTemplate);
            callBinder($"{PREFIX}deleteCityTemplate", DeleteCityTemplate);
            callBinder($"{PREFIX}duplicateCityTemplate", DuplicateCityTemplate);
            callBinder($"{PREFIX}exportCityLayoutAsXml", ExportCityLayoutAsXml);
            callBinder($"{PREFIX}openExportedFilesFolder", OpenExportedFilesFolder);
            callBinder($"{PREFIX}loadAsChildFromCityTemplate", LoadAsChildFromCityTemplate);
            callBinder($"{PREFIX}importAsCityTemplateFromXml", ImportAsCityTemplateFromXml);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            m_prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_controller = World.GetOrCreateSystemManaged<WEWorldPickerController>();
        }

        protected override void OnUpdate() { }
        private bool SaveAsCityTemplate(Entity e, string name) => m_templateManager.SaveCityTemplate(name, e);
        private bool CheckCityTemplateExists(string name) => m_templateManager.CityTemplateExists(name);

        private string ExportComponentAsXml(Entity e, string name)
        {
            var dataTree = WETextDataTree.FromEntity(e, EntityManager);

            return CommonExportAsXml(name, dataTree);
        }

        private static string CommonExportAsXml(string name, WETextDataTree dataTree)
        {
            KFileUtils.EnsureFolderCreation(WETemplateManager.SAVED_PREFABS_FOLDER);
            var effectiveFileName = name;
            var targetFilename = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{effectiveFileName}.{WETemplateManager.SIMPLE_LAYOUT_EXTENSION}");
            if (File.Exists(targetFilename))
            {
                for (int i = 1; File.Exists(targetFilename); i++)
                {
                    effectiveFileName = $"{name}_{i}";
                    targetFilename = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{effectiveFileName}.{WETemplateManager.SIMPLE_LAYOUT_EXTENSION}");
                }
            }
            File.WriteAllText(targetFilename, dataTree.ToXML());
            return effectiveFileName;
        }

        private bool LoadAsChildFromXml(Entity parent, string targetFilename)
        {
            if (!File.Exists(targetFilename)) return false;

            var tree = WETextDataTree.FromXML(File.ReadAllText(targetFilename));
            if (tree is null) return false;
            if (!EntityManager.TryGetComponent<WETextData>(parent, out var weData)) return false;
            WELayoutUtility.DoCreateLayoutItem(WETextDataTreeStruct.FromXml(tree), parent, weData.TargetEntity, EntityManager);
            m_controller.UpdateTree();
            return true;
        }
        private bool LoadAsChildFromCityTemplate(Entity parent, string templateName)
        {
            if (!m_templateManager.CityTemplateExists(templateName)) return false;
            if (!EntityManager.TryGetComponent<WETextData>(parent, out var weData)) return false;
            var layout = m_templateManager[templateName];
            var e = WELayoutUtility.DoCreateLayoutItem(layout, parent, weData.TargetEntity, EntityManager);
            LogUtils.DoLog($"Added entity {e} with layout {layout.Guid} as child of {parent} pointing to target {weData.TargetEntity} | {EntityManager.HasComponent<WETextData>(e)}");
            m_controller.UpdateTree();
            return true;
        }
        private string ImportAsCityTemplateFromXml(string path)
        {
            if (!File.Exists(path)) return null;
            var name = Regex.Replace(Path.GetFileNameWithoutExtension(path.Replace(WETemplateManager.SIMPLE_LAYOUT_EXTENSION, "xml")), "[^A-Za-z0-9_]", "_").Truncate(30);
            if (m_templateManager.CityTemplateExists(name))
            {
                var i = 1;
                var baseName = name;
                do
                {
                    baseName = baseName.Truncate(29 - i.ToString().Length);
                    name = $"{baseName}_{i}";
                } while (m_templateManager.CityTemplateExists(name));
            }
            try
            {
                return m_templateManager.SaveCityTemplate(name, WETextDataTree.FromXML(File.ReadAllText(path)).ToStruct()) ? name : null;
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog($"Exception importing layout: {e}");
                return null;
            }
        }

        private int ExportComponentAsPrefabDefault(Entity e, bool force = false)
        {
            var xml = WESelflessTextDataTree.FromEntity(e, EntityManager);
            var validationResults = m_templateManager.CanBePrefabLayout(xml.ToStruct());
            if (validationResults != 0)
            {
                return -1000 - validationResults;
            }
            var effTarget = WETextData.GetTargetEntityEffective(e, EntityManager, true);
            KFileUtils.EnsureFolderCreation(WETemplateManager.SAVED_PREFABS_FOLDER);
            if (!EntityManager.TryGetComponent(effTarget, out PrefabRef prefabRef))
            {
                return -1;
            }
            var name = m_prefabSystem.GetPrefabName(prefabRef.m_Prefab);
            var targetFilename = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{name}.{WETemplateManager.PREFAB_LAYOUT_EXTENSION}");
            if (File.Exists(targetFilename))
            {
                if (force)
                {
                    try
                    {
                        File.Delete(targetFilename);
                    }
                    catch
                    {
                        return -2;
                    }
                }
                else
                {
                    return 0;
                }
            }
            File.WriteAllText(targetFilename, xml.ToXML());
            m_templateManager.MarkPrefabsDirty();
            return 1;
        }

        private Dictionary<string, string> ListCityTemplates() => m_templateManager.ListCityTemplates();

        private CityDetailResponse GetCityTemplateDetail(string name)
            => name == null || !m_templateManager.CityTemplateExists(name)
                ? null
                : new CityDetailResponse
                {
                    name = name,
                    usages = m_templateManager.GetCityTemplateUsageCount(name)
                };

        private void RenameCityTemplate(string oldName, string newName) => m_templateManager.RenameCityTemplate(oldName, newName);

        private void DeleteCityTemplate(string name) => m_templateManager.DeleteCityTemplate(name);

        private void DuplicateCityTemplate(string srcName, string newName) => m_templateManager.DuplicateCityTemplate(srcName, newName);

        private string ExportCityLayoutAsXml(string layoutName, string saveName)
                    => layoutName.TrimToNull() == null || !m_templateManager.CityTemplateExists(layoutName)
                        ? null
                        : CommonExportAsXml(saveName, m_templateManager[layoutName].ToXml());

        private void OpenExportedFilesFolder() => RemoteProcess.OpenFolder(WETemplateManager.SAVED_PREFABS_FOLDER);

        private class CityDetailResponse
        {
            public string name;
            public int usages;
        }
    }
}