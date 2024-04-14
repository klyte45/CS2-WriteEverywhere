
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.IO.AssetDatabase.Internal;
using Game;
using Kwytto.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static Game.Rendering.Debug.RenderPrefabRenderer;

namespace BelzontWE
{
    public partial class FontServer : GameSystemBase
    {
        private const string defaultShaderName = "BH/SG_DefaultShader";
        public static string FOLDER_PATH => BasicIMod.ModSettingsRootFolder;
        #region Fonts
        public const string DEFAULT_FONT_KEY = "/DEFAULT/";
        public const string FONTS_FILES_FOLDER = "Fonts";
        public static int DefaultTextureSizeFont => 1024;//512 << WriteEverywhereCS2Mod.StartTextureSizeFont;
        public static string FontFilesPath { get; } = FOLDER_PATH + Path.DirectorySeparatorChar + FONTS_FILES_FOLDER;
        public event Action OnFontsLoadedChanged;

        private float m_targetHeight = 100;

        private float m_qualityMultiplier = 1f;

        private Dictionary<string, DynamicSpriteFont> m_fontRegistered = new Dictionary<string, DynamicSpriteFont>();
        public static FontServer Instance { get; private set; }

        public static int DecalLayerMask { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
        }


        public void ReloadFontsFromPath()
        {
            ResetCollection();
            RegisterFont(DEFAULT_FONT_KEY, KResourceLoader.LoadResourceDataMod("Font.Resources.SourceSansPro-Regular.ttf"), DefaultTextureSizeFont);
            KFileUtils.EnsureFolderCreation(FontFilesPath);
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Searching font files @ {FontFilesPath}");
            foreach (string fontFile in Directory.GetFiles(FontFilesPath, "*.ttf"))
            {
                RegisterFont(Path.GetFileNameWithoutExtension(fontFile), File.ReadAllBytes(fontFile), DefaultTextureSizeFont);

                if (BasicIMod.DebugMode) LogUtils.DoLog($"Font loaded: {Path.GetFileName(fontFile)}");
            }
            OnFontsLoadedChanged?.Invoke();
        }
        #endregion



        internal long GetAllFontsCacheSize()
        {
            long size = 0L;
            foreach (var font in m_fontRegistered.Values)
            {
                size += font.GetCacheSize();
            }
            return size;
        }


        private int DefaultTextureSize => 512;// WEMainController.DefaultTextureSizeFont;

        private int FontSizeEffective => Mathf.RoundToInt(m_targetHeight * m_qualityMultiplier);
        public Vector2 ScaleEffective => Vector2.one / m_qualityMultiplier;


        public void ResetCollection() => m_fontRegistered = new Dictionary<string, DynamicSpriteFont>();
        public void SetOverallSize(float f)
        {
            m_targetHeight = f;
            OnChangeSizeParam();
        }

        public void ResetOverallSize() => SetOverallSize(120);

        public void SetQualityMultiplier(float f)
        {
            m_qualityMultiplier = f;
            OnChangeSizeParam();
        }

        public void ResetQualityMultiplier() => SetQualityMultiplier(1);

        private void OnChangeSizeParam()
        {
            foreach (DynamicSpriteFont font in m_fontRegistered.Values)
            {
                font.Height = FontSizeEffective;
                font.Reset(DefaultTextureSize, DefaultTextureSize);
            }
        }

        public bool RegisterFont(string name, byte[] fontData, int textureSize)
        {
            try
            {
                if (name == null)
                {
                    LogUtils.DoErrorLog($"RegisterFont: FONT NAME CANNOT BE NULL!!");
                    return false;
                }

                if (m_fontRegistered.ContainsKey(name))
                {
                    m_fontRegistered[name].Reset(1, 1);
                }
                m_fontRegistered[name] = DynamicSpriteFont.FromTtf(fontData, name, FontSizeEffective, textureSize, textureSize, m_qualityMultiplier);
            }
            catch (FontCreationException)
            {
                LogUtils.DoErrorLog($"RegisterFont: Error creating the font \"{name}\"... Invalid data!");
                return false;
            }
            return true;
        }

        public void ClearFonts() => m_fontRegistered.Clear();

        public DynamicSpriteFont this[string idx]
        {
            get
            {
                if (idx != null)
                {
                    if (Aliases.ContainsKey(idx))
                    {
                        idx = Aliases[idx];
                    }
                    return m_fontRegistered.TryGetValue(idx, out DynamicSpriteFont value) ? value : null;
                }
                return null;
            }
        }

        public DynamicSpriteFont FirstOf(IEnumerable<Func<string>> names)
        {
            foreach (var run in names)
            {
                var idx = run();
                if (idx != null)
                {
                    if (m_fontRegistered.TryGetValue(Aliases.TryGetValue(idx, out string alias) ? alias : idx, out DynamicSpriteFont value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        public Dictionary<string, string> Aliases { get; } = new Dictionary<string, string>();

        public IEnumerable<string> GetAllFonts() => m_fontRegistered.Keys;

        protected override void OnUpdate()
        {
            m_fontRegistered.Values.ForEach(x => x.RunJobs());
        }

        internal static Material CreateDefaultFontMaterial()
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
            HDMaterial.SetEmissiveColor(material, Color.white);
            HDMaterial.SetEmissiveIntensity(material, 0, UnityEditor.Rendering.HighDefinition.EmissiveIntensityUnit.Nits);
            material.SetFloat("_DoubleSidedEnable", 1);
            material.SetVector("_DoubleSidedConstants", new Vector4(1, 1, -1, 0));
            material.SetFloat("_Smoothness", .5f);
            material.SetFloat("_ZTestGBuffer", 7);
            material.SetFloat(FontServer.DecalLayerMask, 8.ToFloatBitFlags());
            HDMaterial.ValidateMaterial(material);
            return material;
        }
    }
}
