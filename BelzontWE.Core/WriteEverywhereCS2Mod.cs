#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Game;
using Game.Modding;
using Game.SceneFlow;

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
#if !ENABLE_EUIS
            SelfRegiterUIEvents("we");
            GameManager.instance.userInterface.view.uiSystem.UIViews[0].Listener.ReadyForBindings += () => SelfRegiterUIEvents("we");
#endif
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