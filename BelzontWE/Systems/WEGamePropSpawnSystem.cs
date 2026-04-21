using Belzont.Interfaces;
using Belzont.Utils;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public partial class WEGamePropSpawnSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.ModificationEnd;

        private const int MAX_SPAWNS_PER_FRAME = 10;
        private const int MAX_SPAWN_DEPTH = 4;

        private EntityQuery m_gamePropTextNodes;
        private WETemplateManager m_templateManager;

        protected override void OnCreateWithBarrier()
        {
            m_gamePropTextNodes = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<WETextDataMesh>(),
                    ComponentType.ReadOnly<WETextDataMain>(),
                    ComponentType.ReadOnly<WETextComponentValid>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                }
            });
            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
        }

        protected override void OnUpdate()
        {
            if (m_templateManager?.WEGamePropIndex == null) return;
            if (m_gamePropTextNodes.IsEmpty) return;

            var cmd = Barrier.CreateCommandBuffer();
            var ownerLkp = GetComponentLookup<WEOwner>(true);
            var mainLkp = GetComponentLookup<WETextDataMain>(true);
            var subObjectLkp = GetBufferLookup<WESubObject>(false);
            var transformLkp = GetComponentLookup<Game.Objects.Transform>(true);
            var weTransformLkp = GetComponentLookup<WETextDataTransform>(true);
            var prefabRefLkp = GetComponentLookup<PrefabRef>(true);

            var entities = m_gamePropTextNodes.ToEntityArray(Allocator.Temp);
            int spawned = 0;

            for (int i = 0; i < entities.Length && spawned < MAX_SPAWNS_PER_FRAME; i++)
            {
                var e = entities[i];
                var mesh = EntityManager.GetComponentData<WETextDataMesh>(e);
                if (mesh.TextType != WESimulationTextType.GameProp) continue;

                var currentPrefabName = mesh.ValueData.EffectiveValue.ToString();
                m_templateManager.WEGamePropIndex.TryGetValue(currentPrefabName, out var prefabEntity);
                var hasValidPrefab = !string.IsNullOrEmpty(currentPrefabName) && prefabEntity != Entity.Null;

                // Check if already spawned with correct prefab - compare WESubObject entries PrefabRef
                if (hasValidPrefab && subObjectLkp.TryGetBuffer(e, out var existingBuf) && existingBuf.Length > 0)
                {
                    bool alreadySpawned = false;
                    for (int j = 0; j < existingBuf.Length; j++)
                    {
                        var sub = existingBuf[j].m_SubObject;
                        if (EntityManager.Exists(sub)
                            && prefabRefLkp.TryGetComponent(sub, out var subPrefabRef)
                            && subPrefabRef.m_Prefab == prefabEntity)
                        {
                            alreadySpawned = true;
                            break;
                        }
                    }
                    if (alreadySpawned) continue;
                }
                else if (!hasValidPrefab && subObjectLkp.TryGetBuffer(e, out var emptyCheckBuf) && emptyCheckBuf.Length == 0)
                {
                    continue;
                }

                // Destroy existing sub-objects (value changed or clearing stale)
                if (subObjectLkp.TryGetBuffer(e, out var subObjectBuff))
                {
                    for (int j = 0; j < subObjectBuff.Length; j++)
                    {
                        var sub = subObjectBuff[j].m_SubObject;
                        if (sub != Entity.Null && EntityManager.Exists(sub))
                            cmd.AddComponent<Deleted>(sub);
                    }
                    subObjectBuff.Clear();
                }

                if (!hasValidPrefab) continue;

                var main = EntityManager.GetComponentData<WETextDataMain>(e);

                if (GetSpawnChainDepth(main.TargetEntity, ownerLkp, mainLkp) >= MAX_SPAWN_DEPTH)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[WEGamePropSpawn] Depth guard triggered for {e}, prefab={currentPrefabName}");
                    continue;
                }

                if (HasCycleInChain(main.TargetEntity, currentPrefabName, ownerLkp, mainLkp))
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[WEGamePropSpawn] Cycle guard triggered for {e}, prefab={currentPrefabName}");
                    continue;
                }

                SpawnProp(e, prefabEntity, main.TargetEntity, weTransformLkp, transformLkp, cmd);
                spawned++;
            }

            entities.Dispose();
        }

        private void SpawnProp(
            Entity textNode,
            Entity prefabEntity,
            Entity geometryEntity,
            ComponentLookup<WETextDataTransform> weTransformLkp,
            ComponentLookup<Game.Objects.Transform> transformLkp,
            EntityCommandBuffer cmd)
        {
            var spawnedEntity = cmd.Instantiate(prefabEntity);

            if (transformLkp.TryGetComponent(geometryEntity, out var geomTransform)
                && weTransformLkp.TryGetComponent(textNode, out var weTransform))
            {
                var geomMatrix = float4x4.TRS(geomTransform.m_Position, geomTransform.m_Rotation, new float3(1f));
                var localPos = weTransform.offsetPosition;
                var localRot = weTransform.offsetRotation;
                var worldPos = math.transform(geomMatrix, localPos);
                var worldRot = math.mul(geomTransform.m_Rotation, localRot);
                cmd.SetComponent(spawnedEntity, new Game.Objects.Transform(worldPos, worldRot));
            }

            cmd.AddComponent(spawnedEntity, new WEOwner { m_weOwnerEntity = textNode });
            cmd.AddComponent(spawnedEntity, new WEChild());
            cmd.AddComponent(spawnedEntity, new WEInheritedVarsCache());
            cmd.SetComponentEnabled<WEInheritedVarsCache>(spawnedEntity, false);
            cmd.AddComponent<Secondary>(spawnedEntity);
            cmd.AppendToBuffer(textNode, new WESubObject { m_SubObject = spawnedEntity });
        }

        private static int GetSpawnChainDepth(
            Entity geometryEntity,
            ComponentLookup<WEOwner> ownerLkp,
            ComponentLookup<WETextDataMain> mainLkp)
        {
            int depth = 0;
            var e = geometryEntity;
            while (ownerLkp.TryGetComponent(e, out var owner))
            {
                depth++;
                if (depth >= MAX_SPAWN_DEPTH) return depth;
                if (!mainLkp.TryGetComponent(owner.m_weOwnerEntity, out var main)) break;
                e = main.TargetEntity;
            }
            return depth;
        }

        private static bool HasCycleInChain(
            Entity geometryEntity,
            string prefabName,
            ComponentLookup<WEOwner> ownerLkp,
            ComponentLookup<WETextDataMain> mainLkp)
        {
            var e = geometryEntity;
            while (ownerLkp.TryGetComponent(e, out var owner))
            {
                if (!mainLkp.TryGetComponent(owner.m_weOwnerEntity, out var main)) break;
                e = main.TargetEntity;
                if (e == Entity.Null) break;
            }
            return false;
        }
    }
}