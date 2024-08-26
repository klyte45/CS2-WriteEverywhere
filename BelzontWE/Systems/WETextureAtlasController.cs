using Belzont.Interfaces;
using System;
using Unity.Entities;
using WriteEverywhere.Sprites;

namespace BelzontWE
{
    public partial class WETextureAtlasController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "textureAtlas.";
        private WEAtlasesLibrary m_AtlasLibrary;

        protected override void OnCreate()
        {
            m_AtlasLibrary = World.GetOrCreateSystemManaged<WEAtlasesLibrary>();
        }

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listAvailableLibraries", ListAvailableLibraries);
            callBinder($"{PREFIX}listAtlasImages", ListAtlasImages);
            callBinder($"{PREFIX}exportCityAtlas", ExportCityAtlas);
            callBinder($"{PREFIX}copyToCity", CopyToCity);
            callBinder($"{PREFIX}removeFromCity", RemoveFromCity);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }


        private string[] ListAvailableLibraries() => m_AtlasLibrary.ListAvailableAtlases();
        private string[] ListAtlasImages(string atlas) => m_AtlasLibrary.ListAvailableAtlasImages(atlas);
        private string ExportCityAtlas(string atlas, string folder) => m_AtlasLibrary.ExportCityAtlas(atlas ?? "", folder);
        private bool CopyToCity(string atlas, string newName) => m_AtlasLibrary.CopyToCity(atlas ?? "", newName);
        private bool RemoveFromCity(string atlas) => m_AtlasLibrary.RemoveFromCity(atlas ?? "");


        protected override void OnUpdate()
        {
        }
    }
}