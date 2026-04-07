using Belzont.Interfaces;
using Colossal.Entities;
using Game;
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
    internal partial class WEWorldPickerTool : IBelzontToolSystem
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
        private ProxyAction m_increasePrecisionValue;
        private ProxyAction m_reducePrecisionValue;
        private ProxyAction m_instanceNavZNext;
        private ProxyAction m_instanceNavZPrev;
        private ProxyAction m_instanceNavXNext;
        private ProxyAction m_instanceNavXPrev;
        private ProxyAction m_instanceNavYNext;
        private ProxyAction m_instanceNavYPrev;
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
        private ProxyAction m_treeNavNext;
        private ProxyAction m_treeNavPrev;
        private ProxyAction m_treeToggleFold;
        private ProxyAction m_treeDelete;
        private ProxyAction m_treeFoldAll;
        private ProxyAction m_treeUnfoldAll;
        private WEWorldPickerController m_Controller;
        private WETextDataMeshController m_MeshDataController;
        private WETextDataTransformController m_TransformController;
        private WETextDataMaterialController m_MaterialController;

        private float2 m_mousePositionRef;
        private float3 m_originalPositionText;
        private float m_mousePositionRefRot;
        private float3 m_originalRotationText;
        private bool m_isDragging;
        private bool m_isRotating;

        private byte m_keyMoveRotateCooldown;
        private CinemachineRestrictToTerrain m_terrainRestriction;

        protected override void OnCreateWithBarrier()
        {
            Enabled = false;
            m_MoveAction = WEModData.Instance.GetAction(WEModData.kActionApplyMouse);
            m_RotateAction = WEModData.Instance.GetAction(WEModData.kActionCancelMouse);

            m_increasePrecisionValue = WEModData.Instance.GetAction(WEModData.kActionIncreaseMovementStrenght);
            m_reducePrecisionValue = WEModData.Instance.GetAction(WEModData.kActionReduceMovementStrenght);

            m_instanceNavZNext = WEModData.Instance.GetAction(WEModData.kActionInstanceNavZNext);
            m_instanceNavZPrev = WEModData.Instance.GetAction(WEModData.kActionInstanceNavZPrev);
            m_instanceNavXNext = WEModData.Instance.GetAction(WEModData.kActionInstanceNavXNext);
            m_instanceNavXPrev = WEModData.Instance.GetAction(WEModData.kActionInstanceNavXPrev);
            m_instanceNavYNext = WEModData.Instance.GetAction(WEModData.kActionInstanceNavYNext);
            m_instanceNavYPrev = WEModData.Instance.GetAction(WEModData.kActionInstanceNavYPrev);

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
            m_treeNavNext = WEModData.Instance.GetAction(WEModData.kActionTreeNavNext);
            m_treeNavPrev = WEModData.Instance.GetAction(WEModData.kActionTreeNavPrev);
            m_treeToggleFold = WEModData.Instance.GetAction(WEModData.kActionTreeToggleFold);
            m_treeDelete = WEModData.Instance.GetAction(WEModData.kActionTreeDelete);
            m_treeFoldAll = WEModData.Instance.GetAction(WEModData.kActionTreeFoldAll);
            m_treeUnfoldAll = WEModData.Instance.GetAction(WEModData.kActionTreeUnfoldAll);

            m_CameraZoomAction = InputManager.instance.FindAction("Camera", "Zoom");
            m_Controller = World.GetOrCreateSystemManaged<WEWorldPickerController>();
            m_MeshDataController = World.GetOrCreateSystemManaged<WETextDataMeshController>();
            m_cameraSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_TransformController = World.GetOrCreateSystemManaged<WETextDataTransformController>();
            m_MaterialController = World.GetOrCreateSystemManaged<WETextDataMaterialController>();
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
            m_instanceNavZPrev.shouldBeEnabled = true;
            m_instanceNavZNext.shouldBeEnabled = true;
            m_instanceNavXNext.shouldBeEnabled = true;
            m_instanceNavXPrev.shouldBeEnabled = true;
            m_instanceNavYNext.shouldBeEnabled = true;
            m_instanceNavYPrev.shouldBeEnabled = true;
            m_alternateFixedCamera.shouldBeEnabled = true;
            m_useXY.shouldBeEnabled = true;
            m_useXZ.shouldBeEnabled = true;
            m_useZY.shouldBeEnabled = true;
            m_cycleAxisLock.shouldBeEnabled = true;
            m_ToggleLockCameraRotation.shouldBeEnabled = true;
            m_treeNavNext.shouldBeEnabled = true;
            m_treeNavPrev.shouldBeEnabled = true;
            m_treeToggleFold.shouldBeEnabled = true;
            m_treeDelete.shouldBeEnabled = true;
            m_treeFoldAll.shouldBeEnabled = true;
            m_treeUnfoldAll.shouldBeEnabled = true;

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
            m_instanceNavZPrev.shouldBeEnabled = false;
            m_instanceNavZNext.shouldBeEnabled = false;
            m_instanceNavXNext.shouldBeEnabled = false;
            m_instanceNavXPrev.shouldBeEnabled = false;
            m_instanceNavYNext.shouldBeEnabled = false;
            m_instanceNavYPrev.shouldBeEnabled = false;
            m_increasePrecisionValue.shouldBeEnabled = false;
            m_reducePrecisionValue.shouldBeEnabled = false;
            m_alternateFixedCamera.shouldBeEnabled = false;
            m_useXY.shouldBeEnabled = false;
            m_useXZ.shouldBeEnabled = false;
            m_useZY.shouldBeEnabled = false;
            m_cycleAxisLock.shouldBeEnabled = false;
            m_treeNavNext.shouldBeEnabled = false;
            m_treeNavPrev.shouldBeEnabled = false;
            m_treeToggleFold.shouldBeEnabled = false;
            m_treeDelete.shouldBeEnabled = false;
            m_treeFoldAll.shouldBeEnabled = false;
            m_treeUnfoldAll.shouldBeEnabled = false;

            m_moveLeft.shouldBeEnabled = false;
            m_moveRight.shouldBeEnabled = false;
            m_moveUp.shouldBeEnabled = false;
            m_moveDown.shouldBeEnabled = false;
            m_rotateClockwise.shouldBeEnabled = false;
            m_rotateCounterClockwise.shouldBeEnabled = false;

            m_cameraSystem.cinematicCameraController.collisionsEnabled = true;
            m_cameraSystem.cinematicCameraController.inputEnabled = true;
            if (m_terrainRestriction != null) m_terrainRestriction.enabled = true;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (m_Controller.CurrentEntity.Value == default)
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.MovingObjects;
                m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.Outside | RaycastFlags.BuildingLots | RaycastFlags.SubBuildings | RaycastFlags.Markers | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.Decals;
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
            else if (m_MeshDataController.TextSourceType.Value != (int)WESimulationTextType.MatrixTransform)
            {
                if (m_increasePrecisionValue.WasPressedThisFrame()) m_Controller.MouseSensibility.ChangeValueWithEffects(Math.Max(m_Controller.MouseSensibility.Value - 1, 0));
                if (m_reducePrecisionValue.WasPressedThisFrame()) m_Controller.MouseSensibility.ChangeValueWithEffects(Math.Min(m_Controller.MouseSensibility.Value + 1, precisionIdx.Length - 1));

                if ((m_instanceNavZNext.WasPressedThisFrame() || m_instanceNavZPrev.WasPressedThisFrame() ||
                     m_instanceNavXNext.WasPressedThisFrame() || m_instanceNavXPrev.WasPressedThisFrame() ||
                     m_instanceNavYNext.WasPressedThisFrame() || m_instanceNavYPrev.WasPressedThisFrame()) &&
                    EntityManager.TryGetComponent<WETextDataTransform>(m_Controller.CurrentSubEntity.Value, out var navTransform))
                {
                    var instCounts = navTransform.InstanceCountByAxisOrder;
                    var totalInstances = (int)math.min((long)instCounts[0] * instCounts[1] * instCounts[2], 256);
                    if (totalInstances > 1)
                    {
                        var iIdx = m_Controller.CurrentInstanceIdx.Value;
                        int iM = (int)(iIdx % instCounts[0]);
                        int iN = (int)((iIdx / (long)instCounts[0]) % instCounts[1]);
                        int iO = (int)(iIdx / ((long)instCounts[0] * instCounts[1]));
                        int[] comps = new[] { iM, iN, iO };
                        int[] counts = new[] { (int)instCounts[0], (int)instCounts[1], (int)instCounts[2] };
                        // Map physical axis (X=0, Y=1, Z=2) to M/N/O position based on growth order
                        int xPos, yPos, zPos;
                        switch (navTransform.arrayAxisGrowthOrder)
                        {
                            case WETextDataTransform.ArrayInstancingAxisOrder.XZY: xPos = 0; yPos = 2; zPos = 1; break;
                            case WETextDataTransform.ArrayInstancingAxisOrder.YXZ: xPos = 1; yPos = 0; zPos = 2; break;
                            case WETextDataTransform.ArrayInstancingAxisOrder.YZX: xPos = 2; yPos = 0; zPos = 1; break;
                            case WETextDataTransform.ArrayInstancingAxisOrder.ZXY: xPos = 1; yPos = 2; zPos = 0; break;
                            case WETextDataTransform.ArrayInstancingAxisOrder.ZYX: xPos = 2; yPos = 1; zPos = 0; break;
                            default: xPos = 0; yPos = 1; zPos = 2; break; // XYZ
                        }
                        int targetAxis = -1, delta = 0;
                        if (m_instanceNavZNext.WasPressedThisFrame()) { targetAxis = zPos; delta = 1; }
                        else if (m_instanceNavZPrev.WasPressedThisFrame()) { targetAxis = zPos; delta = -1; }
                        else if (m_instanceNavXNext.WasPressedThisFrame()) { targetAxis = xPos; delta = 1; }
                        else if (m_instanceNavXPrev.WasPressedThisFrame()) { targetAxis = xPos; delta = -1; }
                        else if (m_instanceNavYNext.WasPressedThisFrame()) { targetAxis = yPos; delta = 1; }
                        else if (m_instanceNavYPrev.WasPressedThisFrame()) { targetAxis = yPos; delta = -1; }
                        if (targetAxis >= 0 && counts[targetAxis] > 1)
                        {
                            comps[targetAxis] = math.clamp(comps[targetAxis] + delta, 0, counts[targetAxis] - 1);
                            var calcNewIdx = comps[0] + comps[1] * counts[0] + comps[2] * counts[0] * counts[1];
                            if (calcNewIdx > 0 && calcNewIdx < totalInstances)
                            {
                                m_Controller.CurrentInstanceIdx.ChangeValueWithEffects(calcNewIdx);
                            }
                        }
                    }
                }

                if (m_useXY.WasPressedThisFrame()) m_Controller.CurrentPlaneMode.ChangeValueWithEffects((int)(m_Controller.CurrentPlaneMode.Value == (int)ToolEditMode.PlaneXY ? ToolEditMode.PlaneBackXY : ToolEditMode.PlaneXY));
                if (m_useXZ.WasPressedThisFrame()) m_Controller.CurrentPlaneMode.ChangeValueWithEffects((int)(m_Controller.CurrentPlaneMode.Value == (int)ToolEditMode.PlaneXZ ? ToolEditMode.PlaneBackXZ : ToolEditMode.PlaneXZ));
                if (m_useZY.WasPressedThisFrame()) m_Controller.CurrentPlaneMode.ChangeValueWithEffects((int)(m_Controller.CurrentPlaneMode.Value == (int)ToolEditMode.PlaneZY ? ToolEditMode.PlaneBackZY : ToolEditMode.PlaneZY));
                if (m_alternateFixedCamera.WasPressedThisFrame()) m_Controller.CameraLocked.ChangeValueWithEffects(!m_Controller.CameraLocked.Value);
                if (m_cycleAxisLock.WasPressedThisFrame()) m_Controller.CurrentMoveMode.ChangeValueWithEffects((1 + m_Controller.CurrentMoveMode.Value) % 3);
                if (m_Controller.CameraLocked.Value && m_ToggleLockCameraRotation.WasPressedThisFrame()) m_Controller.CameraRotationLocked.ChangeValueWithEffects(!m_Controller.CameraRotationLocked.Value);

                if (m_treeNavNext.WasPressedThisFrame()) m_Controller.FireTreeNavAction(WEWorldPickerController.TreeNavAction.NavNext);
                else if (m_treeNavPrev.WasPressedThisFrame()) m_Controller.FireTreeNavAction(WEWorldPickerController.TreeNavAction.NavPrev);
                else if (m_treeToggleFold.WasPressedThisFrame()) m_Controller.FireTreeNavAction(WEWorldPickerController.TreeNavAction.ToggleFold);
                else if (m_treeDelete.WasPressedThisFrame() && m_Controller.IsValidEditingItem()) m_Controller.FireTreeNavAction(WEWorldPickerController.TreeNavAction.Delete);
                else if (m_treeFoldAll.WasPressedThisFrame()) m_Controller.FireTreeNavAction(WEWorldPickerController.TreeNavAction.FoldAll);
                else if (m_treeUnfoldAll.WasPressedThisFrame()) m_Controller.FireTreeNavAction(WEWorldPickerController.TreeNavAction.UnfoldAll);


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
                        var currentItem = EntityManager.GetComponentData<WETextDataTransform>(m_Controller.CurrentSubEntity.Value);
                        m_mousePositionRef = new float2(InputManager.instance.mousePosition.x, InputManager.instance.mousePosition.y);
                        m_originalPositionText = currentItem.offsetPosition;
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
                        var currentItem = EntityManager.GetComponentData<WETextDataTransform>(m_Controller.CurrentSubEntity.Value);
                        m_mousePositionRefRot = InputManager.instance.mousePosition.x;
                        m_originalRotationText = ((Quaternion)currentItem.offsetRotation).eulerAngles;
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
                            if (m_terrainRestriction == null)
                                m_terrainRestriction = m_cameraSystem.cinematicCameraController.GetComponentInChildren<CinemachineRestrictToTerrain>();
                            if (m_terrainRestriction != null) m_terrainRestriction.enabled = false;
                        }
#pragma warning restore CS0252 // Possível comparação de referência inesperada; o lado esquerdo precisa de conversão
                        m_cameraDisabledHere = cameraDisabledThisFrame = true;
                        m_cameraDistance = math.clamp(m_cameraDistance + (m_CameraZoomAction.ReadValue<float>() * 4f), 1f, 30f);

                        var targetMatrix = CalculateCameraMatrix();

                        var entityPos = (float3)m_Controller.CurrentItemMatrix.GetPosition();
                        if (EntityManager.TryGetComponent<WETextDataTransform>(m_Controller.CurrentSubEntity.Value, out var camTransform))
                        {
                            var instCounts = camTransform.InstanceCountByAxisOrder;
                            var spacings = camTransform.SpacingByAxisOrder;
                            var iIdx = m_Controller.CurrentInstanceIdx.Value;
                            int iM = (int)(iIdx % instCounts[0]);
                            int iN = (int)((iIdx / (long)instCounts[0]) % instCounts[1]);
                            int iO = (int)(iIdx / ((long)instCounts[0] * instCounts[1]));
                            entityPos += iM * spacings[0] + iN * spacings[1] + iO * spacings[2];
                        }
                        m_cameraSystem.cinematicCameraController.pivot = (Vector3)entityPos + (Matrix4x4.TRS(default, targetMatrix.rotation, Vector3.one)).MultiplyPoint(new Vector3(0, 0, -m_cameraDistance));
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
                if (m_terrainRestriction != null) m_terrainRestriction.enabled = true;
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

            var currentItem = EntityManager.GetComponentData<WETextDataTransform>(m_Controller.CurrentSubEntity.Value);
            ApplyPosition(currentItem.offsetPosition, offsetRef);
        }

        private void ApplyPosition(float3 originalPosition, Vector2 offsetPosition)
        {
            var cmdBuff = Barrier.CreateCommandBuffer();
            var currentPrecision = precisionIdx[m_Controller.MouseSensibility.Value];
            var offsetWithAdjust = offsetPosition * currentPrecision;
            if (!EntityManager.TryGetComponent<WETextDataTransform>(m_Controller.CurrentSubEntity.Value, out var currentItem)) return;

            var itemAngles = m_TransformController.CurrentRotation.Value;
            var isRotationLocked = m_Controller.CameraRotationLocked.Value;

            m_TransformController.CurrentPosition.Value = currentItem.offsetPosition = originalPosition + (ToolEditMode)m_Controller.CurrentPlaneMode.Value switch
            {
                ToolEditMode.PlaneXY => math.mul((Matrix4x4.Rotate(currentItem.offsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(isRotationLocked ? -itemAngles.x : 0, 0, 0))).rotation, new float3(offsetWithAdjust, 0)),
                ToolEditMode.PlaneXZ => math.mul((Matrix4x4.Rotate(currentItem.offsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(0, isRotationLocked ? -itemAngles.y : 0, 0))).rotation, new float3(offsetWithAdjust.x, 0, -offsetWithAdjust.y)),
                ToolEditMode.PlaneZY => math.mul((Matrix4x4.Rotate(currentItem.offsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, isRotationLocked ? -itemAngles.z : 0))).rotation, new float3(0, offsetWithAdjust.y, -offsetWithAdjust.x)),

                // Back planes - inverted horizontal movement
                ToolEditMode.PlaneBackXY => math.mul((Matrix4x4.Rotate(currentItem.offsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(isRotationLocked ? -itemAngles.x : 0, 0, 0))).rotation, new float3(-offsetWithAdjust.x, offsetWithAdjust.y, 0)),
                ToolEditMode.PlaneBackXZ => math.mul((Matrix4x4.Rotate(currentItem.offsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(0, isRotationLocked ? -itemAngles.y : 0, 0))).rotation, new float3(-offsetWithAdjust.x, 0, -offsetWithAdjust.y)),
                ToolEditMode.PlaneBackZY => math.mul((Matrix4x4.Rotate(currentItem.offsetRotation) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, isRotationLocked ? -itemAngles.z : 0))).rotation, new float3(0, offsetWithAdjust.y, offsetWithAdjust.x)),

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

            var currentItem = EntityManager.GetComponentData<WETextDataTransform>(m_Controller.CurrentSubEntity.Value);
            ApplyRotation(((Quaternion)currentItem.offsetRotation).eulerAngles, offset);
        }
        private void ApplyRotation(float3 originalRotation, float value)
        {
            var cmdBuff = Barrier.CreateCommandBuffer();
            var currentPrecision = precisionIdx[m_Controller.MouseSensibility.Value] * 10;
            var offsetWithAdjust = value * currentPrecision;

            if (!EntityManager.TryGetComponent<WETextDataTransform>(m_Controller.CurrentSubEntity.Value, out var currentItem)) return;

            m_TransformController.CurrentRotation.Value = originalRotation + (ToolEditMode)m_Controller.CurrentPlaneMode.Value switch
            {
                ToolEditMode.PlaneXY => new float3(0, 0, offsetWithAdjust),
                ToolEditMode.PlaneXZ => new float3(0, offsetWithAdjust, 0),
                ToolEditMode.PlaneZY => new float3(offsetWithAdjust, 0, 0),

                // Back planes - inverted rotation
                ToolEditMode.PlaneBackXY => new float3(0, 0, -offsetWithAdjust),
                ToolEditMode.PlaneBackXZ => new float3(0, -offsetWithAdjust, 0),
                ToolEditMode.PlaneBackZY => new float3(-offsetWithAdjust, 0, 0),

                _ => default
            };
            currentItem.offsetRotation = Quaternion.Euler(m_TransformController.CurrentRotation.Value);
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

        private Matrix4x4 CalculateCameraMatrix()
        {
            var isDecal = m_MaterialController.ShaderType.Value == WEShader.Decal;
            var itemAngles = m_TransformController.CurrentRotation.Value;
            var isRotationLocked = m_Controller.CameraRotationLocked.Value;
            var planeMode = (ToolEditMode)m_Controller.CurrentPlaneMode.Value;

            var cameraRotation = GetCameraRotationForPlane(planeMode, isDecal, itemAngles, isRotationLocked);
            return m_Controller.CurrentItemMatrix * Matrix4x4.Rotate(cameraRotation);
        }

        private Quaternion GetCameraRotationForPlane(ToolEditMode planeMode, bool isDecal, float3 itemAngles, bool isRotationLocked)
        {
            var decalBaseRotation = Quaternion.Euler(-90, 180, 0);

            return planeMode switch
            {
                // Front planes
                ToolEditMode.PlaneXY => GetPlaneXYRotation(isDecal, itemAngles, isRotationLocked, decalBaseRotation, false),
                ToolEditMode.PlaneXZ => GetPlaneXZRotation(isDecal, itemAngles, isRotationLocked, decalBaseRotation, true),
                ToolEditMode.PlaneZY => GetPlaneZYRotation(isDecal, itemAngles, isRotationLocked, decalBaseRotation, false),

                // Back planes
                ToolEditMode.PlaneBackXY => GetPlaneXYRotation(isDecal, itemAngles, isRotationLocked, decalBaseRotation, true),
                ToolEditMode.PlaneBackXZ => GetPlaneXZRotation(isDecal, itemAngles, isRotationLocked, decalBaseRotation, false),
                ToolEditMode.PlaneBackZY => GetPlaneZYRotation(isDecal, itemAngles, isRotationLocked, decalBaseRotation, true),

                _ => Quaternion.identity
            };
        }

        private Quaternion GetPlaneXYRotation(bool isDecal, float3 itemAngles, bool isRotationLocked, Quaternion decalBaseRotation, bool isBackFacing)
        {
            var baseYaw = isBackFacing ? 0f : 180f;
            var rotationZ = isRotationLocked ? -itemAngles.z : 0f;

            if (isDecal)
            {
                return decalBaseRotation * Quaternion.Euler(0, baseYaw, rotationZ);
            }
            return Quaternion.Euler(0, baseYaw, rotationZ);
        }

        private Quaternion GetPlaneXZRotation(bool isDecal, float3 itemAngles, bool isRotationLocked, Quaternion decalBaseRotation, bool isBackFacing)
        {
            var zFlippedAmmount = Math.Sign(m_TransformController.CurrentScale.Value.z);
            var basePitch = (isBackFacing ? 90f : -90f) + (m_Controller.PlaneTilt.Value * zFlippedAmmount);
            var baseYaw = isBackFacing ? 0f : 180f;
            var rotationY = (isRotationLocked ? -itemAngles.y : 0f) + baseYaw;

            if (isDecal)
            {
                return decalBaseRotation * Quaternion.Euler(basePitch, rotationY, 180);
            }
            return Quaternion.Euler(basePitch, rotationY, 180);
        }

        private Quaternion GetPlaneZYRotation(bool isDecal, float3 itemAngles, bool isRotationLocked, Quaternion decalBaseRotation, bool isBackFacing)
        {
            var zFlippedAmmount = Math.Sign(m_TransformController.CurrentScale.Value.z);
            var baseYaw = (isBackFacing ? 1 : -1) * (90 + (m_Controller.PlaneTilt.Value * zFlippedAmmount));
            var rotationX = isRotationLocked ? -itemAngles.x : 0f;

            if (isDecal)
            {
                return decalBaseRotation * Quaternion.Euler(rotationX, baseYaw, 0);
            }
            return Quaternion.Euler(rotationX, baseYaw, 0);
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
            PlaneXZ,
            PlaneBackXY,
            PlaneBackZY,
            PlaneBackXZ
        }
    }

}