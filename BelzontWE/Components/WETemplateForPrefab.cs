using Unity.Entities;

namespace BelzontWE
{
    public struct WETemplateForPrefab : IComponentData, ICleanupComponentData
    {
        public static int CURRENT_VERSION = 0;

        public Colossal.Hash128 templateRef;
        public Entity childEntity;
    }
    public struct WETemplateForPrefabDirty : IComponentData, IQueryTypeParameter, IEnableableComponent  { }
    public struct WETemplateDirtyInstancing : IComponentData, IQueryTypeParameter, IEnableableComponent { }
    public struct WETemplateForPrefabEmpty : IComponentData, IQueryTypeParameter { }
    public struct WETemplateForPrefabToRunOnMain : IComponentData, IQueryTypeParameter, IEnableableComponent { }
}