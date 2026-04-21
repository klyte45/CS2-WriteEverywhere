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

        Mesh GetMesh(WEShader shader, bool isBackface, int idx = 0);
        bool IsValid();
        bool IsError { get; set; }
        Bounds3 Bounds { get; }
        /// <summary>
        /// Notifies the VT streaming system that this BRI is being rendered this frame.
        /// Atlas-backed implementations call <see cref="Font.WETextureAtlas.NotifyRendering"/>.
        /// Non-atlas BRIs (font, plain texture) may use the default no-op.
        /// </summary>
        void NotifyRendering();

        /// <summary>
        /// Re-binds VT texture stacks to the given material copy.
        /// Unity's <c>new Material(source)</c> copies managed properties but does NOT
        /// preserve the native VT stack binding created by <c>CPUTextureStack.BindToMaterial</c>.
        /// Atlas-backed implementations must call <c>TextureStreamingSystem.BindMaterial</c>
        /// on every material copy that will be rendered with VT.
        /// </summary>
        void BindVTToMaterial(Material material);
    }
}