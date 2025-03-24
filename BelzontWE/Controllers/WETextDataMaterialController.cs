using Belzont.Utils;
using Colossal.Entities;
using System;
using Unity.Entities;
using UnityEngine;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public partial class WETextDataMaterialController : WETextDataBaseController
    {
        private const string PREFIX = "dataMaterial.";

        public MultiUIValueBinding<Color, UIColorRGBA> MainColor { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> EmissiveColor { get; private set; }
        public MultiUIValueBinding<float> Metallic { get; private set; }
        public MultiUIValueBinding<float> Smoothness { get; private set; }
        public MultiUIValueBinding<float> EmissiveIntensity { get; private set; }
        public MultiUIValueBinding<float> CoatStrength { get; private set; }
        public MultiUIValueBinding<float> EmissiveExposureWeight { get; private set; }
        public MultiUIValueBinding<int> DecalFlags { get; private set; }
        public MultiUIValueBinding<WEShader, int> ShaderType { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> GlassColor { get; private set; }
        public MultiUIValueBinding<float> GlassRefraction { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> ColorMask1 { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> ColorMask2 { get; private set; }
        public MultiUIValueBinding<Color, UIColorRGBA> ColorMask3 { get; private set; }
        public MultiUIValueBinding<float> NormalStrength { get; private set; }
        public MultiUIValueBinding<float> GlassThickness { get; private set; }
        public MultiUIValueBinding<string> MainColorFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> MainColorFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> MainColorFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> EmissiveColorFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> EmissiveColorFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> EmissiveColorFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> MetallicFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> MetallicFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> MetallicFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> SmoothnessFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> SmoothnessFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> SmoothnessFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> EmissiveIntensityFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> EmissiveIntensityFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> EmissiveIntensityFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> CoatStrengthFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> CoatStrengthFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> CoatStrengthFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> EmissiveExposureWeightFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> EmissiveExposureWeightFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> EmissiveExposureWeightFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> DecalFlagsFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> DecalFlagsFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> DecalFlagsFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> ShaderTypeFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ShaderTypeFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ShaderTypeFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> GlassColorFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> GlassColorFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> GlassColorFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> GlassRefractionFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> GlassRefractionFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> GlassRefractionFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> ColorMask1FormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ColorMask1FormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ColorMask1FormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> ColorMask2FormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ColorMask2FormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ColorMask2FormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> ColorMask3FormulaeStr { get; private set; }
        public MultiUIValueBinding<int> ColorMask3FormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> ColorMask3FormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> NormalStrengthFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> NormalStrengthFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> NormalStrengthFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<string> GlassThicknessFormulaeStr { get; private set; }
        public MultiUIValueBinding<int> GlassThicknessFormulaeCompileResult { get; private set; }
        public MultiUIValueBinding<string[]> GlassThicknessFormulaeCompileResultErrorArgs { get; private set; }
        public MultiUIValueBinding<bool> AffectSmoothness { get; private set; }
        public MultiUIValueBinding<bool> AffectAO { get; private set; }
        public MultiUIValueBinding<bool> AffectEmission { get; private set; }
        public MultiUIValueBinding<float> DrawOrder { get; private set; }

        private WEWorldPickerController m_pickerController;

        protected override void DoInitValueBindings(Action<string, object[]> EventCaller, Action<string, Delegate> CallBinder)
        {
            m_pickerController = World.GetExistingSystemManaged<WEWorldPickerController>();

            MainColor = new(default, $"{PREFIX}{nameof(MainColor)}", EventCaller, CallBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            EmissiveColor = new(default, $"{PREFIX}{nameof(EmissiveColor)}", EventCaller, CallBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            Metallic = new(default, $"{PREFIX}{nameof(Metallic)}", EventCaller, CallBinder);
            Smoothness = new(default, $"{PREFIX}{nameof(Smoothness)}", EventCaller, CallBinder);
            EmissiveIntensity = new(default, $"{PREFIX}{nameof(EmissiveIntensity)}", EventCaller, CallBinder);
            CoatStrength = new(default, $"{PREFIX}{nameof(CoatStrength)}", EventCaller, CallBinder);
            EmissiveExposureWeight = new(default, $"{PREFIX}{nameof(EmissiveExposureWeight)}", EventCaller, CallBinder);
            DecalFlags = new(default, $"{PREFIX}{nameof(DecalFlags)}", EventCaller, CallBinder);
            ShaderType = new(default, $"{PREFIX}{nameof(ShaderType)}", EventCaller, CallBinder, (x, _) => (int)x, (x, _) => (WEShader)x);
            GlassRefraction = new(default, $"{PREFIX}{nameof(GlassRefraction)}", EventCaller, CallBinder);
            GlassColor = new(default, $"{PREFIX}{nameof(GlassColor)}", EventCaller, CallBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            GlassThickness = new(default, $"{PREFIX}{nameof(GlassThickness)}", EventCaller, CallBinder);
            ColorMask1 = new(default, $"{PREFIX}{nameof(ColorMask1)}", EventCaller, CallBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            ColorMask2 = new(default, $"{PREFIX}{nameof(ColorMask2)}", EventCaller, CallBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            ColorMask3 = new(default, $"{PREFIX}{nameof(ColorMask3)}", EventCaller, CallBinder, (x, _) => new() { r = x.r, g = x.g, b = x.b, a = x.a }, (x, _) => new Color(x.r, x.g, x.b, x.a));
            NormalStrength = new(default, $"{PREFIX}{nameof(NormalStrength)}", EventCaller, CallBinder);
            AffectSmoothness = new(default, $"{PREFIX}{nameof(AffectSmoothness)}", EventCaller, CallBinder);
            AffectAO = new(default, $"{PREFIX}{nameof(AffectAO)}", EventCaller, CallBinder);
            AffectEmission = new(default, $"{PREFIX}{nameof(AffectEmission)}", EventCaller, CallBinder);
            DrawOrder = new(default, $"{PREFIX}{nameof(DrawOrder)}", EventCaller, CallBinder);

            MainColorFormulaeStr = new(default, $"{PREFIX}{nameof(MainColorFormulaeStr)}", EventCaller, CallBinder);
            MainColorFormulaeCompileResult = new(default, $"{PREFIX}{nameof(MainColorFormulaeCompileResult)}", EventCaller, CallBinder);
            MainColorFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(MainColorFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            EmissiveColorFormulaeStr = new(default, $"{PREFIX}{nameof(EmissiveColorFormulaeStr)}", EventCaller, CallBinder);
            EmissiveColorFormulaeCompileResult = new(default, $"{PREFIX}{nameof(EmissiveColorFormulaeCompileResult)}", EventCaller, CallBinder);
            EmissiveColorFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(EmissiveColorFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            MetallicFormulaeStr = new(default, $"{PREFIX}{nameof(MetallicFormulaeStr)}", EventCaller, CallBinder);
            MetallicFormulaeCompileResult = new(default, $"{PREFIX}{nameof(MetallicFormulaeCompileResult)}", EventCaller, CallBinder);
            MetallicFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(MetallicFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            SmoothnessFormulaeStr = new(default, $"{PREFIX}{nameof(SmoothnessFormulaeStr)}", EventCaller, CallBinder);
            SmoothnessFormulaeCompileResult = new(default, $"{PREFIX}{nameof(SmoothnessFormulaeCompileResult)}", EventCaller, CallBinder);
            SmoothnessFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(SmoothnessFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            EmissiveIntensityFormulaeStr = new(default, $"{PREFIX}{nameof(EmissiveIntensityFormulaeStr)}", EventCaller, CallBinder);
            EmissiveIntensityFormulaeCompileResult = new(default, $"{PREFIX}{nameof(EmissiveIntensityFormulaeCompileResult)}", EventCaller, CallBinder);
            EmissiveIntensityFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(EmissiveIntensityFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            CoatStrengthFormulaeStr = new(default, $"{PREFIX}{nameof(CoatStrengthFormulaeStr)}", EventCaller, CallBinder);
            CoatStrengthFormulaeCompileResult = new(default, $"{PREFIX}{nameof(CoatStrengthFormulaeCompileResult)}", EventCaller, CallBinder);
            CoatStrengthFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(CoatStrengthFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            EmissiveExposureWeightFormulaeStr = new(default, $"{PREFIX}{nameof(EmissiveExposureWeightFormulaeStr)}", EventCaller, CallBinder);
            EmissiveExposureWeightFormulaeCompileResult = new(default, $"{PREFIX}{nameof(EmissiveExposureWeightFormulaeCompileResult)}", EventCaller, CallBinder);
            EmissiveExposureWeightFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(EmissiveExposureWeightFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            DecalFlagsFormulaeStr = new(default, $"{PREFIX}{nameof(DecalFlagsFormulaeStr)}", EventCaller, CallBinder);
            DecalFlagsFormulaeCompileResult = new(default, $"{PREFIX}{nameof(DecalFlagsFormulaeCompileResult)}", EventCaller, CallBinder);
            DecalFlagsFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(DecalFlagsFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            ShaderTypeFormulaeStr = new(default, $"{PREFIX}{nameof(ShaderTypeFormulaeStr)}", EventCaller, CallBinder);
            ShaderTypeFormulaeCompileResult = new(default, $"{PREFIX}{nameof(ShaderTypeFormulaeCompileResult)}", EventCaller, CallBinder);
            ShaderTypeFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ShaderTypeFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            GlassColorFormulaeStr = new(default, $"{PREFIX}{nameof(GlassColorFormulaeStr)}", EventCaller, CallBinder);
            GlassColorFormulaeCompileResult = new(default, $"{PREFIX}{nameof(GlassColorFormulaeCompileResult)}", EventCaller, CallBinder);
            GlassColorFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(GlassColorFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            GlassRefractionFormulaeStr = new(default, $"{PREFIX}{nameof(GlassRefractionFormulaeStr)}", EventCaller, CallBinder);
            GlassRefractionFormulaeCompileResult = new(default, $"{PREFIX}{nameof(GlassRefractionFormulaeCompileResult)}", EventCaller, CallBinder);
            GlassRefractionFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(GlassRefractionFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            ColorMask1FormulaeStr = new(default, $"{PREFIX}{nameof(ColorMask1FormulaeStr)}", EventCaller, CallBinder);
            ColorMask1FormulaeCompileResult = new(default, $"{PREFIX}{nameof(ColorMask1FormulaeCompileResult)}", EventCaller, CallBinder);
            ColorMask1FormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ColorMask1FormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            ColorMask2FormulaeStr = new(default, $"{PREFIX}{nameof(ColorMask2FormulaeStr)}", EventCaller, CallBinder);
            ColorMask2FormulaeCompileResult = new(default, $"{PREFIX}{nameof(ColorMask2FormulaeCompileResult)}", EventCaller, CallBinder);
            ColorMask2FormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ColorMask2FormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            ColorMask3FormulaeStr = new(default, $"{PREFIX}{nameof(ColorMask3FormulaeStr)}", EventCaller, CallBinder);
            ColorMask3FormulaeCompileResult = new(default, $"{PREFIX}{nameof(ColorMask3FormulaeCompileResult)}", EventCaller, CallBinder);
            ColorMask3FormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(ColorMask3FormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            NormalStrengthFormulaeStr = new(default, $"{PREFIX}{nameof(NormalStrengthFormulaeStr)}", EventCaller, CallBinder);
            NormalStrengthFormulaeCompileResult = new(default, $"{PREFIX}{nameof(NormalStrengthFormulaeCompileResult)}", EventCaller, CallBinder);
            NormalStrengthFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(NormalStrengthFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);
            GlassThicknessFormulaeStr = new(default, $"{PREFIX}{nameof(GlassThicknessFormulaeStr)}", EventCaller, CallBinder);
            GlassThicknessFormulaeCompileResult = new(default, $"{PREFIX}{nameof(GlassThicknessFormulaeCompileResult)}", EventCaller, CallBinder);
            GlassThicknessFormulaeCompileResultErrorArgs = new(default, $"{PREFIX}{nameof(GlassThicknessFormulaeCompileResultErrorArgs)}", EventCaller, CallBinder);


            DecalFlags.OnScreenValueChanged += (x) => PickerController.EnqueueModification<int, WETextDataMaterial>(x, (x, currentItem) => { currentItem.DecalFlags = x; return currentItem; });
            ShaderType.OnScreenValueChanged += (x) => PickerController.EnqueueModification<WEShader, WETextDataMaterial>(x, (x, currentItem) => { currentItem.Shader = x; return currentItem; });
            MainColor.OnScreenValueChanged += (x) => PickerController.EnqueueModification<Color, WETextDataMaterial>(x, (x, currentItem) => { currentItem.Color = x; return currentItem; });
            EmissiveColor.OnScreenValueChanged += (x) => PickerController.EnqueueModification<Color, WETextDataMaterial>(x, (x, currentItem) => { currentItem.EmissiveColor = x; return currentItem; });
            Metallic.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.Metallic = x; return currentItem; });
            Smoothness.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.Smoothness = x; return currentItem; });
            EmissiveIntensity.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.EmissiveIntensity = x; return currentItem; });
            CoatStrength.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.CoatStrength = x; return currentItem; });
            EmissiveExposureWeight.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.EmissiveExposureWeight = x; return currentItem; });
            GlassRefraction.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.GlassRefraction = x; return currentItem; });
            GlassColor.OnScreenValueChanged += (x) => PickerController.EnqueueModification<Color, WETextDataMaterial>(x, (x, currentItem) => { currentItem.GlassColor = x; return currentItem; });
            GlassThickness.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.GlassThickness = x; return currentItem; });
            ColorMask1.OnScreenValueChanged += (x) => PickerController.EnqueueModification<Color, WETextDataMaterial>(x, (x, currentItem) => { currentItem.ColorMask1 = x; return currentItem; });
            ColorMask2.OnScreenValueChanged += (x) => PickerController.EnqueueModification<Color, WETextDataMaterial>(x, (x, currentItem) => { currentItem.ColorMask2 = x; return currentItem; });
            ColorMask3.OnScreenValueChanged += (x) => PickerController.EnqueueModification<Color, WETextDataMaterial>(x, (x, currentItem) => { currentItem.ColorMask3 = x; return currentItem; });
            NormalStrength.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.NormalStrength = x; return currentItem; });
            AffectSmoothness.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataMaterial>(x, (x, currentItem) => { currentItem.AffectSmoothness = x; return currentItem; });
            AffectAO.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataMaterial>(x, (x, currentItem) => { currentItem.AffectAO = x; return currentItem; });
            AffectEmission.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataMaterial>(x, (x, currentItem) => { currentItem.AffectEmission = x; return currentItem; });
            DrawOrder.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float, WETextDataMaterial>(x, (x, currentItem) => { currentItem.DrawOrder = x; return currentItem; });


            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeMainColor(newFormulae, out errorArgs), MainColorFormulaeStr, MainColorFormulaeCompileResult, MainColorFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeEmissiveColor(newFormulae, out errorArgs), EmissiveColorFormulaeStr, EmissiveColorFormulaeCompileResult, EmissiveColorFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeMetallic(newFormulae, out errorArgs), MetallicFormulaeStr, MetallicFormulaeCompileResult, MetallicFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeSmoothness(newFormulae, out errorArgs), SmoothnessFormulaeStr, SmoothnessFormulaeCompileResult, SmoothnessFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeEmissiveIntensity(newFormulae, out errorArgs), EmissiveIntensityFormulaeStr, EmissiveIntensityFormulaeCompileResult, EmissiveIntensityFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeCoatStrength(newFormulae, out errorArgs), CoatStrengthFormulaeStr, CoatStrengthFormulaeCompileResult, CoatStrengthFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeEmissiveExposureWeight(newFormulae, out errorArgs), EmissiveExposureWeightFormulaeStr, EmissiveExposureWeightFormulaeCompileResult, EmissiveExposureWeightFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeGlassRefraction(newFormulae, out errorArgs), GlassRefractionFormulaeStr, GlassRefractionFormulaeCompileResult, GlassRefractionFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeGlassColor(newFormulae, out errorArgs), GlassColorFormulaeStr, GlassColorFormulaeCompileResult, GlassColorFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeGlassThickness(newFormulae, out errorArgs), GlassThicknessFormulaeStr, GlassThicknessFormulaeCompileResult, GlassThicknessFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeColorMask1(newFormulae, out errorArgs), ColorMask1FormulaeStr, ColorMask1FormulaeCompileResult, ColorMask1FormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeColorMask2(newFormulae, out errorArgs), ColorMask2FormulaeStr, ColorMask2FormulaeCompileResult, ColorMask2FormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeColorMask3(newFormulae, out errorArgs), ColorMask3FormulaeStr, ColorMask3FormulaeCompileResult, ColorMask3FormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeNormalStrength(newFormulae, out errorArgs), NormalStrengthFormulaeStr, NormalStrengthFormulaeCompileResult, NormalStrengthFormulaeCompileResultErrorArgs);
            SetupOnFormulaeChangedAction((ref WETextDataMaterial data, string newFormulae, out string[] errorArgs) => data.SetFormulaeNormalStrength(newFormulae, out errorArgs), NormalStrengthFormulaeStr, NormalStrengthFormulaeCompileResult, NormalStrengthFormulaeCompileResultErrorArgs);

            CallBinder($"{PREFIX}isDecalMesh", () => EntityManager.TryGetComponent<WETextDataMaterial>(m_pickerController.CurrentSubEntity.Value, out var material) && EntityManager.TryGetComponent<WETextDataMesh>(m_pickerController.CurrentSubEntity.Value, out var mesh) ? material.CheckIsDecal(mesh) : false);
        }
        private void SetupOnFormulaeChangedAction(FormulaeSetter<WETextDataMaterial> formulaeSetter, MultiUIValueBinding<string> formulaeStr, MultiUIValueBinding<int> formulaeCompileResult, MultiUIValueBinding<string[]> formulaeCompileResultErrorArgs)
            => WEFormulaeHelper.SetupOnFormulaeChangedAction(PickerController, formulaeSetter, formulaeStr, formulaeCompileResult, formulaeCompileResultErrorArgs);

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataMaterial>(entity, out var material);

            MainColor.Value = material.Color;
            EmissiveColor.Value = material.EmissiveColor;
            Metallic.Value = material.Metallic;
            Smoothness.Value = material.Smoothness;
            EmissiveIntensity.Value = material.EmissiveIntensity;
            CoatStrength.Value = material.CoatStrength;
            DecalFlags.Value = material.DecalFlags;
            ShaderType.Value = material.Shader;
            GlassColor.Value = material.GlassColor;
            GlassRefraction.Value = material.GlassRefraction;
            ColorMask1.Value = material.ColorMask1;
            ColorMask2.Value = material.ColorMask2;
            ColorMask3.Value = material.ColorMask3;
            NormalStrength.Value = material.NormalStrength;
            GlassThickness.Value = material.GlassThickness;
            AffectSmoothness.Value = material.AffectSmoothness;
            AffectAO.Value = material.AffectAO;
            AffectEmission.Value = material.AffectEmission;
            DrawOrder.Value = material.DrawOrder;

            ResetScreenFormulaeValue(material.ColorFormulae, MainColorFormulaeStr, MainColorFormulaeCompileResult, MainColorFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.EmissiveColorFormulae, EmissiveColorFormulaeStr, EmissiveColorFormulaeCompileResult, EmissiveColorFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.MetallicFormulae, MetallicFormulaeStr, MetallicFormulaeCompileResult, MetallicFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.SmoothnessFormulae, SmoothnessFormulaeStr, SmoothnessFormulaeCompileResult, SmoothnessFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.EmissiveIntensityFormulae, EmissiveIntensityFormulaeStr, EmissiveIntensityFormulaeCompileResult, EmissiveIntensityFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.CoatStrengthFormulae, CoatStrengthFormulaeStr, CoatStrengthFormulaeCompileResult, CoatStrengthFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.EmissiveExposureWeightFormulae, EmissiveExposureWeightFormulaeStr, EmissiveExposureWeightFormulaeCompileResult, EmissiveExposureWeightFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.GlassRefractionFormulae, GlassRefractionFormulaeStr, GlassRefractionFormulaeCompileResult, GlassRefractionFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.GlassColorFormulae, GlassColorFormulaeStr, GlassColorFormulaeCompileResult, GlassColorFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.GlassThicknessFormulae, GlassThicknessFormulaeStr, GlassThicknessFormulaeCompileResult, GlassThicknessFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.ColorMask1Formulae, ColorMask1FormulaeStr, ColorMask1FormulaeCompileResult, ColorMask1FormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.ColorMask2Formulae, ColorMask2FormulaeStr, ColorMask2FormulaeCompileResult, ColorMask2FormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.ColorMask3Formulae, ColorMask3FormulaeStr, ColorMask3FormulaeCompileResult, ColorMask3FormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.NormalStrengthFormulae, NormalStrengthFormulaeStr, NormalStrengthFormulaeCompileResult, NormalStrengthFormulaeCompileResultErrorArgs);
            ResetScreenFormulaeValue(material.NormalStrengthFormulae, NormalStrengthFormulaeStr, NormalStrengthFormulaeCompileResult, NormalStrengthFormulaeCompileResultErrorArgs);
        }


    }
}