﻿

using Belzont.Interfaces;
using Belzont.Utils;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public static class WEFormulaeHelper
    {
        internal static readonly BindingFlags MEMBER_FLAGS = ReflectionUtils.allFlags & ~BindingFlags.Static & ~BindingFlags.NonPublic & ~BindingFlags.DeclaredOnly;

        private static MethodInfo r_GetComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "GetComponentData" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private static MethodInfo r_HasComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "HasComponent" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));

        private static readonly Dictionary<FixedString512Bytes, Func<EntityManager, Entity, string>> cachedFns = new();

        public static Func<EntityManager, Entity, string> GetCached(FixedString512Bytes formulae) => cachedFns.TryGetValue(formulae, out var cached) ? cached : null;
        public static bool HasCached(FixedString512Bytes formulae) => cachedFns.ContainsKey(formulae);
        public static byte SetFormulae(FixedString512Bytes newFormulae512, out string[] errorFmtArgs, out FixedString512Bytes resultFormulaeStr, out Func<EntityManager, Entity, string> resultFormulaeFn)
        {
            resultFormulaeStr = default;
            resultFormulaeFn = default;
            if (newFormulae512.Trim() == default)
            {
                errorFmtArgs = null;
                return 0;
            }
            if (cachedFns.TryGetValue(newFormulae512, out var handle) && handle != null)
            {
                resultFormulaeStr = (newFormulae512);
                resultFormulaeFn = (handle.Target as Func<EntityManager, Entity, string>);
                errorFmtArgs = null;
                return 0;
            }
            cachedFns[newFormulae512] = null;
            var newFormulae = newFormulae512.ToString();
            var path = GetPathParts(newFormulae);
            DynamicMethodDefinition dynamicMethodDefinition = new(
                $"__WE_CS2_{nameof(WETextData)}_formulae_{new Regex("[^A-Za-z0-9_]").Replace(newFormulae, "_")}",
                typeof(string),
                new Type[] { typeof(EntityManager), typeof(Entity) }
                );
            ILGenerator iLGenerator = dynamicMethodDefinition.GetILGenerator();
            try
            {
                errorFmtArgs = null;
                var entityLocalField = iLGenerator.DeclareLocal(typeof(Entity));
                var currentComponentType = typeof(Entity);
                LocalBuilder localVarEntity = null;
                var skipValueTypeVar = false;
                for (int i = 0; i < path.Length; i++)
                {
                    var codePart = path[i];

                    if (codePart.StartsWith("&"))
                    {
                        var parts = codePart[1..].Split(";");
                        var className = parts[0];
                        var pathNav = parts[1].Split(".");
                        var method = pathNav[0];
                        var navFields = pathNav.Length > 1 ? pathNav[1..] : new string[0];
                        var candidateMethods = FilterAvailableMethodsForFormulae(currentComponentType, className, method);
                        var resultQuery = candidateMethods.ToList();
                        if (resultQuery.Count == 0)
                        {
                            return 8; // Class or method not found.
                        }
                        if (resultQuery.Count() > 1)
                        {
                            return 9; // Class or method matches more than one result. Please be more specific.
                        }
                        var methodInfo = resultQuery[0];
                        if (i == 0)
                        {
                            iLGenerator.Emit(OpCodes.Ldarg_1);
                        }
                        else if (skipValueTypeVar)
                        {
                            var local0 = iLGenerator.DeclareLocal(currentComponentType);
                            iLGenerator.Emit(OpCodes.Stloc, local0);
                            iLGenerator.Emit(OpCodes.Ldloc_S, local0);
                        }
                        iLGenerator.EmitCall(OpCodes.Call, methodInfo, null);
                        currentComponentType = methodInfo.ReturnType;
                        skipValueTypeVar = false;
                        var navPathRes = NavigateThroughPath(ref currentComponentType, iLGenerator, ref errorFmtArgs, navFields, ref skipValueTypeVar);
                        if (navPathRes != 0) return navPathRes;
                    }
                    else
                    {
                        skipValueTypeVar = false;
                        var result = ProcessEntityPath(ref currentComponentType, iLGenerator, codePart, i, ref localVarEntity, ref errorFmtArgs, ref skipValueTypeVar);
                        if (result != 0) return result;
                    }
                }
                if (currentComponentType.IsEnum)
                {
                    var toStringMethod = typeof(Enum).GetMethod("GetName", ReflectionUtils.allFlags & ~BindingFlags.DeclaredOnly);
                    var local0 = iLGenerator.DeclareLocal(currentComponentType);
                    iLGenerator.Emit(OpCodes.Stloc, local0);
                    iLGenerator.Emit(OpCodes.Ldloc_S, local0);
                    iLGenerator.Emit(OpCodes.Box, currentComponentType);
                    iLGenerator.EmitCall(OpCodes.Call, typeof(object).GetMethod("GetType"), null);
                    iLGenerator.Emit(OpCodes.Ldloc_S, local0);
                    iLGenerator.Emit(OpCodes.Box, currentComponentType);
                    iLGenerator.EmitCall(OpCodes.Call, toStringMethod, null);
                }
                else if (currentComponentType != typeof(string))
                {
                    var toStringMethod = currentComponentType.GetMethod("ToString", ReflectionUtils.allFlags & ~BindingFlags.DeclaredOnly, null, new Type[0], null);
                    if (currentComponentType.IsValueType)
                    {
                        if (!skipValueTypeVar)
                        {
                            var local0 = iLGenerator.DeclareLocal(currentComponentType);
                            iLGenerator.Emit(OpCodes.Stloc, local0);
                            iLGenerator.Emit(OpCodes.Ldloca, local0);
                        }
                        iLGenerator.Emit(OpCodes.Constrained, currentComponentType);
                    }
                    iLGenerator.EmitCall(OpCodes.Callvirt, toStringMethod, null);
                }
                iLGenerator.Emit(OpCodes.Ret);
                resultFormulaeStr = newFormulae;
                var generatedMethod = dynamicMethodDefinition.Generate();
                cachedFns[newFormulae512] = resultFormulaeFn = (x, e) => generatedMethod.Invoke(null, new object[] { x, e }) as string;
                if (BasicIMod.DebugMode) LogUtils.DoLog("FN => (" + string.Join(", ", dynamicMethodDefinition.Definition.Parameters.Select(x => $"{(x.IsIn ? "in " : x.IsOut ? "out " : "")}{x.ParameterType} {x.Name}")) + ")");
                if (BasicIMod.DebugMode) LogUtils.DoLog("FN => \n" + string.Join("\n", dynamicMethodDefinition.Definition.Body.Instructions.Select(x => x.ToString())));

                return 0;
            }
            finally
            {
                dynamicMethodDefinition.Dispose();
            }
        }

        public static string[] GetPathParts(string newFormulae) => newFormulae.Split("/");

        private static List<MethodInfo> CACHED_AVAILABLE_STATIC_METHODS;

        public static IEnumerable<MethodInfo> FilterAvailableMethodsForFormulae(Type currentComponentType, string className = null, string method = null)
        {
            CACHED_AVAILABLE_STATIC_METHODS ??= AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(assembly => assembly.GetTypes())
                                                 .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
                                                 .Where(m => m.GetParameters() is ParameterInfo[] p
                                                    && p.Length == 1
                                                    && !p[0].ParameterType.IsByRefLike
                                                    && m.ReturnType != typeof(void)
                                                 ).ToList();

            return CACHED_AVAILABLE_STATIC_METHODS.Where(m => (className is null || m.DeclaringType.FullName == className || m.DeclaringType.FullName.EndsWith($".{className}"))
                                                    && (method is null || m.Name == method)
                                                    && m.GetParameters()[0].ParameterType == currentComponentType
                                                  );
        }

        private static byte ProcessEntityPath(ref Type currentComponentType, ILGenerator iLGenerator, string path, int blockId, ref LocalBuilder localVarEntity, ref string[] errorFmtArgs, ref bool skipValueTypeVar)
        {
            var parseResult = ParseComponentEntryType(ref currentComponentType, path, out errorFmtArgs, out string[] fieldPath);
            if (parseResult != 0) return parseResult;

            void loadParameters(ref LocalBuilder localVarEntity, bool doStloc)
            {
                if (blockId == 0)
                {
                    iLGenerator.Emit(OpCodes.Ldarga_S, 0);
                    iLGenerator.Emit(OpCodes.Ldarg_1);
                }
                else
                {
                    localVarEntity ??= iLGenerator.DeclareLocal(typeof(Entity));
                    if (doStloc) iLGenerator.Emit(OpCodes.Stloc, localVarEntity);
                    iLGenerator.Emit(OpCodes.Ldarga_S, 0);
                    iLGenerator.Emit(OpCodes.Ldloc, localVarEntity);
                }
            }
            loadParameters(ref localVarEntity, true);
            iLGenerator.EmitCall(OpCodes.Call, r_HasComponent.MakeGenericMethod(currentComponentType), null);
            var lbl_ok = iLGenerator.DefineLabel();
            iLGenerator.Emit(OpCodes.Brtrue_S, lbl_ok);
            iLGenerator.Emit(OpCodes.Ldstr, $"<NO COMPONENT: {currentComponentType} @ Block #{blockId}>");
            iLGenerator.Emit(OpCodes.Ret);
            iLGenerator.MarkLabel(lbl_ok);
            loadParameters(ref localVarEntity, false);
            iLGenerator.EmitCall(OpCodes.Call, r_GetComponent.MakeGenericMethod(currentComponentType), null);
            skipValueTypeVar = false;
            return NavigateThroughPath(ref currentComponentType, iLGenerator, ref errorFmtArgs, fieldPath, ref skipValueTypeVar);
        }

        private static byte NavigateThroughPath(ref Type currentComponentType, ILGenerator iLGenerator, ref string[] errorFmtArgs, string[] fieldPath, ref bool skipValueTypeVar)
        {
            foreach (string field in fieldPath)
            {
                if (currentComponentType.IsValueType && !skipValueTypeVar)
                {
                    var local0 = iLGenerator.DeclareLocal(currentComponentType);
                    iLGenerator.Emit(OpCodes.Stloc, local0);
                    iLGenerator.Emit(OpCodes.Ldloca, local0);
                }
                skipValueTypeVar = false;
                if (currentComponentType.GetField(field, MEMBER_FLAGS) is FieldInfo targetField)
                {
                    iLGenerator.Emit(OpCodes.Ldfld, targetField);
                    currentComponentType = targetField.FieldType;
                    continue;
                }
                else if (currentComponentType.GetProperty(field, MEMBER_FLAGS) is PropertyInfo targetProperty && targetProperty.GetMethod != null)
                {
                    if (currentComponentType.IsValueType && (targetProperty.GetMethod.IsVirtual || currentComponentType.IsGenericType))
                    {
                        iLGenerator.Emit(OpCodes.Constrained, currentComponentType);
                    }
                    iLGenerator.EmitCall(targetProperty.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetProperty.GetMethod, null);
                    currentComponentType = targetProperty.GetMethod.ReturnType;
                    continue;
                }
                else if (currentComponentType.GetMethod(field, MEMBER_FLAGS, null, new Type[0], null) is MethodInfo targetMethod && targetMethod.ReturnType != typeof(void))
                {
                    if (currentComponentType.IsValueType && (targetMethod.IsVirtual || currentComponentType.IsGenericType))
                    {
                        iLGenerator.Emit(OpCodes.Constrained, currentComponentType);
                    }
                    iLGenerator.EmitCall(targetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethod, null);
                    currentComponentType = targetMethod.ReturnType;
                    continue;
                }
                else
                {
                    errorFmtArgs = new[] { field, currentComponentType.FullName, string.Join("\n", currentComponentType.GetMembers(ReflectionUtils.allFlags).Where(x => x is not MethodBase m || m.GetParameters().Length == 0).Select(x => x.Name)) };
                    return 3; // Member {0} not found at component type {1}; Available Members: {2}
                }
            }
            return 0;
        }

        internal static byte ParseComponentEntryType(ref Type currentComponentType, string path, out string[] errorFmtArgs, out string[] fieldPath)
        {
            errorFmtArgs = null;
            if (currentComponentType != typeof(Entity))
            {
                fieldPath = null;
                return 4; // Components only can be got from an Entity
            }
            var itemSplitted = path.Split(";");
            if (itemSplitted.Length != 2)
            {
                fieldPath = null;
                return 6; // Each component getter block must be a pair of component name and field navigation, separated by a semicolon
            }
            var entityTypeName = itemSplitted[0];
            fieldPath = itemSplitted[1].Split(".").Where(x => x.TrimToNull() != null).ToArray();
            var itemComponentType = TypeManager.AllTypes.Where(x => x.Type?.FullName?.EndsWith(entityTypeName) ?? false).ToList();
            if (itemComponentType.Count == 0)
            {
                errorFmtArgs = new[] { entityTypeName };
                return 1; // Component type not found for {0}              
            }
            if (itemComponentType.Count > 1)
            {
                errorFmtArgs = new[] { entityTypeName };
                return 5; // Multiple components found for name {0}
            }

            currentComponentType = itemComponentType[0].Type;
            return 0;
        }
    }
}