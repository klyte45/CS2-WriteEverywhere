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
    }
}
