using Colossal.Entities;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public partial class WEWorldPickerTool : ToolBaseSystem
    {
        public static readonly float[] precisionIdx = new[] { 1f, 1 / 2f, 1 / 4f, 1 / 10f, 1 / 20f, 1 / 40f, 1 / 100f, 1 / 200f, 1 / 400f, 1 / 1000f };

        public override string toolID => $"K45_WE_WEWorldPickerTool";

        public float3 LastPos;
        public Entity HoveredEntity;
        private CameraUpdateSystem m_cameraSystem;
        private IGameCameraController m_oldController;
        private float m_cameraDistance = 5f;
        private bool m_cameraDisabledHere;
        private Entity entityToSelectOnStart;

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        public override int uiModeIndex => base.uiModeIndex;

        public bool IsSelected => m_ToolSystem.activeTool == this;


        private ProxyAction m_MoveAction;
        private ProxyAction m_RotateAction;
        private ProxyAction m_CameraZoomAction;
        private ProxyAction m_CameraZoomActionMouse;
        private ProxyAction m_increasePrecisionValue;
        private ProxyAction m_reducePrecisionValue;
        //private ProxyAction m_nextText;
        //private ProxyAction m_prevText;
        private ProxyAction m_moveLeft;
        private ProxyAction m_moveRight;
        private ProxyAction m_moveUp;
        private ProxyAction m_moveDown;
        private ProxyAction m_rotateClockwise;
        private ProxyAction m_rotateCounterClockwise;
        private ProxyAction m_alternateFixedCamera;
        private ProxyAction m_useXY;
        private ProxyAction m_useXZ;
        private ProxyAction m_useZY;
        private ProxyAction m_cycleAxisLock;
        private ProxyAction m_ToggleLockCameraRotation;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private WEWorldPickerController m_Controller;

        private float2 m_mousePositionRef;
        private float3 m_originalPositionText;
        private float m_mousePositionRefRot;
        private float3 m_originalRotationText;
        private bool m_isDragging;
        private bool m_isRotating;

        private byte m_keyMoveRotateCooldown;

        protected override void OnCreate()
        {
            Enabled = false;
            m_MoveAction = WEModData.Instance.GetAction(WEModData.kActionApplyMouse);
            m_RotateAction = WEModData.Instance.GetAction(WEModData.kActionCancelMouse);

            m_increasePrecisionValue = WEModData.Instance.GetAction(WEModData.kActionIncreaseMovementStrenght);
            m_reducePrecisionValue = WEModData.Instance.GetAction(WEModData.kActionReduceMovementStrenght);


            //m_nextText = WEModData.Instance.GetAction(WEModData.kActionNextText);
            //m_prevText = WEModData.Instance.GetAction(WEModData.kActionPreviousText);

            m_moveLeft = WEModData.Instance.GetAction(WEModData.kActionMoveLeft);
            m_moveRight = WEModData.Instance.GetAction(WEModData.kActionMoveRight);
            m_moveUp = WEModData.Instance.GetAction(WEModData.kActionMoveUp);
            m_moveDown = WEModData.Instance.GetAction(WEModData.kActionMoveDown);
            m_rotateClockwise = WEModData.Instance.GetAction(WEModData.kActionRotateClockwise);
            m_rotateCounterClockwise = WEModData.Instance.GetAction(WEModData.kActionRotateCounterClockwise);


            m_alternateFixedCamera = WEModData.Instance.GetAction(WEModData.kActionAlternateFixedCamera);
            m_useXY = WEModData.Instance.GetAction(WEModData.kActionPerspectiveXY);
            m_useXZ = WEModData.Instance.GetAction(WEModData.kActionPerspectiveXZ);
            m_useZY = WEModData.Instance.GetAction(WEModData.kActionPerspectiveZY);
            m_cycleAxisLock = WEModData.Instance.GetAction(WEModData.kActionCycleEditAxisLock);
            m_ToggleLockCameraRotation = WEModData.Instance.GetAction(WEModData.kActionToggleLockCameraRotation);

            m_CameraZoomAction = InputManager.instance.FindAction("Camera", "Zoom");
            m_CameraZoomActionMouse = InputManager.instance.FindAction("Camera", "Zoom Mouse");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_Controller = World.GetOrCreateSystemManaged<WEWorldPickerController>();
            m_cameraSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();



            base.OnCreate();
        }
        protected override void OnStartRunning()
        {
            m_Controller.CurrentEntity.Value = entityToSelectOnStart;
            entityToSelectOnStart = default;
            m_Controller.OnCurrentItemChanged();
            m_Controller.IsValidEditingItem();

            m_MoveAction.shouldBeEnabled = true;
            m_RotateAction.shouldBeEnabled = true;
            m_increasePrecisionValue.shouldBeEnabled = true;
            m_reducePrecisionValue.shouldBeEnabled = true;
            //m_prevText.shouldBeEnabled = true;
            //m_nextText.shouldBeEnabled = true;
            m_alternateFixedCamera.shouldBeEnabled = true;
            m_useXY.shouldBeEnabled = true;
            m_useXZ.shouldBeEnabled = true;
            m_useZY.shouldBeEnabled = true;
            m_cycleAxisLock.shouldBeEnabled = true;
            m_ToggleLockCameraRotation.shouldBeEnabled = true;

            m_moveLeft.shouldBeEnabled = true;
            m_moveRight.shouldBeEnabled = true;
            m_moveUp.shouldBeEnabled = true;
            m_moveDown.shouldBeEnabled = true;
            m_rotateClockwise.shouldBeEnabled = true;
            m_rotateCounterClockwise.shouldBeEnabled = true;

            m_Controller.FontList.Value = FontServer.Instance.GetLoadedFontsNames();
            m_Controller.FontList.UpdateUIs();
        }
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            if (m_cameraDisabledHere)
            {
                if (m_oldController != null)
                {
                    m_cameraSystem.activeCameraController = m_oldController;
                    m_oldController = null;
                }
                m_cameraDisabledHere = false;
            }
            ChangeHighlighting_MainThread(m_Controller.CurrentEntity.Value, ChangeMode.RemoveHighlight);
            ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.RemoveHighlight);
            m_Controller.CurrentEntity.Value = Entity.Null;
            HoveredEntity = Entity.Null;
            m_MoveAction.shouldBeEnabled = false;
            m_RotateAction.shouldBeEnabled = false;
            //m_prevText.shouldBeEnabled = false;
            //m_nextText.shouldBeEnabled = false;
            m_increasePrecisionValue.shouldBeEnabled = false;
            m_reducePrecisionValue.shouldBeEnabled = false;
            m_alternateFixedCamera.shouldBeEnabled = false;
            m_useXY.shouldBeEnabled = false;
            m_useXZ.shouldBeEnabled = false;
            m_useZY.shouldBeEnabled = false;
            m_cycleAxisLock.shouldBeEnabled = false;

            m_moveLeft.shouldBeEnabled = false;
            m_moveRight.shouldBeEnabled = false;
            m_moveUp.shouldBeEnabled = false;
            m_moveDown.shouldBeEnabled = false;
            m_rotateClockwise.shouldBeEnabled = false;
            m_rotateCounterClockwise.shouldBeEnabled = false;

            m_cameraSystem.cinematicCameraController.collisionsEnabled = true;
            m_cameraSystem.cinematicCameraController.inputEnabled = true;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (m_Controller.CurrentEntity.Value == default)
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.MovingObjects;
                m_ToolRaycastSystem.raycastFlags = (RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.UpgradeIsMain | RaycastFlags.Outside | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.Decals);
                m_ToolRaycastSystem.netLayerMask = (Layer)~0u;
                m_ToolRaycastSystem.iconLayerMask = (IconLayerMask)~0u;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            bool cameraDisabledThisFrame = false;
            if (m_Controller.CurrentEntity.Value == default)
            {
                bool collide = GetRaycastResult(out Entity entity, out RaycastHit raycastHit);

                LastPos = raycastHit.m_HitPosition;
                m_Controller.CurrentSubEntity.Value = Entity.Null;
                if (collide)
                {
                    Entity hoveredEntity = HoveredEntity;
                    HoveredEntity = entity;
                    if (!InputManager.instance.mouseOverUI && m_MoveAction.WasPressedThisFrame() && entity != m_Controller.CurrentEntity.Value)
                    {
                        ChangeHighlighting_MainThread(m_Controller.CurrentEntity.Value, ChangeMode.RemoveHighlight);
                        ChangeHighlighting_MainThread(entity, ChangeMode.AddHighlight);
                        m_Controller.CurrentEntity.Value = entity;
                        m_Controller.CurrentSubEntity.Value = Entity.Null;
                        m_Controller.OnCurrentItemChanged();
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
            }
            else
            {
                if (m_increasePrecisionValue.WasPressedThisFrame()) m_Controller.MouseSensibility.ChangeValueWithEffects(Math.Max(m_Controller.MouseSensibility.Value - 1, 0));
                if (m_reducePrecisionValue.WasPressedThisFrame()) m_Controller.MouseSensibility.ChangeValueWithEffects(Math.Min(m_Controller.MouseSensibility.Value + 1, precisionIdx.Length - 1));

                //if (m_nextText.WasPressedThisFrame()) m_Controller.CurrentItemIdx.ChangeValueWithEffects((m_Controller.CurrentItemIdx.Value + m_Controller.CurrentItemCount.Value - 1) % m_Controller.CurrentItemCount.Value);
                //if (m_prevText.WasPressedThisFrame()) m_Controller.CurrentItemIdx.ChangeValueWithEffects((m_Controller.CurrentItemIdx.Value + 1) % m_Controller.CurrentItemCount.Value);

                if (m_useXY.WasPressedThisFrame()) m_Controller.CurrentPlaneMode.ChangeValueWithEffects((int)ToolEditMode.PlaneXY);
                if (m_useXZ.WasPressedThisFrame()) m_Controller.CurrentPlaneMode.ChangeValueWithEffects((int)ToolEditMode.PlaneXZ);
                if (m_useZY.WasPressedThisFrame()) m_Controller.CurrentPlaneMode.ChangeValueWithEffects((int)ToolEditMode.PlaneZY);
                if (m_alternateFixedCamera.WasPressedThisFrame()) m_Controller.CameraLocked.ChangeValueWithEffects(!m_Controller.CameraLocked.Value);
                if (m_cycleAxisLock.WasPressedThisFrame()) m_Controller.CurrentMoveMode.ChangeValueWithEffects((1 + m_Controller.CurrentMoveMode.Value) % 3);
                if (m_Controller.CameraLocked.Value && m_ToggleLockCameraRotation.WasPressedThisFrame()) m_Controller.CameraRotationLocked.ChangeValueWithEffects(!m_Controller.CameraRotationLocked.Value);


                if (HoveredEntity != Entity.Null)
                {
                    ChangeHighlighting_MainThread(HoveredEntity, ChangeMode.RemoveHighlight);
                    HoveredEntity = Entity.Null;
                }
                if (m_Controller.IsValidEditingItem())
                {
                    if (m_keyMoveRotateCooldown > 0) m_keyMoveRotateCooldown--;
                    var hasKeyPressedMovedRotated = false;

                    if (!InputManager.instance.mouseOverUI && m_MoveAction.WasPressedThisFrame())
                    {
                        var currentItem = m_Controller.CurrentEditingItem;
                        m_mousePositionRef = new float2(InputManager.instance.mousePosition.x, InputManager.instance.mousePosition.y);
                        m_originalPositionText = currentItem.OffsetPosition;
                        m_isDragging = true;
                    }
                    else if (m_isDragging && m_MoveAction.WasReleasedThisFrame())
                    {
                        ApplyPositionMouseRelative();
                        m_mousePositionRef = default;
                        m_originalPositionText = default;
                        m_isDragging = false;
                    }
                    else if (m_isDragging && m_MoveAction.IsPressed())
                    {
                        ApplyPositionMouseRelative();
                    }
                    else if (m_keyMoveRotateCooldown == 0 && (
                        m_moveLeft.IsPressed() ||
                        m_moveRight.IsPressed() ||
                        m_moveUp.IsPressed() ||
                        m_moveDown.IsPressed())
                        )
                    {
                        ApplyPositionKeys();
                        m_keyMoveRotateCooldown = 8;
                        hasKeyPressedMovedRotated = true;
                    }

                    if (!InputManager.instance.mouseOverUI && m_RotateAction.WasPressedThisFrame())
                    {
                        var currentItem = m_Controller.CurrentEditingItem;
                        m_mousePositionRefRot = InputManager.instance.mousePosition.x;
                        m_originalRotationText = ((Quaternion)currentItem.OffsetRotation).eulerAngles;
                        m_isRotating = true;
                    }
                    else if (m_isRotating && m_RotateAction.WasReleasedThisFrame())
                    {
                        ApplyRotationMouseRelative();
                        m_mousePositionRefRot = default;
                        m_originalRotationText = default;
                        m_isRotating = false;
                    }
                    else if (m_isRotating && m_RotateAction.IsPressed())
                    {
                        ApplyRotationMouseRelative();
                    }
                    else if (m_keyMoveRotateCooldown == 0 && (
                        m_rotateClockwise.IsPressed() ||
                        m_rotateCounterClockwise.IsPressed())
                        )
                    {
                        ApplyRotationKeys();
                        m_keyMoveRotateCooldown = 8;
                        hasKeyPressedMovedRotated = true;
                    }

                    if (!hasKeyPressedMovedRotated)
                    {
                        m_keyMoveRotateCooldown = 0;
                    }

                    if (m_Controller.CameraLocked.Value)
                    {
#pragma warning disable CS0252 // Possível comparação de referência inesperada; o lado esquerdo precisa de conversão
                        if (m_cameraSystem.activeCameraController != m_cameraSystem.cinematicCameraController)
                        {
                            m_oldController = m_cameraSystem.activeCameraController;
                            m_cameraSystem.activeCameraController = m_cameraSystem.cinematicCameraController;
                            m_cameraSystem.cinematicCameraController.collisionsEnabled = false;
                            m_cameraSystem.cinematicCameraController.inputEnabled = false;
                        }
#pragma warning restore CS0252 // Possível comparação de referência inesperada; o lado esquerdo precisa de conversão
                        m_cameraDisabledHere = cameraDisabledThisFrame = true;
                        m_cameraDistance = math.clamp(m_cameraDistance + (m_CameraZoomActionMouse.ReadValue<float>() * 4f) + m_CameraZoomAction.ReadValue<float>(), 1f, 20f);
                        var itemAngles = m_Controller.CurrentRotation.Value;
                        var isRotationLocked = m_Controller.CameraRotationLocked.Value;
                        var targetMatrix = (ToolEditMode)m_Controller.CurrentPlaneMode.Value switch
                        {
                            ToolEditMode.PlaneZY => m_Controller.CurrentItemMatrix * Matrix4x4.Rotate(Quaternion.Euler(isRotationLocked ? -itemAngles.x : 0, 255, 0)),
                            ToolEditMode.PlaneXZ => m_Controller.CurrentItemMatrix * Matrix4x4.Rotate(Quaternion.Euler(75, (isRotationLocked ? -itemAngles.y : 0) + 180, 0)),
                            _ => m_Controller.CurrentItemMatrix * Matrix4x4.Rotate(Quaternion.Euler(0, 180, isRotationLocked ? -itemAngles.z : 0)),
                        };

                        m_cameraSystem.cinematicCameraController.pivot = m_Controller.CurrentItemMatrix.GetPosition() + (Matrix4x4.TRS(default, targetMatrix.rotation, Vector3.one)).MultiplyPoint(new Vector3(0, 0, -m_cameraDistance));


                        m_cameraSystem.cinematicCameraController.rotation = targetMatrix.rotation.eulerAngles;

                    }

                }
                else
                {
                    m_isDragging = false;
                    m_isRotating = false;
                }

            }
            if (m_cameraDisabledHere && !cameraDisabledThisFrame)
            {
                m_cameraSystem.activeCameraController = m_oldController;
                m_oldController = null;
                m_cameraDisabledHere = false;
            }
            return inputDeps;
        }

        private void ApplyPositionMouseRelative()
        {
            var moveMode = m_Controller.CurrentMoveMode.Value;
            var currentMousePos = new float2(InputManager.instance.mousePosition.x, InputManager.instance.mousePosition.y);
            var offsetMouse = (Vector2)(currentMousePos - m_mousePositionRef) * moveMode switch
            {
                1 => Vector2.left,
                2 => Vector2.up,
                _ => Vector2.left + Vector2.up,
            };
            ApplyPosition(m_originalPositionText, offsetMouse);
        }

        private void ApplyPositionKeys()
        {
            var offsetRef = new float2(
                m_moveLeft.IsPressed() ? 1 :
                m_moveRight.IsPressed() ? -1 : 0,
                m_moveUp.IsPressed() ? 1 :
                m_moveDown.IsPressed() ? -1 : 0
                );
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) offsetRef *= 10;

            ApplyPosition(m_Controller.CurrentEditingItem.OffsetPosition, offsetRef);
        }

        private void ApplyPosition(float3 originalPosition, Vector2 offsetPosition)
        {
            var cmdBuff = m_ToolOutputBarrier.CreateCommandBuffer();
            var currentPrecision = precisionIdx[m_Controller.MouseSensibility.Value];
            var offsetWithAdjust = offsetPosition * currentPrecision;
            if (!EntityManager.TryGetComponent<WETextData_>(m_Controller.CurrentSubEntity.Value, out var currentItem)) return;

            var itemAngles = m_Controller.CurrentRotation.Value;
            var isRotationLocked = m_Controller.CameraRotationLocked.Value;

            m_Controller.CurrentPosition.Value = currentItem.OffsetPosition = originalPosition + (ToolEditMode)m_Controller.CurrentPlaneMode.Value switch
            {
                ToolEditMode.PlaneXY => math.mul((Matrix4x4.Rotate(currentItem.OffsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(isRotationLocked ? -itemAngles.x : 0, 0, 0))).rotation, new float3(offsetWithAdjust, 0)),
                ToolEditMode.PlaneXZ => math.mul((Matrix4x4.Rotate(currentItem.OffsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(0, isRotationLocked ? -itemAngles.y : 0, 0))).rotation, new float3(offsetWithAdjust.x, 0, -offsetWithAdjust.y)),
                ToolEditMode.PlaneZY => math.mul((Matrix4x4.Rotate(currentItem.OffsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, isRotationLocked ? -itemAngles.z : 0))).rotation, new float3(0, offsetWithAdjust.y, -offsetWithAdjust.x)),
                _ => default
            };
            EntityManager.SetComponentData(m_Controller.CurrentSubEntity.Value, currentItem);
            cmdBuff.AddComponent<BatchesUpdated>(m_Controller.CurrentEntity.Value);
        }

        private void ApplyRotationMouseRelative()
        {
            var offsetMouse = InputManager.instance.mousePosition.x - m_mousePositionRefRot;

            ApplyRotation(m_originalRotationText, offsetMouse);
        }

        private void ApplyRotationKeys()
        {
            var offset = m_rotateClockwise.IsPressed() ? 1 : m_rotateCounterClockwise.IsPressed() ? -1 : 0;
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) offset *= 10;
            ApplyRotation(((Quaternion)m_Controller.CurrentEditingItem.OffsetRotation).eulerAngles, offset);
        }
        private void ApplyRotation(float3 originalRotation, float value)
        {
            var cmdBuff = m_ToolOutputBarrier.CreateCommandBuffer();
            var currentPrecision = precisionIdx[m_Controller.MouseSensibility.Value] * 10;
            var offsetWithAdjust = value * currentPrecision;

            if (!EntityManager.TryGetComponent<WETextData_>(m_Controller.CurrentSubEntity.Value, out var currentItem)) return;

            m_Controller.CurrentRotation.Value = originalRotation + (ToolEditMode)m_Controller.CurrentPlaneMode.Value switch
            {
                ToolEditMode.PlaneXY => new float3(0, 0, offsetWithAdjust),
                ToolEditMode.PlaneXZ => new float3(0, offsetWithAdjust, 0),
                ToolEditMode.PlaneZY => new float3(offsetWithAdjust, 0, 0),
                _ => default
            };
            currentItem.OffsetRotation = Quaternion.Euler(m_Controller.CurrentRotation.Value);
            EntityManager.SetComponentData(m_Controller.CurrentSubEntity.Value, currentItem);
            cmdBuff.AddComponent<BatchesUpdated>(m_Controller.CurrentEntity.Value);
        }

        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
            m_Controller.CurrentEntity.Value = default;
            m_Controller.CurrentSubEntity.Value = default;
        }
        public void Select(Entity e = default)
        {
            entityToSelectOnStart = e;
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