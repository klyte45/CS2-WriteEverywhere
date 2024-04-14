#define BURST
//#define VERBOSE 
using Belzont.Utils;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.Tools;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace BelzontWE
{
    public partial class WETestTool : ToolBaseSystem
    {
        public override string toolID => $"K45_WE_{GetType().Name}";
        private Func<Entity, bool> callback;

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
            bool flag = GetRaycastResult(out Entity e, out RaycastHit hit);
            if (flag)
            {
                if (m_ApplyAction.WasPressedThisFrame())
                {
                    if (flag && !(callback?.Invoke(e) ?? true))
                    {
                        RequestDisable();
                    }
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

}