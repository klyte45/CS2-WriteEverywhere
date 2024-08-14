using Unity.Entities;

namespace BelzontWE
{
    public struct WETemplateForPrefab : IComponentData, ICleanupComponentData
    {
        public static int CURRENT_VERSION = 0;

        public Entity templateRef;
        public Entity childEntity;
    }
    public struct WETemplateForPrefabDirty : ICleanupComponentData, IQueryTypeParameter { }
    public struct WETemplateForPrefabEmpty : ICleanupComponentData, IQueryTypeParameter { }
}