﻿using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BelzontWE.Font
{
    public class FontAtlas : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int NodesNumber { get; private set; }
        public FontAtlasNode[] Nodes { get; private set; }
        public Texture2D Texture { get; set; }
        public uint Version { get; private set; }

        public bool IsPendingApply { get; private set; }

        public FontAtlas(int w, int h, int count)
        {
            Width = w;
            Height = h;
            Nodes = new FontAtlasNode[count];
            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = w;
            NodesNumber++;
        }

        public void InsertNode(int idx, int x, int y, int w)
        {
            if (NodesNumber + 1 > Nodes.Length)
            {
                FontAtlasNode[] oldNodes = Nodes;
                int newLength = Nodes.Length == 0 ? 8 : Nodes.Length * 2;
                Nodes = new FontAtlasNode[newLength];
                for (int i = 0; i < oldNodes.Length; ++i)
                {
                    Nodes[i] = oldNodes[i];
                }
            }

            for (int i = NodesNumber; i > idx; i--)
            {
                Nodes[i] = Nodes[i - 1];
            }

            Nodes[idx].X = x;
            Nodes[idx].Y = y;
            Nodes[idx].Width = w;
            NodesNumber++;
        }

        public void RemoveNode(int idx)
        {
            if (NodesNumber == 0)
            {
                return;
            }

            for (int i = idx; i < NodesNumber - 1; i++)
            {
                Nodes[i] = Nodes[i + 1];
            }

            NodesNumber--;
        }

        public void Expand(int w, int h)
        {
            if (w > Width)
            {
                InsertNode(NodesNumber, Width, 0, w - Width);
            }

            Width = w;
            Height = h;
        }

        public void Reset(int w, int h)
        {
            Width = w;
            Height = h;
            UnityEngine.Object.Destroy(Texture);
            Texture = null;
            NodesNumber = 0;
            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = w;
            NodesNumber++;
            Version++;
        }

        public bool AddSkylineLevel(int idx, int x, int y, int w, int h)
        {
            InsertNode(idx, x, y + h, w);
            for (int i = idx + 1; i < NodesNumber; i++)
            {
                if (Nodes[i].X < Nodes[i - 1].X + Nodes[i - 1].Width)
                {
                    int shrink = Nodes[i - 1].X + Nodes[i - 1].Width - Nodes[i].X;
                    Nodes[i].X += shrink;
                    Nodes[i].Width -= shrink;
                    if (Nodes[i].Width <= 0)
                    {
                        RemoveNode(i);
                        i--;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < NodesNumber - 1; i++)
            {
                if (Nodes[i].Y == Nodes[i + 1].Y)
                {
                    Nodes[i].Width += Nodes[i + 1].Width;
                    RemoveNode(i + 1);
                    i--;
                }
            }

            return true;
        }

        public int RectFits(int i, int w, int h)
        {
            int x = Nodes[i].X;
            int y = Nodes[i].Y;
            if (x + w > Width)
            {
                return -1;
            }

            int spaceLeft = w;
            while (spaceLeft > 0)
            {
                if (i == NodesNumber)
                {
                    return -1;
                }

                y = Math.Max(y, Nodes[i].Y);
                if (y + h > Height)
                {
                    return -1;
                }

                spaceLeft -= Nodes[i].Width;
                ++i;
            }

            return y;
        }

        public bool AddRect(int rw, int rh, ref int rx, ref int ry)
        {
            int besth = Height;
            int bestw = Width;
            int besti = -1;
            int bestx = -1;
            int besty = -1;
            for (int i = 0; i < NodesNumber; i++)
            {
                int y = RectFits(i, rw, rh);
                if (y != -1)
                {
                    if (y + rh < besth || (y + rh == besth && Nodes[i].Width < bestw))
                    {
                        besti = i;
                        bestw = Nodes[i].Width;
                        besth = y + rh;
                        bestx = Nodes[i].X;
                        besty = y;
                    }
                }
            }

            if (besti == -1)
            {
                return false;
            }

            if (!AddSkylineLevel(besti, bestx, besty, rw, rh))
            {
                return false;
            }

            rx = bestx;
            ry = besty;
            return true;
        }

        public bool RenderGlyph(FontGlyph glyph)
        {
            Color[] colorBuffer = GetGlyphColors(glyph);
            if (colorBuffer.Length == 0) return true;
            bool wasRecreated = false;
            // Write to texture
            if (Texture == null)
            {
                Texture = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
                Texture.SetPixels(new Color[Width * Height].Select(x => Color.clear).ToArray());
                wasRecreated = true;
            }

            Texture.SetPixels(Mathf.RoundToInt(glyph.x), Mathf.RoundToInt(glyph.y), Mathf.RoundToInt(glyph.width), Mathf.RoundToInt(glyph.height), colorBuffer, 0);
            IsPendingApply = true;
            Version++;
            return wasRecreated;
        }

        public void Apply()
        {
            if (IsPendingApply)
            {
                Texture.Apply();
                IsPendingApply = false;
            }
        }

        public Color[] GetGlyphColors(FontGlyph glyph)
        {
            int pad = glyph.Pad;

            // Render glyph to byte buffer
            byte[] buffer = new byte[Mathf.RoundToInt(glyph.width) * Mathf.RoundToInt(glyph.height)];
            Array.Clear(buffer, 0, buffer.Length);

            int g = glyph.Index;
            var dst = new FakePtr<byte>(buffer, pad + (pad * Mathf.RoundToInt(glyph.width)));
            var font = glyph.Font;
            if (font is null) return new Color[0];
            glyph.Font.RenderGlyphBitmap(dst,
               Mathf.RoundToInt(glyph.width) - (pad * 2),
               Mathf.RoundToInt(glyph.height) - (pad * 2),
               Mathf.RoundToInt(glyph.width),
                g);

            if (glyph.Blur > 0)
            {
                Blur(buffer, Mathf.RoundToInt(glyph.width), Mathf.RoundToInt(glyph.height), Mathf.RoundToInt(glyph.width), glyph.Blur);
            }
            // Byte buffer to RGBA
            var colorBuffer = new Color[Mathf.RoundToInt(glyph.width) * Mathf.RoundToInt(glyph.height)];
            for (int i = 0; i < colorBuffer.Length; ++i)
            {
                byte c = buffer[i];
                colorBuffer[i].r = colorBuffer[i].g = colorBuffer[i].b = 1;
                colorBuffer[i].a = c;
            }
            return colorBuffer;
        }

        public bool IsDirty { get; private set; } = false;

        public static readonly int _BaseColorMap = Shader.PropertyToID("_BaseColorMap");


        private void Blur(byte[] dst, int w, int h, int dstStride, int blur)
        {
            if (blur < 1)
            {
                return;
            }
            int alpha = 0;
            float sigma = 0;

            sigma = blur * 0.57735f;
            alpha = (int)((1 << 16) * (1.0f - Math.Exp(-2.3f / (sigma + 1.0f))));
            var ptr = new FakePtr<byte>(dst);
            BlurRows(ptr, w, h, dstStride, alpha);
            BlurCols(ptr, w, h, dstStride, alpha);
            BlurRows(ptr, w, h, dstStride, alpha);
            BlurCols(ptr, w, h, dstStride, alpha);
        }

        private static void BlurCols(FakePtr<byte> dst, int w, int h, int dstStride, int alpha)
        {
            int x = 0;
            int y = 0;
            for (y = 0; y < h; y++)
            {
                int z = 0;
                for (x = 1; x < w; x++)
                {
                    z += (alpha * ((dst[x] << 7) - z)) >> 16;
                    dst[x] = (byte)(z >> 7);
                }

                dst[w - 1] = 0;
                z = 0;
                for (x = w - 2; x >= 0; x--)
                {
                    z += (alpha * ((dst[x] << 7) - z)) >> 16;
                    dst[x] = (byte)(z >> 7);
                }

                dst[0] = 0;
                dst += dstStride;
            }
        }

        private static void BlurRows(FakePtr<byte> dst, int w, int h, int dstStride, int alpha)
        {
            int x = 0;
            int y = 0;
            for (x = 0; x < w; x++)
            {
                int z = 0;
                for (y = dstStride; y < h * dstStride; y += dstStride)
                {
                    z += (alpha * ((dst[y] << 7) - z)) >> 16;
                    dst[y] = (byte)(z >> 7);
                }

                dst[(h - 1) * dstStride] = 0;
                z = 0;
                for (y = (h - 2) * dstStride; y >= 0; y -= dstStride)
                {
                    z += (alpha * ((dst[y] << 7) - z)) >> 16;
                    dst[y] = (byte)(z >> 7);
                }

                dst[0] = 0;
                dst++;
            }
        }

        public void Dispose() => GameObject.Destroy(Texture);
    }
}