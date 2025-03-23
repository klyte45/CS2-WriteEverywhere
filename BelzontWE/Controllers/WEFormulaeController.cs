using Belzont.Interfaces;
using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEFormulaeController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "formulae.";
        private WEWorldPickerController m_weToolController;
        private Dictionary<int, Dictionary<string, Dictionary<string, WEComponentTypeDesc[]>>> m_cachedComponentsList;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listAvailableMethodsForType", ListAvailableMethodsForType);
            callBinder($"{PREFIX}formulaeToPathObjects", FormulaeToPathObjects);
            callBinder($"{PREFIX}listAvailableMembersForType", ListAvailableMembersForType);
            callBinder($"{PREFIX}listAvailableComponents", ListAvailableComponents);
            callBinder($"{PREFIX}listComponentsOnCurrentEntity", ListComponentsOnCurrentEntity);
            callBinder($"{PREFIX}isTypeIndexable", IsTypeIndexable);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        protected override void OnCreate()
        {
            m_weToolController = World.GetExistingSystemManaged<WEWorldPickerController>();
        }
        private bool IsTypeIndexable(string assemblyName, string typeFullName)
        {
            var type = Type.GetType($"{typeFullName}, {assemblyName}");
            return type != null && (type.IsArray || (type.GetMethod("get_Item", new[] { typeof(int) }) != null && type.GetInterfaces().Any(x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IList<>) || x.GetGenericTypeDefinition() == typeof(IIndexable<>)))));
        }
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
            var type = Type.GetType($"{typeFullName}, {assemblyName}");
            return type?.GetMembers(WEFormulaeHelper.MEMBER_FLAGS).Where(x =>
            (x is PropertyInfo pi &&  pi.GetMethod?.GetParameters().Length == 0) || x is FieldInfo || (x is MethodInfo mi && mi.GetParameters().Length == 0 && mi.ReturnType != typeof(void) && !mi.Name.StartsWith("get_"))
            ).Select(x => WETypeMemberDesc.FromMemberInfo(x)).ToArray();
        }

        private Dictionary<int, Dictionary<string, Dictionary<string, WEComponentTypeDesc[]>>> ListAvailableComponents() =>
         m_cachedComponentsList ??= TypeManager.AllTypes.Where(x => x.Type != null && (typeof(IComponentData).IsAssignableFrom(x.Type) || typeof(IBufferElementData).IsAssignableFrom(x.Type)))
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
        private WEComponentTypeDesc[] ListComponentsOnCurrentEntity(string formulaeStr)
        {
            var currentEntity = m_weToolController.CurrentEntity.Value;
            if (currentEntity == Entity.Null) return null;
            Entity targetEntity;
            if (formulaeStr.TrimToNull() is null)
            {
                targetEntity = currentEntity;
            }
            else
            {
                if (WEFormulaeHelper.SetFormulae(formulaeStr, out _, out _, out Func<EntityManager, Entity, Entity> resultFormulaeFn) != 0 || resultFormulaeFn is null) return null;
                targetEntity = resultFormulaeFn(EntityManager, m_weToolController.CurrentEntity.Value);
            }
            if (targetEntity == Entity.Null) return null;
            var array = EntityManager.GetComponentTypes(targetEntity);
            try
            {
                return array.ToArray().Select(x => WEComponentTypeDesc.From(x.GetManagedType())).ToArray();
            }
            finally
            {
                array.Dispose();
            }
        }


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
                    if (currentType.GetInterfaces().Any(x => x == typeof(IBufferElementData)))
                    {
                        currentType = typeof(DynamicBuffer<>).MakeGenericType(currentType);
                    }

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
                if (int.TryParse(field, out int val))
                {
                    if (currentType.IsArray)
                    {
                        currentType = currentType.GetElementType();
                        result.Add(WETypeMemberDesc.FromIndexing(val, currentType));
                    }
                    else if (currentType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IList<>) || x.GetGenericTypeDefinition() == typeof(IIndexable<>))) is Type t)
                    {
                        currentType = t.GetGenericArguments()[0];
                        result.Add(WETypeMemberDesc.FromIndexing(val, currentType));
                    }
                    else
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"INVALID IDX for non-array type: {currentType} [{field}]");
                        return false;
                    }
                }
                else if (currentType.GetField(field, ReflectionUtils.allFlags) is FieldInfo targetField)
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