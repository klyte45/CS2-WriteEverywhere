using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Sprites;
using BelzontWE.UI;
using BelzontWE.Utils;
using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;
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
            updateSystem.UpdateAfter<WETemplateUpdateSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAfter<WETemplateQuerySystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAfter<WEPrefabLayoutSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAfter<WETemplateDisposalSystem>(SystemUpdatePhase.Rendering);

            updateSystem.UpdateAt<WENodeExtraDataUpdater2B>(SystemUpdatePhase.Modification2B);
            updateSystem.UpdateAt<WENodeExtraDataUpdater>(SystemUpdatePhase.Rendering);

            updateSystem.UpdateAt<WEPreCullingSystem>(SystemUpdatePhase.PreCulling);

            updateSystem.UpdateAfter<WERendererSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAfter<WEPostRendererSystem>(SystemUpdatePhase.MainLoop);

            var reloadAssetsWeStuff = () =>
            {
                WEAssetsSettingsLoaderUtility.ResetCooldown();
                MainThreadDispatcher.RegisterUpdater(WEAssetsSettingsLoaderUtility.ReloadAssetsSettings);
            };
            (AssetDatabase<ParadoxMods>.instance.dataSource as ParadoxModsDataSource).onAfterActivePlaysetOrModStatusChanged += reloadAssetsWeStuff;
            reloadAssetsWeStuff();
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

            MainThreadDispatcher.RegisterUpdater(() =>
            {
                var asset = AssetDatabase.global.GetAsset(SearchFilter<UIModuleAsset>.ByCondition(asset => asset.name == "k45-we-vuio"));
                LogUtils.DoInfoLog($"Forcing loading UI asset: {asset?.name} ({asset?.path})");
                GameManager.instance.modManager.AddUIModule(asset);
            });
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
