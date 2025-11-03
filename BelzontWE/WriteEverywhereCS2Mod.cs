using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Sprites;
using BelzontWE.UI;
using Game;
using Game.Modding;
using Game.UI.InGame;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod, IMod
    {
        public override string Acronym => "WE";

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<WEWorldPickerTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WEWorldPickerTooltip>(SystemUpdatePhase.UITooltip);

            updateSystem.UpdateAt<WEWorldPickerController>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<WEUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WEMainUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WELayoutController>(SystemUpdatePhase.UIUpdate);

            updateSystem.UpdateAt<FontServer>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WEAtlasesLibrary>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WECustomMeshLibrary>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAfter<WETemplateManager>(SystemUpdatePhase.Rendering);

            updateSystem.UpdateAt<WENodeExtraDataUpdater2B>(SystemUpdatePhase.Modification2B);
            updateSystem.UpdateAt<WENodeExtraDataUpdater>(SystemUpdatePhase.Rendering);

            updateSystem.UpdateAt<WEPreCullingSystem>(SystemUpdatePhase.PreCulling);

            updateSystem.UpdateAt<WEPreRendererSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAfter<WERendererSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAfter<WEPostRendererSystem>(SystemUpdatePhase.MainLoop);
        }

        public override void OnDispose()
        {
        }

        public unsafe override void DoOnLoad()
        {
            LogUtils.DoInfoLog("WETextDataMaterial = " + sizeof(WETextDataMaterial));
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GamePanelUISystem>().SetDefaultArgs(new WEMainPanel());
            LogUtils.DoInfoLog($"Registered panel: {typeof(WEMainPanel).FullName}");
            initFrame = Time.frameCount;
        }

        internal static int initFrame = 0;
        internal static bool IsInitializationComplete => Time.frameCount - initFrame >= 120;

        public override BasicModData CreateSettingsFile()
        {
            return new WEModData(this);
        }

        internal static WEModData WeData => ModData as WEModData;
    }
}
