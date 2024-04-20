#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Game;
using Game.Modding;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod, IMod
    {
        public override string Acronym => "WE";

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateBefore<FontServer>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WEWorldPickerTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WEWorldPickerTooltip>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<WEPreRendererSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WERendererSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<WEWorldPickerController>(SystemUpdatePhase.ModificationEnd);
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

}