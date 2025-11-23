using Colossal.Mathematics;
using System;
using UnityEngine;

namespace BelzontWE
{
    public interface IBasicRenderInformation : IDisposable
    {
        Bounds2 BoundsUV { get; }
        Colossal.Hash128 Guid { get; }
        public Material BaseMaterialDefault { get; }
        public Material BaseMaterialDecal { get; }
        public Material BaseMaterialGlass { get; }

        Mesh GetMesh(WEShader shader, int idx = 0);
        bool IsValid();
        bool IsError { get; set; }
        Bounds3 Bounds { get; }
    }
}