using Unity.Entities;

namespace BelzontWE
{
    public struct WETemplateForPrefab : IComponentData, ICleanupComponentData
    {
        public static int CURRENT_VERSION = 0;

        public Colossal.Hash128 templateRef;
        public Entity childEntity;
    }
    public struct WETemplateForPrefabDirty : IComponentData, IQueryTypeParameter { }
    public struct WETemplateDirtyInstancing : IComponentData, IQueryTypeParameter { }
    public struct WETemplateForPrefabEmpty : IComponentData, IQueryTypeParameter { }
    public struct WETemplateForPrefabToRunOnMain : IComponentData, IQueryTypeParameter { }
}