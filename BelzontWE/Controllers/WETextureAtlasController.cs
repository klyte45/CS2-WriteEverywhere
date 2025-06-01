using Belzont.Interfaces;
using BelzontWE.Sprites;
using Colossal;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using static BelzontWE.Sprites.WEAtlasesLibrary;

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
            callBinder($"{PREFIX}listModAtlases", ListModAtlases);
            callBinder($"{PREFIX}listAtlasImages", ListAtlasImages);
            callBinder($"{PREFIX}exportCityAtlas", ExportCityAtlas);
            callBinder($"{PREFIX}copyToCity", CopyToCity);
            callBinder($"{PREFIX}removeFromCity", RemoveFromCity);
            callBinder($"{PREFIX}getCityAtlasDetail", GetCityAtlasDetail);
            callBinder($"{PREFIX}openExportFolder", OpenExportFolder);
            callBinder($"{PREFIX}exportModAtlas", ExportModAtlas);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        private Dictionary<string, bool> ListAvailableLibraries() => m_AtlasLibrary.ListAvailableAtlases();
        private ModAtlasRegistry[] ListModAtlases() => m_AtlasLibrary.ListModAtlases();
        private string[] ListAtlasImages(string atlas) => m_AtlasLibrary.ListAvailableAtlasImages(atlas);
        private string ExportCityAtlas(string atlas, string folder) => m_AtlasLibrary.ExportCityAtlas(atlas ?? "", folder);
        private string ExportModAtlas(string atlasFullName, string folder) => m_AtlasLibrary.ExportModAtlas(atlasFullName, folder);
        private bool CopyToCity(string atlas, string newName) => m_AtlasLibrary.CopyToCity(atlas ?? "", newName);
        private bool RemoveFromCity(string atlas) => m_AtlasLibrary.RemoveFromCity(atlas ?? "");
        private void OpenExportFolder(string exportFolder)
        {
            var targetDir = Path.Combine(WEAtlasesLibrary.ATLAS_EXPORT_FOLDER, exportFolder);
            if (Directory.Exists(targetDir)) RemoteProcess.OpenFolder(targetDir);
        }

        private AtlasCityDetailResponse GetCityAtlasDetail(string name)
            => name == null || !m_AtlasLibrary.AtlasExists(name)
                ? null
                : new AtlasCityDetailResponse
                {
                    name = name,
                    usages = m_AtlasLibrary.GetAtlasUsageCount(name),
                    isFromSavegame = m_AtlasLibrary.AtlasExistsInSavegame(name),
                    imageCount = m_AtlasLibrary.ListAvailableAtlasImages(name).Length,
                    textureSize = m_AtlasLibrary.GetAtlasImageSize(name)
                };
        private class AtlasCityDetailResponse
        {
            public string name;
            public bool isFromSavegame;
            public int usages;
            public int imageCount;
            public float[] textureSize;
        }
        protected override void OnUpdate()
        {
        }
    }

}