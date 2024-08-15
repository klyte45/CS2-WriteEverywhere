
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
using System.Linq;
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
        private Dictionary<FixedString32Bytes, Entity> LoadedFonts { get; } = new();

        private EndFrameBarrier m_endFrameBarrier;

        public Entity this[string name] => LoadedFonts.TryGetValue(name, out var e) ? e : Entity.Null;

        public Entity GetOrCreateFontAsDefault(string name)
        {
            if (name.TrimToNull() == null) return Entity.Null;
            if (LoadedFonts.TryGetValue(name, out var e)) return e;
            var fontEntity = EntityManager.CreateEntity();
            var defaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY);
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
            DefaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY, true);
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        }

        #endregion

        public Vector2 ScaleEffective => Vector2.one / QualitySize * 2.250f;

        public void OnChangeSizeParam()
        {
            requiresUpdateParameter = true;
        }

        public bool RegisterFont(string name, byte[] fontData) => RegisterFont(name, fontData, false);
        private bool RegisterFont(string name, byte[] fontData, bool isWeak)
        {
            try
            {
                var fontSystemData = FontSystemData.From(fontData, name, isWeak);
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
                OnFontsLoadedChanged?.Invoke();
            }
            catch (FontCreationException)
            {
                LogUtils.DoErrorLog($"RegisterFont: Error creating the font \"{name}\"... Invalid data!");
                return false;
            }
            return true;
        }
        public void DestroyFont(string name)
        {
            if (name != null && LoadedFonts.ContainsKey(name))
            {

                EntityManager.TryGetComponent(LoadedFonts[name], out FontSystemData fontData);
                fontData.Dispose();
                EntityManager.DestroyEntity(LoadedFonts[name]);
                LoadedFonts.Remove(name);
            }
        }
        public void RenameFont(string oldName, string newName)
        {
            if (oldName == newName || oldName.TrimToNull() == null || newName.TrimToNull() == null) return;
            if (LoadedFonts.TryGetValue(oldName, out var entity))
            {
                DestroyFont(newName);
                EntityManager.TryGetComponent(entity, out FontSystemData fontData);
                LoadedFonts.Remove(oldName);
                fontData.Name = newName;
                EntityManager.SetComponentData(entity, fontData);
                LoadedFonts[newName] = entity;
                fontData.FontSystem.Reset();
                DefaultFont.FontSystem.Reset();
            }
        }
        public void DuplicateFont(string srcFont, string newName)
        {
            if (srcFont == newName || srcFont.TrimToNull() == null || newName.TrimToNull() == null) return;
            if (LoadedFonts.TryGetValue(srcFont, out var entity))
            {
                DestroyFont(newName);
                EntityManager.TryGetComponent(entity, out FontSystemData fontData);
                var newEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(newEntity, FontSystemData.From(fontData.Font._font.data.ArrayData, newName));
                LoadedFonts[newName] = newEntity;
                DefaultFont.FontSystem.Reset();
            }
        }
        public bool TryGetFont(FixedString32Bytes name, out FontSystemData data)
        {
            data = default;
            return LoadedFonts.TryGetValue(name, out var entity) && EntityManager.TryGetComponent(entity, out data);
        }
        public bool TryGetFontEntity(FixedString32Bytes name, out Entity entity)
        {
            return LoadedFonts.TryGetValue(name, out entity);
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isLoading) return;
            EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
            bool fontsChanged = false;
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
                                    cmd.DestroyEntity(otherEntity);
                                    fontsChanged = true;
                                }
                                else
                                {
                                    cmd.DestroyEntity(entity);
                                    fontsChanged = true;
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            LoadedFonts[data.Name] = entity;
                            fontsChanged = true;
                        }
                        if (!UpdateFontSystem(data))
                        {
                            cmd.DestroyEntity(entity);
                        }
                    }
                }
            }
            UpdateFontSystem(DefaultFont);
            Dependency.Complete();
            if (fontsChanged) OnFontsLoadedChanged?.Invoke();
            requiresUpdateParameter = false;
        }

        private bool UpdateFontSystem(FontSystemData data)
        {
            try
            {
                if (requiresUpdateParameter)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Resetting font system!");
                    data.FontSystem.Reset();
                }
                Dependency = data.FontSystem.RunJobs(Dependency);
                return true;
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog($"Error on UpdateFontSystem for {data.Name}: {e}");
                return false;
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
            material.SetFloat(DecalLayerMask, 8.ToFloatBitFlags());
            material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
            HDMaterial.ValidateMaterial(material);
            return material;
        }

        internal bool FontExists(string name) => LoadedFonts.ContainsKey(name);

        internal string[] GetLoadedFontsNames() => LoadedFonts.Keys.Select(x => x.ToString()).ToArray();

        internal void EnsureFont(FixedString32Bytes fontName)
        {
            fontName = fontName.Trim();
            if (fontName == "" || LoadedFonts.ContainsKey(fontName)) return;
            RegisterFont(fontName.ToString(), DefaultFont.Font._font.data.ArrayData.ToArray(), true);
        }
    }
}
