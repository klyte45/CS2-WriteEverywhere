using Unity.Entities;

namespace BelzontWE
{
    public struct WETemplateUpdater : IComponentData, ICleanupComponentData
    {
        public static int CURRENT_VERSION = 0;

        private bool templateDirty;
        public Colossal.Hash128 templateEntity;
        public Entity childEntity;

        public readonly bool IsTemplateDirty() => templateDirty;
        public void ClearTemplateDirty() => templateDirty = false;
        public void MarkTemplateDirty() => templateDirty = true;
    }
}