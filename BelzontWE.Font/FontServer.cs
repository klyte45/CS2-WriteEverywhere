
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Kwytto.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    public partial class FontServer : GameSystemBase
    {
        public const string defaultShaderName = "BH/SG_DefaultShader";
        public static string FOLDER_PATH => BasicIMod.ModSettingsRootFolder;
        #region Fonts
        public const string DEFAULT_FONT_KEY = "/DEFAULT/";
        public const string FONTS_FILES_FOLDER = "Fonts";
        public static int DefaultTextureSizeFont => 512 << (WEModData.InstanceWE?.StartTextureSizeFont ?? 1);
        public static string FontFilesPath { get; } = FOLDER_PATH + Path.DirectorySeparatorChar + FONTS_FILES_FOLDER;
        public event Action OnFontsLoadedChanged;

        public static int QualitySize
        {
            get => qualitySize; set
            {
                qualitySize = value;
                Instance?.OnChangeSizeParam();
            }
        }
        public FontSystemData DefaultFont { get; private set; }
        private EntityQuery m_fontEntitiesQuery;
        private bool requiresUpdateParameter;
        private static int qualitySize = 100;

        public static FontServer Instance { get; private set; }
        public static int DecalLayerMask { get; private set; }
        public Dictionary<string, Entity> LoadedFonts { get; } = new();

        private EndFrameBarrier m_endFrameBarrier;

        public Entity this[string name] => LoadedFonts.TryGetValue(name, out var e) ? e : Entity.Null;

        public Entity GetOrCreateFontAsDefault(string name)
        {
            if (LoadedFonts.TryGetValue(name, out var e)) return e;
            var fontEntity = EntityManager.CreateEntity();
            var defaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Font.Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY);
            EntityManager.AddComponent<Created>(fontEntity);
            EntityManager.AddComponentData(fontEntity, defaultFont);
            return fontEntity;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
            m_fontEntitiesQuery = GetEntityQuery(new EntityQueryDesc[]
           {
                new() {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<FontSystemData>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
           });
            DefaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Font.Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY, true);
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        }

        #endregion

        public Vector2 ScaleEffective => Vector2.one / QualitySize * 2250;

        public void OnChangeSizeParam()
        {
            requiresUpdateParameter = true;
        }

        public bool RegisterFont(string name, byte[] fontData)
        {
            try
            {
                var fontSystemData = FontSystemData.From(fontData, name);
                if (name == null)
                {
                    LogUtils.DoErrorLog($"RegisterFont: FONT NAME CANNOT BE NULL!!");
                    return false;
                }
                if (LoadedFonts.TryGetValue(name, out var e))
                {
                    EntityManager.SetComponentData(e, fontSystemData);
                }
                else
                {
                    var fontEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponent<Created>(fontEntity);
                    EntityManager.AddComponentData(fontEntity, fontSystemData);
                }
            }
            catch (FontCreationException)
            {
                LogUtils.DoErrorLog($"RegisterFont: Error creating the font \"{name}\"... Invalid data!");
                return false;
            }
            return true;
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isLoading) return;
            EntityCommandBuffer cmd = default;
            if (!m_fontEntitiesQuery.IsEmpty)
            {
                var entities = m_fontEntitiesQuery.ToEntityArray(Allocator.Temp);
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (EntityManager.TryGetComponent(entity, out FontSystemData data))
                    {
                        if (LoadedFonts.TryGetValue(data.Name, out var otherEntity))
                        {
                            if (otherEntity != entity)
                            {
                                if (!EntityManager.TryGetComponent(otherEntity, out FontSystemData otherData) || otherData.IsWeak)
                                {
                                    LoadedFonts[data.Name] = entity;
                                    if (!cmd.IsCreated) cmd = m_endFrameBarrier.CreateCommandBuffer();
                                    cmd.DestroyEntity(otherEntity);
                                }
                                else
                                {
                                    if (!cmd.IsCreated) cmd = m_endFrameBarrier.CreateCommandBuffer();
                                    cmd.DestroyEntity(entity);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            LoadedFonts[data.Name] = entity;
                        }
                        UpdateFontSystem(data);
                    }
                }
            }
            UpdateFontSystem(DefaultFont);
            requiresUpdateParameter = false;
        }

        private void UpdateFontSystem(FontSystemData data)
        {
            try
            {
                if (requiresUpdateParameter)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Resetting font system!");
                    data.FontSystem.Reset();
                }
                data.FontSystem.RunJobs();
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog($"Error on UpdateFontSystem for {data.Name}: {e}");
            }
        }

        public static Material CreateDefaultFontMaterial()
        {
            return Instance.CreateDefaultFontMaterial_Impl();
        }
        private Material CreateDefaultFontMaterial_Impl()
        {
            var material = new Material(Shader.Find(defaultShaderName));
            material.EnableKeyword("_GPU_ANIMATION_OFF");
            HDMaterial.SetAlphaClipping(material, true);
            HDMaterial.SetAlphaCutoff(material, .7f);
            HDMaterial.SetUseEmissiveIntensity(material, true);
            HDMaterial.SetEmissiveColor(material, UnityEngine.Color.white);
            HDMaterial.SetEmissiveIntensity(material, 0, UnityEditor.Rendering.HighDefinition.EmissiveIntensityUnit.Nits);
            material.SetFloat("_DoubleSidedEnable", 1);
            material.SetVector("_DoubleSidedConstants", new Vector4(1, 1, -1, 0));
            material.SetFloat("_Smoothness", .5f);
            material.SetFloat("_ZTestGBuffer", 7);
            material.SetFloat(FontServer.DecalLayerMask, 8.ToFloatBitFlags());
            material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
            HDMaterial.ValidateMaterial(material);
            return material;
        }


    }
}
