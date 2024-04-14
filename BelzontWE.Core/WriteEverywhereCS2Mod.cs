#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Game;
using Game.Modding;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod, IMod
    {
        public override string Acronym => "WE";

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateBefore<FontServer>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WETestTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WETestTooltip>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<WEPreRendererSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WERendererSystem>(SystemUpdatePhase.MainLoop);
        }

        public override void OnDispose()
        {
        }

        public override void DoOnLoad()
        {
        }


        public override BasicModData CreateSettingsFile()
        {
            return new WEModData(this);
        }
    }

    public partial class WETestTooltip : TooltipSystemBase
    {
        private ToolSystem m_ToolSystem;
        private StringTooltip m_Tooltip;
        private WETestTool m_WETestTool;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            m_WETestTool = base.World.GetOrCreateSystemManaged<WETestTool>();
            m_Tooltip = new StringTooltip
            {
                path = "Tooltip.LABEL[XX.MyTool]"
            };
        }
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool != m_WETestTool)
            {
                return;
            }
            m_Tooltip.value = LocalizedString.IdWithFallback("Tooltip.LABEL[XX.MyTool]", "My Tool");
            AddMouseTooltip(m_Tooltip);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

}