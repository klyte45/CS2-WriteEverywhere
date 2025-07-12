using Colossal.Mathematics;
using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public class CustomMeshRenderInformation : IBasicRenderInformation
    {
        private Mesh mesh;

        public Bounds2 BoundsUV { get; private set; }
        public Texture Control { get; private set; }
        public Texture Emissive { get; private set; }
        public Colossal.Hash128 Guid { get; private set; }
        public Texture Main { get; private set; }
        public Texture Mask { get; private set; }
        public Texture Normal { get; private set; }


        public CustomMeshRenderInformation(Mesh targetMesh, Vector2 minUv, Vector2 maxUv, Texture main, Texture normal = null, Texture control = null, Texture emissive = null, Texture mask = null)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            Normal = normal;
            Control = control;
            Emissive = emissive;
            Mask = mask;
            Guid = System.Guid.NewGuid();
            BoundsUV = new Bounds2(Vector2.zero, Vector2.one);

            mesh = Mesh.Instantiate(targetMesh) ?? throw new ArgumentNullException(nameof(targetMesh));
            mesh.uv = targetMesh.uv2.Select(x => (Vector2)(math.lerp(minUv, maxUv, math.clamp(x, Vector2.zero, Vector2.one)))).ToArray();

            Bounds = new Bounds3(targetMesh.bounds.min, targetMesh.bounds.max);
        }

        public void SetNewBounds(Vector2 minUV, Vector2 maxUV)
        {
            BoundsUV = new Bounds2(minUV, maxUV);
            if (mesh != null && mesh.uv2 != null)
            {
                mesh.uv = mesh.uv2.Select(x => (Vector2)(math.lerp(minUV, maxUV, math.clamp(x, Vector2.zero, Vector2.one)))).ToArray();
            }
        }

        public Mesh GetMesh(WEShader shader) => mesh;
        public bool IsValid() => Main != null;
        public bool IsError { get => false; set { } }

        public Bounds3 Bounds { get; }

        public void Dispose()
        {
            GameObject.Destroy(mesh);
        }
    }
}