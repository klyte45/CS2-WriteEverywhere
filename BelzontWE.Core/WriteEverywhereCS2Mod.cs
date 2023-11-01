#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Game;
using Game.Common;
using Game.Input;
using Game.Modding;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Tooltip;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod<WEModData>, IMod
    {
        public override string SimpleName => "Write Everywhere";

        public override string SafeName => "WriteEverywhere";

        public override string Acronym => "WE";

        public override string Description => "Write Everywhere for Cities Skylines 2";

        public override WEModData CreateNewModData() => new();

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateBefore<FontServer>(SystemUpdatePhase.Rendering);

            updateSystem.UpdateAt<WETestTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WETestTooltip>(SystemUpdatePhase.UITooltip);
        }

        public override void OnDispose()
        {
        }

        public override void DoOnLoad()
        {
        }

        protected override IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections()
        {
            yield break;
        }
    }

    public struct WECustomComponent : IComponentData { }

    public partial class WETestTool : ToolBaseSystem
    {
        public override string toolID => $"K45_WE_{GetType().Name}";
        private Func<Entity, bool> callback;

        private struct AddMyCustomComponentJob : IJob
        {
            public WECustomComponent myCustomComponent;
            public EntityCommandBuffer buffer;
            public Entity entity;
            public void Execute()
            {
                buffer.AddComponent<WECustomComponent>(entity);
                buffer.SetComponent(entity, myCustomComponent);
                buffer.AddComponent<Highlighted>(entity);
                LogUtils.DoInfoLog($"[WETestTool.AddMyCustomComponentJob] Scheduled MyCustomComponent to be added to Entity.Index = " + entity.Index.ToString() + " to Entity.Version = " + entity.Version.ToString());
            }
        }

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }


        private ProxyAction m_ApplyAction;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private WETestController m_Controller;

        protected override void OnCreate()
        {
            Enabled = false;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            LogUtils.DoLog("{MyTool.OnCreate} MyTool Created.");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_Controller = World.GetOrCreateSystemManaged<WETestController>();
            base.OnCreate();
        }
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
        }
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
        }
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.MovingObjects;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_ApplyAction.WasPressedThisFrame())
            {
                bool flag = GetRaycastResult(out Entity e, out RaycastHit hit);
                if (flag && !(callback?.Invoke(e) ?? true))
                {
                    RequestDisable();
                }
            }
            return inputDeps;
        }
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }
        public void Select()
        {
            m_ToolSystem.activeTool = this;
        }

        public void SetCallbackAndEnable(Func<Entity, bool> callback)
        {
            this.callback = callback;
            Select();
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

    public class WETestController : ComponentSystemBase, IBelzontBindable
    {
        private Action<string, object[]> eventCaller;
        private WETestTool m_WETestTool;
        private FontServer m_FontServer;

        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            eventCaller("test.enableTestTool", EnableTestTool);
            eventCaller("test.reloadFonts", ReloadFonts);
            eventCaller("test.listFonts", ListFonts);
            eventCaller("test.requestTextMesh", RequestTextMesh);
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            this.eventCaller = eventCaller;
        }

        public void SetupEventBinder(Action<string, Delegate> eventCaller)
        {
        }
        protected override void OnCreate()
        {
            m_WETestTool = World.GetExistingSystemManaged<WETestTool>();
            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            m_FontServer.OnFontsLoadedChanged += () => SendToFrontend("test.fontsChanged->", new object[] { ListFonts() });
            base.OnCreate();
        }
        public override void Update() { }

        private void SendToFrontend(string eventName, params object[] args) => eventCaller?.Invoke(eventName, args);

        private void EnableTestTool()
        {
            m_WETestTool.SetCallbackAndEnable((e) =>
            {
                SendToFrontend("test.enableTestTool->", e);
                return false;
            });
        }

        private void ReloadFonts()
        {
            m_FontServer.ReloadFontsFromPath();
        }

        private string[] ListFonts() => m_FontServer.GetAllFonts()?.ToArray();

        private BasicRenderInformation RequestTextMesh(string text, string fontName)
        {
            return m_FontServer[fontName]?.DrawString(text, Vector2.one);
        }
    }
}