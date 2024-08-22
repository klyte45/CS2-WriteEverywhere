﻿
using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using Kwytto.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BelzontWE
{
    public partial class FontServer : GameSystemBase, IBelzontSerializableSingleton<FontServer>
    {
        public const int CURRENT_VERSION = 0;
        public const string defaultShaderName = "BH/SG_DefaultShader";
        public const string defaultGlassShaderName = "BH/GlsShader";
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
        private bool requiresUpdateParameter;
        private static int qualitySize = 100;

        public static FontServer Instance { get; private set; }
        public static int DecalLayerMask { get; private set; } = -1;
        public static int Transmittance { get; private set; } = -1;
        public static int IOR { get; private set; }
        private Dictionary<FixedString32Bytes, FontSystemData> LoadedFonts { get; } = new();

        private EndFrameBarrier m_endFrameBarrier;

        public FontSystemData this[string name] => LoadedFonts.TryGetValue(name, out var e) ? e : null;

        public FontSystemData GetOrCreateFontAsDefault(string name)
        {
            if (name.TrimToNull() == null) return default;
            if (LoadedFonts.TryGetValue(name, out var e)) return e;
            var defaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY, true);
            LoadedFonts[name] = defaultFont;
            return defaultFont;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");

            var glassShader = Shader.Find(defaultGlassShaderName);
            var propertyCount = glassShader.GetPropertyCount();
            for (int i = 0; i < propertyCount && (Transmittance == -1 || IOR == -1); i++)
            {
                switch (glassShader.GetPropertyDescription(i))
                {
                    case "IOR":
                        IOR = glassShader.GetPropertyNameId(i);
                        break;
                    case "TransmittanceColor":
                        Transmittance = glassShader.GetPropertyNameId(i);
                        break;
                }
            }
            DefaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY, true);
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            dictPtr = GCHandle.Alloc(LoadedFonts);
        }

        #endregion

        public Vector2 ScaleEffective => Vector2.one / QualitySize * 2.250f;

        private GCHandle dictPtr;
        public GCHandle DictPtr => dictPtr;

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
                    e.Dispose();
                }
                else
                {
                    LoadedFonts[fontSystemData.Name] = fontSystemData;
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
                LoadedFonts[name].Dispose();
                LoadedFonts.Remove(name);
            }
        }
        public void RenameFont(string oldName, string newName)
        {
            if (oldName == newName || oldName.TrimToNull() == null || newName.TrimToNull() == null) return;
            if (LoadedFonts.TryGetValue(oldName, out var data))
            {
                if (LoadedFonts.TryGetValue(newName, out var item)) item.Dispose();
                LoadedFonts[newName] = data;
                data.FontSystem.Reset();
                DefaultFont.FontSystem.Reset();
            }
        }
        public void DuplicateFont(string srcFont, string newName)
        {
            if (srcFont == newName || srcFont.TrimToNull() == null || newName.TrimToNull() == null) return;
            if (LoadedFonts.TryGetValue(srcFont, out var fontData))
            {
                if (LoadedFonts.TryGetValue(newName, out var item)) item.Dispose();
                LoadedFonts[newName] = FontSystemData.From(fontData.Font._font.data.ArrayData, newName);
                DefaultFont.FontSystem.Reset();
            }
        }
        public bool TryGetFont(FixedString32Bytes name, out FontSystemData data)
        {
            return LoadedFonts.TryGetValue(name, out data);
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isLoading) return;
            EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
            var keysToDispose = new List<FixedString32Bytes>();
            foreach (var (key, data) in LoadedFonts)
            {
                if (!UpdateFontSystem(data))
                {
                    data.Dispose();
                    keysToDispose.Add(key);
                }
            }
            UpdateFontSystem(DefaultFont);
            requiresUpdateParameter = false;
            if (keysToDispose.Count > 0)
            {
                foreach (var key in keysToDispose)
                {
                    LoadedFonts.Remove(key);
                }
                OnFontsLoadedChanged?.Invoke();
            }
            Dependency.Complete();
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

        public static Material CreateDefaultFontMaterial(int type)
        {
            return Instance.CreateDefaultFontMaterial_Impl(type);
        }
        private Material CreateDefaultFontMaterial_Impl(int type)
        {
            Material material = null; new Material(Shader.Find(defaultShaderName));
            switch (type)
            {
                case 0:
                    material = new Material(Shader.Find(defaultShaderName));
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
                    break;
                case 1:
                    material = new Material(Shader.Find(defaultGlassShaderName));
                    material.SetFloat("_DoubleSidedEnable", 1);
                    material.SetVector("_DoubleSidedConstants", new Vector4(1, 1, -1, 0));
                    material.SetFloat(DecalLayerMask, 8.ToFloatBitFlags());
                    material.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
                    break;
            }
            HDMaterial.ValidateMaterial(material);
            return material;
        }

        internal bool FontExists(string name) => LoadedFonts.ContainsKey(name);

        public string[] GetLoadedFontsNames() => LoadedFonts.Keys.Select(x => x.ToString()).ToArray();

        internal void EnsureFont(FixedString32Bytes fontName)
        {
            fontName = fontName.Trim();
            if (fontName == "" || LoadedFonts.ContainsKey(fontName)) return;
            RegisterFont(fontName.ToString(), DefaultFont.Font._font.data.ArrayData.ToArray(), true);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            var fontsSerializable = LoadedFonts.Where(x => !x.Value.IsWeak).ToArray();
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Serializing fonts: {fontsSerializable.Length} able to be serialized");
            writer.Write(fontsSerializable.Length);
            foreach (var item in fontsSerializable)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Serializing font {item.Value.Name}");
                writer.Write(item.Key);
                var dataToSerialize = new NativeArray<byte>(item.Value.Font._font.data.ArrayData, Allocator.Temp);
                writer.Write(dataToSerialize.Length);
                writer.Write(dataToSerialize);
                dataToSerialize.Dispose();
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            foreach (var item in LoadedFonts)
            {
                item.Value.Dispose();
            }
            LoadedFonts.Clear();
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out int fontsLength);
            for (var i = 0; i < fontsLength; i++)
            {
                reader.Read(out FixedString32Bytes key);
                reader.Read(out int arrLength);

                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loading font: {key} {arrLength}B");
                var dataToSerialize = new NativeArray<byte>(arrLength, Allocator.Temp);
                reader.Read(dataToSerialize);
                RegisterFont(key.ToString(), dataToSerialize.ToArray(), false);
                dataToSerialize.Dispose();
            }
        }

        public JobHandle SetDefaults(Context context)
        {
            LoadedFonts.Clear();
            return Dependency;
        }
    }
}
