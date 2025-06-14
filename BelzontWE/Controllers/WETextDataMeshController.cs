using Belzont.Utils;
using Colossal.Entities;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using static BelzontWE.WEFormulaeHelper;
namespace BelzontWE
{
    public partial class WETextDataMeshController : WETextDataBaseController
    {
        private const string PREFIX = "dataMesh.";
        public MultiUIValueBinding<string> ValueText { get; private set; }
        public MultiUIValueBinding<string> SelectedFont { get; private set; }
        public MultiUIValueBinding<int> TextSourceType { get; private set; }
        public MultiUIValueBinding<string> ImageAtlasName { get; private set; }
        public MultiUIValueBinding<bool> RescaleHeightOnTextOverflow { get; private set; }
        public MultiUIValueBinding<bool> ChildrenRefersToFrontFace { get; private set; }


        public MultiUIValueBinding<string> ValueTextFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ValueTextFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ValueTextFormulaeCompileResultErrorArgs { get; private set; }

        public MultiUIValueBinding<float> MaxWidth { get; private set; }
        public MultiUIValueBinding<string> MaxWidthFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> MaxWidthFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> MaxWidthFormulaeCompileResultErrorArgs { get; private set; }



        public MultiUIValueBinding<float3, float[]> Scaler { get; private set; }
        public MultiUIValueBinding<string> ScalerFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ScalerFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ScalerFormulaeCompileResultErrorArgs { get; private set; }



        public MultiUIValueBinding<float3, float[]> OffsetPosition { get; private set; }
        public MultiUIValueBinding<string> OffsetPositionFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> OffsetPositionFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> OffsetPositionFormulaeCompileResultErrorArgs { get; private set; }


        public MultiUIValueBinding<float3, float[]> OffsetRotation { get; private set; }
        public MultiUIValueBinding<string> OffsetRotationFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> OffsetRotationFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> OffsetRotationFormulaeCompileResultErrorArgs { get; private set; }


        protected override void DoInitValueBindings(Action<string, object[]> EventCaller, Action<string, Delegate> CallBinder)
        {
            ValueText = new(default, $"{PREFIX}{nameof(ValueText)}", EventCaller, CallBinder);
            SelectedFont = new(default, $"{PREFIX}{nameof(SelectedFont)}", EventCaller, CallBinder);
            TextSourceType = new(default, $"{PREFIX}{nameof(TextSourceType)}", EventCaller, CallBinder);
            ImageAtlasName = new(default, $"{PREFIX}{nameof(ImageAtlasName)}", EventCaller, CallBinder);
            RescaleHeightOnTextOverflow = new(default, $"{PREFIX}{nameof(RescaleHeightOnTextOverflow)}", EventCaller, CallBinder);
            ChildrenRefersToFrontFace = new(default, $"{PREFIX}{nameof(ChildrenRefersToFrontFace)}", EventCaller, CallBinder);

            ValueTextFormulaeStr = new(default, $"{PREFIX}{nameof(ValueTextFormulaeStr)}", EventCaller, CallBinder);
            ValueTextFormulaeCompileResult = new(default, $"{PREFIX}{nameof(ValueTextFormulaeCompileResult)}", EventCaller, CallBinder);
            ValueTextFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ValueTextFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            MaxWidth = new(default, $"{PREFIX}{nameof(MaxWidth)}", EventCaller, CallBinder);
            MaxWidthFormulaeStr = new(default, $"{PREFIX}{nameof(MaxWidthFormulaeStr)}", EventCaller, CallBinder);
            MaxWidthFormulaeCompileResult = new(default, $"{PREFIX}{nameof(MaxWidthFormulaeCompileResult)}", EventCaller, CallBinder);
            MaxWidthFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(MaxWidthFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            Scaler = new(default, $"{PREFIX}{nameof(Scaler)}", EventCaller, CallBinder, (x, _) => new float[] { x.x, x.y, x.z }, (x, _) => x.Length == 3 ? new float3(x[0], x[1], x[2]) : default);
            ScalerFormulaeStr = new(default, $"{PREFIX}{nameof(ScalerFormulaeStr)}", EventCaller, CallBinder);
            ScalerFormulaeCompileResult = new(default, $"{PREFIX}{nameof(ScalerFormulaeCompileResult)}", EventCaller, CallBinder);
            ScalerFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ScalerFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            OffsetPosition = new(default, $"{PREFIX}{nameof(OffsetPosition)}", EventCaller, CallBinder, (x, _) => new float[] { x.x, x.y, x.z }, (x, _) => x.Length == 3 ? new float3(x[0], x[1], x[2]) : default);
            OffsetPositionFormulaeStr = new(default, $"{PREFIX}{nameof(OffsetPositionFormulaeStr)}", EventCaller, CallBinder);
            OffsetPositionFormulaeCompileResult = new(default, $"{PREFIX}{nameof(OffsetPositionFormulaeCompileResult)}", EventCaller, CallBinder);
            OffsetPositionFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(OffsetPositionFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            OffsetRotation = new(default, $"{PREFIX}{nameof(OffsetRotation)}", EventCaller, CallBinder, (x, _) => new float[] { x.x, x.y, x.z }, (x, _) => x.Length == 3 ? new float3(x[0], x[1], x[2]) : default);
            OffsetRotationFormulaeStr = new(default, $"{PREFIX}{nameof(OffsetRotationFormulaeStr)}", EventCaller, CallBinder);
            OffsetRotationFormulaeCompileResult = new(default, $"{PREFIX}{nameof(OffsetRotationFormulaeCompileResult)}", EventCaller, CallBinder);
            OffsetRotationFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(OffsetRotationFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);

            Scaler.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataMesh>(x, (x, currentItem) => { currentItem.ScaleFormulae.defaultValue = x; return currentItem; });
            SetupOnFormulaeChangedAction(PickerController, (ref WETextDataMesh data, string newFormulae, out string[] errorArgs) => data.ScaleFormulae.SetFormulae(newFormulae, out errorArgs), ScalerFormulaeStr, ScalerFormulaeCompileResult, ScalerFormulaeCompileResultErrorArgs);

            OffsetPosition.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataMesh>(x, (x, currentItem) => { currentItem.OffsetPositionFormulae.defaultValue = x; return currentItem; });
            SetupOnFormulaeChangedAction(PickerController, (ref WETextDataMesh data, string newFormulae, out string[] errorArgs) => data.OffsetPositionFormulae.SetFormulae(newFormulae, out errorArgs), OffsetPositionFormulaeStr, OffsetPositionFormulaeCompileResult, OffsetPositionFormulaeCompileResultErrorArgs);

            OffsetRotation.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataMesh>(x, (x, currentItem) => { currentItem.OffsetRotationFormulae.defaultValue = x; return currentItem; });
            SetupOnFormulaeChangedAction(PickerController, (ref WETextDataMesh data, string newFormulae, out string[] errorArgs) => data.OffsetRotationFormulae.SetFormulae(newFormulae, out errorArgs), OffsetRotationFormulaeStr, OffsetRotationFormulaeCompileResult, OffsetRotationFormulaeCompileResultErrorArgs);

            ValueText.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.Text = x.Truncate(500); return currentItem; });

            MaxWidth.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMesh>(x, (x, currentItem) => { currentItem.MaxWidthMeters.defaultValue = x; return currentItem; });
            SetupOnFormulaeChangedAction(PickerController, (ref WETextDataMesh data, string newFormulae, out string[] errorArgs) => data.MaxWidthMeters.SetFormulae(newFormulae, out errorArgs), MaxWidthFormulaeStr, MaxWidthFormulaeCompileResult, MaxWidthFormulaeCompileResultErrorArgs);


            SelectedFont.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.FontName = FontServer.Instance.TryGetFont(x, out var data) ? data.Name : default(FixedString32Bytes); return currentItem; });
            ValueTextFormulaeStr.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { ValueTextFormulaeCompileResult.Value = currentItem.SetFormulae(x, out var cmpErr); ValueTextFormulaeCompileResultErrorArgs.Value = cmpErr; return currentItem; });
            TextSourceType.OnScreenValueChanged += (x) => PickerController.EnqueueModification<int, WETextDataMesh>(x, (x, currentItem) => { currentItem.TextType = (WESimulationTextType)x; PickerController.ReloadTreeDelayed(); return currentItem; });
            ImageAtlasName.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.Atlas = x ?? ""; return currentItem; });
            RescaleHeightOnTextOverflow.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataMesh>(x, (x, currentItem) => { currentItem.RescaleHeightOnTextOverflow = x; return currentItem; });
            ChildrenRefersToFrontFace.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataMesh>(x, (x, currentItem) => { currentItem.childrenRefersToFrontFace = x; return currentItem; });

        }

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataMesh>(entity, out var mesh);
            ValueText.Value = mesh.ValueData.DefaultValue;
            SelectedFont.Value = FontServer.Instance.TryGetFont(mesh.FontName, out var fsd) ? fsd.Name : "";
            ValueTextFormulaeStr.Value = mesh.ValueData.Formulae;
            TextSourceType.Value = (int)mesh.TextType;
            ImageAtlasName.Value = mesh.Atlas.ToString();
            RescaleHeightOnTextOverflow.Value = mesh.RescaleHeightOnTextOverflow;
            ChildrenRefersToFrontFace.Value = mesh.childrenRefersToFrontFace;

            MaxWidth.Value = mesh.MaxWidthMeters.defaultValue;
            ResetScreenFormulaeValue(mesh.MaxWidthMeters.Formulae, MaxWidthFormulaeStr, MaxWidthFormulaeCompileResult, MaxWidthFormulaeCompileResultErrorArgs);

            Scaler.Value = mesh.ScaleFormulae.defaultValue;
            ResetScreenFormulaeValue(mesh.ScaleFormulae.Formulae, ScalerFormulaeStr, ScalerFormulaeCompileResult, ScalerFormulaeCompileResultErrorArgs);
            OffsetPosition.Value = mesh.OffsetPositionFormulae.defaultValue;
            ResetScreenFormulaeValue(mesh.OffsetPositionFormulae.Formulae, OffsetPositionFormulaeStr, OffsetPositionFormulaeCompileResult, OffsetPositionFormulaeCompileResultErrorArgs);
            OffsetRotation.Value = mesh.OffsetRotationFormulae.defaultValue;
            ResetScreenFormulaeValue(mesh.OffsetRotationFormulae.Formulae, OffsetRotationFormulaeStr, OffsetRotationFormulaeCompileResult, OffsetRotationFormulaeCompileResultErrorArgs);
        }
    }
}