using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using static BelzontWE.WEModulesSystem;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class ModuleOptionsBridge
    {
        private static WEModulesSystem instance;

        /// <summary>
        /// Register options to be used in the mod options panel for a WE Module.
        /// </summary>
        /// <param name="mainAssembly">The assembly of the module</param>
        /// <param name="options">
        /// A dictionary of options to register. The key is the option locale id. The value is a tuple of 2 items, first being the option type, second being the actions for the field. Use the helper methods to create the tuple.
        /// </param>
        public static bool RegisterOptions(Assembly mainAssembly, Dictionary<string, (int, object)> options)
        {
            instance ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WEModulesSystem>();
            try
            {
                instance.RegisterOptions(mainAssembly, options);
                return true;
            }
            catch (Exception ex)
            {
                LogUtils.DoErrorLog($"An error occurred while registering module options for {mainAssembly?.FullName}.", ex);
                return false;
            }
        }

        public static (int, object) AsBooleanOptionData(Func<bool> getter, Action<bool> setter) => (0, new WEModuleOptionField_Boolean.Options(getter, setter));
        public static (int, object) AsDropdownOptionData(Func<string> getter, Action<string> setter, Func<Dictionary<string, string>> optionsLister_value_displayNameI18n) => (1, new WEModuleOptionField_Dropdown.Options(getter, setter, optionsLister_value_displayNameI18n));
        public static (int, object) AsSectionTitleOptionData() => (2, null);
        public static (int, object) AsButtonRowOptionData(Action<string> handler, Func<Dictionary<string, string>> buttons_key_displayName) => (3, new WEModuleOptionField_ButtonRow.Options(handler, buttons_key_displayName));
        public static (int, object) AsSliderOptionData(Func<float> getter, Action<float> setter, float min = float.MinValue, float max = float.MaxValue) => (4, new WEModuleOptionField_Slider.Options(getter, setter, min, max));
        public static (int, object) AsFilePickerOptionData(Func<string> getter, Action<string> setter, string promptText, string initialPath, string fileExtension = "*") => (5, new WEModuleOptionField_FilePicker.Options(getter, setter, fileExtension, initialPath, promptText));
        public static (int, object) AsColorPickerOptionData(Func<UnityEngine.Color> getter, Action<UnityEngine.Color> setter) => (6, new WEModuleOptionField_ColorPicker.Options(getter, setter));
        public static (int, object) AsSpacerOptionData() => (7, null);
        public static (int, object) AsTextInputOptionData(Func<string> getter, Action<string> setter) => (8, new WEModuleOptionField_TextInput.Options(getter, setter));
        public static (int, object) AsMultiLineTextInputOptionData(Func<string> getter, Action<string> setter) => (9, new WEModuleOptionField_MultiLineTextInput.Options(getter, setter));
    }
}
