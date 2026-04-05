using Belzont.Interfaces;
using Colossal.Entities;
using System.Collections.Generic;
using Unity.Entities;
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
                if (mesh.TextType == WESimulationTextType.MatrixTransform || mesh.TextType == WESimulationTextType.Archetype || mesh.TextType == WESimulationTextType.Placeholder)
                    continue;
                    
                // Only Default shader with positive emissive intensity and UseGlobalLight enabled emits scene light.
                if (materialData.Shader != WEShader.Default || materialData.EmissiveIntensityEffective <= 0f || !materialData.UseGlobalLight)
                {
                    if (m_lights.TryGetValue(item.textDataEntity, out var existing))
                        existing.go.SetActive(false);
                    continue;
                }

                m_seenThisFrame.Add(item.textDataEntity);

                if (!m_lights.TryGetValue(item.textDataEntity, out var entry))
                    entry = CreateLightEntry(item.textDataEntity);

                // Update world position from the pre-culled transform matrix.
                // Column 3 of a Matrix4x4 is the translation component.
                var col = item.transformMatrix.GetColumn(3);
                entry.go.transform.position = new Vector3(col.x, col.y, col.z);

                // Sync color – use emissive color so the light tint matches the surface glow.
                var emissiveColor = materialData.EmissiveColorEffective;
                entry.light.color = new Color(emissiveColor.r, emissiveColor.g, emissiveColor.b, 1f);

                // Sync intensity (Lumen = total luminous flux, most intuitive for a glowing surface).
                float lumens = materialData.EmissiveIntensityEffective * LumensPerUnit;
                entry.hdLight.intensity = lumens;

                // Range heuristic: brighter light reaches farther, capped at 100 m.
                entry.light.range = Mathf.Clamp(Mathf.Sqrt(lumens) * 1.5f, 2f, 100f);

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

        private LightEntry CreateLightEntry(Entity entity)
        {
            var go = new GameObject($"[WELight]_{entity.Index}");

            // AddHDLight adds both the Unity Light component and HDAdditionalLightData.
            var hdLight = go.AddHDLight(HDLightTypeAndShape.RectangleArea);

            // Mirror the defaults CS2 uses in LightCullingSystem for all dynamic lights:
            hdLight.lightlayersMask = LightLayerEnum.Everything;
            hdLight.includeForRayTracing = false;
            hdLight.affectDiffuse = true;
            hdLight.affectSpecular = true;
            hdLight.applyRangeAttenuation = true;

            // Set unit once; only intensity is updated per frame afterwards.
            hdLight.lightUnit = LightUnit.Lumen;

            var entry = new LightEntry
            {
                go = go,
                light = go.GetComponent<Light>(),
                hdLight = hdLight,
            };
            m_lights[entity] = entry;
            return entry;
        }
    }
}
