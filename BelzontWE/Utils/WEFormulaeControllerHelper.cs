using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    // Pure static helper extracted from WEFormulaeController.
    // Contains type classification and method/member discovery helpers
    // that do not require ECS runtime state.
    internal static class WEFormulaeControllerHelper
    {
        internal static bool IsTypeIndexable(string assemblyName, string typeFullName)
        {
            var type = Type.GetType($"{typeFullName}, {assemblyName}");
            return type != null && (
                type.IsArray
                || (type.GetMethod("get_Item", new[] { typeof(int) }) != null
                    && type.GetInterfaces().Any(x => x.IsGenericType
                        && (x.GetGenericTypeDefinition() == typeof(IList<>)
                            || x.GetGenericTypeDefinition() == typeof(IIndexable<>)))));
        }

        internal static Dictionary<int, Dictionary<string, Dictionary<string, WEStaticMethodDesc[]>>> ListAvailableMethodsForType(string assemblyName, string typeFullName)
        {
            var type = Type.GetType($"{typeFullName}, {assemblyName}");
            return type == null ? null : FilterAvailableMethodsForFormulae(type)
                .Select(x => WEStaticMethodDesc.From(x))
                .OrderBy(x => x.source)
                .GroupBy(x => x.source)
                .ToDictionary(
                    srcGrouping => (int)srcGrouping.Key, srcGrouping => srcGrouping
                    .OrderBy(x => x.dllName)
                    .GroupBy(x => x.dllName)
                    .ToDictionary(
                        dllGrouping => dllGrouping.Key, dllGrouping => dllGrouping
                        .OrderBy(x => x.source == WEMemberSource.BuiltinFormulae ? x.weCategory : x.className)
                        .GroupBy(x => x.source == WEMemberSource.BuiltinFormulae ? x.weCategory : x.className)
                        .ToDictionary(classGrouping => classGrouping.Key, classGrouping => classGrouping.OrderBy(x => x.methodName).ToArray())
                    )
                );
        }

        internal static WETypeMemberDesc[] ListAvailableMembersForType(string assemblyName, string typeFullName)
        {
            var type = Type.GetType($"{typeFullName}, {assemblyName}");
            return type?.GetMembers(WEFormulaeHelper.MEMBER_FLAGS).Where(x =>
                (x is PropertyInfo pi && pi.GetMethod?.GetParameters().Length == 0)
                || x is FieldInfo
                || (x is MethodInfo mi
                    && mi.GetParameters() is ParameterInfo[] p
                    && (p.Length == 0 || (p.Length == 1 && p[0].ParameterType == typeof(Dictionary<string, string>) && !WEFormulaeHelper.IsByRefLikeSafe(p[0].ParameterType)))
                    && mi.ReturnType != typeof(void) && !mi.Name.StartsWith("get_"))
            ).Select(x => WETypeMemberDesc.FromMemberInfo(x)).ToArray();
        }
    }
}
