
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.IO.AssetDatabase.Internal;
using Game;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BelzontWE
{
    public partial class FontServer : GameSystemBase
    {
        public static string FOLDER_PATH => WriteEverywhereCS2Mod.ModSettingsRootFolder;
        #region Fonts
        public const string DEFAULT_FONT_KEY = "/DEFAULT/";
        public const string FONTS_FILES_FOLDER = "Fonts";
        public static int DefaultTextureSizeFont => 1024;//512 << WriteEverywhereCS2Mod.StartTextureSizeFont;
        public static string FontFilesPath { get; } = FOLDER_PATH + Path.DirectorySeparatorChar + FONTS_FILES_FOLDER;

        public event Action OnFontsLoadedChanged;

        public void ReloadFontsFromPath()
        {
            ResetCollection();
            //RegisterFont(DEFAULT_FONT_KEY, KResourceLoader.LoadResourceDataMod("UI.DefaultFont.SourceSansPro-Regular.ttf"), DefaultTextureSizeFont);
            KFileUtils.EnsureFolderCreation(FontFilesPath);
            if (WriteEverywhereCS2Mod.DebugMode) LogUtils.DoLog($"Searching font files @ {FontFilesPath}");
            foreach (string fontFile in Directory.GetFiles(FontFilesPath, "*.ttf"))
            {
                RegisterFont(Path.GetFileNameWithoutExtension(fontFile), File.ReadAllBytes(fontFile), DefaultTextureSizeFont);

                if (WriteEverywhereCS2Mod.DebugMode) LogUtils.DoLog($"Font loaded: {Path.GetFileName(fontFile)}");
            }
            OnFontsLoadedChanged?.Invoke();
        }
        #endregion



        private Dictionary<string, DynamicSpriteFont> m_fontRegistered = new Dictionary<string, DynamicSpriteFont>();

        internal long GetAllFontsCacheSize()
        {
            long size = 0L;
            foreach (var font in m_fontRegistered.Values)
            {
                size += font.GetCacheSize();
            }
            return size;
        }

        private float m_targetHeight = 100;

        private float m_qualityMultiplier = 1f;

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
                m_fontRegistered[name] = DynamicSpriteFont.FromTtf(fontData, name, FontSizeEffective, textureSize, textureSize, m_qualityMultiplier, GetDefaultFontShader);
            }
            catch (FontCreationException)
            {
                LogUtils.DoErrorLog($"RegisterFont: Error creating the font \"{name}\"... Invalid data!");
                return false;
            }
            return true;
        }
        private static string defaultShaderName = "BH/SG_DefaultShader";

        internal void SetDefaultShader(string shaderName)
        {
            if (Shader.Find(shaderName))
            {
                defaultShaderName = shaderName;
                ReloadFontsFromPath();
            }
        }
        internal string GetDefaultShader() => defaultShaderName;

        private static Shader GetDefaultFontShader() => Shader.Find(defaultShaderName);

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

    }
}
