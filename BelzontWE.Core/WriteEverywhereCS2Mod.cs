﻿using Belzont.Interfaces;
using Colossal.UI;
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
            updateSystem.UpdateAt<FontServer>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WEWorldPickerTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WEWorldPickerTooltip>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateBefore<WEPreRendererSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WERendererSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WEWorldPickerController>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<WEUISystem>(SystemUpdatePhase.UIUpdate);
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