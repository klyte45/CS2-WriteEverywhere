//#define JOBS_DEBUG

using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BelzontWE.Font
{
    public partial class FontSystem
    {
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
            public Vector3 scale;

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
                DrawGlyphsForString(input.text);

#if JOBS_DEBUG
                LogUtils.DoLog($"Result tris {result[0].triangles.Length} ");
#endif
            }

            private void DrawGlyphsForString(FixedString512Bytes strOr)
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
                    result.vertices = new NativeArray<Vector3>(AlignVertices(vertices), Allocator.Persistent);
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
            private Vector3[] AlignVertices(IList<Vector3> points)
            {
                if (points.Count == 0)
                {
                    return points.ToArray();
                }

                var max = new Vector3(points.Select(x => x.x).Max(), 0, points.Select(x => x.z).Max());
                var min = new Vector3(points.Select(x => x.x).Min(), 0, points.Select(x => x.z).Min());
                Vector3 offset = (max + min) / 2;

                return points.Select(x => x - offset).ToArray();
            }

            private void AddUVCoords(IList<Vector2> uvs, FontGlyph glyph)
            {
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"glyph ({glyph.IsValid})>  {glyph.xMin} {glyph.xMax} {glyph.yMin} {glyph.yMax}");
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