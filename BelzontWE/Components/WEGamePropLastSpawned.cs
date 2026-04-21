using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    /// <summary>
    /// Tracks the last prefab name spawned by a GameProp text node.
    /// Stored as a separate component to avoid modifying WETextDataMesh struct layout.
    /// </summary>
    public struct WEGamePropLastSpawned : IComponentData
    {
        public FixedString128Bytes LastSpawnedPrefabName;
    }
}
