#define BURST
//#define VERBOSE 
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Serialization.Entities;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{

    public struct WESimulationTextComponent : IBufferElementData, ISerializable, IDisposable
    {
        public const uint CURRENT_VERSION = 1;
        public unsafe static int Size => sizeof(WESimulationTextComponent);

        public Entity targetEntity;
        public WEPropertyDescription targetProperty;

        public FixedString32Bytes FontName
        {
            readonly get => fontName; set
            {
                fontName = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public FixedString512Bytes Text
        {
            readonly get => text; set
            {
                text = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        public GCHandle basicRenderInformation;
        public GCHandle materialBlockPtr;
        public bool dirty;
        public Color32 color;
        public Color32 emissiveColor;
        public float metallic;
        public float smoothness;
        public float emissiveIntensity;
        public FixedString512Bytes text;
        public FixedString32Bytes fontName;
        public FixedString32Bytes itemName;
        public WEShader shader;

        public float BriOffsetScaleX { get; private set; }
        public float BriPixelDensity { get; private set; }
        public MaterialPropertyBlock MaterialProperties
        {
            get
            {
                MaterialPropertyBlock block;
                if (!materialBlockPtr.IsAllocated)
                {
                    block = new MaterialPropertyBlock();
                    materialBlockPtr = GCHandle.Alloc(block, GCHandleType.Normal);
                }
                else
                {
                    block = materialBlockPtr.Target as MaterialPropertyBlock;
                }
                if (dirty)
                {
                    //  block.SetColor("_EmissiveColor", emissiveColor);
                    block.SetColor("_BaseColor", color);
                    // block.SetFloat("_Metallic", metallic);
                    // block.SetFloat("_EmissiveIntensity", emissiveIntensity);


                    // block.SetTexture("unity_Lightmaps", Texture2D.blackTexture);
                    //   block.SetTexture("unity_LightmapsInd", Texture2D.blackTexture);
                    //   block.SetTexture("unity_ShadowMasks", Texture2D.blackTexture);
                    dirty = false;
                }
                return block;
            }
        }

        public readonly bool IsDirty() => dirty;
        public Color32 Color
        {
            readonly get => color; set
            {
                color = value;
                dirty = true;
            }
        }
        public Color32 EmmissiveColor
        {
            readonly get => emissiveColor; set
            {
                emissiveColor = value;
                dirty = true;
            }
        }
        public float Metallic
        {
            readonly get => metallic; set
            {
                metallic = Mathf.Clamp01(value);
                dirty = true;
            }
        }

        public static WESimulationTextComponent From(WEWaitingRenderingComponent src, BasicRenderInformation bri)
        {
            src.src.dirty = true;
            if (src.src.basicRenderInformation.IsAllocated) src.src.basicRenderInformation.Free();
            src.src.basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
            src.src.BriOffsetScaleX = bri.m_offsetScaleX;
            src.src.BriPixelDensity = bri.m_pixelDensityMeters;

            return src.src;
        }

        public void Dispose()
        {
            basicRenderInformation.Free();
            materialBlockPtr.Free();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(targetEntity);
            writer.Write((uint)targetProperty);
            writer.Write((byte)shader);
            writer.Write(offsetPosition);
            writer.Write(offsetRotation);
            writer.Write(scale);
            writer.Write(color);
            writer.Write(emissiveColor);
            writer.Write(emissiveIntensity);
            writer.Write(metallic);
            writer.Write(smoothness);
            writer.Write(text);
            writer.Write(fontName);
            writer.Write(itemName);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out targetEntity);
            reader.Read(out uint targetProperty);
            this.targetProperty = (WEPropertyDescription)targetProperty;
            reader.Read(out byte shader);
            this.shader = (WEShader)shader;
            reader.Read(out offsetPosition);
            reader.Read(out offsetRotation);
            reader.Read(out scale);
            reader.Read(out color);
            reader.Read(out emissiveColor);
            reader.Read(out emissiveIntensity);
            reader.Read(out metallic);
            reader.Read(out smoothness);
            reader.Read(out text);
            reader.Read(out fontName);
            if (version >= 1)
            {
                reader.Read(out itemName);
            }

        }
    }

}