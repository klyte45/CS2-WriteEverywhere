using Belzont.Interfaces;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WECustomMeshLibraryController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "customMesh.";
        private WECustomMeshLibrary m_MeshLibrary;

        protected override void OnCreate()
        {
            m_MeshLibrary = World.GetOrCreateSystemManaged<WECustomMeshLibrary>();
        }

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listAvailableLibraries", ListAvailableLibraries);
            //callBinder($"{PREFIX}listModMeshes", ListModMeshes);
            callBinder($"{PREFIX}copyToCity", CopyToCity);
            callBinder($"{PREFIX}removeFromCity", RemoveFromCity);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        private Dictionary<string, string> ListAvailableLibraries() => m_MeshLibrary.ListAvailableMeshesUI();
        private bool CopyToCity(string mesh, string newName) => m_MeshLibrary.CopyToCity(mesh ?? "", newName);
        private bool RemoveFromCity(string mesh) => m_MeshLibrary.RemoveFromCity(mesh ?? "");

        protected override void OnUpdate()
        {
        }
    }

}