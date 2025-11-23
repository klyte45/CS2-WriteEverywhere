//#define JOBS_DEBUG
#if JOBS_DEBUG
using Belzont.Interfaces;
using Belzont.Utils;
#else
using Unity.Burst;
#endif
using BelzontWE.Font.Utility;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Collections.Unicode;
using Color = UnityEngine.Color;

namespace BelzontWE.Font
{
    public partial class FontSystem
    {
#if !JOBS_DEBUG && BURST
        [Unity.Burst.BurstCompile]
#endif
        private unsafe struct StringRenderingJob : IJobParallelForBatch
        {
            public readonly int Size => sizeof(StringRenderingJob);

            public NativeArray<int> TriangleIndicesCube;
            public NativeArray<Vector3> VerticesPositionsCube;
            public NativeArray<int> TriangleIndices;

            public NativeQueue<BasicRenderInformationJob>.ParallelWriter output;
            public NativeArray<StringRenderingQueueItem>.ReadOnly inputArray;

            public NativeHashMap<int, FontGlyph> glyphs;
            public Vector3 CurrentAtlasSize;
            public uint AtlasVersion;
            public Vector3 scale;
            public float ascent;
            public float descent;
            public float lineHeight;
            public float capital;
            public float fontScale;

            public void Execute(int idx, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    StringRenderingQueueItem input = inputArray[idx + i];
#if JOBS_DEBUG
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Rendering text {input.text} ");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"glyphs.length = {glyphs.Count}");
                    if (BasicIMod.DebugMode)
                    {
                        var kv = glyphs.GetKeyValueArrays(Allocator.Temp);
                        string[] output = new string[kv.Length];
                        for (int i = 0; i < kv.Length; i++)
                        {
                            output[i] = $"[{kv.Keys[i]}] = {kv.Values[i]}";
                        }
                        LogUtils.DoVerboseLog($"All Glyphs:\n{string.Join("\n", output)}");
                        kv.Dispose();
                    }
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"CurrentAtlasSize = {CurrentAtlasSize}");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Input = {input}");
                }
                var result =
#endif
                    DrawGlyphsForString(input.text);
                }
            }
            private NativeList<Rune> GetRunes(FixedString512Bytes text)
            {
                var runes = new NativeList<Rune>(Allocator.Persistent);
                var enumerator = text.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    runes.Add(enumerator.Current);
                }
                return runes;
            }

            private BasicRenderInformationJob DrawGlyphsForString(FixedString512Bytes strOr)
            {
                var result = new BasicRenderInformationJob
                {
                    invertUv = new Unity.Mathematics.bool2(false, true),
                    AtlasVersion = ~0u,
                    originalText = strOr,
                    m_YAxisOverflows = new RangeVector { min = float.MaxValue, max = float.MinValue }
                };
#if JOBS_DEBUG
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Result created");
#endif

                NativeList<Vector3> vertices;
                NativeList<Color32> colors;
                NativeList<Vector2> uvs;
                NativeList<int> triangles;
                NativeList<Vector3> verticesCube;
                NativeList<Color32> colorsCube;
                NativeList<Vector2> uvsCube;
                NativeList<int> trianglesCube;
                NativeList<Rune> str;

                str = GetRunes(strOr);                

                if (str.IsEmpty)
                {
                    output.Enqueue(result);
                    return result;
                }

                // Determine ascent and lineHeight from first character
                float ascent = 0, lineHeight = 0;


                var q = new FontGlyphBounds();

                float originX = 0.0f;
                float originY = 0.0f;

                vertices = new(Allocator.Temp);
                colors = new(Allocator.Temp);
                uvs = new(Allocator.Temp);
                triangles = new(Allocator.Temp);

                verticesCube = new(Allocator.Temp);
                colorsCube = new(Allocator.Temp);
                uvsCube = new(Allocator.Temp);
                trianglesCube = new(Allocator.Temp);


                FontGlyph prevGlyph = default;

                for (int i = 0; i < str.Length; i++)
                {
                    int codepoint = str[i].value;
#if JOBS_DEBUG
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[Main] codepoint #{i}: {codepoint}");
#endif
                    if (codepoint == '\n')
                    {
                        originX = 0.0f;
                        originY += lineHeight;
                        prevGlyph = default;
                        continue;
                    }

                    if (!glyphs.TryGetValue(codepoint, out var glyph))
                    {
#if JOBS_DEBUG
                        if (BasicIMod.DebugMode) LogUtils.DoWarnLog($"[Main] Glyph for codepoint {codepoint} not found returned [{glyph}], what is invalid.");
#endif
                        continue;
                    }

                    GetQuad(ref glyph, ref prevGlyph, 1, ref originX, ref originY, ref q);
                    result.m_YAxisOverflows.min = Mathf.Min(result.m_YAxisOverflows.min, glyph.YOffset - lineHeight + ascent);
                    result.m_YAxisOverflows.max = Mathf.Max(result.m_YAxisOverflows.max, glyph.height + glyph.YOffset - ascent);

                    q.X0 *= scale.x;
                    q.X1 *= scale.x;
                    q.Y0 *= scale.y;
                    q.Y1 *= scale.y;
                    var destRect = new Rect(q.X0, q.Y0, q.X1 - q.X0, q.Y1 - q.Y0);
#if JOBS_DEBUG
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"[Main] codepoint #{i}: destRect = {destRect} scale = {scale}");
#endif
                    DrawChar(glyph, vertices, triangles, uvs, colors, Color.black, Color.white, destRect);
                    DrawCharCube(glyph, verticesCube, trianglesCube, uvsCube, colorsCube, Color.black, Color.white, destRect);

                    prevGlyph = glyph;
                }
#if JOBS_DEBUG

                if (BasicIMod.DebugMode) LogUtils.DoLog($"vertices: {vertices.Length}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"triangles: {triangles.Length}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"uvs: {uvs.Length}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"colors: {colors.Length}");
#endif
                result.m_YAxisOverflows.min *= scale.y;
                result.m_YAxisOverflows.max *= scale.y;
                result.vertices = AlignVertices(vertices);
                result.colors = colors.ToArray(Allocator.Persistent);
                result.uv1 = uvs.ToArray(Allocator.Persistent);
                result.triangles = triangles.ToArray(Allocator.Persistent);

                result.m_fontBaseLimits = new RangeVector { min = descent, max = ascent };
                result.AtlasVersion = AtlasVersion;
#if JOBS_DEBUG

                if (BasicIMod.DebugMode) LogUtils.DoLog($"result.m_YAxisOverflows.min: {result.m_YAxisOverflows.min}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"result.m_YAxisOverflows.max: {result.m_YAxisOverflows.max}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"result.vertices: {result.vertices.Length}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"uvs: {uvs.Length}");
                if (BasicIMod.DebugMode) LogUtils.DoLog($"colors: {colors.Length}");
#endif
                result.Valid = true;
                output.Enqueue(result);
                if (vertices.IsCreated) vertices.Dispose();
                if (colors.IsCreated) colors.Dispose();
                if (uvs.IsCreated) uvs.Dispose();
                if (triangles.IsCreated) triangles.Dispose();
                if (verticesCube.IsCreated) verticesCube.Dispose();
                if (colorsCube.IsCreated) colorsCube.Dispose();
                if (uvsCube.IsCreated) uvsCube.Dispose();
                if (trianglesCube.IsCreated) trianglesCube.Dispose();
                if (str.IsCreated) str.Dispose();
                return result;
            }

            private void DrawChar(FontGlyph glyph, NativeList<Vector3> vertices, NativeList<int> triangles, NativeList<Vector2> uvs, NativeList<Color32> colors, Color overrideColor, Color bottomColor, Rect bounds)
            {
                AddTriangleIndices(triangles);
                vertices.Add(new Vector2(-bounds.xMax, bounds.yMin));
                vertices.Add(new Vector2(-bounds.xMin, bounds.yMin));
                vertices.Add(new Vector2(-bounds.xMin, bounds.yMax));
                vertices.Add(new Vector2(-bounds.xMax, bounds.yMax));
                Color32 item3 = overrideColor.linear;
                Color32 item4 = bottomColor.linear;
                colors.Add(item3);
                colors.Add(item3);
                colors.Add(item4);
                colors.Add(item4);
                AddUVCoords(uvs, glyph);
            }
            private void DrawCharCube(FontGlyph glyph, NativeList<Vector3> vertices, NativeList<int> triangles, NativeList<Vector2> uvs, NativeList<Color32> colors, Color overrideColor, Color bottomColor, Rect bounds)
            {
                AddTriangleIndicesCube(triangles);
                Color32 item3 = overrideColor.linear;
                Color32 item4 = bottomColor.linear;
                foreach (var vertex in VerticesPositionsCube)
                {
                    vertices.Add(new(vertex.x > 0 ? bounds.xMax : bounds.xMin, vertex.y * .5f, vertex.z < 0 ? bounds.yMax : bounds.yMin));
                    colors.Add(vertex.z < 0 ? item4 : item3);
                    uvs.Add(new Vector2((vertex.x > 0 ? glyph.xMax : glyph.xMin) / CurrentAtlasSize.x, (vertex.z > 0 ? glyph.yMax : glyph.yMin) / CurrentAtlasSize.y));
                }
            }


            private NativeArray<Vector3> AlignVertices(NativeList<Vector3> points)
            {
                if (!points.IsEmpty)
                {
                    float maxX = float.MinValue;
                    float minX = float.MaxValue;
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (points[i].x > maxX) maxX = points[i].x;
                        if (points[i].x < minX) minX = points[i].x;
                    }
                    var max = new Vector3(maxX, 0, 0);
                    var min = new Vector3(minX, 0, 0);
                    Vector3 offset = (max + min) / 2;

                    for (int i = 0; i < points.Length; i++)
                    {
                        var p = points[i];
                        p.x -= offset.x;
                        points[i] = p;
                    }
                }
                return points.ToArray(Allocator.Persistent);
            }

            private void AddUVCoords(NativeList<Vector2> uvs, FontGlyph glyph)
            {
#if JOBS_DEBUG
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"glyph ({glyph.IsValid})>  {glyph.xMin} {glyph.xMax} {glyph.yMin} {glyph.yMax}");
#endif
                uvs.Add(new Vector2(glyph.xMax / CurrentAtlasSize.x, glyph.yMax / CurrentAtlasSize.y));
                uvs.Add(new Vector2(glyph.xMin / CurrentAtlasSize.x, glyph.yMax / CurrentAtlasSize.y));
                uvs.Add(new Vector2(glyph.xMin / CurrentAtlasSize.x, glyph.yMin / CurrentAtlasSize.y));
                uvs.Add(new Vector2(glyph.xMax / CurrentAtlasSize.x, glyph.yMin / CurrentAtlasSize.y));
            }


            private void AddTriangleIndices(NativeList<int> triangles)
            {
                int count = triangles.Length * 2 / 3;
                for (int i = 0; i < TriangleIndices.Length; i++)
                {
                    triangles.Add(count + TriangleIndices[i]);
                }
            }
            private void AddTriangleIndicesCube(NativeList<int> triangles)
            {
                int verricesCount = triangles.Length * 2 / 3;
                for (int i = TriangleIndicesCube.Length - 1; i >= 0; i--)
                {
                    triangles.Add(verricesCount + TriangleIndicesCube[i]);
                }
            }


            private unsafe void GetQuad(ref FontGlyph glyph, ref FontGlyph prevGlyph, float spacingFactor, ref float x, ref float y, ref FontGlyphBounds q)
            {
                if (prevGlyph.IsValidSimple)
                {
                    float adv = prevGlyph.GetKerningCached(glyph) * fontScale;

                    x += (int)(((adv + 0) * spacingFactor) + 0.5f);
                }
#if JOBS_DEBUG
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"'{char.ConvertFromUtf32(glyph.Codepoint)}' = {glyph}");
#endif
                float rx = x + glyph.XOffset;
                float ry = y - glyph.YOffset - (capital * .5f);
                q.X0 = rx;
                q.Y1 = ry;
                q.X1 = rx + glyph.width;
                q.Y0 = ry - glyph.height;

                x += glyph.XAdvance * .1f * spacingFactor;
#if JOBS_DEBUG
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"x={x} y={y} Q={q}");
#endif
            }
        }
    }
}