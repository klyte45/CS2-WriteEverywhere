using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public struct WETextDataTransform : IComponentData
    {
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        public bool useAbsoluteSizeEditing;
        public static WETextDataTransform CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                offsetPosition = new(0, 0, 0),
                offsetRotation = new(),
                scale = new(1, 1, 1),
            };
    }
}