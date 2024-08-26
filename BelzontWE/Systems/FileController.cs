using Belzont.Interfaces;
using System;
using System.IO;
using System.Linq;
using Unity.Entities;

namespace BelzontWE
{
    public partial class FileController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "file.";

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listFiles", ListFiles);
            callBinder($"{PREFIX}getLayoutFolder", GetLayoutFolder);
            callBinder($"{PREFIX}getPrefabLayoutExtension", GetPrefabLayoutExtension);
            callBinder($"{PREFIX}getStoredLayoutExtension", GetStoredLayoutExtension);
            callBinder($"{PREFIX}getFontDefaultLocation", GetFontDefaultLocation);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        private class ListFileResult
        {
            public string displayName;
            public bool directory;
            public string fullPath;
        }

        private ListFileResult[] ListFiles(string folder, string allowedExtensions)
        {
            return Directory.Exists(folder)
                ? Directory.GetDirectories(folder)
                    .Select(x => new ListFileResult
                    {
                        displayName = Path.GetFileName(x),
                        directory = true,
                        fullPath = x
                    }).Concat(
                    allowedExtensions.Split("|").SelectMany(ext =>
                        Directory.GetFiles(folder, ext)
                        .Select(x => new ListFileResult
                        {
                            displayName = Path.GetFileName(x),
                            directory = false,
                            fullPath = x
                        })).OrderBy(x => x.displayName)
                    ).ToArray()
                : null;
        }

        private string GetLayoutFolder() => WETemplateManager.SAVED_PREFABS_FOLDER;
        private string GetPrefabLayoutExtension() => WETemplateManager.PREFAB_LAYOUT_EXTENSION;
        private string GetStoredLayoutExtension() => WETemplateManager.SIMPLE_LAYOUT_EXTENSION;
        private string GetFontDefaultLocation() => FontServer.FontFilesPath;
        protected override void OnUpdate()
        {
        }
    }
}