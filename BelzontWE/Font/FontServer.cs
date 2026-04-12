
using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace BelzontWE
{
    public partial class FontServer : GameSystemBase, IBelzontSerializableSingleton<FontServer>
    {



        public const int CURRENT_VERSION = 0;
        #region Fonts
        public const string DEFAULT_FONT_KEY = "\0DEFAULT\0";
        private const string FONTS_FILES_FOLDER = "fonts";
        public static int DefaultTextureSizeFont => 512 << (WEModData.InstanceWE?.StartTextureSizeFont ?? 1);
        public static string FontFilesPath => Path.Combine(BasicIMod.ModSettingsRootFolder, FONTS_FILES_FOLDER);
        public event Action OnFontsLoadedChanged;
        private readonly Queue<Action> OnUpdateActionQueue = new();

        private readonly Dictionary<Assembly, ModFolder> integrationFontsAvailable = new();

        internal void RegisterModFonts(Assembly mainAssembly, ModFolder fontFolder) { integrationFontsAvailable[mainAssembly] = fontFolder; }
        internal List<ModFolder> ListModsExtraFolders() => integrationFontsAvailable.Values.ToList();

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
        private Dictionary<FixedString64Bytes, FontSystemData> LoadedFonts { get; } = new();

        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_weTextEntities;

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
            DefaultFont = FontSystemData.From(KResourceLoader.LoadResourceDataMod("Resources.SourceSansPro-Regular.ttf"), DEFAULT_FONT_KEY, true);
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            m_weTextEntities = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<WETextDataMesh>(),
                }
            });

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
        public void CleanTextCache(string name)
        {
            if (name == null)
            {
                DefaultFont.FontSystem.ResetCache();
            }
            else if (LoadedFonts.ContainsKey(name))
            {
                LoadedFonts[name].FontSystem.ResetCache();
            }
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
                data.Name = newName;
                LoadedFonts[newName] = data;
                LoadedFonts.Remove(oldName);
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
        public bool TryGetFont(FixedString64Bytes name, out FontSystemData data)
        {
            return LoadedFonts.TryGetValue(name, out data) || LoadedFonts.TryGetValue(DEFAULT_FONT_KEY, out data);
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            while (OnUpdateActionQueue.TryDequeue(out var action))
            {
                action();
            }
            EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
            var keysToDispose = new List<FixedString64Bytes>();
            var i = 0;
            var scheduledFonts = new List<(FontSystemData data, bool updateAtlases)>();

            // Pass 1: Schedule jobs for all fonts (glyph prep + job scheduling, no Complete)
            foreach (var (key, data) in LoadedFonts)
            {
                bool updateAtlases = UnityEngine.Time.frameCount % 60 == ++i % 60;
                if (!ScheduleFontSystem(data))
                {
                    data.Dispose();
                    keysToDispose.Add(key);
                }
                else
                {
                    scheduledFonts.Add((data, updateAtlases));
                }
            }
            bool defaultUpdateAtlases = UnityEngine.Time.frameCount % 60 == ++i % 60;
            ScheduleFontSystem(DefaultFont);
            scheduledFonts.Add((DefaultFont, defaultUpdateAtlases));

            // Single barrier: complete all scheduled jobs at once
            Dependency.Complete();

            // Pass 2: Process results and apply atlases for all fonts
            foreach (var (data, updateAtlases) in scheduledFonts)
            {
                ProcessFontSystem(data, updateAtlases);
            }

            if (requiresUpdateParameter)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog("Font params changed — marking all WE text entities for re-render");
                EntityManager.AddComponent<WEWaitingRendering>(m_weTextEntities);
                EntityManager.SetComponentEnabled<WEWaitingRendering>(m_weTextEntities, true);
            }
            requiresUpdateParameter = false;
            if (keysToDispose.Count > 0)
            {
                foreach (var key in keysToDispose)
                {
                    LoadedFonts.Remove(key);
                }
                OnFontsLoadedChanged?.Invoke();
            }
        }

        private bool ScheduleFontSystem(FontSystemData data)
        {
            try
            {
                if (requiresUpdateParameter)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Resetting font system!");
                    data.FontSystem.Reset();
                }
                Dependency = data.FontSystem.ScheduleJobs(Dependency);
                return true;
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog($"Error on ScheduleFontSystem for {data.Name ?? "<Default>"}: {e.Message}\n{e}");
                return false;
            }
        }

        private void ProcessFontSystem(FontSystemData data, bool updateAtlases)
        {
            try
            {
                data.FontSystem.ProcessResults();
                if (updateAtlases) data.FontSystem.CurrentAtlas.Apply();
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog($"Error on ProcessFontSystem for {data.Name ?? "<Default>"}: {e.Message}\n{e}");
            }
        }



        internal bool FontExists(string name) => LoadedFonts.ContainsKey(name);

        public string[] GetLoadedFontsNames() => [.. LoadedFonts.Keys.Select(x => x.ToString())];


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
                if (item.Value != DefaultFont)
                {
                    var font = item.Value;
                    OnUpdateActionQueue.Enqueue(() => font.Dispose());
                }
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
