using Belzont.Interfaces;
using Belzont.Utils;
using Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEFormulaeController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "formulae.";

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listAvailableMethodsForType", ListAvailableMethodsForType);
            callBinder($"{PREFIX}formulaeToPathObjects", FormulaeToPathObjects);
            callBinder($"{PREFIX}listAvailableMembersForType", ListAvailableMembersForType);
            callBinder($"{PREFIX}listAvailableComponents", ListAvailableComponents);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }



        private Dictionary<int, Dictionary<string, Dictionary<string, WEStaticMethodDesc[]>>> ListAvailableMethodsForType(string assemblyName, string typeFullName)
        {
            var type = Type.GetType($"{typeFullName}, {assemblyName}");
            return type == null ? null : WEFormulaeHelper.FilterAvailableMethodsForFormulae(type)
                .Select(x => WEStaticMethodDesc.From(x))
                .OrderBy(x => x.source)
                .GroupBy(x => x.source)
                .ToDictionary(
                    srcGrouping => (int)srcGrouping.Key, srcGrouping => srcGrouping
                    .OrderBy(x => x.dllName)
                    .GroupBy(y => y.dllName)
                    .ToDictionary(
                        dllGrouping => dllGrouping.Key, dllGrouping => dllGrouping
                        .OrderBy(x => x.className)
                        .GroupBy(z => z.className)
                        .ToDictionary(classGrouping => classGrouping.Key, classGrouping => classGrouping.OrderBy(x => x.methodName).ToArray()
                        )
                    )
                );
        }

        private WETypeMemberDesc[] ListAvailableMembersForType(string assemblyName, string typeFullName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == assemblyName).SelectMany(assembly => assembly.GetTypes()).Where(t => t.FullName == typeFullName).FirstOrDefault();
            return type == null ? null : type.GetMembers(WEFormulaeHelper.MEMBER_FLAGS).Where(x =>
            (x is PropertyInfo pi && pi.GetMethod != null) || x is FieldInfo || (x is MethodInfo mi && mi.GetParameters().Length == 0 && mi.ReturnType != typeof(void) && !mi.Name.StartsWith("get_"))
            ).Select(x => WETypeMemberDesc.FromMemberInfo(x)).ToArray();
        }

        private Dictionary<int, Dictionary<string, Dictionary<string, WEComponentTypeDesc[]>>> ListAvailableComponents() =>
            TypeManager.AllTypes.Where(x => x.Type != null)
                .Select(x => WEComponentTypeDesc.From(x.Type))
                .OrderBy(x => x.source)
                .GroupBy(x => x.source)
                .ToDictionary(
                    srcGrouping => (int)srcGrouping.Key, srcGrouping => srcGrouping
                    .OrderBy(x => x.dllName)
                    .GroupBy(y => y.dllName)
                    .ToDictionary(
                        dllGrouping => dllGrouping.Key, dllGrouping => dllGrouping
                        .OrderBy(x => x.className)
                        .GroupBy(z => z.className.Contains(".") ? string.Join(".", z.className.Split(".")[..^1]) : "<ROOT>")
                        .ToDictionary(classGrouping => classGrouping.Key, classGrouping => classGrouping.OrderBy(x => x.className).ToArray())
                    )
                );

        private List<object> FormulaeToPathObjects(string formulae)
        {
            var result = new List<object>();
            var pathParts = WEFormulaeHelper.GetPathParts(formulae);
            var currentType = typeof(Entity);
            foreach (var part in pathParts)
            {
                if (part.StartsWith("&"))
                {
                    var kv = part[1..].Split(";");
                    if (kv.Length != 2) break;
                    var parts = kv[1].Split(".");
                    var methodName = parts[0];
                    var fieldPath = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
                    var methodQuery = WEFormulaeHelper.FilterAvailableMethodsForFormulae(currentType, kv[0], methodName).ToList();
                    if (methodQuery.Count != 1) break;
                    var resultMethod = methodQuery[0];
                    result.Add(WEStaticMethodDesc.From(resultMethod));
                    currentType = resultMethod.ReturnType;
                    if (!IterateFieldPath(result, ref currentType, fieldPath)) break;
                }
                else
                {
                    if (WEFormulaeHelper.ParseComponentEntryType(ref currentType, part, out _, out var fieldPath) != 0) break;
                    result.Add(WEComponentTypeDesc.From(currentType));
                    if (!IterateFieldPath(result, ref currentType, fieldPath)) break;
                }
            }
            return result;
        }

        private static bool IterateFieldPath(List<object> result, ref Type currentType, string[] fieldPath)
        {
            for (int j = 0; j < fieldPath.Length; j++)
            {
                string field = fieldPath[j];
                if (currentType.GetField(field, ReflectionUtils.allFlags) is FieldInfo targetField)
                {
                    currentType = targetField.FieldType;
                    result.Add(WETypeMemberDesc.FromMemberInfo(targetField));
                    continue;
                }
                else if (currentType.GetProperty(field, ReflectionUtils.allFlags & ~BindingFlags.Static & ~BindingFlags.NonPublic & ~BindingFlags.DeclaredOnly) is PropertyInfo targetProperty && targetProperty.GetMethod != null)
                {
                    currentType = targetProperty.GetMethod.ReturnType;
                    result.Add(WETypeMemberDesc.FromMemberInfo(targetProperty));
                    continue;
                }
                else if (currentType.GetMethod(field, ReflectionUtils.allFlags & ~BindingFlags.Static & ~BindingFlags.NonPublic & ~BindingFlags.DeclaredOnly, null, new Type[0], null) is MethodInfo targetMethod && targetMethod.ReturnType != typeof(void))
                {
                    currentType = targetMethod.ReturnType;
                    result.Add(WETypeMemberDesc.FromMemberInfo(targetMethod));
                    continue;
                }
                else
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"INVALID MEMBER: {currentType}.{field}");
                    return false;
                }
            }

            return true;
        }

        protected override void OnUpdate()
        {
        }
    }
}