using System;
using System.Reflection;
using UnityEngine;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class MeshManagementBridge
    {

        public static bool RegisterMesh(Assembly mainAssembly, string meshName, string meshObjFilePath) 
            => WECustomMeshLibrary.Instance.LoadMeshToMod(mainAssembly, meshName, meshObjFilePath);

        public static bool RegisterMeshFromMemory(Assembly mainAssembly, string meshName, Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles) 
            => WECustomMeshLibrary.Instance.LoadMeshToMod(mainAssembly, meshName, vertices, normals, uv, triangles);
    }
}
