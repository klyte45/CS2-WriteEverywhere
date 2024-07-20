using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game.Common;
using Game.Input;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using WriteEverywhere.Sprites;

namespace BelzontWE
{
    public class WEWorldPickerController : ComponentSystemBase, IBelzontBindable
    {
        private const string PREFIX = "wpicker.";

        Action<string, object[]> m_eventCaller;
        Action<string, Delegate> m_callBinder;
        private ModificationEndBarrier m_EndBarrier;
        private WEAtlasesLibrary m_AtlasLibrary;
        private WEWorldPickerTool m_pickerTool;
        private ProxyAction m_enableToolAction;

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
            callBinder($"{PREFIX}requireFontInstallation", RequireFontInstallation);
            if (m_eventCaller != null) InitValueBindings();
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            m_eventCaller = eventCaller;
            if (m_callBinder != null) InitValueBindings();
        }

        public void SetupEventBinder(Action<string, Delegate> eventBinder)
        {
        }

        private void OnEnableTool(Entity e)
        {
            var wpt = World.GetExistingSystemManaged<WEWorldPickerTool>();
            wpt.Select(e);
        }

        private bool m_initialized = false;
        private readonly Queue<System.Action> m_executionQueue = new();

        public int IncrementalVersion { get; private set; }
        public Matrix4x4 CurrentItemMatrix { get; private set; }

        public MultiUIValueBinding<string> CurrentItemName { get; private set; }
        public MultiUIValueBinding<int> CurrentItemIdx { get; private set; }
        public MultiUIValueBinding<int> CurrentItemCount { get; private set; }
        public MultiUIValueBinding<Entity> CurrentEntity { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentScale { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentRotation { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentPosition { get; private set; }
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
        public MultiUIValueBinding<Dictionary<string, Entity>, string[]> FontList { get; private set; }
        public MultiUIValueBinding<string> FormulaeStr { get; private set; }
        public MultiUIValueBinding<int> FormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> FormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<int> TextSourceType { get; private set; }
        public MultiUIValueBinding<string> ImageAtlasName { get; private set; }

        private string[] ListAvailableLibraries()
        {
            return m_AtlasLibrary.ListLocalAtlases();
        }
        private string[] ListAtlasImages(string atlas)
        {
            return m_AtlasLibrary.ListLocalAtlasImages(atlas);
        }
        private string RequireFontInstallation(string path)
        {
            if (!File.Exists(path)) return "";
            var name = Path.GetFileNameWithoutExtension(path);
            if (FontServer.Instance.RegisterFont(name, File.ReadAllBytes(path)))
            {
                return name;
            }
            return "";
        }

        public bool IsValidEditingItem()
        {
            return CurrentItemIsValid.Value = CurrentEntity.Value != default && EntityManager.TryGetBuffer<WESimulationTextComponent>(CurrentEntity.Value, true, out var buffer) && buffer.Length > CurrentItemIdx.Value && CurrentItemIdx.Value >= 0;
        }

        public WESimulationTextComponent CurrentEditingItem => EntityManager.TryGetBuffer<WESimulationTextComponent>(CurrentEntity.Value, true, out var buffer) && buffer.Length > CurrentItemIdx.Value && CurrentItemIdx.Value >= 0 ? buffer[CurrentItemIdx.Value] : default;

        private string[] GetItemsAvailable()
        {
            if (CurrentEntity.Value == Entity.Null)
            {
                return null;
            }
            if (!EntityManager.TryGetBuffer<WESimulationTextComponent>(CurrentEntity.Value, true, out var buffer))
            {
                return new string[0];
            }
            string[] result = new string[buffer.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = $"{buffer[i].itemName.ToString().TrimToNull() ?? "N/A"}";
            }
            return result;
        }

        private void InitValueBindings()
        {
            if (m_initialized) return;
            CurrentItemIdx = new(default, $"{PREFIX}{nameof(CurrentItemIdx)}", m_eventCaller, m_callBinder);
            CurrentEntity = new(default, $"{PREFIX}{nameof(CurrentEntity)}", m_eventCaller, m_callBinder);
            CurrentItemCount = new(default, $"{PREFIX}{nameof(CurrentItemCount)}", m_eventCaller, m_callBinder);

            CurrentScale = new(default, $"{PREFIX}{nameof(CurrentScale)}", m_eventCaller, m_callBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentRotation = new(default, $"{PREFIX}{nameof(CurrentRotation)}", m_eventCaller, m_callBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentPosition = new(default, $"{PREFIX}{nameof(CurrentPosition)}", m_eventCaller, m_callBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));

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
            FontList = new(new Dictionary<string, Entity>(), $"{PREFIX}{nameof(FontList)}", m_eventCaller, m_callBinder, (x, t) => x.Keys.ToArray(), (_, t) => t.Value);
            FormulaeStr = new(default, $"{PREFIX}{nameof(FormulaeStr)}", m_eventCaller, m_callBinder);
            FormulaeCompileResult = new(default, $"{PREFIX}{nameof(FormulaeCompileResult)}", m_eventCaller, m_callBinder);
            FormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(FormulaeCompileResultErrorArgs)}", m_eventCaller, m_callBinder);
            TextSourceType = new(default, $"{PREFIX}{nameof(TextSourceType)}", m_eventCaller, m_callBinder);
            ImageAtlasName = new(default, $"{PREFIX}{nameof(ImageAtlasName)}", m_eventCaller, m_callBinder);


            CurrentScale.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.scale = x; return currentItem; });
            CurrentRotation.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.offsetRotation = KMathUtils.UnityEulerToQuaternion(x); return currentItem; });
            CurrentPosition.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.offsetPosition = x; return currentItem; });
            CurrentItemName.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.itemName = x.Truncate(24); return currentItem; });
            CurrentItemText.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Text = x.Truncate(500); return currentItem; });
            CurrentItemIdx.OnScreenValueChanged += (x) => OnCurrentItemChanged();

            MainColor.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Color = x; return currentItem; });
            EmissiveColor.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.EmissiveColor = x; return currentItem; });
            Metallic.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Metallic = x; return currentItem; });
            Smoothness.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Smoothness = x; return currentItem; });
            EmissiveIntensity.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.EmissiveIntensity = x; return currentItem; });
            CoatStrength.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.CoatStrength = x; return currentItem; });
            EmissiveExposureWeight.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.EmissiveExposureWeight = x; return currentItem; });

            SelectedFont.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Font = FontList.Value.TryGetValue(x, out var entity) ? entity : Entity.Null; return currentItem; });
            FormulaeStr.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { FormulaeCompileResult.Value = currentItem.SetFormulae(FormulaeStr.Value, out var cmpErr); FormulaeCompileResultErrorArgs.Value = cmpErr; if (currentItem.targetEntity == Entity.Null) currentItem.targetEntity = CurrentEntity.Value; return currentItem; });
            TextSourceType.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.TextType = (WESimulationTextType)x; return currentItem; });
            ImageAtlasName.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Atlas = x; return currentItem; });


            m_initialized = true;
        }

        private void EnqueueModification<T>(T newVal, Func<T, WESimulationTextComponent, WESimulationTextComponent> x)
        {
            if (IsValidEditingItem())
            {
                m_executionQueue.Enqueue(() => DoWithBuffer<WESimulationTextComponent>(
                    (buff) =>
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"CurrentItemIdx => {CurrentItemIdx.Value}\nbuff = {buff} : {buff.Length}");
                        var currentItem = buff[CurrentItemIdx.Value];
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"x = {x}; currentItem[{CurrentItemIdx.Value}] = {currentItem.itemName}");
                        currentItem = x(newVal, currentItem);
                        buff[CurrentItemIdx.Value] = currentItem;
                    }));
            }
        }

        private void DoWithBuffer<T>(Action<DynamicBuffer<T>> task) where T : unmanaged, IBufferElementData
        {
            if (!EntityManager.TryGetBuffer<T>(CurrentEntity.Value, false, out var buff))
            {
                buff = EntityManager.AddBuffer<T>(CurrentEntity.Value);
            }
            task(buff);
            if (BasicIMod.DebugMode) LogUtils.DoLog($"!!ad  + {m_EndBarrier} | {CurrentEntity}");
            var cmd = m_EndBarrier.CreateCommandBuffer();
            cmd.AddBuffer<T>(CurrentEntity.Value).CopyFrom(buff);
            cmd.AddComponent<BatchesUpdated>(CurrentEntity.Value);

        }

        private void AddItem()
        {
            if (CurrentEntity.Value != Entity.Null)
            {
                m_executionQueue.Enqueue(() => DoWithBuffer<WESimulationTextComponent>(
                   (buff) =>
                   {
                       buff.Add(WESimulationTextComponent.CreateDefault());
                       var newCount = buff.Length;
                       CurrentItemIdx.Value = buff.Length - 1;
                       CurrentItemCount.Value = newCount;
                       OnCurrentItemChanged(buff[CurrentItemIdx.Value]);
                       IncrementalVersion++;
                   }));
            }
        }

        private void RemoveItem()
        {

            if (IsValidEditingItem())
            {
                m_executionQueue.Enqueue(() => DoWithBuffer<WESimulationTextComponent>((buff) =>
                {
                    buff.RemoveAt(CurrentItemIdx.Value);
                    var buffLen = buff.Length;
                    CurrentItemIdx.Value = buffLen == 0 ? 0 : (CurrentItemIdx.Value + buffLen - 1) % buffLen;
                    CurrentItemCount.Value = buffLen;
                    if (buffLen > 0) OnCurrentItemChanged(buff[CurrentItemIdx.Value]);
                    IncrementalVersion++;
                }));
            }
        }

        internal void OnCurrentItemChanged()
        {
            var currentItem = CurrentEditingItem;
            OnCurrentItemChanged(currentItem);
        }

        private void OnCurrentItemChanged(WESimulationTextComponent currentItem)
        {
            CurrentPosition.Value = currentItem.offsetPosition;
            CurrentRotation.Value = KMathUtils.UnityQuaternionToEuler(currentItem.offsetRotation);
            CurrentScale.Value = currentItem.scale;
            CurrentItemText.Value = currentItem.Text.ToString();
            CurrentItemName.Value = currentItem.itemName.ToString();


            MainColor.Value = currentItem.Color;
            EmissiveColor.Value = currentItem.EmissiveColor;
            Metallic.Value = currentItem.Metallic;
            Smoothness.Value = currentItem.Smoothness;
            EmissiveIntensity.Value = currentItem.EmissiveIntensity;
            CoatStrength.Value = currentItem.CoatStrength;

            SelectedFont.Value = EntityManager.TryGetComponent<FontSystemData>(currentItem.Font, out var fsd) ? fsd.Name : "";
            FormulaeStr.Value = currentItem.Formulae;
            TextSourceType.Value = (int)currentItem.TextType;
            ImageAtlasName.Value = currentItem.Atlas;
        }

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

        internal void SetCurrentTargetMatrix(Matrix4x4 transformMatrix)
        {
            m_executionQueue.Enqueue(() => CurrentItemMatrix = transformMatrix);
        }
    }

}