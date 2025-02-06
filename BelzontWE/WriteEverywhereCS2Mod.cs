using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Sprites;
using BelzontWE.UI;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.UI.InGame;
using Unity.Entities;

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
            updateSystem.UpdateAfter<WERendererSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAfter<WEPostRendererSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WEWorldPickerController>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<WEUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WEMainUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WELayoutController>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WEAtlasesLibrary>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAfter<WETemplateManager>(SystemUpdatePhase.Rendering);
        }

        public override void OnDispose()
        {
        }

        public unsafe override void DoOnLoad()
        {
            LogUtils.DoInfoLog("WETextDataMaterial = " + sizeof(WETextDataMaterial));
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GamePanelUISystem>().SetDefaultArgs(new WEMainPanel());
            LogUtils.DoInfoLog($"Registered panel: {typeof(WEMainPanel).FullName}");
        }


        public override BasicModData CreateSettingsFile()
        {
            return new WEModData(this);
        }
    }
}
