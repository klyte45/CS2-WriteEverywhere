using Belzont.Interfaces;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WECustomMeshLibraryController : WEBindableSystemBase
    {
        private const string PREFIX = "customMesh.";
        private WECustomMeshLibrary m_MeshLibrary;

        protected override void OnCreate()
        {
            m_MeshLibrary = World.GetOrCreateSystemManaged<WECustomMeshLibrary>();
        }

        public override void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listAvailableLibraries", ListAvailableLibraries);
            //callBinder($"{PREFIX}listModMeshes", ListModMeshes);
            callBinder($"{PREFIX}copyToCity", CopyToCity);
            callBinder($"{PREFIX}removeFromCity", RemoveFromCity);
        }

        private Dictionary<string, string> ListAvailableLibraries() => m_MeshLibrary.ListAvailableMeshesUI();
        private bool CopyToCity(string mesh, string newName) => m_MeshLibrary.CopyToCity(mesh ?? "", newName);
        private bool RemoveFromCity(string mesh) => m_MeshLibrary.RemoveFromCity(mesh ?? "");

    }

}