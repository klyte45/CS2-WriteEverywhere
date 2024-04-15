#define BURST
//#define VERBOSE 
using Belzont.Utils;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
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

        // Token: 0x0400019A RID: 410
        public Entity HoveredEntity;

        // Token: 0x0400019C RID: 412
        public Entity Selected;
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
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            this.m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
            this.m_ToolRaycastSystem.typeMask = (TypeMask.StaticObjects);
            this.m_ToolRaycastSystem.raycastFlags = (RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.UpgradeIsMain | RaycastFlags.Outside | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.Decals);
            this.m_ToolRaycastSystem.netLayerMask = (Layer)~0u;
            this.m_ToolRaycastSystem.iconLayerMask = (IconLayerMask)~0u;
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
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            ChangeHighlighting_MainThread(Selected, ChangeMode.RemoveHighlight);
            ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.RemoveHighlight);
            this.Selected = Entity.Null;
            this.HoveredEntity = Entity.Null;
            m_ApplyAction.shouldBeEnabled = false;
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

        internal void ChangeHighlighting_MainThread(Entity entity, ChangeMode mode)
        {
            if (entity == Entity.Null || !EntityManager.Exists(entity))
            {
                return;
            }
            bool flag = false;
            if (mode == ChangeMode.AddHighlight && !EntityManager.HasComponent<Highlighted>(entity))
            {
                EntityManager.AddComponent<Highlighted>(entity);
                flag = true;
            }
            else if (mode == ChangeMode.RemoveHighlight && EntityManager.HasComponent<Highlighted>(entity))
            {
                EntityManager.RemoveComponent<Highlighted>(entity);
                flag = true;
            }
            if (flag && !EntityManager.HasComponent<BatchesUpdated>(entity))
            {
                EntityManager.AddComponent<BatchesUpdated>(entity);
            }
        }

        internal enum ChangeMode
        {
            // Token: 0x040001BD RID: 445
            AddHighlight,
            // Token: 0x040001BE RID: 446
            RemoveHighlight
        }
    }

}