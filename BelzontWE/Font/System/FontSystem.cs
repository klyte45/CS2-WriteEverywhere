//#define JOBS_DEBUG

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using Game.SceneFlow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
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
                    bri = new PrimitiveRenderInformation(str, null, null, null, null, null, null, null);
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

        private static void AddTriangleIndices(IList<int> triangles)
        {
            int count = triangles.Count * 2 / 3;
            for (int i = 0; i < kTriangleIndices.Length; i++)
            {
                triangles.Add(count + kTriangleIndices[i]);
            }
        }
        private static void AddTriangleIndicesCube(IList<int> triangles)
        {
            int verricesCount = triangles.Count * 2 / 3;
            for (int i = WERenderingHelper.kTriangleIndicesCube.Length - 1; i >= 0; i--)
            {
                triangles.Add(verricesCount + WERenderingHelper.kTriangleIndicesCube[i]);
            }
        }
        private readonly static int[] kTriangleIndices = new int[]{
                0,
                1,
                3,
                3,
                1,
                2
        };

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


        private static FontGlyph GetGlyphWithoutBitmap(NativeHashMap<int, FontGlyph> glyphs, int codepoint, FontSystemData data)
        {
            if (glyphs.TryGetValue(codepoint, out FontGlyph glyph))
            {
                return glyph;
            }

            if (data.Font?.GetGlyphIndex(codepoint) is not int g || g == 0)
            {
                return FontGlyph.Null;
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
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Rendered glyph #{glyph} @ texture {CurrentAtlas.Texture}");

            glyph.AtlasGenerated = true;

            glyphs[codepoint] = glyph;

            return glyph;
        }

        private FontGlyph GetGlyph(NativeHashMap<int, FontGlyph> glyphs, int codepoint, out bool hasResetted, bool ignoreDefaultChar = false)
        {
            FontGlyph result = GetGlyphInternal(glyphs, codepoint, out hasResetted);
            if (!ignoreDefaultChar && DefaultCharacter != null)
            {
                result = GetGlyphInternal(glyphs, DefaultCharacter.Value, out hasResetted);
            }

            return result;
        }

        private unsafe static void GetQuad(ref FontGlyph glyph, ref FontGlyph prevGlyph, float spacingFactor, ref float x, ref float y, ref FontGlyphBounds q)
        {
            if (prevGlyph.IsValid)
            {
                float adv = prevGlyph.GetKerning(glyph) * glyph.Font.Scale;

                x += (int)((adv + 0) * spacingFactor + 0.5f);
            }
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"'{char.ConvertFromUtf32(glyph.Codepoint)}' = {glyph}");
            float rx = x + glyph.XOffset;
            float ry = y - glyph.YOffset - (glyph.Font.Capital * .5f);
            q.X0 = rx;
            q.Y1 = ry;
            q.X1 = rx + glyph.width;
            q.Y0 = ry - glyph.height;

            x += glyph.XAdvance * .1f * spacingFactor;

            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"x={x} y={y} Q={q}");
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

        private const int queueConsumptionFrame = 256;
        private byte framesBuffering = 0;

        public unsafe struct StringRenderingQueueItem
        {
            public FixedString512Bytes text;
        }

        private static IEnumerable Enumerate(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
            ;
        }
        public JobHandle RunJobs(JobHandle dependency)
        {
            if (!m_textCache.ContainsKey(""))
            {
                m_textCache[""] = new PrimitiveRenderInformation("", new Vector3[0], new int[0], new Vector2[0], null);
            }
            if (itemsQueue.Count >= queueConsumptionFrame || framesBuffering++ > 60)
            {
                framesBuffering = 0;
                if (itemsQueue.Count != 0)
                {
                    NativeArray<StringRenderingQueueItem> itemsStarted;
                    if (itemsQueue.Count > queueConsumptionFrame)
                    {
                        itemsStarted = new(queueConsumptionFrame, Allocator.TempJob);
                        for (int i = 0; i < queueConsumptionFrame; i++)
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
                    var charsToRender = itemsStarted.ToArray().SelectMany(x => Enumerate(StringInfo.GetTextElementEnumerator(x.text.ToString())).Cast<string>()).GroupBy(x => x).Select(x => x.Key);
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"charsToRender = ['{string.Join("', '", charsToRender)}']");

                    var countSucceeded = 0;
                    foreach (var charact in charsToRender)
                    {
                        var result = GetGlyph(glyphs, char.ConvertToUtf32(charact, 0), out bool hasReseted);
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
                    var job = new StringRenderingJob
                    {
                        fsd = dataPointer,
                        CurrentAtlasSize = new Vector3(CurrentAtlas.Width, CurrentAtlas.Height),
                        inputArray = itemsStarted.AsReadOnly(),
                        glyphs = GetGlyphsCollection(FontHeight),
                        output = results.AsParallelWriter(),
                        AtlasVersion = CurrentAtlas.Version,
                        scale = FontServer.Instance.ScaleEffective
                    };
                    dependency = job.Schedule(itemsStarted.Length, 32, dependency);
                    itemsStarted.Dispose(dependency);
                    dependency.Complete();
                }
            }
            var postJobCounter = 0;
            while (results.TryDequeue(out var result))
            {
                PostJob(result);
                if (++postJobCounter > queueConsumptionFrame)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Skipping next frame; strings yet to process at font {Name}: {results.Count}");
                    break;
                }
            }
            return dependency;
        }
    }
}