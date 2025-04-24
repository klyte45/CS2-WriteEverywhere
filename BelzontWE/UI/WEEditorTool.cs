using Game.UI.Editor;
using Game.UI.Localization;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE.UI
{
    public class WEEditorTool : EditorTool
    {
        public const string TOOL_ID = "k45__we_MainWindow";

        // Token: 0x06002389 RID: 9097 RVA: 0x0010F1CD File Offset: 0x0010D3CD
        public WEEditorTool(World world) : base(world)
        {
            id = TOOL_ID;
            icon = "coui://ui-mods/images/WE-White.svg";        
        }
    }
}
