using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Entities;
using System;
using System.IO;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WELayoutController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "layouts.";
        private const string PREFAB_EXTENSION = "welayout.xml";
        private readonly string SAVED_PREFABS_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "prefabs");
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
            var templateEntity = WELayoutUtility.DoCloneTextItem(e, default, EntityManager, default);
            if(!EntityManager.HasComponent<WETemplateData>(templateEntity)) EntityManager.AddComponent<WETemplateData>(templateEntity);
            m_templateManager[name] = templateEntity;
        }

        private string ExportComponentAsXml(Entity e, string name)
        {
            KFileUtils.EnsureFolderCreation(SAVED_PREFABS_FOLDER);
            var targetFilename = Path.Combine(SAVED_PREFABS_FOLDER, $"{name}.{PREFAB_EXTENSION}");
            if (File.Exists(targetFilename))
            {
                for (int i = 1; File.Exists(targetFilename); i++)
                {
                    targetFilename = Path.Combine(SAVED_PREFABS_FOLDER, $"{name}_{i}.{PREFAB_EXTENSION}");
                }
            }
            File.WriteAllText(targetFilename, WETextDataTree.FromEntity(e, EntityManager).ToXML());
            return targetFilename;
        }
        private bool LoadAsChildFromXml(Entity parent, string layoutName)
        {
            KFileUtils.EnsureFolderCreation(SAVED_PREFABS_FOLDER);
            var targetFilename = Path.Combine(SAVED_PREFABS_FOLDER, $"{layoutName}.{PREFAB_EXTENSION}");
            if (!File.Exists(targetFilename)) return false;

            var tree = WETextDataTree.FromXML(File.ReadAllText(targetFilename));
            if (tree == null) return false;

            CreateEntityFromTree(parent, tree);
            return true;
        }

        private void CreateEntityFromTree(Entity parent, WETextDataTree tree)
        {
            var selfComponent = WETextData.FromDataXml(tree.self, parent, EntityManager);
            var selfEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(selfEntity, selfComponent);
            if (!EntityManager.TryGetBuffer<WESubTextRef>(parent, true, out var subBuff)) subBuff = EntityManager.AddBuffer<WESubTextRef>(parent);
            subBuff.Add(new WESubTextRef
            {
                m_weTextData = selfEntity
            });

            for (int i = 0; i < tree.children?.Length; i++)
            {
                var child = tree.children[i];
                CreateEntityFromTree(selfEntity, child);
            }
        }
    }
}