using Belzont.Interfaces;
using Colossal.Entities;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    // Creates and manages one HDRP point light per WE text entity that emits light
    // (Shader == Default && EmissiveIntensityEffective > 0).
    // Mirrors the pattern used by CS2's LightCullingSystem, but uses HDAdditionalLightData
    // GameObjects instead of the internal HDRPDotsInputs DOTS buffer (which is inaccessible
    // from mod code without reflection).
    public partial class WEEmissiveLightSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.EndFrame;
        // Scale factor: 1 material emissive unit -> LumensPerUnit lumens.
        // Tune this constant if the emitted light appears too dim or too bright in-game.
        private const float LumensPerUnit = 25f;

        private WEPreCullingSystem m_wePreCullSys;

        private struct LightEntry
        {
            public GameObject go;
            public Light light;
            public HDAdditionalLightData hdLight;
            public WESimulationTextType textType;
        }

        private readonly Dictionary<Entity, LightEntry> m_lights = new();
        private readonly HashSet<Entity> m_seenThisFrame = new();
        private readonly List<Entity> m_orphaned = new();

        protected override void OnCreateWithBarrier()
        {
            m_wePreCullSys = World.GetExistingSystemManaged<WEPreCullingSystem>();
        }

        protected override void OnDestroy()
        {
            foreach (var entry in m_lights.Values)
                GameObject.Destroy(entry.go);
            m_lights.Clear();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!m_wePreCullSys.m_availToDraw.IsCreated) return;

            m_seenThisFrame.Clear();
            int count = m_wePreCullSys.m_availToDraw.Length;

            for (int j = 0; j < count; j++)
            {
                var item = m_wePreCullSys.m_availToDraw[j];

                if (!EntityManager.TryGetComponent(item.textDataEntity, out WETextDataMaterial materialData)
                    || !EntityManager.TryGetComponent(item.textDataEntity, out WETextDataMesh mesh))
                    continue;

                // MatrixTransform nodes are never rendered; skip them.
                if (mesh.TextType == WESimulationTextType.MatrixTransform || mesh.TextType == WESimulationTextType.Archetype || mesh.TextType == WESimulationTextType.Placeholder || mesh.TextType == WESimulationTextType.GameProp)
                    continue;

                // Only Default shader with positive emissive intensity and UseGlobalLight enabled emits scene light.
                if (materialData.Shader != WEShader.Default || materialData.EmissiveIntensityEffective <= 0f || !materialData.UseGlobalLight)
                {
                    if (m_lights.TryGetValue(item.textDataEntity, out var existing))
                        existing.go.SetActive(false);
                    continue;
                }

                m_seenThisFrame.Add(item.textDataEntity);

                // Rebuild entry when the entity is new or its text type changed (BoxSpot ↔ RectangleArea).
                if (!m_lights.TryGetValue(item.textDataEntity, out var entry) || entry.textType != mesh.TextType)
                {
                    if (m_lights.TryGetValue(item.textDataEntity, out var stale))
                        GameObject.Destroy(stale.go);
                    entry = CreateLightEntry(item.textDataEntity, mesh.TextType);
                }

                // Update world position and rotation from the pre-culled transform matrix.
                var col = item.transformMatrix.GetColumn(3);
                entry.go.transform.SetPositionAndRotation(
                    new Vector3(col.x, col.y, col.z),
                    item.transformMatrix.rotation);

                // Compute world-space dimensions: mesh bounds × matrix scale.
                // WhiteCube stores a flat default in Bounds.z; use known unit-cube extents instead.
                var matScale = new Vector3(
                    item.transformMatrix.GetColumn(0).magnitude,
                    item.transformMatrix.GetColumn(1).magnitude,
                    item.transformMatrix.GetColumn(2).magnitude);
                bool isWhiteCube = mesh.TextType == WESimulationTextType.WhiteCube;
                float width = (mesh.Bounds.max.x - mesh.Bounds.min.x) * matScale.x;
                float height = (mesh.Bounds.max.y - mesh.Bounds.min.y) * matScale.y;
                float depth = (mesh.Bounds.max.z - mesh.Bounds.min.z) * matScale.z;

                entry.hdLight.shapeWidth = math.abs(width);
                entry.hdLight.shapeHeight = math.abs(height);                

                // Sync color – use emissive color so the light tint matches the surface glow.
                var emissiveColor = materialData.EmissiveColorEffective;
                entry.light.color = new Color(emissiveColor.r, emissiveColor.g, emissiveColor.b, 1f);

                // Sync intensity (Lumen = total luminous flux, most intuitive for a glowing surface).
                float lumens = materialData.EmissiveIntensityEffective * LumensPerUnit;
                entry.hdLight.intensity = lumens;

                // BoxSpot depth comes from the mesh Z extent; RectangleArea uses a brightness heuristic.
                if (isWhiteCube)
                    entry.light.range = Mathf.Max(depth, 0.01f);
                else
                    entry.light.range = new Vector2(width, height).magnitude * 3f;

                if (!entry.go.activeSelf) entry.go.SetActive(true);
            }

            // Deactivate lights whose entities are no longer in the visible set.
            foreach (var kv in m_lights)
            {
                if (!m_seenThisFrame.Contains(kv.Key))
                    kv.Value.go.SetActive(false);
            }

            // Destroy light GameObjects for entities that no longer exist in the world.
            m_orphaned.Clear();
            foreach (var kv in m_lights)
            {
                if (!EntityManager.Exists(kv.Key))
                {
                    GameObject.Destroy(kv.Value.go);
                    m_orphaned.Add(kv.Key);
                }
            }
            foreach (var entity in m_orphaned)
                m_lights.Remove(entity);
        }

        private LightEntry CreateLightEntry(Entity entity, WESimulationTextType textType)
        {
            var go = new GameObject($"[WELight]_{entity.Index}");

            // AddHDLight adds both the Unity Light component and HDAdditionalLightData.
            // BoxSpot matches WhiteCube geometry; RectangleArea matches all flat/text meshes.
            var lightShape = textType == WESimulationTextType.WhiteCube
                ? HDLightTypeAndShape.BoxSpot
                : HDLightTypeAndShape.RectangleArea;
            var hdLight = go.AddHDLight(lightShape);

            // Mirror the defaults CS2 uses in LightCullingSystem for all dynamic lights:
            hdLight.lightlayersMask = LightLayerEnum.Everything;
            hdLight.includeForRayTracing = false;
            hdLight.affectDiffuse = true;
            hdLight.affectSpecular = true;
            hdLight.applyRangeAttenuation = true;

            // BoxSpot only accepts Lux; RectangleArea accepts Lumen.
            hdLight.lightUnit = lightShape == HDLightTypeAndShape.BoxSpot ? LightUnit.Lux : LightUnit.Lumen;

            var entry = new LightEntry
            {
                go = go,
                light = go.GetComponent<Light>(),
                hdLight = hdLight,
                textType = textType,
            };
            m_lights[entity] = entry;
            return entry;
        }
    }
}
