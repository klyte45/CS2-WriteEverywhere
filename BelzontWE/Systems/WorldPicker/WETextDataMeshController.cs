using Belzont.Utils;
using Colossal.Entities;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WETextDataMeshController : WETextDataBaseController
    {
        private const string PREFIX = "dataMesh.";
        public MultiUIValueBinding<string> CurrentItemText { get; private set; }
        public MultiUIValueBinding<float> MaxWidth { get; private set; }
        public MultiUIValueBinding<string> SelectedFont { get; private set; }
        public MultiUIValueBinding<int> TextSourceType { get; private set; }
        public MultiUIValueBinding<string> ImageAtlasName { get; private set; }
        public MultiUIValueBinding<string> FormulaeStr { get; private set; }
        public MultiUIValueBinding<int> FormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> FormulaeCompileResultErrorArgs { get; private set; }

        protected override void DoInitValueBindings()
        {
            MaxWidth = new(default, $"{PREFIX}{nameof(MaxWidth)}", EventCaller, CallBinder);
            CurrentItemText = new(default, $"{PREFIX}{nameof(CurrentItemText)}", EventCaller, CallBinder);
            SelectedFont = new(default, $"{PREFIX}{nameof(SelectedFont)}", EventCaller, CallBinder);
            TextSourceType = new(default, $"{PREFIX}{nameof(TextSourceType)}", EventCaller, CallBinder);
            ImageAtlasName = new(default, $"{PREFIX}{nameof(ImageAtlasName)}", EventCaller, CallBinder);

            FormulaeStr = new(default, $"{PREFIX}{nameof(FormulaeStr)}", EventCaller, CallBinder);
            FormulaeCompileResult = new(default, $"{PREFIX}{nameof(FormulaeCompileResult)}", EventCaller, CallBinder);
            FormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(FormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            CurrentItemText.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.Text = x.Truncate(500); return currentItem; });
            MaxWidth.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMesh>(x, (x, currentItem) => { currentItem.MaxWidthMeters = x; return currentItem; });
            SelectedFont.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.FontName = FontServer.Instance.TryGetFont(x, out var data) ? data.Name : default(FixedString32Bytes); return currentItem; });
            FormulaeStr.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { FormulaeCompileResult.Value = currentItem.SetFormulae(FormulaeStr.Value, out var cmpErr); FormulaeCompileResultErrorArgs.Value = cmpErr; return currentItem; });
            TextSourceType.OnScreenValueChanged += (x) => PickerController.EnqueueModification<int, WETextDataMesh>(x, (x, currentItem) => { currentItem.TextType = (WESimulationTextType)x; PickerController.ReloadTreeDelayed(); return currentItem; });
            ImageAtlasName.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMesh>(x, (x, currentItem) => { currentItem.Atlas = x ?? ""; return currentItem; });

        }

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataMesh>(entity, out var mesh);
            CurrentItemText.Value = mesh.ValueData.defaultValue.ToString();
            MaxWidth.Value = mesh.MaxWidthMeters;
            SelectedFont.Value = FontServer.Instance.TryGetFont(mesh.FontName, out var fsd) ? fsd.Name : "";
            FormulaeStr.Value = mesh.ValueData.formulaeStr.ToString();
            TextSourceType.Value = (int)mesh.TextType;
            ImageAtlasName.Value = mesh.Atlas.ToString();
        }
    }
}