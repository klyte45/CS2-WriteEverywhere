using Colossal.Mathematics;
using System;
using UnityEngine;

namespace BelzontWE
{
    public interface IBasicRenderInformation : IDisposable
    {
        Bounds2 BoundsUV { get; }
        Texture Control { get; }
        Texture Emissive { get; }
        Colossal.Hash128 Guid { get; }
        Texture Main { get; }
        Texture Mask { get; }
        Texture Normal { get; }

        Mesh GetMesh(WEShader shader, int idx = 0);
        bool IsValid();
        bool IsError { get; set; }
        Bounds3 Bounds { get; }

        Material SharedMaterial { get; }
    }
}