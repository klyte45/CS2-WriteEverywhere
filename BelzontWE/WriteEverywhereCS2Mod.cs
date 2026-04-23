using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Sprites;
using BelzontWE.UI;
using BelzontWE.Utils;
using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
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
            // ┌──────────────────────────────────────────────────────────────────────────┐
            // │              SYSTEM DEPENDENCY GRAPH — Write Everywhere                  │
            // │  Update this comment when adding, moving, or removing systems.           │
            // ├──────────────────────────────────────────────────────────────────────────┤
            // │                                                                          │
            // │  Phase: UITooltip                                                        │
            // │    WEWorldPickerTooltip → reads WEWorldPickerTool hover state            │
            // │                                                                          │
            // │  Phase: ModificationEnd                                                  │
            // │    WEWorldPickerController → reads WEWorldPickerTool selection            │
            // │                                                                          │
            // │  Phase: UIUpdate                                                         │
            // │    WEUISystem          → toggles WEWorldPickerTool via keyboard          │
            // │    WEMainUISystem      → registers UI panel (no WE deps)                 │
            // │    WELayoutController  → reads WETemplateManager, WEWorldPickerController│
            // │    WETemplateQuerySystem → reads WETemplateManager (on-demand queries)   │
            // │                                                                          │
            // │  Phase: Rendering (ordered)                                              │
            // │    FontServer          → produces font data (no WE deps)                 │
            // │    WEAtlasesLibrary    → produces texture atlases (no WE deps)           │
            // │    WECustomMeshLibrary → produces custom meshes (no WE deps)             │
            // │    WETemplateManager   → produces templates; reads WETemplateQuerySystem  │
            // │      ↓ [UpdateAfter]                                                     │
            // │    WETemplateUpdateSystem → reads WETemplateManager; writes WETextData   │
            // │      ↓ [UpdateAfter]                                                     │
            // │    WEPrefabLayoutSystem  → reads WETemplateManager; loads prefab layouts │
            // │                                                                          │
            // │  Phase: PreCulling                                                       │
            // │    WEPreCullingSystem → reads WEWorldPickerTool, WEWorldPickerController │
            // │                         produces m_availToDraw for WERendererSystem       │
            // │                                                                          │
            // │  Phase: Cleanup                                                          │
            // │    WETemplateDisposalSystem → disposes orphaned WETextData components    │
            // │                                                                          │
            // │  Self-registered (Modification2B via BelzontBasicSystem):                │
            // │    WEGamePropSpawnSystem  → reads WETemplateManager; spawns game props  │
            // │                                                                          │
            // │  Self-registered (EndFrame via BelzontBasicSystem):                     │
            // │    WERendererSystem       → reads WEPreCullingSystem, WEWorldPicker*    │
            // │    WEPostRendererSystem   → reads WETemplateManager; resolves text/image│
            // │    WEEmissiveLightSystem  → reads WEPreCullingSystem; emissive lighting │
            // │                                                                          │
            // └──────────────────────────────────────────────────────────────────────────┘
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
            updateSystem.UpdateAt<WETemplateQuerySystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAfter<WEPrefabLayoutSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WETemplateDisposalSystem>(SystemUpdatePhase.Cleanup);

            updateSystem.UpdateAt<WEPreCullingSystem>(SystemUpdatePhase.PreCulling);            

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

            MainThreadDispatcher.RegisterUpdater(() =>
            {
                var asset = AssetDatabase.global.GetAsset(SearchFilter<UIModuleAsset>.ByCondition(asset => asset.name == "k45-we-vuio"));
                LogUtils.DoInfoLog($"Forcing loading UI asset: {asset?.name} ({asset?.path})");
                GameManager.instance.modManager.AddUIModule(asset);
            });
        }

        internal static bool IsInitializationComplete
        {
            get
            {
                var tss = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<TextureStreamingSystem>();
                return tss != null
                    && tss.VTMaterialsCountAssetsCount > 0
                    && tss.VTMaterialsLeftToLoadCount == 0
                    && tss.VTMaterialsDuplicatesToProcessCount == 0;
            }
        }

        public override BasicModData CreateSettingsFile()
        {
            return new WEModData(this);
        }

        internal static WEModData WeData => ModData as WEModData;
    }
}
