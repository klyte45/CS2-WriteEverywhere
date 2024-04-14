#define BURST
//#define VERBOSE 
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public struct WESimulationTextComponent : IBufferElementData, IDisposable
    {
        public unsafe static int Size => sizeof(WESimulationTextComponent);

        public Guid parentSourceGuid;
        public Guid propertySourceGuid;
        public FixedString512Bytes FontName
        {
            get => fontName; set
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
            get => text; set
            {
                text = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public Vector3 offsetPosition;
        public Quaternion offsetRotation;
        public Vector3 scale;
        public GCHandle basicRenderInformation;
        public GCHandle materialBlockPtr;
        public bool dirty;
        public Color32 color;
        public Color32 emissiveColor;
        public float metallic;
        public float emissiveIntensity;
        public FixedString512Bytes text;
        public FixedString512Bytes fontName;

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

        public bool IsDirty() => dirty;
        public Color32 Color
        {
            get => color; set
            {
                color = value;
                dirty = true;
            }
        }
        public Color32 EmmissiveColor
        {
            get => emissiveColor; set
            {
                emissiveColor = value;
                dirty = true;
            }
        }
        public float Metallic
        {
            get => metallic; set
            {
                metallic = Mathf.Clamp01(value);
                dirty = true;
            }
        }

        public static WESimulationTextComponent From(WEWaitingRenderingComponent src, DynamicSpriteFont font, BasicRenderInformation bri)
        {
            return new WESimulationTextComponent
            {
                propertySourceGuid = src.propertySourceGuid,
                FontName = src.fontName,
                Text = src.text,
                offsetPosition = src.offsetPosition,
                offsetRotation = src.offsetRotation,
                scale = src.scale,
                Color = src.color,
                EmmissiveColor = src.emmissiveColor,
                Metallic = src.metallic,
                dirty = true,
                basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak),
                BriOffsetScaleX = bri.m_offsetScaleX,
                BriPixelDensity = bri.m_pixelDensityMeters
            };
        }

        public void Dispose()
        {
            basicRenderInformation.Free();
            materialBlockPtr.Free();
        }
    }

}