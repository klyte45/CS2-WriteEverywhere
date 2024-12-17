using Belzont.Utils;
using Colossal.Entities;
using System;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WETextDataMeshController : WETextDataBaseController
    {
        private const string PREFIX = "dataMesh.";
        public MultiUIValueBinding<string> ValueText { get; private set; }
        public MultiUIValueBinding<float> MaxWidth { get; private set; }
        public MultiUIValueBinding<string> SelectedFont { get; private set; }
        public MultiUIValueBinding<int> TextSourceType { get; private set; }
        public MultiUIValueBinding<string> ImageAtlasName { get; private set; }
        public MultiUIValueBinding<string> ValueTextFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ValueTextFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ValueTextFormulaeCompileResultErrorArgs { get; private set; }

        protected override void DoInitValueBindings(Action<string, object[]> EventCaller, Action<string, Delegate> CallBinder)
        {
            MaxWidth = new(default, $"{PREFIX}{nameof(MaxWidth)}", EventCaller, CallBinder);
            ValueText = new(default, $"{PREFIX}{nameof(ValueText)}", EventCaller, CallBinder);
            SelectedFont = new(default, $"{PREFIX}{nameof(SelectedFont)}", EventCaller, CallBinder);
            TextSourceType = new(default, $"{PREFIX}{nameof(TextSourceType)}", EventCaller, CallBinder);
            ImageAtlasName = new(default, $"{PREFIX}{nameof(ImageAtlasName)}", EventCaller, CallBinder);

            ValueTextFormulaeStr = new(default, $"{PREFIX}{nameof(ValueTextFormulaeStr)}", EventCaller, CallBinder);
            ValueTextFormulaeCompileResult = new(default, $"{PREFIX}{nameof(ValueTextFormulaeCompileResult)}", EventCaller, CallBinder);
            ValueTextFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ValueTextFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            ValueText.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.Text = x.Truncate(500); return currentItem; });
            MaxWidth.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMesh>(x, (x, currentItem) => { currentItem.MaxWidthMeters = x; return currentItem; });
            SelectedFont.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.FontName = FontServer.Instance.TryGetFont(x, out var data) ? data.Name : default(FixedString32Bytes); return currentItem; });
            ValueTextFormulaeStr.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { ValueTextFormulaeCompileResult.Value = currentItem.SetFormulae(ValueTextFormulaeStr.Value, out var cmpErr); ValueTextFormulaeCompileResultErrorArgs.Value = cmpErr; return currentItem; });
            TextSourceType.OnScreenValueChanged += (x) => PickerController.EnqueueModification<int, WETextDataMesh>(x, (x, currentItem) => { currentItem.TextType = (WESimulationTextType)x; PickerController.ReloadTreeDelayed(); return currentItem; });
            ImageAtlasName.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.Atlas = x ?? ""; return currentItem; });

        }

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataMesh>(entity, out var mesh);
            ValueText.Value = mesh.ValueData.DefaultValue;
            MaxWidth.Value = mesh.MaxWidthMeters;
            SelectedFont.Value = FontServer.Instance.TryGetFont(mesh.FontName, out var fsd) ? fsd.Name : "";
            ValueTextFormulaeStr.Value = mesh.ValueData.Formulae;
            TextSourceType.Value = (int)mesh.TextType;
            ImageAtlasName.Value = mesh.Atlas.ToString();
        }
    }
}