using Belzont.Interfaces;
using Belzont.Utils;
using System;
using System.IO;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WELayoutController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "layouts.";
        private WETemplateManager m_templateManager;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}exportComponentAsXml", ExportComponentAsXml);
            callBinder($"{PREFIX}loadAsChildFromXml", LoadAsChildFromXml);
            callBinder($"{PREFIX}saveAsCityTemplate", SaveAsCityTemplate);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
        }

        protected override void OnUpdate() { }
        private void SaveAsCityTemplate(Entity e, string name)
        {
            var templateEntity = WELayoutUtility.DoCloneTextItemReferenceSelf(e, default, EntityManager);
            if (!EntityManager.HasComponent<WETemplateData>(templateEntity)) EntityManager.AddComponent<WETemplateData>(templateEntity);
            m_templateManager[name] = templateEntity;
        }

        private string ExportComponentAsXml(Entity e, string name)
        {
            KFileUtils.EnsureFolderCreation(WETemplateManager.SAVED_PREFABS_FOLDER);
            var targetFilename = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{name}.{WETemplateManager.SIMPLE_LAYOUT_EXTENSION}");
            if (File.Exists(targetFilename))
            {
                for (int i = 1; File.Exists(targetFilename); i++)
                {
                    targetFilename = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{name}_{i}.{WETemplateManager.SIMPLE_LAYOUT_EXTENSION}");
                }
            }
            File.WriteAllText(targetFilename, WETextDataTree.FromEntity(e, EntityManager).ToXML());
            return targetFilename;
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


    }
}