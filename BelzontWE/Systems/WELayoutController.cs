using Belzont.Interfaces;
using Belzont.Utils;
using Colossal;
using Colossal.Entities;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WELayoutController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "layouts.";
        private WETemplateManager m_templateManager;
        private PrefabSystem m_prefabSystem;

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
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            m_prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnUpdate() { }
        private void SaveAsCityTemplate(Entity e, string name)
        {
            var templateEntity = WELayoutUtility.DoCloneTextItemReferenceSelf(e, default, EntityManager);
            if (!EntityManager.HasComponent<WETemplateData>(templateEntity)) EntityManager.AddComponent<WETemplateData>(templateEntity);
            m_templateManager[name] = templateEntity;
        }
        private bool CheckCityTemplateExists(string name) => m_templateManager.CityTemplateExists(name);

        private string ExportComponentAsXml(Entity e, string name)
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
            File.WriteAllText(targetFilename, WETextDataTree.FromEntity(e, EntityManager).ToXML());
            return effectiveFileName;
        }
        private bool LoadAsChildFromXml(Entity parent, string layoutName)
        {
            KFileUtils.EnsureFolderCreation(WETemplateManager.SAVED_PREFABS_FOLDER);
            var targetFilename = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{layoutName}.{WETemplateManager.SIMPLE_LAYOUT_EXTENSION}");
            if (!File.Exists(targetFilename)) return false;

            var tree = WETextDataTree.FromXML(File.ReadAllText(targetFilename));
            if (tree == null) return false;

            WELayoutUtility.CreateEntityFromTree(parent, tree, EntityManager);
            return true;
        }

        private int ExportComponentAsPrefabDefault(Entity e, bool force = false)
        {
            KFileUtils.EnsureFolderCreation(WETemplateManager.SAVED_PREFABS_FOLDER);
            var effTarget = WETextData.GetTargetEntityEffective(e, EntityManager, true);
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
            File.WriteAllText(targetFilename, WESelflessTextDataTree.FromEntity(e, EntityManager).ToXML());
            m_templateManager.MarkPrefabsDirty();
            return 1;
        }

        private Dictionary<string, Entity> ListCityTemplates() => m_templateManager.ListCityTemplates();

        private CityDetailResponse GetCityTemplateDetail(string name)
            => name == null || !m_templateManager.CityTemplateExists(name)
                ? null
                : new CityDetailResponse
                {
                    name = name,
                    usages = m_templateManager.GetCityTemplateUsageCount(name)
                };

        private void RenameCityTemplate(string oldName, string newName)
        {
            if (oldName == newName || oldName.TrimToNull() == null || newName.TrimToNull() == null || !m_templateManager.CityTemplateExists(oldName)) return;
            m_templateManager[newName] = m_templateManager[oldName];
            m_templateManager[oldName] = default;
        }

        private void DeleteCityTemplate(string name)
        {
            if (name != null && m_templateManager.CityTemplateExists(name))
            {
                m_templateManager[name] = Entity.Null;
            }
        }
        private void DuplicateCityTemplate(string srcName, string newName)
        {
            if (srcName == newName || srcName.TrimToNull() == null || newName.TrimToNull() == null || !m_templateManager.CityTemplateExists(srcName)) return;
            m_templateManager[newName] = m_templateManager[srcName];
        }

        private string ExportCityLayoutAsXml(string layoutName, string saveName)
            => layoutName.TrimToNull() == null || !m_templateManager.CityTemplateExists(layoutName)
                ? null
                : ExportComponentAsXml(m_templateManager[layoutName], saveName);

        private void OpenExportedFilesFolder() => RemoteProcess.OpenFolder(WETemplateManager.SAVED_PREFABS_FOLDER);

        private class CityDetailResponse
        {
            public string name;
            public int usages;
        }
    }
}