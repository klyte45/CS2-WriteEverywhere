//#define JOBS_DEBUG

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Serialization.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
        public struct FontSystemData : IDisposable, IComponentData, ISerializable
        {
            private const uint CURRENT_VERSION = 0;

            private GCHandle _fontAddr;
            public Font Font
            {
                get => _fontAddr.IsAllocated ? _fontAddr.Target as Font : null;
                set
                {
                    if (_fontAddr.IsAllocated) _fontAddr.Free();
                    _fontAddr = GCHandle.Alloc(value);
                }
            }
            public float _itw { get; private set; }
            public float _ith { get; private set; }

            public int Width
            {
                get => width; set
                {
                    width = math.max(1, value);
                    _itw = 1f / width;
                }
            }
            public int Height
            {
                get => height; set
                {
                    height = math.max(1, value);
                    _ith = 1f / height;
                }
            }

            public FixedString32Bytes name;
            private int width;
            private int height;

            public void Dispose()
            {
                if (_fontAddr.IsAllocated)
                {
                    _fontAddr.Free();
                }
            }

            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(name);
                writer.Write(width);
                writer.Write(height);
                var zippedFontFile = new NativeArray<byte>(ZipUtils.ZipBytes(Font._font.data.ArrayData), Allocator.Temp);
                writer.Write(zippedFontFile.Length);
                writer.Write(zippedFontFile);
                zippedFontFile.Dispose();
            }

            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out uint version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out name);
                reader.Read(out width); Width = width;
                reader.Read(out height); Height = height;
                reader.Read(out int length);
                var zippedFontFile = new NativeArray<byte>(length, Allocator.Temp);
                try
                {
                    reader.Read(zippedFontFile);
                    Font = Font.FromMemory(ZipUtils.UnzipBytes(zippedFontFile.ToArray()));
                }
                finally
                {
                    zippedFontFile.Dispose();
                }
            }
        }

        private FontSystemData data = new() { };

        public NativeHashMap<int, NativeHashMap<int, FontGlyph>> _glyphs;


        private FontAtlas _currentAtlas;
        private Vector2Int _size;
        private int _fontHeight;

        private Dictionary<string, BasicRenderInformation> m_textCache = new Dictionary<string, BasicRenderInformation>();

        public static int MaxCoroutines = 5;//new SavedInt("WTS_MaxFontWordsCoroutinesRunning", Settings.gameSettingsFile, 8, true);

        internal long GetCacheSize()
        {
            long size = 0;
            foreach (var bri in m_textCache.Values)
            {
                size += bri.GetSize();
            }

            return size;
        }
        private bool metricsCalculated = false;
        private float qualityMultiplier;
        public float ReferenceHeight { get; private set; }
        public float BaselineOffset { get; private set; }

        public int FontHeight
        {
            get => _fontHeight;
            set
            {
                _fontHeight = value;
                if (data.Font is not null)
                {
                    data.Font.RecalculateBasedOnHeight(_fontHeight);
                }
            }
        }

        public Color Color;
        public readonly int Blur;
        public float Spacing;
        public bool UseKernings = true;
        private string Name => data.name.ToString();

        public long LastUpdateAtlas { get; private set; }

        public int? DefaultCharacter = ' ';

        public FontAtlas CurrentAtlas
        {
            get
            {
                if (_currentAtlas == null)
                {
                    _currentAtlas = new FontAtlas(Mathf.RoundToInt(_size.x), Mathf.RoundToInt(_size.y), 256);
                    Atlases.Add(_currentAtlas);
                    LastUpdateAtlas = DateTime.Now.Ticks;
                }

                return _currentAtlas;
            }
        }

        public List<FontAtlas> Atlases { get; } = new List<FontAtlas>();

        public event Action CurrentAtlasFull;

        public FontSystem(string name, int width, int height, int blur = 0)
        {
            data.name = name;
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (blur < 0 || blur > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(blur));
            }
            Blur = blur;

            _size = new(width, height);

            data.Width = width;
            data.Height = height;
            ClearState();
        }

        public FontSystem(FontSystemData data)
        {
            Blur = 0;
            if (data.Width <= 0)
            {
                throw new ArgumentOutOfRangeException("data.Width");
            }

            if (data.Height <= 0)
            {
                throw new ArgumentOutOfRangeException("data.Height");
            }
            _size = new(data.Width, data.Height);
            ClearState();
        }

        public void ClearState()
        {
            FontHeight = 100;
            Color = Color.white;
            Spacing = 0;
        }

        public void AddFontMem(byte[] fontData, float qualityMultiplier)
        {
            var font = Font.FromMemory(fontData);
            font.RecalculateBasedOnHeight(FontHeight);
            data.Font = font;
            this.qualityMultiplier = qualityMultiplier;
            metricsCalculated = false;
        }

        private IEnumerator CalculateMetrics()
        {
            yield return 0;
            var bounds = new Bounds();
            TextBounds(0, 0, "A", 1, ref bounds);
            yield return 0;
            ReferenceHeight = (bounds.maxY - bounds.minY) / qualityMultiplier;
            BaselineOffset = bounds.minY / ReferenceHeight;
            metricsCalculated = true;
        }



        public void EnsureText(float x, float y, string str, Vector3 scale, UIHorizontalAlignment alignment = UIHorizontalAlignment.Center)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }
            if (!m_textCache.ContainsKey(str))
            {
                m_textCache[str] = default;
                itemsQueue.Enqueue(new StringRenderingQueueItem() { x = x, y = y, text = str, scale = scale, alignment = alignment });
            }
        }
        public BasicRenderInformation DrawText(float x, float y, string str, Vector3 scale, UIHorizontalAlignment alignment = UIHorizontalAlignment.Center)
        {
            BasicRenderInformation bri;
            if (string.IsNullOrWhiteSpace(str))
            {
                if (!m_textCache.TryGetValue("", out bri))
                {
                    bri = new BasicRenderInformation
                    {
                        m_refText = str
                    };
                    m_textCache[""] = bri;
                }
                return bri;
            }
            if (m_textCache.TryGetValue(str, out bri))
            {
                return bri;
            }
            else
            {
                m_textCache[str] = default;
                itemsQueue.Enqueue(new StringRenderingQueueItem() { x = x, y = y, text = str, scale = scale, alignment = alignment });
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

        public float TextBounds(float x, float y, string str, float charSpacingFactor, ref Bounds bounds)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0.0f;
            }

            var glyphs = GetGlyphsCollection(FontHeight);

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);
                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);
                if (!glyph.IsValid)
                {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }


            var q = new FontGlyphSquad();
            float startx = 0;
            float advance = 0;

            y += ascent;

            float minx, maxx, miny, maxy;
            minx = maxx = x;
            miny = maxy = y;
            startx = x;

            FontGlyph prevGlyph = default;

            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

                if (codepoint == '\n')
                {
                    x = startx;
                    y += lineHeight;

                    continue;
                }

                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);

                if (!glyph.IsValid)
                {
                    continue;
                }

                GetQuad(ref glyph, ref prevGlyph, i == str.Length - 1 ? 1f : charSpacingFactor, ref x, ref y, ref q, ref data);
                if (q.X0 < minx)
                {
                    minx = q.X0;
                }

                if (x > maxx)
                {
                    maxx = x;
                }

                if (q.Y0 < miny)
                {
                    miny = q.Y0;
                }

                if (q.Y1 > maxy)
                {
                    maxy = q.Y1;
                }

                prevGlyph = glyph;
            }

            advance = x - startx;

            bounds.minX = minx;
            bounds.minY = miny;
            bounds.maxX = maxx;
            bounds.maxY = maxy;

            return advance;
        }

        public void Reset(int width, int height)
        {
            foreach (var atlas in Atlases)
            {
                atlas.Reset(width, height);
            }

            if (_glyphs.IsCreated) _glyphs.Clear();

            m_textCache.Clear();

            if (width == _size.x && height == _size.y)
            {
                return;
            }

            _size = new(width, height);
            data.Width = width;
            data.Height = height;
        }

        public void Reset() => Reset(_size.x, _size.y);

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
                Height = default,
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

            FontAtlas currentAtlas = CurrentAtlas;
            int gx = 0, gy = 0;
            int gw = Mathf.RoundToInt(glyph.width);
            int gh = Mathf.RoundToInt(glyph.height);
            if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy))
            {
                CurrentAtlasFull?.Invoke();
                do
                {
                    // This code will force creation of new atlas with 4x size
                    _currentAtlas = null;
                    if (_size.x * _size.y < 8192 * 8192)
                    {
                        _size *= 2;
                    }
                    else
                    {
                        throw new Exception(string.Format("Could not add rect to the newly created atlas. gw={0}, gh={1} - MAP REACHED 8K * 8K LIMIT!", gw, gh));
                    }
                    glyphs.Clear();
                    glyphs[codepoint] = glyph;

                    currentAtlas = CurrentAtlas;
                    m_textCache.Clear();

                    hasResetted = true;
                    // Try to add again
                } while (!currentAtlas.AddRect(gw, gh, ref gx, ref gy));
            }

            glyph.x = gx;
            glyph.y = gy;

            currentAtlas.RenderGlyph(glyph);

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

        private unsafe static void GetQuad(ref FontGlyph glyph, ref FontGlyph prevGlyph, float spacingFactor, ref float x, ref float y, ref FontGlyphSquad q, ref FontSystemData data)
        {
            if (prevGlyph.IsValid)
            {
                float adv = 0;
                if (true)
                {
                    adv = prevGlyph.GetKerning(glyph) * glyph.Font.Scale;
                }
                x += (int)((adv + 0) * spacingFactor + 0.5f);
            }

            float rx = x + glyph.XOffset;
            float ry = y + glyph.YOffset;
            q.X0 = rx;
            q.Y0 = ry;
            q.X1 = rx + glyph.width;
            q.Y1 = ry + glyph.height;
            q.S0 = glyph.x * data._itw;
            q.T0 = glyph.y * data._ith;
            q.S1 = glyph.xMax * data._itw;
            q.T1 = glyph.yMax * data._ith;

            x += (int)((glyph.XAdvance / 10.0f * spacingFactor) + 0.5f);
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
        public Texture2D WriteTexture2D(string str, float charSpacingFactor)
        {
            if (charSpacingFactor < 0)
            {
                charSpacingFactor = 0;
            }

            var glyphs = GetGlyphsCollection(FontHeight);
            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);
                if (!glyph.IsValid)
                {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }

            var q = new FontGlyphSquad();

            float originX = 0.0f;
            float originY = 0.0f;

            originY += ascent;

            var bounds = new Bounds();
            TextBounds(0, 0, str, charSpacingFactor, ref bounds);

            var targetTexture = new Texture2D(Mathf.CeilToInt((bounds.maxX - bounds.minX)), Mathf.CeilToInt((bounds.maxY - bounds.minY)), TextureFormat.ARGB32, false);
            targetTexture.SetPixels(new Color[targetTexture.width * targetTexture.height]);

            FontGlyph prevGlyph = default;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

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

                GetQuad(ref glyph, ref prevGlyph, charSpacingFactor, ref originX, ref originY, ref q, ref data);

                q.X0 = (int)(q.X0);
                q.X1 = (int)(q.X1);
                q.Y0 = (int)(q.Y0);
                q.Y1 = (int)(q.Y1);


                Color[] arr = CurrentAtlas.GetGlyphColors(glyph);

                MergeTextures(tex: targetTexture,
                              colors: arr,
                              startX: Mathf.RoundToInt(q.X0 - bounds.minX),
                              startY: Mathf.RoundToInt(bounds.maxY - q.Y1),
                              sizeX: (int)(q.X1 - q.X0),
                              sizeY: (int)(q.Y1 - q.Y0),
                              swapXY: false,
                              flipVertical: true);

                prevGlyph = glyph;
            }

            targetTexture.Apply();

            return targetTexture;
        }
        internal static void MergeTextures(Texture2D tex, Color[] colors, int startX, int startY, int sizeX, int sizeY, bool swapXY = false, bool flipVertical = false, bool flipHorizontal = false, bool plain = false)
        {
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    Color orPixel = tex.GetPixel(startX + i, startY + j);
                    Color newPixel = colors[((flipVertical ? sizeY - j - 1 : j) * (swapXY ? 1 : sizeX)) + ((flipHorizontal ? sizeX - i - 1 : i) * (swapXY ? sizeY : 1))];

                    if (plain && newPixel.a != 1)
                    {
                        continue;
                    }

                    tex.SetPixel(startX + i, startY + j, Color.Lerp(orPixel, newPixel, newPixel.a));
                }
            }
        }




        private void PrepareJob(ref StringRenderingJob job, StringRenderingQueueItem item)
        {
            if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] PrepareJob for {item.text}");
            job.data = data;
            job.CurrentAtlasSize = new Vector3(_currentAtlas.Width, _currentAtlas.Height);
            job.input = item;
            job.glyphs = GetGlyphsCollection(FontHeight);
            job.result = new NativeArray<BasicRenderInformationJob>(1, Allocator.Persistent);
        }

        private void PostJob(StringRenderingJob jobResult)
        {

            var originalText = jobResult.input.text.ToString();
            if (BasicIMod.DebugMode) LogUtils.DoLog($"[FontSystem: {Name}] Post job for {originalText} ");
            if (jobResult.CurrentAtlasSize.x != _currentAtlas.Width)
            {
                m_textCache.Remove(originalText);
            }
            if (!metricsCalculated)
            {
                CalculateMetrics();
            }
            BasicRenderInformation result = new();
            result.Fill(jobResult.result[0], _currentAtlas.Material);
            result.m_refY = ReferenceHeight;
            result.m_baselineOffset = BaselineOffset;
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

        }

        public void Dispose()
        {
            data.Dispose();
            _glyphs.Dispose();
        }

        public unsafe struct StringRenderingQueueItem
        {
            public float x;
            public float y;
            public FixedString512Bytes text;
            public Vector3 scale;
            public UIHorizontalAlignment alignment;
        }

        private Queue<StringRenderingQueueItem> itemsQueue = new Queue<StringRenderingQueueItem>();
        private Dictionary<StringRenderingJob, JobHandle> runningJobs = new();

        private static IEnumerable Enumerate(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            };
        }
        public void RunJobs()
        {
            if (itemsQueue.Count != 0)
            {
                List<StringRenderingQueueItem> itemsStarted = itemsQueue.ToList();
                itemsQueue.Clear();
                var glyphs = GetGlyphsCollection(FontHeight);
                var charsToRender = itemsStarted.SelectMany(x => Enumerate(StringInfo.GetTextElementEnumerator(x.text.ToString())).Cast<string>()).GroupBy(x => x).Select(x => x.Key);
                while (charsToRender.Any(x => GetGlyph(glyphs, char.ConvertToUtf32(x, 0), out bool hasReseted).IsValid && hasReseted))
                {
                    LogUtils.DoInfoLog($"[FontSystem: {Name}] Reset texture! (Now {CurrentAtlas.Texture.width})");
                    m_textCache.Clear();
                }
                CurrentAtlas.UpdateMaterial();
                foreach (var item in itemsStarted)
                {
                    var job = new StringRenderingJob();
                    PrepareJob(ref job, item);
                    runningJobs[job] = job.Schedule();
                }
            }
            if (runningJobs.Count > 0)
            {
                var originalList = runningJobs.ToArray();
                foreach (var jobRan in originalList)
                {
                    if (jobRan.Value.IsCompleted)
                    {
                        PostJob(jobRan.Key);
                        runningJobs.Remove(jobRan.Key);
                    }
                }
            }
        }

        private unsafe struct StringRenderingJob : IJob
        {
            public int Size => sizeof(StringRenderingJob);
            public int Size2 => sizeof(FontSystemData);

            public StringRenderingQueueItem input;
            public NativeArray<BasicRenderInformationJob> result;

            public FontSystemData data;
            public NativeHashMap<int, FontGlyph> glyphs;
            public Vector3 CurrentAtlasSize;
#if JOBS_DEBUG
            const bool debug = true;
#else
            const bool debug = false;
#endif

            public void Execute()
            {
                if (debug)
                {
                    LogUtils.DoLog($"Rendering text {input.text} ");
                    LogUtils.DoLog($"data = {data}");
                    LogUtils.DoLog($"glyphs.length = {glyphs.Count}");
                    LogUtils.DoLog($"CurrentAtlasSize = {CurrentAtlasSize}");
                    LogUtils.DoLog($"Input = {input}");
                }
                WriteTextureCoroutine(input.x, input.y, input.text, input.scale, input.alignment);
                if (debug) LogUtils.DoLog($"Result tris {result[0].triangles.Length} ");
            }

            private void WriteTextureCoroutine(float x, float y, FixedString512Bytes strOr, Vector3 scale, UIHorizontalAlignment alignment)
            {
                var result = new BasicRenderInformationJob();
                result.m_YAxisOverflows = new RangeVector { min = float.MaxValue, max = float.MinValue };

                if (debug) LogUtils.DoLog($"Result created");

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

                    if (debug) LogUtils.DoLog($"codepoint #{i}: {codepoint}");
                    FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint, ref data);
                    if (!glyph.IsValid)
                    {
                        continue;
                    }

                    ascent = glyph.Font.Ascent;
                    lineHeight = glyph.Font.LineHeight;
                    break;
                }

                var q = new FontGlyphSquad();

                float originX = 0.0f;
                float originY = 0.0f;

                originY += ascent;

                try
                {
                    IList<Vector3> vertices = new List<Vector3>();
                    IList<Vector3> normals = new List<Vector3>();
                    IList<Color32> colors = new List<Color32>();
                    IList<Vector2> uvs = new List<Vector2>();
                    IList<int> triangles = new List<int>();


                    FontGlyph prevGlyph = default;
                    for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
                    {
                        int codepoint = char.ConvertToUtf32(str, i);

                        if (debug) LogUtils.DoLog($"[Main] codepoint #{i}: {codepoint}");
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

                        GetQuad(ref glyph, ref prevGlyph, 1, ref originX, ref originY, ref q, ref data);
                        result.m_YAxisOverflows.min = Mathf.Min(result.m_YAxisOverflows.min, glyph.YOffset - glyph.Font.Descent + glyph.Font.Ascent);
                        result.m_YAxisOverflows.max = Mathf.Max(result.m_YAxisOverflows.max, glyph.height + glyph.YOffset - glyph.Font.Ascent + glyph.Font.Descent);

                        q.X0 = (int)(q.X0 * scale.x);
                        q.X1 = (int)(q.X1 * scale.x);
                        q.Y0 = (int)(q.Y0 * scale.y);
                        q.Y1 = (int)(q.Y1 * scale.y);

                        var destRect = new Rect((int)(x + q.X0),
                                                    (int)(y + q.Y0),
                                                    (int)(q.X1 - q.X0),
                                                    (int)(q.Y1 - q.Y0));

                        if (debug) LogUtils.DoLog($"[Main] codepoint #{i}: destRect = {destRect}");
                        DrawChar(glyph, vertices, triangles, uvs, colors, Color.black, Color.white, destRect);

                        prevGlyph = glyph;
                    }
                    if (debug)
                    {
                        LogUtils.DoLog($"vertices: {vertices.Count}");
                        LogUtils.DoLog($"normals: {normals.Count}");
                        LogUtils.DoLog($"triangles: {triangles.Count}");
                        LogUtils.DoLog($"uvs: {uvs.Count}");
                        LogUtils.DoLog($"colors: {colors.Count}");
                    }
                    result.m_YAxisOverflows.min *= scale.y;
                    result.m_YAxisOverflows.max *= scale.y;
                    result.vertices = new NativeArray<Vector3>(AlignVertices(vertices, alignment), Allocator.Persistent);
                    result.colors = new NativeArray<Color32>(colors.ToArray(), Allocator.Persistent);
                    result.uv1 = new(uvs.ToArray(), Allocator.Persistent);
                    result.triangles = new(triangles.ToArray(), Allocator.Persistent);
                    result.m_fontBaseLimits = new RangeVector { min = prevGlyph.Font.Descent, max = prevGlyph.Font.Ascent };
                    if (debug)
                    {
                        LogUtils.DoLog($"result.m_YAxisOverflows.min: {result.m_YAxisOverflows.min}");
                        LogUtils.DoLog($"result.m_YAxisOverflows.max: {result.m_YAxisOverflows.max}");
                        LogUtils.DoLog($"result.vertices: {result.vertices.Count()}");
                        LogUtils.DoLog($"uvs: {uvs.Count}");
                        LogUtils.DoLog($"colors: {colors.Count}");
                    }
                    this.result[0] = result;
                }
                finally
                {
                }

            }

            private void DrawChar(FontGlyph glyph, IList<Vector3> vertices, IList<int> triangles, IList<Vector2> uvs, IList<Color32> colors, Color overrideColor, Color bottomColor, Rect bounds)
            {
                AddTriangleIndices(vertices, triangles);
                vertices.Add(new Vector2(bounds.xMax, 1 - bounds.yMax));
                vertices.Add(new Vector2(bounds.xMin, 1 - bounds.yMax));
                vertices.Add(new Vector2(bounds.xMin, 1 - bounds.yMin));
                vertices.Add(new Vector2(bounds.xMax, 1 - bounds.yMin));
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

                var max = new Vector3(points.Select(x => x.x).Max(), points.Select(x => x.y).Max(), points.Select(x => x.z).Max());
                var min = new Vector3(points.Select(x => x.x).Min(), points.Select(x => x.y).Min(), points.Select(x => x.z).Min());
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
                LogUtils.DoLog($"glyph ({glyph.IsValid})>  {glyph.xMin} {glyph.xMax} {glyph.yMin} {glyph.yMax}");
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