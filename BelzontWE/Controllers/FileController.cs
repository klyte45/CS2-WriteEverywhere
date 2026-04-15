using Belzont.Systems;
using System;

namespace BelzontWE
{
    public partial class FileController : BaseFileController
    {
        private const string PREFIX = "file.";

        public override void SetupOtherCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}getLayoutFolder", GetLayoutFolder);
            callBinder($"{PREFIX}getPrefabLayoutExtension", GetPrefabLayoutExtension);
            callBinder($"{PREFIX}getStoredLayoutExtension", GetStoredLayoutExtension);
            callBinder($"{PREFIX}getFontDefaultLocation", GetFontDefaultLocation);
        }

        private string GetLayoutFolder() => WETemplateManager.SAVED_PREFABS_FOLDER;
        private string GetPrefabLayoutExtension() => WETemplateManager.PREFAB_LAYOUT_EXTENSION;
        private string GetStoredLayoutExtension() => WETemplateManager.SIMPLE_LAYOUT_EXTENSION;
        private string GetFontDefaultLocation() => FontServer.FontFilesPath;
    }
}