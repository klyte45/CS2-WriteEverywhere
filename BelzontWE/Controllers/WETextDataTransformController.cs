using Belzont.Utils;
using Colossal.Entities;
using System;
using Unity.Entities;
using Unity.Mathematics;
using static BelzontWE.WEFormulaeHelper;
using static BelzontWE.WETextDataTransform;

namespace BelzontWE
{
    public partial class WETextDataTransformController : WETextDataBaseController
    {
        private const string PREFIX = "dataTransform.";
        private WEWorldPickerController m_pickerController;

        public MultiUIValueBinding<float3, float[]> CurrentScale { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentRotation { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentPosition { get; private set; }
        public MultiUIValueBinding<WEPlacementPivot, int> Pivot { get; private set; }
        public MultiUIValueBinding<bool> UseAbsoluteSizeEditing { get; private set; }

        public MultiUIValueBinding<bool> UseFormulaeToCheckIfDraw { get; private set; }
        public MultiUIValueBinding<float> MustDrawFn { get; private set; }
        public MultiUIValueBinding<string> MustDrawFnFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> MustDrawFnFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> MustDrawFnFormulaeCompileResultErrorArgs { get; private set; }

        public MultiUIValueBinding<uint3, uint[]> ArrayInstancing { get; private set; }
        public MultiUIValueBinding<float3, float[]> ArrayInstancingGapMeters { get; private set; }
        public MultiUIValueBinding<ArrayInstancingAxisOrder, int> ArrayAxisGrowthOrder { get; private set; }

        protected override void DoInitValueBindings(Action<string, object[]> EventCaller, Action<string, Delegate> CallBinder)
        {
            m_pickerController = World.GetExistingSystemManaged<WEWorldPickerController>();

            CurrentScale = new(default, $"{PREFIX}{nameof(CurrentScale)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentRotation = new(default, $"{PREFIX}{nameof(CurrentRotation)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentPosition = new(default, $"{PREFIX}{nameof(CurrentPosition)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            UseAbsoluteSizeEditing = new(default, $"{PREFIX}{nameof(UseAbsoluteSizeEditing)}", EventCaller, CallBinder);
            Pivot = new(default, $"{PREFIX}{nameof(Pivot)}", EventCaller, CallBinder, (x, _) => (int)x, (x, _) => (WEPlacementPivot)x);
            UseFormulaeToCheckIfDraw = new(default, $"{PREFIX}{nameof(UseFormulaeToCheckIfDraw)}", EventCaller, CallBinder);
            MustDrawFn = new(default, $"{PREFIX}{nameof(MustDrawFn)}", EventCaller, CallBinder);
            MustDrawFnFormulaeStr = new(default, $"{PREFIX}{nameof(MustDrawFnFormulaeStr)}", EventCaller, CallBinder);
            MustDrawFnFormulaeCompileResult = new(default, $"{PREFIX}{nameof(MustDrawFnFormulaeCompileResult)}", EventCaller, CallBinder);
            MustDrawFnFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(MustDrawFnFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            ArrayInstancing = new(default, $"{PREFIX}{nameof(ArrayInstancing)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new uint3(x[0], x[1], x[2]));
            ArrayInstancingGapMeters = new(default, $"{PREFIX}{nameof(ArrayInstancingGapMeters)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            ArrayAxisGrowthOrder = new(default, $"{PREFIX}{nameof(ArrayAxisGrowthOrder)}", EventCaller, CallBinder, (x, _) => (int)x, (x, _) => (ArrayInstancingAxisOrder)x);


            CurrentScale.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.scale = x; return currentItem; });
            CurrentRotation.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.offsetRotation = KMathUtils.UnityEulerToQuaternion(x); return currentItem; });
            CurrentPosition.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.offsetPosition = x; return currentItem; });
            UseAbsoluteSizeEditing.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataTransform>(x, (x, currentItem) => { currentItem.useAbsoluteSizeEditing = x; return currentItem; });
            Pivot.OnScreenValueChanged += (x) => PickerController.EnqueueModification<WEPlacementPivot, WETextDataTransform>(x, (x, currentItem) => { currentItem.pivot = x; if (EntityManager.HasComponent<WEIsPlaceholder>(m_pickerController.CurrentSubEntity.Value)) EntityManager.AddComponent<WETemplateDirtyInstancing>(m_pickerController.CurrentSubEntity.Value); return currentItem; });
            UseFormulaeToCheckIfDraw.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataTransform>(x, (x, currentItem) => { currentItem.useFormulaeToCheckIfDraw = x; return currentItem; });
            SetupOnFormulaeChangedAction(PickerController, (ref WETextDataTransform data, string newFormulae, out string[] errorArgs) => data.SetFormulaeMustDraw(newFormulae, out errorArgs), MustDrawFnFormulaeStr, MustDrawFnFormulaeCompileResult, MustDrawFnFormulaeCompileResultErrorArgs);


            ArrayInstancing.OnScreenValueChanged += (x) => PickerController.EnqueueModification<uint3, WETextDataTransform>(x, (x, currentItem) => { currentItem.ArrayInstancing = x; EntityManager.AddComponent<WETemplateDirtyInstancing>(m_pickerController.CurrentSubEntity.Value); ; return currentItem; });
            ArrayInstancingGapMeters.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.arrayInstancingGapMeters = x; EntityManager.AddComponent<WETemplateDirtyInstancing>(m_pickerController.CurrentSubEntity.Value); return currentItem; });
            ArrayAxisGrowthOrder.OnScreenValueChanged += (x) => PickerController.EnqueueModification<ArrayInstancingAxisOrder, WETextDataTransform>(x, (x, currentItem) => { currentItem.arrayAxisGrowthOrder = x; EntityManager.AddComponent<WETemplateDirtyInstancing>(m_pickerController.CurrentSubEntity.Value); return currentItem; });

        }

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataTransform>(entity, out var transform);

            CurrentPosition.Value = transform.offsetPosition;
            CurrentRotation.Value = KMathUtils.UnityQuaternionToEuler(transform.offsetRotation);
            CurrentScale.Value = transform.scale;
            UseAbsoluteSizeEditing.Value = transform.useAbsoluteSizeEditing;
            Pivot.Value = transform.pivot;
            UseFormulaeToCheckIfDraw.Value = transform.useFormulaeToCheckIfDraw;
            ResetScreenFormulaeValue(transform.MustDrawFormulae, MustDrawFnFormulaeStr, MustDrawFnFormulaeCompileResult, MustDrawFnFormulaeCompileResultErrorArgs);

            ArrayInstancing.Value = transform.ArrayInstancing;
            ArrayInstancingGapMeters.Value = transform.arrayInstancingGapMeters;
            ArrayAxisGrowthOrder.Value = transform.arrayAxisGrowthOrder;
        }
    }
}