using Colossal.Entities;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace BelzontWE
{
    public partial class WEWorldPickerTool : ToolBaseSystem
    {
        public static readonly float[] precisionIdx = new[] { 1f, 1 / 2f, 1 / 4f, 1 / 10f, 1 / 20f, 1 / 40f, 1 / 100f, 1 / 200f, 1 / 400f, 1 / 1000f };

        public override string toolID => $"K45_WE_WEWorldPickerTool";

        public float3 LastPos;
        public Entity HoveredEntity;
        private Func<Entity, bool> callback;

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        public override int uiModeIndex => base.uiModeIndex;


        private ProxyAction m_ApplyAction;
        private ProxyAction m_SecondaryApplyAction;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private WEWorldPickerController m_Controller;

        private float2 m_mousePositionRef;
        private float3 m_originalPositionText;
        private float3 m_originalRotationText;
        private bool m_isDragging;

        private int m_lastControllerVersion = -1;

        protected override void OnCreate()
        {
            Enabled = false;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_Controller = World.GetOrCreateSystemManaged<WEWorldPickerController>();
            base.OnCreate();
        }
        protected override void OnStartRunning()
        {
            m_Controller.CurrentEntity.Value = default;
            m_ApplyAction.shouldBeEnabled = true;
            m_Controller.IsValidEditingItem();
        }
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (m_Controller.CurrentEntity.Value == default)
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = (TypeMask.StaticObjects);
                m_ToolRaycastSystem.raycastFlags = (RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.UpgradeIsMain | RaycastFlags.Outside | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.Decals);
                m_ToolRaycastSystem.netLayerMask = (Layer)~0u;
                m_ToolRaycastSystem.iconLayerMask = (IconLayerMask)~0u;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_Controller.CurrentEntity.Value == default)
            {
                bool collide = GetRaycastResult(out Entity entity, out RaycastHit raycastHit);

                LastPos = raycastHit.m_HitPosition;
                m_Controller.CurrentItemIdx.Value = -1;
                if (collide)
                {
                    Entity hoveredEntity = HoveredEntity;
                    HoveredEntity = entity;
                    if (!InputManager.instance.mouseOverUI && m_ApplyAction.WasPressedThisFrame() && entity != m_Controller.CurrentEntity.Value)
                    {
                        ChangeHighlighting_MainThread(m_Controller.CurrentEntity.Value, ChangeMode.RemoveHighlight);
                        ChangeHighlighting_MainThread(entity, ChangeMode.AddHighlight);
                        m_Controller.CurrentEntity.Value = entity;
                        m_Controller.CurrentItemIdx.Value = 0;
                        UpdateItemCount();

                        m_Controller.OnCurrentItemChanged();

                        if (collide && !(callback?.Invoke(entity) ?? true))
                        {
                            RequestDisable();
                        }
                    }

                    else if (hoveredEntity != HoveredEntity)
                    {
                        if (hoveredEntity != m_Controller.CurrentEntity.Value)
                        {
                            ChangeHighlighting_MainThread(hoveredEntity, ChangeMode.RemoveHighlight);
                        }
                        ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.AddHighlight);
                    }
                }
                else if (HoveredEntity != Entity.Null)
                {
                    if (HoveredEntity != m_Controller.CurrentEntity.Value)
                    {
                        ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.RemoveHighlight);
                    }
                    HoveredEntity = Entity.Null;
                }

                return inputDeps;
            }
            else
            {
                if (HoveredEntity != Entity.Null)
                {
                    ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.RemoveHighlight);
                    HoveredEntity = Entity.Null;
                }
                if (m_Controller.IsValidEditingItem())
                {
                    if (!InputManager.instance.mouseOverUI && m_ApplyAction.WasPressedThisFrame())
                    {
                        var currentItem = m_Controller.CurrentEditingItem;
                        m_mousePositionRef = new float2(InputManager.instance.mousePosition.x, InputManager.instance.mousePosition.y);
                        m_originalPositionText = currentItem.offsetPosition;
                        m_isDragging = true;
                    }
                    else if (m_isDragging && m_ApplyAction.WasReleasedThisFrame())
                    {
                        var cmdBuff = m_ToolOutputBarrier.CreateCommandBuffer();
                        ApplyPosition(cmdBuff);
                        m_mousePositionRef = default;
                        m_originalPositionText = default;
                        m_isDragging = false;
                    }
                    else if (m_isDragging && m_ApplyAction.IsPressed())
                    {
                        var cmdBuff = m_ToolOutputBarrier.CreateCommandBuffer();
                        ApplyPosition(cmdBuff);
                    }
                }
                else
                {
                    m_isDragging = false;
                }
                return inputDeps;
            }
        }

        private void UpdateItemCount()
        {
            m_Controller.CurrentItemCount.Value = EntityManager.TryGetBuffer<WESimulationTextComponent>(m_Controller.CurrentEntity.Value, true, out var buff) ? buff.Length : 0;
        }

        private void ApplyPosition(EntityCommandBuffer cmdBuff)
        {
            var currentMousePos = new float2(InputManager.instance.mousePosition.x, InputManager.instance.mousePosition.y);
            var offsetMouse = currentMousePos - m_mousePositionRef;
            var currentPrecision = precisionIdx[m_Controller.MouseSensibility.Value];
            var offsetWithAdjust = offsetMouse * currentPrecision;

            if (!EntityManager.TryGetBuffer<WESimulationTextComponent>(m_Controller.CurrentEntity.Value, false, out var currentBuffer))
            {
                currentBuffer = new DynamicBuffer<WESimulationTextComponent>();
            };
            var currentItem = currentBuffer[m_Controller.CurrentItemIdx.Value];
            m_Controller.CurrentPosition.Value = currentItem.offsetPosition = m_originalPositionText + (ToolEditMode)m_Controller.CurrentPlaneMode.Value switch
            {
                ToolEditMode.PlaneXY => math.mul(currentItem.offsetRotation, new float3(offsetWithAdjust, 0)),
                ToolEditMode.PlaneXZ => math.mul(currentItem.offsetRotation, new float3(offsetWithAdjust.x, 0, offsetWithAdjust.y)),
                ToolEditMode.PlaneZY => math.mul(currentItem.offsetRotation, new float3(0, offsetWithAdjust.y, -offsetWithAdjust.x)),
                _ => default
            };
            currentBuffer[m_Controller.CurrentItemIdx.Value] = currentItem;
            cmdBuff.SetBuffer<WESimulationTextComponent>(m_Controller.CurrentEntity.Value).CopyFrom(currentBuffer);
            cmdBuff.AddComponent<BatchesUpdated>(m_Controller.CurrentEntity.Value);
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            ChangeHighlighting_MainThread(m_Controller.CurrentEntity.Value, ChangeMode.RemoveHighlight);
            ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.RemoveHighlight);
            m_Controller.CurrentEntity.Value = Entity.Null;
            HoveredEntity = Entity.Null;
            m_ApplyAction.shouldBeEnabled = false;
        }
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
            m_Controller.CurrentEntity.Value = default;
            m_Controller.CurrentItemIdx.Value = 0;
        }
        public void Select()
        {
            m_ToolSystem.activeTool = this;
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
            AddHighlight,
            RemoveHighlight
        }

        internal enum ToolMode
        {
            ParentPicker,
            ItemEditor
        }
        public enum ToolEditMode
        {
            PlaneXY,
            PlaneZY,
            PlaneXZ
        }
    }

}