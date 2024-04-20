using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Entities;
using Game.Common;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public class WEWorldPickerController : ComponentSystemBase, IBelzontBindable
    {
        private const string PREFIX = "wpicker.";

        Action<string, object[]> m_eventCaller;
        Action<string, Delegate> m_callBinder;
        private ModificationEndBarrier m_EndBarrier;
        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            m_callBinder = callBinder;
            callBinder($"{PREFIX}getItemsAvailable", GetItemsAvailable);
            callBinder($"{PREFIX}toolPrecisions", () => WEWorldPickerTool.precisionIdx);
            callBinder($"{PREFIX}enableTool", () => WEWorldPickerTool.precisionIdx);
            callBinder($"{PREFIX}addItem", AddItem);
            callBinder($"{PREFIX}removeItem", RemoveItem);
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

        private bool m_initialized = false;
        private readonly Queue<Action> m_executionQueue = new();

        public int IncrementalVersion { get; private set; }

        public MultiUIValueBinding<string> CurrentItemName { get; private set; }
        public MultiUIValueBinding<int> CurrentItemIdx { get; private set; }
        public MultiUIValueBinding<int> CurrentItemCount { get; private set; }
        public MultiUIValueBinding<Entity> CurrentEntity { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentScale { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentRotation { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentPosition { get; private set; }
        public MultiUIValueBinding<int> MouseSensibility { get; private set; }
        public MultiUIValueBinding<int> CurrentPlaneMode { get; private set; }
        public MultiUIValueBinding<string> CurrentItemText { get; private set; }
        public MultiUIValueBinding<bool> CurrentItemIsValid { get; private set; }

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
            CurrentItemName = new(default, $"{PREFIX}{nameof(CurrentItemName)}", m_eventCaller, m_callBinder);
            CurrentItemIdx = new(default, $"{PREFIX}{nameof(CurrentItemIdx)}", m_eventCaller, m_callBinder);
            CurrentEntity = new(default, $"{PREFIX}{nameof(CurrentEntity)}", m_eventCaller, m_callBinder);
            CurrentItemCount = new(default, $"{PREFIX}{nameof(CurrentItemCount)}", m_eventCaller, m_callBinder);

            CurrentScale = new(default, $"{PREFIX}{nameof(CurrentScale)}", m_eventCaller, m_callBinder, (x) => new[] { x.x, x.y, x.z }, (x) => new float3(x[0], x[1], x[2]));
            CurrentRotation = new(default, $"{PREFIX}{nameof(CurrentRotation)}", m_eventCaller, m_callBinder, (x) => new[] { x.x, x.y, x.z }, (x) => new float3(x[0], x[1], x[2]));
            CurrentPosition = new(default, $"{PREFIX}{nameof(CurrentPosition)}", m_eventCaller, m_callBinder, (x) => new[] { x.x, x.y, x.z }, (x) => new float3(x[0], x[1], x[2]));

            CurrentItemText = new(default, $"{PREFIX}{nameof(CurrentItemText)}", m_eventCaller, m_callBinder);
            CurrentItemIsValid = new(default, $"{PREFIX}{nameof(CurrentItemIsValid)}", m_eventCaller, m_callBinder);
            MouseSensibility = new(default, $"{PREFIX}{nameof(MouseSensibility)}", m_eventCaller, m_callBinder, (x) => x % WEWorldPickerTool.precisionIdx.Length);
            CurrentPlaneMode = new(default, $"{PREFIX}{nameof(CurrentPlaneMode)}", m_eventCaller, m_callBinder, (x) => x % 3); // WEWorldPickerTool.ToolEditMode Count

            CurrentScale.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.scale = x; return currentItem; });
            CurrentRotation.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.offsetRotation = KMathUtils.UnityEulerToQuaternion(x); return currentItem; });
            CurrentPosition.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.offsetPosition = x; return currentItem; });
            CurrentItemName.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.itemName = x.Truncate(24); return currentItem; });
            CurrentItemText.OnScreenValueChanged += (x) => EnqueueModification(x, (x, currentItem) => { currentItem.Text = x.Truncate(500); return currentItem; });
            CurrentItemIdx.OnScreenValueChanged += (x) => OnCurrentItemChanged();


            m_initialized = true;
        }

        private void EnqueueModification<T>(T newVal, Func<T, WESimulationTextComponent, WESimulationTextComponent> x)
        {
            if (IsValidEditingItem())
            {
                m_executionQueue.Enqueue(() => DoWithBuffer<WESimulationTextComponent>(
                    (buff) =>
                    {
                        var currentItem = buff[CurrentItemIdx.Value];
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
                       buff.Add(new WESimulationTextComponent
                       {
                           text = "NEW TEXT",
                           color = Color.white,
                           scale = Vector3.one
                       });
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
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndBarrier = World.GetExistingSystemManaged<ModificationEndBarrier>();
        }

        public override void Update()
        {
            while (m_executionQueue.TryDequeue(out var action))
            {
                action();
            }
        }
    }

}