//#define JOBS_DEBUG

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using Colossal.OdinSerializer.Utilities;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using static Unity.Collections.Unicode;
using Color = UnityEngine.Color;

namespace BelzontWE.Font
{
    public enum UIHorizontalAlignment
    {
        Left,
        Center,
        Right
    }
    public partial class FontSystem : IDisposable
    {
        public FontSystemData Data { get; } = new() { };
        private GCHandle dataPointer;

        public NativeHashMap<int, NativeHashMap<int, FontGlyph>> _glyphs;

        private FontAtlas _currentAtlas;
        private Vector2Int _size;
        public int FontHeight => FontServer.QualitySize;

        private readonly Dictionary<string, IBasicRenderInformation> m_textCache = new();

        public Color Color;
        public readonly int Blur;
        public float Spacing;
        public bool UseKernings = true;
        public string Name => Data.Name;

        public long LastUpdateAtlas { get; private set; }

        public int? DefaultCharacter = ' ';

        public FontAtlas CurrentAtlas
        {
            get
            {
                if (_currentAtlas == null)
                {
                    _currentAtlas = new FontAtlas(_size.x, _size.y, 256);
                    LastUpdateAtlas = DateTime.Now.Ticks;
                }

                return _currentAtlas;
            }
        }


        public event Action CurrentAtlasFull;

        private readonly NativeQueue<StringRenderingQueueItem> itemsQueue = new(Allocator.Persistent);
        private readonly NativeQueue<BasicRenderInformationJob> results = new(Allocator.Persistent);
        private readonly NativeQueue<StringRenderingQueueItem>.ParallelWriter itemsQueueWriter;

        public FontSystem(FontSystemData data)
        {
            Blur = 0;
            this.Data = data;
            _size = new(FontServer.DefaultTextureSizeFont, FontServer.DefaultTextureSizeFont);
            ClearState();
            itemsQueueWriter = itemsQueue.AsParallelWriter();
            dataPointer = GCHandle.Alloc(data);
        }

        public void ClearState()
        {
            Color = Color.white;
            Spacing = 0;
        }


        public void EnsureText(string str, Vector3 scale)
        {
            if (str.TrimToNull() == null)
            {
                return;
            }
            if (!m_textCache.ContainsKey(str))
            {
                m_textCache[str] = default;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Enqueued String to ensure: {str} ({Data.Name})");
                itemsQueue.Enqueue(new StringRenderingQueueItem() { text = str });
            }
        }

        public IBasicRenderInformation DrawText(string str)
        {
            if (GameManager.instance.isGameLoading) return null;
            IBasicRenderInformation bri;
            if (string.IsNullOrWhiteSpace(str))
            {
                if (!m_textCache.TryGetValue("", out bri))
                {
                    bri = new PrimitiveRenderInformation(str, null, null, null, default, null, null, null, default);
                    m_textCache.TryAdd("", bri);
                    return bri;
                }
                return bri;
            }
            if (m_textCache.TryGetValue(str, out bri) && bri != null)
            {
                return bri == PrimitiveRenderInformation.LOADING_PLACEHOLDER ? null : bri;
            }
            else
            {
                var result = m_textCache[str] = PrimitiveRenderInformation.LOADING_PLACEHOLDER;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Enqueued String: {str} ({Data.Name}) {result}");
                itemsQueueWriter.Enqueue(new StringRenderingQueueItem() { text = str });
                if (BasicIMod.DebugMode) LogUtils.DoLog($"itemsQueue: {itemsQueue.Count}");
                return default;
            }
        }


        public void ResetCache()
        {
            foreach (var entry in m_textCache)
            {
                entry.Value.Dispose();
            }
            m_textCache.Clear();
        }

        public void Reset()
        {
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Resetting font cache for {Data.Name} => {FontServer.DefaultTextureSizeFont}");
            var width = FontServer.DefaultTextureSizeFont;
            var height = width;
            Data.Font.RecalculateBasedOnHeight(FontServer.QualitySize);
            CurrentAtlas.Reset(width, height);
            if (_glyphs.IsCreated) _glyphs.Clear();
            m_textCache.Clear();
            if (width == _size.x && height == _size.y)
            {
                return;
            }

            _size = new(width, height);
        }


        private static int GetCodepointIndex(int codepoint, Font f) => f.GetGlyphIndex(codepoint);

        private static void EnsureAllGlyphsWithoutBitmap(NativeHashMap<int, FontGlyph> glyphs, IEnumerable<int> codepoints, FontSystemData data)
        {
            foreach (var cp in codepoints)
            {
                GetGlyphWithoutBitmap(glyphs, cp, data);
            }
        }


        private static FontGlyph GetGlyphWithoutBitmap(NativeHashMap<int, FontGlyph> glyphs, int codepoint, FontSystemData data)
        {
            if (glyphs.TryGetValue(codepoint, out FontGlyph glyph))
            {
                return glyph;
            }

            if (data.Font?.GetGlyphIndex(codepoint) is not int g || g == 0)
            {
                return glyphs[codepoint] = FontGlyph.Null;
            }
            var font = data.Font;
            int advance = 0, lsb = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            font.BuildGlyphBitmap(g, font.Scale, ref advance, ref lsb, ref x0, ref y0, ref x1, ref y1);

            int pad = FontGlyph.PadFromBlur(default);
            int gw = x1 - x0 + pad * 2;
            int gh = y1 - y0 + pad * 2;

            glyph = new FontGlyph
            {
                Font = font,
                Codepoint = codepoint,
                Height = FontServer.QualitySize,
                Blur = default,
                Index = g,
                width = gw,
                height = gh,
                XAdvance = (int)(font.Scale * advance * 10.0f),
                XOffset = x0 - pad,
                YOffset = y0 - pad
            };

            glyphs[codepoint] = glyph;

            return glyph;
        }

        private FontGlyph GetGlyphInternal(NativeHashMap<int, FontGlyph> glyphs, int codepoint, out bool hasResetted)
        {
            hasResetted = false;
            FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint, Data);
            if (!glyph.IsValid)
            {
                return default;
            }

            if (glyph.AtlasGenerated)
            {
                return glyph;
            }

            int gx = 0, gy = 0;
            int gw = Mathf.RoundToInt(glyph.width);
            int gh = Mathf.RoundToInt(glyph.height);
            do
            {
                if (!CurrentAtlas.AddRect(gw, gh, ref gx, ref gy))
                {
                    CurrentAtlasFull?.Invoke();
                    do
                    {
                        // This code will force creation of new atlas with 4x size
                        this._currentAtlas = null;
                        if (_size.x * _size.y < 8192 * 8192)
                        {
                            _size *= 2;
                        }
                        else
                        {
                            throw new Exception(string.Format("Could not add rect to the newly created atlas. gw={0}, gh={1} - MAP REACHED 16K * 16K LIMIT!", gw, gh));
                        }
                        glyphs.Clear();
                        glyphs[codepoint] = glyph;

                        if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Resetting size to {_size}");
                        m_textCache.Clear();

                        hasResetted = true;
                        // Try to add again
                    } while (!CurrentAtlas.AddRect(gw, gh, ref gx, ref gy));
                }

                glyph.x = gx;
                glyph.y = gy;

                if (CurrentAtlas.RenderGlyph(glyph) && glyphs.Count > 0)
                {
                    CurrentAtlas.Reset(_size.x, _size.y);
                    m_textCache.Clear();
                    glyphs.Clear();
                    continue;
                }
                break;
            } while (true);
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Rendered glyph #{glyph} (unicode 0x{codepoint:X}) @ texture {CurrentAtlas.Texture}");

            glyph.AtlasGenerated = true;

            glyphs[codepoint] = glyph;

            return glyph;
        }

        private FontGlyph GetGlyph(NativeHashMap<int, FontGlyph> glyphs, int codepoint, out bool hasResetted)
        {
            FontGlyph result = GetGlyphInternal(glyphs, codepoint, out hasResetted);

            return result;
        }

        private NativeHashMap<int, FontGlyph> GetGlyphsCollection(int size)
        {
            if (!_glyphs.IsCreated)
            {
                _glyphs = new NativeHashMap<int, NativeHashMap<int, FontGlyph>>(1, Allocator.Persistent);
            }
            if (_glyphs.TryGetValue(size, out var result))
            {
                return result;
            }

            result = new(0, Allocator.Persistent);
            _glyphs[size] = result;
            return result;
        }


        private void PostJob(BasicRenderInformationJob brij)
        {

            var originalText = brij.originalText.ToString();

            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[FontSystem: {Name}] Post job for {originalText} ");

            if (brij.vertices.Length == 0)
            {
                if (originalText.TrimToNull() is not null)
                {
                    m_textCache[originalText] = WEAtlasesLibrary.Instance.GetFromLocalAtlases(WEImages.FontHasNoGlyphs);
                }
                return;
            }
            if (brij.Invalid || brij.AtlasVersion != CurrentAtlas.Version)
            {

                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[FontSystem: {Name}] removing {originalText} since atlas changed");

                m_textCache[originalText] = null;
                itemsQueueWriter.Enqueue(new StringRenderingQueueItem() { text = originalText });
                return;
            }

            var result = PrimitiveRenderInformation.Fill(brij, CurrentAtlas.Texture);
            if (result is null)
            {
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[FontSystem: {Name}] removing {originalText} ");
                m_textCache[originalText] = null;
                itemsQueueWriter.Enqueue(new StringRenderingQueueItem() { text = originalText });
            }
            else if (m_textCache.TryGetValue(originalText, out var currentVal))
            {
                if ((currentVal == null || currentVal == PrimitiveRenderInformation.LOADING_PLACEHOLDER))
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[FontSystem: {Name}] SET UP to val '{originalText}'");
                    m_textCache[originalText] = result;
                }
                else
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"[FontSystem: {Name}] KEEPING '{originalText}' (already filled with {currentVal})");
                }
            }
            else
            {
                m_textCache[originalText] = result;
            }
        }

        public void Dispose()
        {
            if (_glyphs.IsCreated) _glyphs.Dispose();
            if (itemsQueue.IsCreated) itemsQueue.Dispose();
            if (results.IsCreated) results.Dispose();
            if (dataPointer.IsAllocated) dataPointer.Free();
            _currentAtlas?.Dispose();
        }

        private const int QUEUE_CONSUMPTION_FRAME = 256;
        private byte framesBuffering = 0;

        public unsafe struct StringRenderingQueueItem
        {
            public FixedString512Bytes text;
        }

        public JobHandle RunJobs(JobHandle dependency)
        {
            if (!m_textCache.ContainsKey(""))
            {
                m_textCache[""] = new PrimitiveRenderInformation("", [], [], [], default, null, null);
            }
            if (itemsQueue.Count >= QUEUE_CONSUMPTION_FRAME || framesBuffering++ > 60)
            {
                framesBuffering = 0;
                if (itemsQueue.Count != 0)
                {
                    NativeArray<StringRenderingQueueItem> itemsStarted;
                    if (itemsQueue.Count > QUEUE_CONSUMPTION_FRAME)
                    {
                        itemsStarted = new(QUEUE_CONSUMPTION_FRAME, Allocator.TempJob);
                        for (int i = 0; i < QUEUE_CONSUMPTION_FRAME; i++)
                        {
                            itemsStarted[i] = itemsQueue.Dequeue();
                        }
                    }
                    else
                    {
                        itemsStarted = itemsQueue.ToArray(Allocator.TempJob);
                        itemsQueue.Clear();
                    }
                    var glyphs = GetGlyphsCollection(FontHeight);
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Gliphs collection size = {glyphs.Count}");
                    var wordsToRender = itemsStarted.Select(x => GetRunes(x.text)).ToArray();
                    var charsToRender = wordsToRender.SelectMany(x => x).GroupBy(x => x.value).Select(x => x.Key);

                    static (Rune prev, Rune next) GetPairs(Rune current, int index, List<Rune> list) => index > 0 ? (list[index - 1], current) : default;

                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"charsToRender = ['{string.Join("', '", charsToRender)}']");

                    var countSucceeded = 0;
                    foreach (var charact in charsToRender)
                    {
                        var result = GetGlyph(glyphs, charact, out bool hasReseted);
                        if (!result.IsValid)
                        {
                            var normalizedChar = char.ConvertFromUtf32(charact).Normalize(System.Text.NormalizationForm.FormKD);
                            if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] Normalizing char ID 0x{charact:X} ({char.ConvertFromUtf32(charact)}) got: {string.Join(", ", normalizedChar.ToArray().Select(x => "0x" + ((int)x).ToString("X")))}");
                            if (normalizedChar.Length > 1)
                            {
                                result = GetGlyph(glyphs, char.ConvertToUtf32(normalizedChar, 0), out hasReseted);
                                glyphs[charact] = result;
                            }
                        }
                        if (result.IsValid)
                        {
                            if (hasReseted)
                            {
                                LogUtils.DoInfoLog($"[FontSystem: {Name}] Reset texture! (Now {CurrentAtlas.Texture.width})");
                                m_textCache.Clear();
                                continue;
                            }

                            countSucceeded++;
                        }
                    }
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Glyphs rendered: {countSucceeded}");
                    EnsureAllGlyphsWithoutBitmap(glyphs, charsToRender, Data);
                    wordsToRender.SelectMany((x) => x.Select((y, i) => GetPairs(y, i, x))).Distinct().ForEach(x =>
                    {
                        if (glyphs[x.prev.value].IsValid && glyphs[x.next.value].IsValid)
                        {
                            glyphs[x.prev.value].GetKerning(glyphs[x.next.value]);
                        }
                    });
                    var job = new StringRenderingJob
                    {
                        ascent = Data.Font.Ascent,
                        descent = Data.Font.Descent,
                        lineHeight = Data.Font.LineHeight,
                        capital = Data.Font.Capital,
                        fontScale = Data.Font.Scale,
                        CurrentAtlasSize = new Vector3(CurrentAtlas.Width, CurrentAtlas.Height),
                        inputArray = itemsStarted.AsReadOnly(),
                        glyphs = glyphs,
                        output = results.AsParallelWriter(),
                        AtlasVersion = CurrentAtlas.Version,
                        scale = FontServer.Instance.ScaleEffective,
                        TriangleIndices = TriangleIndices,
                        VerticesPositionsCube = VerticesPositionsCube,
                        TriangleIndicesCube = TriangleIndicesCube,
                    };
                    dependency = job.ScheduleParallel(itemsStarted.Length, 32, dependency);
                    itemsStarted.Dispose(dependency);
                    dependency.Complete();
                }
            }
            var postJobCounter = 0;
            while (results.TryDequeue(out var result))
            {
                PostJob(result);
                if (++postJobCounter > QUEUE_CONSUMPTION_FRAME)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Skipping next frame; strings yet to process at font {Name}: {results.Count}");
                    break;
                }
            }
            return dependency;
        }


        private List<Rune> GetRunes(FixedString512Bytes text)
        {
            var runes = new List<Rune>();
            var enumerator = text.GetEnumerator();
            while (enumerator.MoveNext())
            {
                runes.Add(enumerator.Current);
            }
            return runes;
        }
        private static readonly NativeArray<int> TriangleIndicesCube;
        private static readonly NativeArray<Vector3> VerticesPositionsCube;
        private static readonly NativeArray<int> TriangleIndices;



        static FontSystem()
        {
            TriangleIndicesCube = new NativeArray<int>(WERenderingHelper.kTriangleIndicesCube.Reverse().ToArray(), Allocator.Persistent);
            VerticesPositionsCube = new NativeArray<Vector3>(WERenderingHelper.kVerticesPositionsCube, Allocator.Persistent);
            TriangleIndices = new(WERenderingHelper.kTriangleIndices.Reverse().ToArray(), Allocator.Persistent);
        }
    }
}