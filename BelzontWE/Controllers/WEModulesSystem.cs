using Belzont.Interfaces;
using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;

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
                    IWEModuleOptionField<bool> boolField => boolField.Getter(),
                    IWEModuleOptionField<string> stringField => stringField.Getter(),
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
                case IWEModuleOptionField<bool> boolField:
                    if (!bool.TryParse(value, out var boolValue))
                    {
                        throw new ArgumentException($"Invalid value type for option '{i18nKey}' in module '{modIdentifier}'. Expected bool. Val = {value}");
                    }
                    boolField.Setter(boolValue);
                    break;
                case IWEModuleOptionField<string> stringField:
                    stringField.Setter(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported field type for option '{i18nKey}' in module '{modIdentifier}'.");
            }
        }

        private Dictionary<string, string> GetFieldOptions(string modIdentifier, string i18nKey)
            => modulesOptions.TryGetValue(modIdentifier, out var options) && options.TryGetValue(i18nKey, out var field) && field is IWEModuleWithOptionList listField
                ? listField.ListOptions_value_displayI18n()
                : throw new KeyNotFoundException($"Option '{i18nKey}' not found in module '{modIdentifier}' or does not support option listing.");

        private interface IWEModuleOptionField
        {
            public string I18nKey { get; }
            public int GetTypeId();
        }
        private interface IWEModuleOptionField<T> : IWEModuleOptionField
        {
            public Func<T> Getter { get; }
            public Action<T> Setter { get; }
        }

        private interface IWEModuleWithOptionList
        {
            public Func<Dictionary<string, string>> ListOptions_value_displayI18n { get; }
        }

        internal class WEModuleOptionField_Boolean : IWEModuleOptionField<bool>
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
        internal class WEModuleOptionField_Dropdown : IWEModuleOptionField<string>, IWEModuleWithOptionList
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
    }


}