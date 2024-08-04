//#define JOBS_DEBUG

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
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
    public class FontSystem : IDisposable
    {
        private FontSystemData data = new() { };
        public FontSystemData Data => data;

        public NativeHashMap<int, NativeHashMap<int, FontGlyph>> _glyphs;

        private FontAtlas _currentAtlas;
        private Vector2Int _size;
        public int FontHeight => FontServer.QualitySize;

        private readonly Dictionary<string, BasicRenderInformation> m_textCache = new();
        

        public Color Color;
        public readonly int Blur;
        public float Spacing;
        public bool UseKernings = true;
        public string Name => data.Name;

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
            this.data = data;
            _size = new(FontServer.DefaultTextureSizeFont, FontServer.DefaultTextureSizeFont);
            ClearState();
            itemsQueueWriter = itemsQueue.AsParallelWriter();
        }

        public void ClearState()
        {
            Color = Color.white;
            Spacing = 0;
        }


        public void EnsureText(string str, Vector3 scale, UIHorizontalAlignment alignment = UIHorizontalAlignment.Center)
        {
            if (str.TrimToNull() == null)
            {
                return;
            }
            if (!m_textCache.ContainsKey(str))
            {
                m_textCache[str] = default;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Enqueued String to ensure: {str} ({data.Name})");
                itemsQueue.Enqueue(new StringRenderingQueueItem() { text = str, scale = scale, alignment = alignment });
            }
        }

        public BasicRenderInformation DrawText(string str, Vector3 scale, UIHorizontalAlignment alignment = UIHorizontalAlignment.Center)
        {
            if (GameManager.instance.isGameLoading) return null;
            BasicRenderInformation bri;
            if (string.IsNullOrWhiteSpace(str))
            {
                if (!m_textCache.TryGetValue("", out bri))
                {
                    bri = new BasicRenderInformation(null, null, null)
                    {
                        m_refText = str
                    };
                    m_textCache.TryAdd("", bri);
                    return bri;
                }
                return bri;
            }
            if (m_textCache.TryGetValue(str, out bri))
            {
                return bri;
            }
            else
            {
                var result = m_textCache.TryAdd(str, default);
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Enqueued String: {str} ({data.Name}) {result}");
                itemsQueueWriter.Enqueue(new StringRenderingQueueItem() { text = str, scale = scale, alignment = alignment });
                if (BasicIMod.DebugMode) LogUtils.DoLog($"itemsQueue: {itemsQueue.Count}");
                return default;
            }
        }

        private static void AddTriangleIndices(IList<Vector3> verts, IList<int> triangles)
        {
            int count = verts.Count;
            int[] array = kTriangleIndices;
            for (int i = 0; i < array.Length; i++)
            {
                triangles.Add(count + array[i]);
            }
        }
        private static int[] kTriangleIndices = new int[]{
                0,
                1,
                3,
                3,
                1,
                2
        };


        public void Reset()
        {
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Resetting font cache for {Data.Name} => {FontServer.DefaultTextureSizeFont}");
            var width = FontServer.DefaultTextureSizeFont;
            var height = width;
            data.Font.RecalculateBasedOnHeight(FontServer.QualitySize);
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


        private static FontGlyph GetGlyphWithoutBitmap(NativeHashMap<int, FontGlyph> glyphs, int codepoint, ref FontSystemData data)
        {
            if (glyphs.TryGetValue(codepoint, out FontGlyph glyph))
            {
                return glyph;
            }

            int g = GetCodepointIndex(codepoint, data.Font);
            if (g == 0)
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
            FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint, ref data);
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

                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Resetting size to {_size}");
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
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Rendered glyph #{glyph} @ texture {CurrentAtlas.Texture}");

            glyph.AtlasGenerated = true;

            glyphs[codepoint] = glyph;

            return glyph;
        }

        private FontGlyph GetGlyph(NativeHashMap<int, FontGlyph> glyphs, int codepoint, out bool hasResetted, bool ignoreDefaultChar = false)
        {
            FontGlyph result = GetGlyphInternal(glyphs, codepoint, out hasResetted);
            if (!ignoreDefaultChar && result.Font is not null && DefaultCharacter != null)
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
            if (BasicIMod.DebugMode) LogUtils.DoLog($"'{char.ConvertFromUtf32(glyph.Codepoint)}' = {glyph}");
            float rx = x + glyph.XOffset;
            float ry = y - glyph.YOffset - (glyph.Font.Capital * .5f);
            q.X0 = rx;
            q.Y1 = ry;
            q.X1 = rx + glyph.width;
            q.Y0 = ry - glyph.height;

            x += glyph.XAdvance * .1f * spacingFactor;

            if (BasicIMod.DebugMode) LogUtils.DoLog($"x={x} y={y} Q={q}");
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
            if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] Post job for {originalText} ");
            if (brij.AtlasVersion != CurrentAtlas.Version)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] removing {originalText} since atlas changed");
                m_textCache.Remove(originalText);
                return;
            }
            BasicRenderInformation result = BasicRenderInformation.Fill(brij, CurrentAtlas.Material);
            result.m_refY = data.Font.Ascent;
            result.m_baselineOffset = -data.Font.Descent;
            result.m_materialGeneratedTick = LastUpdateAtlas;
            result.m_refText = originalText;

            if (m_textCache.TryGetValue(originalText, out var currentVal) && currentVal == null)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] SET UP to val '{originalText}'");
                m_textCache[originalText] = result;
            }
            else
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] REMOVING '{originalText}'");
                m_textCache.Remove(originalText);
            }
            if (!CurrentAtlas.UpdateMaterial())
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Failed updating material... Restarting process");
                m_textCache.Clear();
                _glyphs.Clear();
                CurrentAtlas.Reset(_size.x, _size.y);
            }
        }

        public void Dispose()
        {
            data.Dispose();
            _glyphs.Dispose();
            itemsQueue.Dispose();
            results.Dispose();
        }

        public unsafe struct StringRenderingQueueItem
        {
            public FixedString512Bytes text;
            public Vector3 scale;
            public UIHorizontalAlignment alignment;
        }

        private static IEnumerable Enumerate(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            };
        }
        public JobHandle RunJobs(JobHandle dependency)
        {
            if (itemsQueue.Count != 0)
            {
                NativeArray<StringRenderingQueueItem> itemsStarted = itemsQueue.ToArray(Allocator.TempJob);
                itemsQueue.Clear();
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
                    data = data,
                    CurrentAtlasSize = new Vector3(CurrentAtlas.Width, CurrentAtlas.Height),
                    inputArray = itemsStarted.AsReadOnly(),
                    glyphs = GetGlyphsCollection(FontHeight),
                    output = results.AsParallelWriter(),
                    AtlasVersion = CurrentAtlas.Version,
                };
                dependency = job.Schedule(itemsStarted.Length, 32, dependency);
                itemsStarted.Dispose(dependency);
            }
            while (results.TryDequeue(out var result))
            {
                PostJob(result);
            }
            return dependency;
        }

        private unsafe struct StringRenderingJob : IJobParallelFor
        {
            public int Size => sizeof(StringRenderingJob);
            public int Size2 => sizeof(FontSystemData);


            public NativeQueue<BasicRenderInformationJob>.ParallelWriter output;
            public NativeArray<StringRenderingQueueItem>.ReadOnly inputArray;

            public FontSystemData data;
            public NativeHashMap<int, FontGlyph> glyphs;
            public Vector3 CurrentAtlasSize;
            public uint AtlasVersion;

            public void Execute(int idx)
            {
                StringRenderingQueueItem input = inputArray[idx];

#if JOBS_DEBUG
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Rendering text {input.text} ");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"data = {data}");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"glyphs.length = {glyphs.Count}");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"CurrentAtlasSize = {CurrentAtlasSize}");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Input = {input}");
                }
#endif
                DrawGlyphsForString(input.text, input.scale, input.alignment);

#if JOBS_DEBUG
                LogUtils.DoLog($"Result tris {result[0].triangles.Length} ");
#endif
            }

            private void DrawGlyphsForString(FixedString512Bytes strOr, Vector3 scale, UIHorizontalAlignment alignment)
            {
                var result = new BasicRenderInformationJob();
                result.m_YAxisOverflows = new RangeVector { min = float.MaxValue, max = float.MinValue };
#if JOBS_DEBUG
                LogUtils.DoLog($"Result created");
#endif

                var str = ToFilteredString(strOr.ConvertToString());
                if (string.IsNullOrEmpty(str))
                {
                    return;
                }

                // Determine ascent and lineHeight from first character
                float ascent = 0, lineHeight = 0;
                for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
                {
                    int codepoint = char.ConvertToUtf32(str, i);
#if JOBS_DEBUG
                LogUtils.DoLog($"codepoint #{i}: {codepoint}");
#endif
                    FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint, ref data);
                    if (!glyph.IsValid)
                    {
                        continue;
                    }

                    ascent = glyph.Font.Ascent;
                    lineHeight = glyph.Font.LineHeight;
                    break;
                }

                var q = new FontGlyphBounds();

                float originX = 0.0f;
                float originY = 0.0f;

                try
                {
                    IList<Vector3> vertices = new List<Vector3>();
                    IList<Color32> colors = new List<Color32>();
                    IList<Vector2> uvs = new List<Vector2>();
                    IList<int> triangles = new List<int>();


                    FontGlyph prevGlyph = default;

                    for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
                    {
                        int codepoint = char.ConvertToUtf32(str, i);
#if JOBS_DEBUG
                 LogUtils.DoLog($"[Main] codepoint #{i}: {codepoint}");
#endif
                        if (codepoint == '\n')
                        {
                            originX = 0.0f;
                            originY += lineHeight;
                            prevGlyph = default;
                            continue;
                        }

                        FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint, ref data);
                        if (!glyph.IsValid)
                        {
                            continue;
                        }

                        GetQuad(ref glyph, ref prevGlyph, 1, ref originX, ref originY, ref q);
                        result.m_YAxisOverflows.min = Mathf.Min(result.m_YAxisOverflows.min, glyph.YOffset - glyph.Font.LineHeight + glyph.Font.Ascent);
                        result.m_YAxisOverflows.max = Mathf.Max(result.m_YAxisOverflows.max, glyph.height + glyph.YOffset - glyph.Font.Ascent);

                        q.X0 *= scale.x;
                        q.X1 *= scale.x;
                        q.Y0 *= scale.y;
                        q.Y1 *= scale.y;
                        var destRect = new Rect(q.X0, q.Y0, q.X1 - q.X0, q.Y1 - q.Y0);
#if JOBS_DEBUG
                LogUtils.DoLog($"[Main] codepoint #{i}: destRect = {destRect} scale = {scale}");
#endif
                        DrawChar(glyph, vertices, triangles, uvs, colors, Color.black, Color.white, destRect);

                        prevGlyph = glyph;
                    }
#if JOBS_DEBUG
                
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"vertices: {vertices.Count}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"triangles: {triangles.Count}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"uvs: {uvs.Count}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"colors: {colors.Count}");
#endif
                    result.m_YAxisOverflows.min *= scale.y;
                    result.m_YAxisOverflows.max *= scale.y;
                    result.vertices = new NativeArray<Vector3>(AlignVertices(vertices, alignment), Allocator.Persistent);
                    result.colors = new NativeArray<Color32>(colors.ToArray(), Allocator.Persistent);
                    result.uv1 = new(uvs.ToArray(), Allocator.Persistent);
                    result.triangles = new(triangles.ToArray(), Allocator.Persistent);
                    result.m_fontBaseLimits = new RangeVector { min = prevGlyph.Font.Descent, max = prevGlyph.Font.Ascent };
                    result.AtlasVersion = AtlasVersion;
                    result.originalText = strOr;
#if JOBS_DEBUG
                
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"result.m_YAxisOverflows.min: {result.m_YAxisOverflows.min}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"result.m_YAxisOverflows.max: {result.m_YAxisOverflows.max}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"result.vertices: {result.vertices.Count()}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"uvs: {uvs.Count}");
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"colors: {colors.Count}");
#endif
                    output.Enqueue(result);
                }
                finally
                {
                }

            }

            private void DrawChar(FontGlyph glyph, IList<Vector3> vertices, IList<int> triangles, IList<Vector2> uvs, IList<Color32> colors, Color overrideColor, Color bottomColor, Rect bounds)
            {
                AddTriangleIndices(vertices, triangles);
                vertices.Add(new Vector2(bounds.xMax, bounds.yMin));
                vertices.Add(new Vector2(bounds.xMin, bounds.yMin));
                vertices.Add(new Vector2(bounds.xMin, bounds.yMax));
                vertices.Add(new Vector2(bounds.xMax, bounds.yMax));
                Color32 item3 = overrideColor.linear;
                Color32 item4 = bottomColor.linear;
                colors.Add(item3);
                colors.Add(item3);
                colors.Add(item4);
                colors.Add(item4);
                AddUVCoords(uvs, glyph);
            }
            private Vector3[] AlignVertices(IList<Vector3> points, UIHorizontalAlignment alignment)
            {
                if (points.Count == 0)
                {
                    return points.ToArray();
                }

                var max = new Vector3(points.Select(x => x.x).Max(), 0, points.Select(x => x.z).Max());
                var min = new Vector3(points.Select(x => x.x).Min(), 0, points.Select(x => x.z).Min());
                Vector3 offset = default;
                switch (alignment)
                {
                    case UIHorizontalAlignment.Left:
                        offset = min;
                        break;
                    case UIHorizontalAlignment.Center:
                        offset = (max + min) / 2;
                        break;
                    case UIHorizontalAlignment.Right:
                        offset = max;
                        break;
                }

                return points.Select(x => x - offset).ToArray();
            }

            private void AddUVCoords(IList<Vector2> uvs, FontGlyph glyph)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"glyph ({glyph.IsValid})>  {glyph.xMin} {glyph.xMax} {glyph.yMin} {glyph.yMax}");
                uvs.Add(new Vector2(glyph.xMax / CurrentAtlasSize.x, glyph.yMax / CurrentAtlasSize.y));
                uvs.Add(new Vector2(glyph.xMin / CurrentAtlasSize.x, glyph.yMax / CurrentAtlasSize.y));
                uvs.Add(new Vector2(glyph.xMin / CurrentAtlasSize.x, glyph.yMin / CurrentAtlasSize.y));
                uvs.Add(new Vector2(glyph.xMax / CurrentAtlasSize.x, glyph.yMin / CurrentAtlasSize.y));
            }

            private string ToFilteredString(string str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                var result = "";
                for (int i = 0; i < str.Length; i++)
                {
                    var code = char.ConvertToUtf32(str, i);
                    var glyph = GetGlyphWithoutBitmap(glyphs, code, ref data);
                    if (!glyph.IsValid)
                    {
                        if (!char.ConvertFromUtf32(code).IsNormalized(System.Text.NormalizationForm.FormKD))
                        {
                            var normalizedStr = char.ConvertFromUtf32(code).Normalize(System.Text.NormalizationForm.FormKD);
                            for (int j = 0; j < normalizedStr.Length; j++)
                            {
                                var codeJ = normalizedStr[j];
                                var glyphJ = GetGlyphWithoutBitmap(glyphs, codeJ, ref data);
                                if (glyphJ.IsValid)
                                {
                                    result += normalizedStr[j];
                                }
                            }
                        }
                        if (char.IsHighSurrogate(str[i]))
                        {
                            i++;
                        }
                    }
                    else
                    {
                        result += str[i];
                        if (char.IsHighSurrogate(str[i]))
                        {
                            result += str[++i];
                        }
                    }
                }
                return result.Trim();
            }
        }
    }
}