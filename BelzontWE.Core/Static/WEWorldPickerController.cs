using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Entities;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public class WEWorldPickerController : ComponentSystemBase, IBelzontBindable
    {
        private const string PREFIX = "wpicker.";

        Action<string, object[]> m_eventCaller;
        Action<string, Delegate> m_callBinder;
        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            m_callBinder = callBinder;
            callBinder($"{PREFIX}isValidEditingItem", () => IsValidEditingItem);
            callBinder($"{PREFIX}getItemsAvailable", () => GetItemsAvailable);
            callBinder($"{PREFIX}toolPrecisions", () => WEWorldPickerTool.precisionIdx);
            callBinder($"{PREFIX}enableTool", () => WEWorldPickerTool.precisionIdx);
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
        public MultiUIValueBinding<string> CurrentItemName { get; private set; }
        public MultiUIValueBinding<int> CurrentItemIdx { get; private set; }
        public MultiUIValueBinding<Entity> CurrentEntity { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentScale { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentRotation { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentPosition { get; private set; }
        public MultiUIValueBinding<int> MouseSensibility { get; private set; }
        public MultiUIValueBinding<int> CurrentPlaneMode { get; private set; }
        public bool IsValidEditingItem => CurrentEntity.Value != default && EntityManager.TryGetBuffer<WESimulationTextComponent>(CurrentEntity.Value, true, out var buffer) && buffer.Length > CurrentItemIdx.Value && CurrentItemIdx.Value >= 0;
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
                result[i] = $"#{i}: {buffer[i].itemName.ToString().TrimToNull() ?? "N/A"}";
            }
            return result;


        }

        private void InitValueBindings()
        {
            CurrentItemName ??= new(default, $"{PREFIX}{nameof(CurrentItemName)}", m_eventCaller, m_callBinder);
            CurrentItemIdx ??= new(default, $"{PREFIX}{nameof(CurrentItemIdx)}", m_eventCaller, m_callBinder);
            CurrentEntity ??= new(default, $"{PREFIX}{nameof(CurrentEntity)}", m_eventCaller, m_callBinder);
            CurrentScale ??= new(default, $"{PREFIX}{nameof(CurrentScale)}", m_eventCaller, m_callBinder, (x) => new[] { x.x, x.y, x.z });
            CurrentRotation ??= new(default, $"{PREFIX}{nameof(CurrentRotation)}", m_eventCaller, m_callBinder, (x) => new[] { x.x, x.y, x.z });
            CurrentPosition ??= new(default, $"{PREFIX}{nameof(CurrentPosition)}", m_eventCaller, m_callBinder, (x) => new[] { x.x, x.y, x.z });
            MouseSensibility ??= new(default, $"{PREFIX}{nameof(MouseSensibility)}", m_eventCaller, m_callBinder, (x) => x % WEWorldPickerTool.precisionIdx.Length);
            CurrentPlaneMode ??= new(default, $"{PREFIX}{nameof(CurrentPlaneMode)}", m_eventCaller, m_callBinder, (x) => x % 3); // WEWorldPickerTool.ToolEditMode Count
        }

        protected override void OnCreate()
        {
            base.OnCreate();

        }

        public override void Update()
        {
        }
    }

}