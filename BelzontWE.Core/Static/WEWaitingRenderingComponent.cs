#define BURST
//#define VERBOSE 
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public struct WEWaitingRenderingComponent : IBufferElementData
    {
        public Guid propertySourceGuid;
        public FixedString512Bytes fontName;
        public FixedString512Bytes text;
        public Vector3 offsetPosition;
        public Quaternion offsetRotation;
        public Vector3 scale;
        public Color32 color;
        public Color32 emmissiveColor;
        public float metallic;

        public static WEWaitingRenderingComponent From(WESimulationTextComponent src)
        {
            var result = new WEWaitingRenderingComponent
            {
                propertySourceGuid = src.propertySourceGuid,
                fontName = src.FontName,
                text = src.Text,
                offsetPosition = src.offsetPosition,
                offsetRotation = src.offsetRotation,
                scale = src.scale,
                color = src.Color,
                emmissiveColor = src.EmmissiveColor,
                metallic = src.Metallic,
            };
            return result;
        }
    }

}