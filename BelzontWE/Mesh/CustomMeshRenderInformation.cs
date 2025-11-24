using BelzontWE.Font;
using Colossal.Core;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Mathematics;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static BelzontWE.IO.ObjFileHandler;

namespace BelzontWE
{
    public class CustomMeshRenderInformation : IBasicRenderInformation
    {
        private Mesh mesh;

        private Mesh CachedMesh
        {
            get
            {
                if (mesh == null)
                {
                    mesh = new Mesh
                    {
                        name = "CustomMesh",
                        vertices = vertices,
                        normals = normals,
                        uv = originalUv.Select(x => (Vector2)(math.lerp(imageBounds.min, imageBounds.max, math.clamp(x, Vector2.zero, Vector2.one)))).ToArray(),
                        triangles = triangles
                    };
                }
                return mesh;
            }
        }

        public Bounds2 BoundsUV { get; private set; }
        public Colossal.Hash128 Guid { get; private set; }

        public Material BaseMaterialDefault { get; private set; }
        public Material BaseMaterialDecal { get; private set; }
        public Material BaseMaterialGlass => null;

        private GCHandle handleCheck;

        private readonly Vector3[] vertices;
        private readonly Vector2[] originalUv;
        private Bounds2 imageBounds;
        private readonly Vector3[] normals;
        private readonly int[] triangles;

        public CustomMeshRenderInformation(WEMeshDescriptor descriptor, Vector2 minUv, Vector2 maxUv, Texture main, Texture normal = null, Texture control = null, Texture emissive = null, Texture mask = null)
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                BaseMaterialDecal = WERenderingHelper.GenerateMaterial(WEShader.Decal, main, normal, mask, control, emissive);
                BaseMaterialDefault = WERenderingHelper.GenerateMaterial(WEShader.Default, main, normal, mask, control, emissive);
            });
            handleCheck = GCHandle.Alloc(main, GCHandleType.Weak);
            Guid = System.Guid.NewGuid();
            BoundsUV = new Bounds2(Vector2.zero, Vector2.one);

            vertices = descriptor.Vertices.ToArray();
            originalUv = descriptor.UVs;
            imageBounds = new Bounds2(minUv, maxUv);

            normals = descriptor.Normals.ToArray();
            triangles = descriptor.Triangles.ToArray();

            Bounds = new Bounds3(vertices.Aggregate(new float3(float.MaxValue, float.MaxValue, float.MaxValue), (p, n) => math.min(p, n)), vertices.Aggregate(new float3(float.MinValue, float.MinValue, float.MinValue), (p, n) => math.max(p, n)));
        }

        public CustomMeshRenderInformation(WETextureAtlas atlasInfo, WEMeshDescriptor descriptor, Vector2 minUv, Vector2 maxUv)
        {
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                var tss = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
                BaseMaterialDecal = atlasInfo.GenerateMaterial(WEShader.Decal, tss);
                BaseMaterialDefault = atlasInfo.GenerateMaterial(WEShader.Default, tss);
            });
            Guid = System.Guid.NewGuid();
            BoundsUV = new Bounds2(Vector2.zero, Vector2.one);
            vertices = [.. descriptor.Vertices];
            originalUv = descriptor.UVs;
            imageBounds = new Bounds2(minUv, maxUv);
            normals = [.. descriptor.Normals];
            triangles = [.. descriptor.Triangles];
            Bounds = new Bounds3(vertices.Aggregate(new float3(float.MaxValue, float.MaxValue, float.MaxValue), (p, n) => math.min(p, n)), vertices.Aggregate(new float3(float.MinValue, float.MinValue, float.MinValue), (p, n) => math.max(p, n)));

            handleCheck = GCHandle.Alloc(atlasInfo, GCHandleType.Weak);
        }

        public void SetNewBounds(Vector2 minUV, Vector2 maxUV)
        {
            imageBounds = new Bounds2(minUV, maxUV);
            GameObject.Destroy(mesh);
        }

        public Mesh GetMesh(WEShader shader, int idx = 0) => CachedMesh;
        public bool IsValid() => handleCheck.IsAllocated && handleCheck.Target is not null;
        public bool IsError { get => false; set { } }

        public Bounds3 Bounds { get; }

        public void Dispose()
        {
            if (mesh) GameObject.Destroy(mesh);
            if (handleCheck.IsAllocated) handleCheck.Free();

        }
    }
}