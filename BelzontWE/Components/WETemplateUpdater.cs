using Unity.Entities;

namespace BelzontWE
{
    public struct WETemplateUpdater : IComponentData, ICleanupComponentData
    {
        public static int CURRENT_VERSION = 0;

        public Entity templateEntity;
        public Entity childEntity;
    }
}