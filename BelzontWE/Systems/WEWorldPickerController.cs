using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game.Common;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using WriteEverywhere.Sprites;

namespace BelzontWE
{

    public partial class WEWorldPickerController : ComponentSystemBase, IBelzontBindable
    {
        private const string PREFIX = "wpicker.";

        #region UI Bindings
        Action<string, object[]> m_eventCaller;
        Action<string, Delegate> m_callBinder;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            m_callBinder = callBinder;
            callBinder($"{PREFIX}getItemsAvailable", GetItemsAvailable);
            callBinder($"{PREFIX}toolPrecisions", () => WEWorldPickerTool.precisionIdx);
            callBinder($"{PREFIX}enableTool", OnEnableTool);
            callBinder($"{PREFIX}addItem", AddItem);
            callBinder($"{PREFIX}removeItem", RemoveItem);
            callBinder($"{PREFIX}listAvailableLibraries", ListAvailableLibraries);
            callBinder($"{PREFIX}listAtlasImages", ListAtlasImages);
            callBinder($"{PREFIX}changeParent", ChangeParent);
            callBinder($"{PREFIX}cloneAsChild", CloneAsChild);
            callBinder($"{PREFIX}dumpBris", DumpBris);
            if (m_eventCaller != null) InitValueBindings();
        }

        private void DumpBris()
        {
            WERendererSystem.dumpNextFrame = true;
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            m_eventCaller = eventCaller;
            if (m_callBinder != null) InitValueBindings();
        }

        public void SetupEventBinder(Action<string, Delegate> eventBinder)
        {
        }

        #endregion

        #region UI Calls Fn

        private string[] GetItemsAvailable()
        {
            if (CurrentEntity.Value == Entity.Null)
            {
                return null;
            }
            if (!EntityManager.TryGetBuffer<WESubTextRef>(CurrentEntity.Value, true, out var buffer))
            {
                return new string[0];
            }
            string[] result = new string[buffer.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = $"{EntityManager.GetComponentData<WETextData>(buffer[i].m_weTextData).ItemName.ToString().TrimToNull() ?? "N/A"}";
            }
            return result;
        }
        private string[] ListAvailableLibraries() => m_AtlasLibrary.ListLocalAtlases();
        private string[] ListAtlasImages(string atlas) => m_AtlasLibrary.ListLocalAtlasImages(atlas);


        private bool ChangeParent(Entity target, Entity newParent)
        {
            if (target == newParent) return false;
            var parentCheck = newParent;
            while (EntityManager.TryGetComponent<WETextData>(parentCheck, out var data))
            {
                if (data.ParentEntity == target) return false;
                parentCheck = data.ParentEntity;
            }
            if (!EntityManager.TryGetComponent<WETextData>(target, out var weData)) return false;

            if (weData.ParentEntity == newParent) return true;
            if (weData.TargetEntity != newParent && (!EntityManager.TryGetComponent<WETextData>(newParent, out var weDataParent) || weDataParent.TargetEntity != weData.TargetEntity)) return false;
            if (!EntityManager.TryGetBuffer<WESubTextRef>(weData.ParentEntity, false, out var buff)) return false;
            if (!EntityManager.HasBuffer<WESubTextRef>(newParent))
            {
                EntityManager.AddBuffer<WESubTextRef>(newParent);
            }
            if (!RemoveSubItemRef(buff, target, weData.ParentEntity, false)) return false;
            var newBuff = EntityManager.GetBuffer<WESubTextRef>(newParent, false);
            newBuff.Add(new WESubTextRef
            {
                m_weTextData = target
            });
            if (weData.SetNewParent(newParent, EntityManager))
            {
                EntityManager.SetComponentData(target, weData);
            }
            ReloadTree();
            return true;
        }


        private bool CloneAsChild(Entity target, Entity newParent)
        {
            if (!EntityManager.HasComponent<WETextData>(target)) return false;
            if (!EntityManager.HasBuffer<WESubTextRef>(newParent))
            {
                EntityManager.AddBuffer<WESubTextRef>(newParent);
            }
            var newBuff = EntityManager.GetBuffer<WESubTextRef>(newParent, false);
            newBuff.Add(new WESubTextRef
            {
                m_weTextData = WELayoutUtility.DoCloneTextItem(target, newParent, EntityManager)
            });
            ReloadTree();
            return true;
        }




        private void OnEnableTool(Entity e)
        {
            var wpt = World.GetExistingSystemManaged<WEWorldPickerTool>();
            wpt.Select(e);
        }

        #endregion

        #region Bindings settings

        private bool m_initialized = false;
        public MultiUIValueBinding<string> CurrentItemName { get; private set; }
        public MultiUIValueBinding<WETextItemResume[]> CurrentTree { get; private set; }
        public MultiUIValueBinding<Entity> CurrentSubEntity { get; private set; }
        public MultiUIValueBinding<Entity> CurrentEntity { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentScale { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentRotation { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentPosition { get; private set; }
        public MultiUIValueBinding<float> MaxWidth { get; private set; }
        public MultiUIValueBinding<int> MouseSensibility { get; private set; }
        public MultiUIValueBinding<int> CurrentPlaneMode { get; private set; }
        public MultiUIValueBinding<int> CurrentMoveMode { get; private set; }
        public MultiUIValueBinding<string> CurrentItemText { get; private set; }
        public MultiUIValueBinding<bool> CurrentItemIsValid { get; private set; }
        public MultiUIValueBinding<bool> CameraLocked { get; private set; }
        public MultiUIValueBinding<bool> CameraRotationLocked { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> MainColor { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> EmissiveColor { get; private set; }
        public MultiUIValueBinding<float> Metallic { get; private set; }
        public MultiUIValueBinding<float> Smoothness { get; private set; }
        public MultiUIValueBinding<float> EmissiveIntensity { get; private set; }
        public MultiUIValueBinding<float> CoatStrength { get; private set; }
        public MultiUIValueBinding<float> EmissiveExposureWeight { get; private set; }
        public MultiUIValueBinding<string> SelectedFont { get; private set; }
        public MultiUIValueBinding<string[]> FontList { get; private set; }
        public MultiUIValueBinding<string> FormulaeStr { get; private set; }
        public MultiUIValueBinding<int> FormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> FormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<int> TextSourceType { get; private set; }
        public MultiUIValueBinding<string> ImageAtlasName { get; private set; }
        public MultiUIValueBinding<int> DecalFlags { get; private set; }
        public MultiUIValueBinding<bool> UseAbsoluteSizeEditing { get; private set; }
        public MultiUIValueBinding<WEShader, int> ShaderType { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> GlassColor { get; private set; }
        public MultiUIValueBinding<float> GlassRefraction { get; private set; }
        private void InitValueBindings()
        {
            if (m_initialized) return;
            CurrentSubEntity = new(default, $"{PREFIX}{nameof(CurrentSubEntity)}", m_eventCaller, m_callBinder);
            CurrentTree = new(default, $"{PREFIX}{nameof(CurrentTree)}", m_eventCaller, m_callBinder);
            CurrentEntity = new(default, $"{PREFIX}{nameof(CurrentEntity)}", m_eventCaller, m_callBinder);

            CurrentScale = new(default, $"{PREFIX}{nameof(CurrentScale)}", m_eventCaller, m_callBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentRotation = new(default, $"{PREFIX}{nameof(CurrentRotation)}", m_eventCaller, m_callBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentPosition = new(default, $"{PREFIX}{nameof(CurrentPosition)}", m_eventCaller, m_callBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            MaxWidth = new(default, $"{PREFIX}{nameof(MaxWidth)}", m_eventCaller, m_callBinder);

            CurrentItemName = new(default, $"{PREFIX}{nameof(CurrentItemName)}", m_eventCaller, m_callBinder);
            CurrentItemText = new(default, $"{PREFIX}{nameof(CurrentItemText)}", m_eventCaller, m_callBinder);
            CurrentItemIsValid = new(default, $"{PREFIX}{nameof(CurrentItemIsValid)}", m_eventCaller, m_callBinder);
            CurrentPlaneMode = new(default, $"{PREFIX}{nameof(CurrentPlaneMode)}", m_eventCaller, m_callBinder, (x, _) => x % 3); // WEWorldPickerTool.ToolEditMode Count
            CurrentMoveMode = new(default, $"{PREFIX}{nameof(CurrentMoveMode)}", m_eventCaller, m_callBinder, (x, _) => x % 3); // All, Horizontal, Vertical

            MouseSensibility = new(6, $"{PREFIX}{nameof(MouseSensibility)}", m_eventCaller, m_callBinder, (x, _) => x % WEWorldPickerTool.precisionIdx.Length);
            CameraLocked = new(default, $"{PREFIX}{nameof(CameraLocked)}", m_eventCaller, m_callBinder);
            CameraRotationLocked = new(default, $"{PREFIX}{nameof(CameraRotationLocked)}", m_eventCaller, m_callBinder);


            MainColor = new(default, $"{PREFIX}{nameof(MainColor)}", m_eventCaller, m_callBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            EmissiveColor = new(default, $"{PREFIX}{nameof(EmissiveColor)}", m_eventCaller, m_callBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));

            Metallic = new(default, $"{PREFIX}{nameof(Metallic)}", m_eventCaller, m_callBinder, (x, _) => math.clamp(x, 0, 1));
            Smoothness = new(default, $"{PREFIX}{nameof(Smoothness)}", m_eventCaller, m_callBinder, (x, _) => math.clamp(x, 0, 1));
            EmissiveIntensity = new(default, $"{PREFIX}{nameof(EmissiveIntensity)}", m_eventCaller, m_callBinder, (x, _) => math.clamp(x, 0, 1));
            CoatStrength = new(default, $"{PREFIX}{nameof(CoatStrength)}", m_eventCaller, m_callBinder, (x, _) => math.clamp(x, 0, 1));
            EmissiveExposureWeight = new(default, $"{PREFIX}{nameof(EmissiveExposureWeight)}", m_eventCaller, m_callBinder, (x, _) => math.clamp(x, 0, 1));
            SelectedFont = new(default, $"{PREFIX}{nameof(SelectedFont)}", m_eventCaller, m_callBinder);
            FontList = new(default, $"{PREFIX}{nameof(FontList)}", m_eventCaller, m_callBinder);
            FormulaeStr = new(default, $"{PREFIX}{nameof(FormulaeStr)}", m_eventCaller, m_callBinder);
            FormulaeCompileResult = new(default, $"{PREFIX}{nameof(FormulaeCompileResult)}", m_eventCaller, m_callBinder);
            FormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(FormulaeCompileResultErrorArgs)}", m_eventCaller, m_callBinder);
            TextSourceType = new(default, $"{PREFIX}{nameof(TextSourceType)}", m_eventCaller, m_callBinder);
            ImageAtlasName = new(default, $"{PREFIX}{nameof(ImageAtlasName)}", m_eventCaller, m_callBinder);
            DecalFlags = new(default, $"{PREFIX}{nameof(DecalFlags)}", m_eventCaller, m_callBinder);
            UseAbsoluteSizeEditing = new(default, $"{PREFIX}{nameof(UseAbsoluteSizeEditing)}", m_eventCaller, m_callBinder);
            ShaderType = new(default, $"{PREFIX}{nameof(ShaderType)}", m_eventCaller, m_callBinder, (x, _) => (int)x, (x, _) => (WEShader)x);
            GlassRefraction = new(default, $"{PREFIX}{nameof(GlassRefraction)}", m_eventCaller, m_callBinder, (x, _) => math.clamp(x, 1, 1000));
            GlassColor = new(default, $"{PREFIX}{nameof(GlassColor)}", m_eventCaller, m_callBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));


            CurrentScale.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.scale = x; return currentItem; });
            CurrentRotation.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.offsetRotation = KMathUtils.UnityEulerToQuaternion(x); return currentItem; });
            CurrentPosition.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.offsetPosition = x; return currentItem; });
            CurrentItemName.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.ItemName = x.Truncate(24); m_executionQueue.Enqueue(() => ReloadTree()); return currentItem; });
            CurrentItemText.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Text = x.Truncate(500); return currentItem; });
            CurrentSubEntity.OnScreenValueChanged += (x) => OnCurrentItemChanged();
            MaxWidth.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.maxWidthMeters = x; return currentItem; });

            MainColor.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Color = x; return currentItem; });
            EmissiveColor.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.EmissiveColor = x; return currentItem; });
            Metallic.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Metallic = x; return currentItem; });
            Smoothness.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Smoothness = x; return currentItem; });
            EmissiveIntensity.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.EmissiveIntensity = x; return currentItem; });
            CoatStrength.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.CoatStrength = x; return currentItem; });
            EmissiveExposureWeight.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.EmissiveExposureWeight = x; return currentItem; });

            SelectedFont.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Font = FontServer.Instance.TryGetFontEntity(x, out var entity) ? entity : Entity.Null; return currentItem; });
            FormulaeStr.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { FormulaeCompileResult.Value = currentItem.SetFormulae(FormulaeStr.Value, out var cmpErr); FormulaeCompileResultErrorArgs.Value = cmpErr; return currentItem; });
            TextSourceType.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.TextType = (WESimulationTextType)x; m_executionQueue.Enqueue(() => ReloadTree()); return currentItem; });
            ImageAtlasName.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Atlas = x ?? ""; return currentItem; });
            DecalFlags.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.DecalFlags = x; return currentItem; });
            UseAbsoluteSizeEditing.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.useAbsoluteSizeEditing = x; return currentItem; });
            ShaderType.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Shader = x; return currentItem; });
            GlassRefraction.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.GlassRefraction = x; return currentItem; });
            GlassColor.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.GlassColor = x; return currentItem; });

            FontList.Value = FontServer.Instance.GetLoadedFontsNames();
            FontList.UpdateUIs();

            FontServer.Instance.OnFontsLoadedChanged += () =>
            {
                FontList.Value = FontServer.Instance.GetLoadedFontsNames();
                FontList.UpdateUIs();
            };

            m_initialized = true;
        }

        private void OnCurrentItemChanged(WETextData currentItem)
        {
            CurrentPosition.Value = currentItem.offsetPosition;
            CurrentRotation.Value = KMathUtils.UnityQuaternionToEuler(currentItem.offsetRotation);
            CurrentScale.Value = currentItem.scale;
            CurrentItemText.Value = currentItem.Text.ToString();
            CurrentItemName.Value = currentItem.ItemName.ToString();
            MaxWidth.Value = currentItem.maxWidthMeters;

            MainColor.Value = currentItem.Color;
            EmissiveColor.Value = currentItem.EmissiveColor;
            Metallic.Value = currentItem.Metallic;
            Smoothness.Value = currentItem.Smoothness;
            EmissiveIntensity.Value = currentItem.EmissiveIntensity;
            CoatStrength.Value = currentItem.CoatStrength;

            SelectedFont.Value = EntityManager.TryGetComponent<FontSystemData>(currentItem.Font, out var fsd) ? fsd.Name : "";
            FormulaeStr.Value = currentItem.Formulae;
            TextSourceType.Value = (int)currentItem.TextType;
            ImageAtlasName.Value = currentItem.Atlas.ToString();
            DecalFlags.Value = currentItem.DecalFlags;
            UseAbsoluteSizeEditing.Value = currentItem.useAbsoluteSizeEditing;
            ShaderType.Value = currentItem.Shader;
            GlassColor.Value = currentItem.GlassColor;
            GlassRefraction.Value = currentItem.GlassRefraction;
        }

        #endregion

        #region Queue actions

        private readonly Queue<System.Action> m_executionQueue = new();

        private void EnqueueModification<T>(T newVal, Func<T, WETextData, WETextData> x)
        {
            if (IsValidEditingItem())
            {
                var subEntity = CurrentSubEntity.Value;
                m_executionQueue.Enqueue(() =>
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"CurrentSubEntity => {subEntity}");
                    var currentItem = EntityManager.GetComponentData<WETextData>(subEntity);
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"x = {x}; CurrentSubEntity = {currentItem.ItemName}");
                    currentItem = x(newVal, currentItem);
                    EntityManager.SetComponentData(subEntity, currentItem);
                });
            }
        }

        private void DoWithBuffer<T>(Entity parent, Action<DynamicBuffer<T>> task) where T : unmanaged, IBufferElementData
        {
            if (BasicIMod.DebugMode) LogUtils.DoLog($"!!ad  + {m_EndBarrier} | {parent}");
            if (!EntityManager.TryGetBuffer<T>(parent, false, out var buff))
            {
                buff = EntityManager.AddBuffer<T>(parent);
            }
            task(buff);
            var cmd = m_EndBarrier.CreateCommandBuffer();
            cmd.AddBuffer<T>(parent).CopyFrom(buff);
            cmd.AddComponent<BatchesUpdated>(parent);

        }


        private void AddItem(Entity parent)
        {
            if (CurrentEntity.Value != Entity.Null)
            {
                var targetParent = parent.Index == 0 ? CurrentEntity.Value : parent;
                var currentEntity = CurrentEntity.Value;
                m_executionQueue.Enqueue(() => DoWithBuffer(targetParent,
                   (Action<DynamicBuffer<WESubTextRef>>)((buff) =>
                   {
                       var subref = new WESubTextRef
                       {
                           m_weTextData = EntityManager.CreateEntity(typeof(WEWaitingRendering))
                       };
                       var newData = WETextData.CreateDefault(currentEntity, targetParent);
                       EntityManager.AddComponentData(subref.m_weTextData, newData);
                       buff.Add(subref);
                       CurrentSubEntity.ChangeValueWithEffects(subref.m_weTextData);
                       UpdateTree();
                   })));
            }
        }

        public void UpdateTree()
        {
            CurrentTree.Value = GetTextTreeForEntity(CurrentEntity.Value);
        }

        private void RemoveItem()
        {
            if (IsValidEditingItem())
            {
                var subEntity = CurrentSubEntity.Value;
                var parent = CurrentEditingItem.ParentEntity;
                m_executionQueue.Enqueue(() => DoWithBuffer(CurrentEditingItem.ParentEntity
                    , (Action<DynamicBuffer<WESubTextRef>>)((buff) =>
                    {
                        if (RemoveSubItemRef(buff, subEntity, parent, true))
                        {
                            CurrentSubEntity.ChangeValueWithEffects(Entity.Null);
                            UpdateTree();
                        }
                    })));
            }
        }
        #endregion

        #region Utility
        public bool IsValidEditingItem() => CurrentItemIsValid.Value = CurrentEntity.Value != default && EntityManager.HasComponent<WETextData>(CurrentSubEntity.Value);

        public WETextData CurrentEditingItem => EntityManager.TryGetComponent<WETextData>(CurrentSubEntity.Value, out var item) ? item : default;

        private WETextItemResume[] GetTextTreeForEntity(Entity e)
        {
            if (!EntityManager.TryGetBuffer<WESubTextRef>(e, true, out var refSubs)) return new WETextItemResume[0];
            var result = new WETextItemResume[refSubs.Length];
            for (int i = 0; i < refSubs.Length; i++)
            {
                if (!EntityManager.TryGetComponent<WETextData>(refSubs[i].m_weTextData, out var data)) continue;
                result[i] = new()
                {
                    name = data.ItemName.ToString(),
                    id = refSubs[i].m_weTextData,
                    type = (int)data.TextType,
                    children = GetTextTreeForEntity(refSubs[i].m_weTextData)
                };
            }
            return result;
        }
        private bool RemoveSubItemRef(DynamicBuffer<WESubTextRef> buff, Entity subEntity, Entity parent, bool destroy)
        {
            for (int i = 0; i < buff.Length; i++)
            {
                if (buff[i].m_weTextData == subEntity)
                {
                    if (destroy)
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {subEntity} - subEntity");
                        EntityManager.DestroyEntity(subEntity);
                    }

                    buff.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public Matrix4x4 CurrentItemMatrix { get; private set; }

        internal void SetCurrentTargetMatrix(Matrix4x4 transformMatrix)
        {
            if (transformMatrix.ValidTRS())
            {
                m_executionQueue.Enqueue(() => CurrentItemMatrix = transformMatrix);
            }
        }

        internal void OnCurrentItemChanged()
        {
            var currentItem = CurrentEditingItem;
            ReloadTree();
            OnCurrentItemChanged(currentItem);
        }

        private void ReloadTree()
        {
            CurrentTree.Value = GetTextTreeForEntity(CurrentEntity.Value);
        }
        #endregion

        #region System overrides

        private ModificationEndBarrier m_EndBarrier;
        private WEAtlasesLibrary m_AtlasLibrary;

        protected override void OnCreate()
        {
            m_EndBarrier = World.GetExistingSystemManaged<ModificationEndBarrier>();
            m_AtlasLibrary = World.GetOrCreateSystemManaged<WEAtlasesLibrary>();

            GameManager.instance.userInterface.view.Listener.BindingsReleased += () => m_initialized = false;
        }

        public override void Update()
        {
            while (m_executionQueue.TryDequeue(out var action))
            {
                action();
            }
        }
        #endregion
    }
}