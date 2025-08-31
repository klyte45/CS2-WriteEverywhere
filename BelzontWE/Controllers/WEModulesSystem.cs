using Belzont.Interfaces;
using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public partial class WEModulesSystem : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "modules.";
        private readonly Dictionary<string, Dictionary<string, IWEModuleOptionField>> modulesOptions = new();

        public void RegisterOptions(Assembly mainAssembly, Dictionary<string, (int, object)> options)
        {
            if (mainAssembly == null) throw new ArgumentNullException(nameof(mainAssembly));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Count == 0) return;
            var dict = new Dictionary<string, IWEModuleOptionField>();
            foreach (var kv in options)
            {
                string i18nKey = kv.Key;
                var (type, actions) = kv.Value;
                if (string.IsNullOrWhiteSpace(i18nKey))
                {
                    throw new ArgumentException("Option key cannot be null or whitespace.");
                }
                if (dict.ContainsKey(i18nKey))
                {
                    throw new ArgumentException($"Option with key '{i18nKey}' is already registered for this module.");
                }
                switch (type)
                {
                    case 0:
                        {
                            if (actions is not WEModuleOptionField_Boolean.Options optsBool)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type Boolean.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_Boolean(i18nKey, optsBool);
                            break;
                        }
                    case 1:
                        {
                            if (actions is not WEModuleOptionField_Dropdown.Options optsDropdown)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type Dropdown.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_Dropdown(i18nKey, optsDropdown);
                            break;
                        }
                    case 2:
                        {
                            dict[i18nKey] = new WEModuleOptionField_SectionTitle(i18nKey);
                            break;
                        }
                    case 3:
                        {
                            if (actions is not WEModuleOptionField_ButtonRow.Options optsButtonRow)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type ButtonRow.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_ButtonRow(i18nKey, optsButtonRow);
                            break;
                        }
                    case 4:
                        {
                            if (actions is not WEModuleOptionField_Slider.Options optsSlider)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type Slider.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_Slider(i18nKey, optsSlider);
                            break;
                        }
                    case 5:
                        {
                            if (actions is not WEModuleOptionField_FilePicker.Options optsFilePicker)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type FilePicker.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_FilePicker(i18nKey, optsFilePicker);
                            break;
                        }
                    case 6:
                        {
                            if (actions is not WEModuleOptionField_ColorPicker.Options optsColorPicker)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type ColorPicker.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_ColorPicker(i18nKey, optsColorPicker);
                            break;
                        }
                    case 7:
                        {
                            dict[i18nKey] = new WEModuleOptionField_Spacer(i18nKey);
                            break;
                        }
                    case 8:
                        {
                            if (actions is not WEModuleOptionField_TextInput.Options optsTextInput)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type TextInput.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_TextInput(i18nKey, optsTextInput);
                            break;
                        }
                    case 9:
                        {
                            if (actions is not WEModuleOptionField_MultiLineTextInput.Options optsMultiLineTextInput)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type MultiLineTextInput.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_MultiLineTextInput(i18nKey, optsMultiLineTextInput);
                            break;
                        }
                    case 10:
                        {
                            if (actions is not WEModuleOptionField_RadioButtons.Options optsRadioButtons)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type RadioButtons.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_RadioButtons(i18nKey, optsRadioButtons);
                            break;
                        }
                    case 11:
                        {
                            if (actions is not WEModuleOptionField_MultiSelect.Options optsMultiSelect)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type MultiSelect.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_MultiSelect(i18nKey, optsMultiSelect);
                            break;
                        }
                    case 12:
                        {
                            if (actions is not WEModuleOptionField_Vector2Input.Options optsVector2Input)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type Vector2Input.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_Vector2Input(i18nKey, optsVector2Input);
                            break;
                        }
                    case 13:
                        {
                            if (actions is not WEModuleOptionField_Vector3Input.Options optsVector3Input)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type Vector3Input.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_Vector3Input(i18nKey, optsVector3Input);
                            break;
                        }
                    case 14:
                        {
                            if (actions is not WEModuleOptionField_Vector4Input.Options optsVector4Input)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type Vector4Input.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_Vector4Input(i18nKey, optsVector4Input);
                            break;
                        }
                    case 15:
                        {
                            if (actions is not WEModuleOptionField_IntInput.Options optsIntInput)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type IntInput.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_IntInput(i18nKey, optsIntInput);
                            break;
                        }
                    case 16:
                        {
                            if (actions is not WEModuleOptionField_FloatInput.Options optsFloatInput)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type FloatInput.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_FloatInput(i18nKey, optsFloatInput);
                            break;
                        }
                    case 17:
                        {
                            if (actions is not WEModuleOptionField_RangeInput.Options optsRangeInput)
                            {
                                throw new ArgumentException($"Option with key '{i18nKey}' has invalid actions for type RangeInput.");
                            }
                            dict[i18nKey] = new WEModuleOptionField_RangeInput(i18nKey, optsRangeInput);
                            break;
                        }
                    default:
                        throw new ArgumentException($"Option with key '{i18nKey}' has unknown type '{type}'.");
                }
            }
            modulesOptions[WEModIntegrationUtility.GetModIdentifier(mainAssembly)] = dict;
        }

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}getFieldValue", GetFieldValue);
            callBinder($"{PREFIX}setFieldValue", SetFieldValue);
            callBinder($"{PREFIX}getFieldOptions", GetFieldOptions);
            callBinder($"{PREFIX}listAllOptions", ListAllOptions);
            callBinder($"{PREFIX}getMinMax", GetMinMax);
            callBinder($"{PREFIX}getFilePickerOptions", GetFilePickerOptions);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        protected override void OnUpdate() { }

        private Dictionary<string, Dictionary<string, int>> ListAllOptions()
            => modulesOptions.Select(y => new KeyValuePair<string, Dictionary<string, int>>(y.Key, y.Value.Select(x => new KeyValuePair<string, int>(x.Key, x.Value.GetTypeId())).ToDictionary(x => x.Key, x => x.Value))).ToDictionary(x => x.Key, x => x.Value);


        private object GetFieldValue(string modIdentifier, string i18nKey)
            => modulesOptions.TryGetValue(modIdentifier, out var options) && options.TryGetValue(i18nKey, out var field)
                ? field switch
                {
                    IWEModuleGetterField<bool> boolField => boolField.Getter(),
                    IWEModuleGetterField<string> stringField => stringField.Getter(),
                    IWEModuleGetterField<int> intField => intField.Getter(),
                    IWEModuleGetterField<float> floatField => floatField.Getter(),
                    IWEModuleGetterField<float2> float2Field => float2Field.Getter() is float2 f2 ? new float[] { f2.x, f2.y } : new float[0],
                    IWEModuleGetterField<float3> float3Field => float3Field.Getter() is float3 f3 ? new float[] { f3.x, f3.y, f3.z } : new float[0],
                    IWEModuleGetterField<float4> float4Field => float4Field.Getter() is float4 f4 ? new float[] { f4.x, f4.y, f4.z, f4.w } : new float[0],
                    IWEModuleGetterField<List<string>> listStringField => listStringField.Getter() ?? new List<string>(),
                    IWEModuleGetterField<Color> colorField => colorField.Getter() is Color c ? ColorExtensions.ToRGB(c) : "",
                    _ => throw new InvalidOperationException($"Unsupported field type for option '{i18nKey}' in module '{modIdentifier}'."),
                }
                : throw new KeyNotFoundException($"Option '{i18nKey}' not found in module '{modIdentifier}'.");

        private void SetFieldValue(string modIdentifier, string i18nKey, string value)
        {
            if (!modulesOptions.TryGetValue(modIdentifier, out var options) || !options.TryGetValue(i18nKey, out var field))
            {
                throw new KeyNotFoundException($"Option '{i18nKey}' not found in module '{modIdentifier}'.");
            }
            switch (field)
            {
                case IWEModuleSetterField<bool> boolField:
                    if (!bool.TryParse(value, out var boolValue))
                    {
                        throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected bool. Val = {value}");
                    }
                    boolField.Setter(boolValue);
                    break;
                case IWEModuleSetterField<string> stringField:
                    stringField.Setter(value);
                    break;
                case IWEModuleSetterField<int> intField:
                    if (!int.TryParse(value, out var intValue))
                    {
                        throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected int. Val = {value}");
                    }
                    intField.Setter(intValue);
                    break;
                case IWEModuleSetterField<float> floatField:
                    if (!float.TryParse(value, out var floatValue))
                    {
                        throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected float. Val = {value}");
                    }
                    floatField.Setter(floatValue);
                    break;
                case IWEModuleSetterField<float2> float2Field:
                    {
                        var parts = value.Split('|');
                        if (parts.Length != 2 || !float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) || !float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y))
                        {
                            throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected float2 as 'x,y'. Val = {value}");
                        }
                        float2Field.Setter(new float2(x, y));
                        break;
                    }
                case IWEModuleSetterField<float3> float3Field:
                    {
                        var parts = value.Split('|');
                        if (parts.Length != 3 || !float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) || !float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y) || !float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var z))
                        {
                            throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected float3 as 'x,y,z'. Val = {value}");
                        }
                        float3Field.Setter(new float3(x, y, z));
                        break;
                    }
                case IWEModuleSetterField<float4> float4Field:
                    {
                        var parts = value.Split('|');
                        if (parts.Length != 4 || !float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) || !float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y) || !float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var z) || !float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var w))
                        {
                            throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected float4 as 'x,y,z,w'. Val = {value}");
                        }
                        float4Field.Setter(new float4(x, y, z, w));
                        break;
                    }
                case IWEModuleSetterField<List<string>> listStringField:
                    {
                        var parts = value.Split('|').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                        listStringField.Setter(parts);
                        break;
                    }
                case IWEModuleSetterField<Color> colorField:
                    {
                        var parts = ColorExtensions.FromRGB(value);
                        colorField.Setter(parts);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Unsupported field type for option '{i18nKey}' in module '{modIdentifier}'.");
            }
        }

        private Dictionary<string, string> GetFieldOptions(string modIdentifier, string i18nKey)
            => modulesOptions.TryGetValue(modIdentifier, out var options) && options.TryGetValue(i18nKey, out var field) && field is IWEModuleWithOptionList listField
                ? listField.ListOptions_value_displayI18n()
                : throw new KeyNotFoundException($"Option '{i18nKey}' not found in module '{modIdentifier}' or does not support option listing.");

        private float[][] GetMinMax(string modIdentifier, string i18nKey)
            => modulesOptions.TryGetValue(modIdentifier, out var options) && options.TryGetValue(i18nKey, out var field)
                ? field switch
                {
                    IWEMinMaxField<int> intMinMaxField => new float[][] { new float[] { intMinMaxField.Min }, new float[] { intMinMaxField.Max } },
                    IWEMinMaxField<float> floatMinMaxField => new float[][] { new float[] { floatMinMaxField.Min }, new float[] { floatMinMaxField.Max } },
                    IWEMinMaxField<float2> float2MinMaxField => new float[][] { new float[] { float2MinMaxField.Min.x, float2MinMaxField.Min.y }, new float[] { float2MinMaxField.Max.x, float2MinMaxField.Max.y } },
                    IWEMinMaxField<float3> float3MinMaxField => new float[][] { new float[] { float3MinMaxField.Min.x, float3MinMaxField.Min.y, float3MinMaxField.Min.z }, new float[] { float3MinMaxField.Max.x, float3MinMaxField.Max.y, float3MinMaxField.Max.z } },
                    IWEMinMaxField<float4> float4MinMaxField => new float[][] { new float[] { float4MinMaxField.Min.x, float4MinMaxField.Min.y, float4MinMaxField.Min.z, float4MinMaxField.Min.w }, new float[] { float4MinMaxField.Max.x, float4MinMaxField.Max.y, float4MinMaxField.Max.z, float4MinMaxField.Max.w } },
                    _ => throw new InvalidOperationException($"Option '{i18nKey}' in module '{modIdentifier}' does not support Min/Max."),
                }
                : throw new KeyNotFoundException($"Option '{i18nKey}' not found in module '{modIdentifier}'.");

        private (string fileExtensionFilter, string initialPath, string promptText) GetFilePickerOptions(string modIdentifier, string i18nKey)
            => modulesOptions.TryGetValue(modIdentifier, out var options) && options.TryGetValue(i18nKey, out var field) && field is IWEFilePickerOptionField filePickerField
                ? (filePickerField.FileExtensionFilter, filePickerField.InitialPath, filePickerField.PromptText)
                : throw new KeyNotFoundException($"Option '{i18nKey}' not found in module '{modIdentifier}' or is not a file picker.");
        private interface IWEModuleOptionField
        {
            public string I18nKey { get; }
            public int GetTypeId();
        }
        private interface IWEModuleSetterField<T> : IWEModuleOptionField
        {
            public Action<T> Setter { get; }
        }

        private interface IWEModuleGetterField<T> : IWEModuleOptionField
        {
            public Func<T> Getter { get; }
        }

        private interface IWEModuleWithOptionList
        {
            public Func<Dictionary<string, string>> ListOptions_value_displayI18n { get; }
        }

        private interface IWEMinMaxField<T>
        {
            public T Min { get; }
            public T Max { get; }
        }

        private interface IWEFilePickerOptionField
        {
            public string FileExtensionFilter { get; }
            public string InitialPath { get; }
            public string PromptText { get; }
        }

        internal class WEModuleOptionField_Boolean : IWEModuleGetterField<bool>, IWEModuleSetterField<bool>
        {
            public string I18nKey { get; }
            public Func<bool> Getter { get; }
            public Action<bool> Setter { get; }
            public WEModuleOptionField_Boolean(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
            }
            internal record class Options(Func<bool> Getter, Action<bool> Setter) { }

            public int GetTypeId() => 0;
        }
        internal class WEModuleOptionField_Dropdown : IWEModuleGetterField<string>, IWEModuleSetterField<string>, IWEModuleWithOptionList
        {
            public string I18nKey { get; }
            public Func<string> Getter { get; }
            public Action<string> Setter { get; }
            public Func<Dictionary<string, string>> ListOptions_value_displayI18n { get; }
            public WEModuleOptionField_Dropdown(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                ListOptions_value_displayI18n = options.ListOptions_value_displayI18n;
            }
            internal record class Options(Func<string> Getter, Action<string> Setter, Func<Dictionary<string, string>> ListOptions_value_displayI18n) { }
            public int GetTypeId() => 1;
        }

        internal class WEModuleOptionField_SectionTitle : IWEModuleOptionField
        {
            public string I18nKey { get; }
            public WEModuleOptionField_SectionTitle(string i18nKey)
            {
                I18nKey = i18nKey;
            }
            public int GetTypeId() => 2;
        }

        internal class WEModuleOptionField_ButtonRow : IWEModuleSetterField<string>, IWEModuleWithOptionList
        {
            public string I18nKey { get; }

            public Action<string> Setter { get; }
            public Func<Dictionary<string, string>> ListOptions_value_displayI18n { get; }

            internal record class Options(Action<string> Setter, Func<Dictionary<string, string>> ListOptions_value_displayI18n) { }
            public WEModuleOptionField_ButtonRow(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Setter = options.Setter;
                ListOptions_value_displayI18n = options.ListOptions_value_displayI18n;
            }
            public int GetTypeId() => 3;
        }

        internal class WEModuleOptionField_Slider : IWEModuleGetterField<float>, IWEModuleSetterField<float>, IWEMinMaxField<float>
        {
            public string I18nKey { get; }
            public Func<float> Getter { get; }
            public Action<float> Setter { get; }
            public float Min { get; }
            public float Max { get; }
            internal record class Options(Func<float> Getter, Action<float> Setter, float Min, float Max) { }
            public WEModuleOptionField_Slider(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.Min;
                Max = options.Max;
            }
            public int GetTypeId() => 4;
        }

        internal class WEModuleOptionField_FilePicker : IWEModuleGetterField<string>, IWEModuleSetterField<string>, IWEFilePickerOptionField
        {
            public string I18nKey { get; }
            public Func<string> Getter { get; }
            public Action<string> Setter { get; }

            public string FileExtensionFilter { get; }
            public string InitialPath { get; }
            public string PromptText { get; }

            internal record class Options(Func<string> Getter, Action<string> Setter, string FileExtensionFilter, string InitialPath, string PromptText) { }
            public WEModuleOptionField_FilePicker(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                FileExtensionFilter = options.FileExtensionFilter;
                InitialPath = options.InitialPath;
                PromptText = options.PromptText;
            }
            public int GetTypeId() => 5;
        }

        internal class WEModuleOptionField_ColorPicker : IWEModuleGetterField<Color>, IWEModuleSetterField<Color>
        {
            public string I18nKey { get; }
            public Func<Color> Getter { get; }
            public Action<Color> Setter { get; }
            internal record class Options(Func<Color> Getter, Action<Color> Setter) { }
            public WEModuleOptionField_ColorPicker(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
            }
            public int GetTypeId() => 6;
        }

        internal class WEModuleOptionField_Spacer : IWEModuleOptionField
        {
            public string I18nKey { get; }
            public WEModuleOptionField_Spacer(string i18nKey)
            {
                I18nKey = i18nKey;
            }
            public int GetTypeId() => 7;
        }

        internal class WEModuleOptionField_TextInput : IWEModuleGetterField<string>, IWEModuleSetterField<string>, IWEMinMaxField<int>
        {
            public string I18nKey { get; }
            public Func<string> Getter { get; }
            public Action<string> Setter { get; }
            public int Min { get; }
            public int Max { get; }
            internal record class Options(Func<string> Getter, Action<string> Setter, int MinLength = 0, int MaxLength = int.MaxValue) { }
            public WEModuleOptionField_TextInput(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.MinLength;
                Max = options.MaxLength;
            }
            public int GetTypeId() => 8;
        }

        internal class WEModuleOptionField_MultiLineTextInput : IWEModuleGetterField<string>, IWEModuleSetterField<string>, IWEMinMaxField<int>
        {
            public string I18nKey { get; }
            public Func<string> Getter { get; }
            public Action<string> Setter { get; }
            public int Min { get; }
            public int Max { get; }
            internal record class Options(Func<string> Getter, Action<string> Setter, int MinLength = 0, int MaxLength = int.MaxValue) { }
            public WEModuleOptionField_MultiLineTextInput(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.MinLength;
                Max = options.MaxLength;
            }
            public int GetTypeId() => 9;
        }

        internal class WEModuleOptionField_RadioButtons : IWEModuleGetterField<string>, IWEModuleSetterField<string>, IWEModuleWithOptionList
        {
            public string I18nKey { get; }
            public Func<string> Getter { get; }
            public Action<string> Setter { get; }
            public Func<Dictionary<string, string>> ListOptions_value_displayI18n { get; }
            internal record class Options(Func<string> Getter, Action<string> Setter, Func<Dictionary<string, string>> ListOptions_value_displayI18n) { }
            public WEModuleOptionField_RadioButtons(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                ListOptions_value_displayI18n = options.ListOptions_value_displayI18n;
            }
            public int GetTypeId() => 10;
        }

        internal class WEModuleOptionField_MultiSelect : IWEModuleGetterField<HashSet<string>>, IWEModuleSetterField<HashSet<string>>, IWEModuleWithOptionList, IWEMinMaxField<int>
        {
            public string I18nKey { get; }
            public Func<HashSet<string>> Getter { get; }
            public Action<HashSet<string>> Setter { get; }
            public Func<Dictionary<string, string>> ListOptions_value_displayI18n { get; }
            public int Min { get; }
            public int Max { get; }
            internal record class Options(Func<HashSet<string>> Getter, Action<HashSet<string>> Setter, Func<Dictionary<string, string>> ListOptions_value_displayI18n, int MinSelected = 0, int MaxSelected = int.MaxValue) { }
            public WEModuleOptionField_MultiSelect(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                ListOptions_value_displayI18n = options.ListOptions_value_displayI18n;
                Min = options.MinSelected;
                Max = options.MaxSelected;
            }
            public int GetTypeId() => 11;
        }

        internal class WEModuleOptionField_Vector2Input : IWEModuleGetterField<float2>, IWEModuleSetterField<float2>, IWEMinMaxField<float2>
        {
            public string I18nKey { get; }
            public Func<float2> Getter { get; }
            public Action<float2> Setter { get; }
            public float2 Min { get; }
            public float2 Max { get; }
            internal record class Options(Func<float2> Getter, Action<float2> Setter, float2? Min = null, float2? Max = null) { }
            public WEModuleOptionField_Vector2Input(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.Min ?? new float2(float.NegativeInfinity, float.NegativeInfinity);
                Max = options.Max ?? new float2(float.PositiveInfinity, float.PositiveInfinity);
            }
            public int GetTypeId() => 12;
        }

        internal class WEModuleOptionField_Vector3Input : IWEModuleGetterField<float3>, IWEModuleSetterField<float3>, IWEMinMaxField<float3>
        {
            public string I18nKey { get; }
            public Func<float3> Getter { get; }
            public Action<float3> Setter { get; }
            public float3 Min { get; }
            public float3 Max { get; }
            internal record class Options(Func<float3> Getter, Action<float3> Setter, float3? Min = null, float3? Max = null) { }
            public WEModuleOptionField_Vector3Input(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.Min ?? new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                Max = options.Max ?? new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            }
            public int GetTypeId() => 13;
        }

        internal class WEModuleOptionField_Vector4Input : IWEModuleGetterField<float4>, IWEModuleSetterField<float4>, IWEMinMaxField<float4>
        {
            public string I18nKey { get; }
            public Func<float4> Getter { get; }
            public Action<float4> Setter { get; }
            public float4 Min { get; }
            public float4 Max { get; }
            internal record class Options(Func<float4> Getter, Action<float4> Setter, float4? Min = null, float4? Max = null) { }
            public WEModuleOptionField_Vector4Input(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.Min ?? new float4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                Max = options.Max ?? new float4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            }
            public int GetTypeId() => 14;
        }

        internal class WEModuleOptionField_IntInput : IWEModuleGetterField<int>, IWEModuleSetterField<int>
        {
            public string I18nKey { get; }
            public Func<int> Getter { get; }
            public Action<int> Setter { get; }
            internal record class Options(Func<int> Getter, Action<int> Setter) { }
            public WEModuleOptionField_IntInput(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
            }
            public int GetTypeId() => 15;
        }

        internal class WEModuleOptionField_FloatInput : IWEModuleGetterField<float>, IWEModuleSetterField<float>
        {
            public string I18nKey { get; }
            public Func<float> Getter { get; }
            public Action<float> Setter { get; }
            internal record class Options(Func<float> Getter, Action<float> Setter) { }
            public WEModuleOptionField_FloatInput(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
            }
            public int GetTypeId() => 16;
        }

        internal class WEModuleOptionField_RangeInput : IWEModuleGetterField<Vector2>, IWEModuleSetterField<Vector2>
        {
            public string I18nKey { get; }
            public Func<Vector2> Getter { get; }
            public Action<Vector2> Setter { get; }
            public float Min { get; }
            public float Max { get; }
            internal record class Options(Func<Vector2> Getter, Action<Vector2> Setter, float Min, float Max) { }
            public WEModuleOptionField_RangeInput(string i18nKey, Options options)
            {
                I18nKey = i18nKey;
                Getter = options.Getter;
                Setter = options.Setter;
                Min = options.Min;
                Max = options.Max;
            }
            public int GetTypeId() => 17;
        }
    }
}