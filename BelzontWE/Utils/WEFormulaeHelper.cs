

using Belzont.Interfaces;
using Belzont.Utils;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public static class WEFormulaeHelper
    {
        public delegate T FormulaeFn<T>(EntityManager em, Entity e, Dictionary<string, string> vars);

        private interface IBaseCache
        {
            byte ResultCode { get; }
            string[] ErrorArgs { get; }
            object FnObj { get; }
            void SetData(byte resultCode, object fn, string[] errorArgs = null);
        }
        private class BaseCache<T> : IBaseCache
        {

            public byte ResultCode { get; private set; }

            public string[] ErrorArgs { get; private set; }

            public object FnObj => Fn;

            public FormulaeFn<T> Fn { get; private set; }

            private void SetData_Eff(byte resultCode, FormulaeFn<T> fn, string[] errorArgs = null)
            {
                ResultCode = resultCode;
                Fn = fn;
                ErrorArgs = errorArgs;
            }

            public void SetData(byte resultCode, object fn, string[] errorArgs = null)
            {
                SetData_Eff(resultCode, (FormulaeFn<T>)fn, errorArgs);
            }
        }

        internal static readonly BindingFlags MEMBER_FLAGS = ReflectionUtils.allFlags & ~BindingFlags.Static & ~BindingFlags.NonPublic & ~BindingFlags.DeclaredOnly;

        private static readonly MethodInfo r_GetComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "GetComponentData" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private static readonly MethodInfo r_HasComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "HasComponent" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private static readonly MethodInfo r_HasBuffer = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "HasBuffer" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private static readonly MethodInfo r_GetBuffer = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "GetBuffer" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 2 && pi[0].ParameterType == typeof(Entity) && pi[1].ParameterType == typeof(bool));

        private static readonly Dictionary<string, BaseCache<string>> cachedFnsString = new();
        private static readonly Dictionary<string, BaseCache<float>> cachedFnsFloat = new();
        private static readonly Dictionary<string, BaseCache<Color>> cachedFnsColor = new();
        private static readonly Dictionary<string, BaseCache<IList<Entity>>> cachedFnsEntityArray = new();

        public static FormulaeFn<string> GetCachedStringFn(string formulae) => cachedFnsString.TryGetValue(formulae, out var cached) ? cached.Fn : null;
        public static FormulaeFn<float> GetCachedFloatFn(string formulae) => cachedFnsFloat.TryGetValue(formulae, out var cached) ? cached.Fn : null;
        public static FormulaeFn<Color> GetCachedColorFn(string formulae) => cachedFnsColor.TryGetValue(formulae, out var cached) ? cached.Fn : null;
        public static FormulaeFn<IList<Entity>> GetCachedEntityArrayFn(string formulae) => cachedFnsEntityArray.TryGetValue(formulae, out var cached) ? cached.Fn : null;

        public static byte SetFormulae<T>(string newFormulae512, out string[] errorFmtArgs, out string resultFormulaeStr, out FormulaeFn<T> resultFormulaeFn)
        {
            IDictionary refDic = typeof(T) == typeof(string) ? cachedFnsString
                : typeof(T) == typeof(float) ? cachedFnsFloat
                : typeof(T) == typeof(Color) ? cachedFnsColor
                : typeof(T) == typeof(IList<Entity>) ? (IDictionary)cachedFnsEntityArray
                : typeof(T) == typeof(Entity) ? null
                : throw new InvalidCastException("Formulae only support types float, string, UnityEngine.Color or list of Entities");
            resultFormulaeStr = default;
            resultFormulaeFn = default;
            if (newFormulae512.Trim() == default)
            {
                errorFmtArgs = null;
                return 0;
            }
            if (refDic != null && refDic.Contains(newFormulae512))
            {
                var handle = (IBaseCache)refDic[newFormulae512];
                if (handle.FnObj != null || handle.ResultCode != 0)
                {
                    resultFormulaeStr = newFormulae512;
                    resultFormulaeFn = handle.FnObj as FormulaeFn<T>;
                    errorFmtArgs = handle.ErrorArgs;
                    return handle.ResultCode;
                }
            }
            var newFormulae = newFormulae512;
            var path = GetPathParts(newFormulae);
            DynamicMethodDefinition dynamicMethodDefinition = new(
                $"__WE_CS2_{typeof(T).Name}_formulae_{new Regex("[^A-Za-z0-9_]").Replace(newFormulae, "_")}",
                typeof(T),
                new Type[] { typeof(EntityManager), typeof(Entity), typeof(Dictionary<string, string>) }
                );
            ILGenerator iLGenerator = dynamicMethodDefinition.GetILGenerator();
            byte result = 0;
            errorFmtArgs = null;
            try
            {
                var entityLocalField = iLGenerator.DeclareLocal(typeof(Entity));
                var currentComponentType = typeof(Entity);
                LocalBuilder localVarEntity = null;
                var skipValueTypeVar = false;
                for (int i = 0; i < path.Length; i++)
                {
                    var codePart = path[i];

                    switch (codePart[0])
                    {
                        case '&':
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
                                    return result = 8; // Class or method not found.
                                }
                                if (resultQuery.Count() > 1)
                                {
                                    errorFmtArgs = new[] { currentComponentType.ToString(), className, method };
                                    return result = 9; // Class or method matches more than one result. Please be more specific.
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
                                if (methodInfo.GetParameters().Length == 2)
                                {
                                    iLGenerator.Emit(OpCodes.Ldarg_2);
                                }
                                iLGenerator.EmitCall(OpCodes.Call, methodInfo, null);
                                currentComponentType = methodInfo.ReturnType;
                                skipValueTypeVar = false;
                                var navPathRes = NavigateThroughPath(ref currentComponentType, iLGenerator, ref errorFmtArgs, navFields, ref skipValueTypeVar);
                                if (navPathRes != 0) return result = navPathRes;
                            }
                            break;
                        default:
                            {
                                skipValueTypeVar = false;
                                var processResult = ProcessEntityPath<T>(ref currentComponentType, iLGenerator, codePart, i, ref localVarEntity, ref errorFmtArgs, ref skipValueTypeVar);
                                if (processResult != 0) return result = processResult;
                            }
                            break;
                    }

                }
                if (currentComponentType != typeof(T))
                {
                    if (typeof(T) == typeof(Entity)) return 252;
#if USE_LOCALE_FORMATTING_NUMERIC_STRING_CAST
                    if (typeof(T) == typeof(string) && (currentComponentType.IsIntegerType() || currentComponentType.IsDecimalType()))
                    {                        
                        var toStringMethod = currentComponentType.GetMethod("ToString", ReflectionUtils.allFlags & ~BindingFlags.DeclaredOnly, null, new Type[] { typeof(IFormatProvider) }, null);                       
                        iLGenerator.Emit(OpCodes.Call, typeof(WEModData).GetProperty("InstanceWE").GetMethod);
                        iLGenerator.Emit(OpCodes.Call, typeof(WEModData).GetProperty("FormatCulture", RedirectorUtils.allFlags).GetMethod);
                        iLGenerator.Emit(OpCodes.Call, toStringMethod);
                    }
                    else 
#endif
                    if (CanCast(currentComponentType, typeof(T)))
                    {
                        var local0 = iLGenerator.DeclareLocal(currentComponentType);
                        iLGenerator.Emit(OpCodes.Stloc, local0);
                        iLGenerator.Emit(OpCodes.Ldloc_S, local0);
                        if (currentComponentType.IsValueType) iLGenerator.Emit(OpCodes.Box, currentComponentType);
                        iLGenerator.Emit(OpCodes.Ldtoken, typeof(T));
                        iLGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), RedirectorUtils.allFlags));
                        iLGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), RedirectorUtils.allFlags, null, new[] { typeof(object), typeof(Type) }, null));
                        iLGenerator.Emit(OpCodes.Unbox_Any, typeof(T));
                    }
                    else if (typeof(T) != typeof(string))
                    {
                        iLGenerator.Emit(OpCodes.Pop);
                        if (typeof(T) == typeof(float))
                        {
                            iLGenerator.Emit(OpCodes.Ldc_R4, float.NaN);
                        }
                        else if (typeof(T) == typeof(Color))
                        {
                            iLGenerator.Emit(OpCodes.Call, typeof(Color).GetProperty("magenta", RedirectorUtils.allFlags).GetMethod);
                        }
                        result = 255;
                    }
                    else if (currentComponentType.IsEnum)
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
                    else
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
                }
                iLGenerator.Emit(OpCodes.Ret);
                resultFormulaeStr = newFormulae;
                var generatedMethod = dynamicMethodDefinition.Generate();
                if (BasicIMod.DebugMode) LogUtils.DoLog("FN => (" + string.Join(", ", dynamicMethodDefinition.Definition.Parameters.Select(x => $"{(x.IsIn ? "in " : x.IsOut ? "out " : "")}{x.ParameterType} {x.Name}")) + ")");
                if (BasicIMod.DebugMode) LogUtils.DoLog("FN => \n" + string.Join("\n", dynamicMethodDefinition.Definition.Body.Instructions.Select(x => x.ToString())));
                resultFormulaeFn = (x, e, d) => (T)generatedMethod.Invoke(null, new object[] { x, e, d });
                if (refDic != null)
                {
                    var cacheType = refDic.GetType().GetGenericArguments()[1];
                    var cache = cacheType.GetConstructors()[0].Invoke(new object[0]) as IBaseCache;
                    cache.SetData(result, resultFormulaeFn);
                    refDic[newFormulae512] = refDic[resultFormulaeStr] = cache;
                }

                return result;
            }
            finally
            {
                dynamicMethodDefinition.Dispose();
                if (refDic != null && result != 0 && result != 255)
                {
                    var cacheType = refDic.GetType().GetGenericArguments()[1];
                    var cache = cacheType.GetConstructors()[0].Invoke(new object[0]) as IBaseCache;
                    cache.SetData(result, null, errorFmtArgs);
                    refDic[newFormulae512] = cache;
                }
            }
        }

        private static bool CanCast(Type source, Type target)
        {
            try
            {
                Convert.ChangeType(source.IsValueType ? Activator.CreateInstance(source) : source.GetConstructor(new Type[0]).Invoke(new object[0]), target);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string[] GetPathParts(string newFormulae) => newFormulae?.Split("/");

        private static List<MethodInfo> CACHED_AVAILABLE_STATIC_METHODS;

        public static IEnumerable<MethodInfo> FilterAvailableMethodsForFormulae(Type currentComponentType, string className = null, string method = null)
        {
            CACHED_AVAILABLE_STATIC_METHODS ??= AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(assembly => assembly.GetTypes())
                                                 .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
                                                 .Where(m => m.GetParameters() is ParameterInfo[] p
                                                    && (p.Length == 1 || (p.Length == 2 && p[1].ParameterType == typeof(Dictionary<string, string>)))
                                                    && !p[0].ParameterType.IsByRefLike
                                                    && m.ReturnType != typeof(void)
                                                 ).ToList();

            return CACHED_AVAILABLE_STATIC_METHODS.Where(m => CheckMethodIsCompatible(currentComponentType, className, method, m));
        }

        private static bool CheckMethodIsCompatible(Type currentComponentType, string className, string method, MethodInfo m)
        {
            return (className is null || m.DeclaringType.FullName == className || m.DeclaringType.FullName.EndsWith($".{className}"))
                                                                                && (method is null || m.Name == method)
                                                                                && currentComponentType == m.GetParameters()[0].ParameterType;
        }

        private static byte ProcessEntityPath<T>(ref Type currentComponentType, ILGenerator iLGenerator, string path, int blockId, ref LocalBuilder localVarEntity, ref string[] errorFmtArgs, ref bool skipValueTypeVar)
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

            var isBufferElement = currentComponentType.GetInterfaces().Any(x => x == typeof(IBufferElementData));

            var componentCheckFn = isBufferElement ? r_HasBuffer : r_HasComponent;
            var componentGetFn = isBufferElement ? r_GetBuffer : r_GetComponent;

            loadParameters(ref localVarEntity, true);
            iLGenerator.EmitCall(OpCodes.Call, componentCheckFn.MakeGenericMethod(currentComponentType), null);
            var lbl_ok = iLGenerator.DefineLabel();
            iLGenerator.Emit(OpCodes.Brtrue_S, lbl_ok);
            if (typeof(T) == typeof(string))
            {
                iLGenerator.Emit(OpCodes.Ldstr, $"<NO COMPONENT: {currentComponentType} @ Block #{blockId}>");
            }
            else
            {
                //var local0 = iLGenerator.DeclareLocal(currentComponentType);
                //iLGenerator.Emit(OpCodes.Stloc, local0);
                //var lbl_ret = iLGenerator.DefineLabel();
                //iLGenerator.Emit(OpCodes.Call, typeof(BasicIMod).GetProperty(nameof(BasicIMod.VerboseMode)).GetMethod);
                //iLGenerator.Emit(OpCodes.Brfalse_S, lbl_ret);
                //iLGenerator.Emit(OpCodes.Ldloc_S, local0);
                //iLGenerator.Emit(OpCodes.Call, typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(object)));
                //iLGenerator.Emit(OpCodes.Call, typeof(LogUtils).GetMethod(nameof(LogUtils.DoVerboseLog)));
                //iLGenerator.MarkLabel(lbl_ret);
                if (typeof(T) == typeof(float))
                {
                    iLGenerator.Emit(OpCodes.Ldc_R4, float.NaN);
                }
                else if (typeof(T) == typeof(Color))
                {
                    iLGenerator.Emit(OpCodes.Call, typeof(Color).GetProperty("magenta", RedirectorUtils.allFlags).GetMethod);
                }
                else if (typeof(T) == typeof(Entity))
                {
                    iLGenerator.Emit(OpCodes.Call, typeof(Entity).GetProperty(nameof(Entity.Null), RedirectorUtils.allFlags).GetMethod);
                }
            }
            iLGenerator.Emit(OpCodes.Ret);
            iLGenerator.MarkLabel(lbl_ok);
            loadParameters(ref localVarEntity, false);
            if (isBufferElement)
            {
                iLGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            iLGenerator.EmitCall(OpCodes.Call, componentGetFn.MakeGenericMethod(currentComponentType), null);
            if (isBufferElement)
            {
                currentComponentType = typeof(DynamicBuffer<>).MakeGenericType(currentComponentType);
            }
            skipValueTypeVar = false;
            return NavigateThroughPath(ref currentComponentType, iLGenerator, ref errorFmtArgs, fieldPath, ref skipValueTypeVar);
        }

        public static bool IsIntegerType(this Type t) => Type.GetTypeCode(t) switch
        {
            TypeCode.Byte or TypeCode.SByte
            or TypeCode.UInt16 or TypeCode.UInt32
            or TypeCode.UInt64 or TypeCode.Int16
            or TypeCode.Int32 or TypeCode.Int64
            => true,
            _ => false,
        };
        public static bool IsDecimalType(this Type t) => Type.GetTypeCode(t) switch
        {
            TypeCode.Decimal or TypeCode.Double
            or TypeCode.Single => true,
            _ => false,
        };
        private static OpCode CheckAritmeticOp(string opText) => opText[0] switch
        {
            '+' => OpCodes.Add,
            '-' => OpCodes.Sub,
            '*' => OpCodes.Mul,
            '÷' => OpCodes.Div,
            _ => OpCodes.Nop,
        };

        private static bool SetupMathOperands(ref Type currentType, string targetValue, OpCode operation, ILGenerator generator)
        {
            var currentOperand = Type.GetTypeCode(currentType);
            if (targetValue.EndsWith("f"))
            {
                targetValue = targetValue[..^1];
                if (currentOperand != TypeCode.Single)
                {
                    generator.Emit(OpCodes.Conv_R4);
                    currentOperand = TypeCode.Single;
                }
            }
            else if (targetValue.EndsWith("d"))
            {
                targetValue = targetValue[..^1];
                if (currentOperand != TypeCode.Double)
                {
                    generator.Emit(OpCodes.Conv_R8);
                    currentOperand = TypeCode.Double;
                }
            }
            else if (targetValue.Contains(".") && currentType.IsIntegerType())
            {
                generator.Emit(OpCodes.Conv_R4);
                currentOperand = TypeCode.Single;
            }
            switch (currentOperand)
            {
                case TypeCode.Byte:
                    {
                        if (!byte.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I4_S, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(int);
                        return true;
                    }
                case TypeCode.SByte:
                    {
                        if (!sbyte.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I4_S, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(int);
                        return true;
                    }
                case TypeCode.UInt16:
                    {
                        if (!ushort.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I4_S, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(int);
                        return true;
                    }
                case TypeCode.UInt32:
                    {
                        if (!uint.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I4, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(int);
                        return true;
                    }
                case TypeCode.UInt64:
                    {
                        if (!ulong.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I8, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(long);
                        return true;
                    }
                case TypeCode.Int16:
                    {
                        if (!short.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I4_S, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(int);
                        return true;
                    }
                case TypeCode.Int32:
                    {
                        if (!int.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I4, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(int);
                        return true;
                    }
                case TypeCode.Int64:
                    {
                        if (!long.TryParse(targetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_I8, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(long);
                        return true;
                    }
                case TypeCode.Double:
                case TypeCode.Decimal:
                    {
                        if (!double.TryParse(targetValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_R8, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(double);
                        return true;
                    }
                case TypeCode.Single:
                    {
                        if (!float.TryParse(targetValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueConv)) return false;
                        generator.Emit(OpCodes.Ldc_R4, valueConv);
                        generator.Emit(operation);
                        currentType = typeof(float);
                        return true;
                    }
                default:
                    return false;
            }
        }

        private static byte NavigateThroughPath(ref Type currentComponentType, ILGenerator iLGenerator, ref string[] errorFmtArgs, string[] fieldPath, ref bool skipValueTypeVar)
        {
            foreach (string field in fieldPath)
            {
                var op = CheckAritmeticOp(field);
                if (op != OpCodes.Nop)
                {
                    var value = field[1..].Replace(',', '.').ToLower();
                    var oldCompType = currentComponentType;
                    if (!SetupMathOperands(ref currentComponentType, value, op, iLGenerator))
                    {
                        errorFmtArgs = new[] { oldCompType.FullName, field[0..1], field[1..] };
                        return 11; // Invalid arithmetic operation: {0} {1} {2} (comma is decimal separator)
                    }
                    continue;
                }
                if (currentComponentType.IsValueType && !skipValueTypeVar)
                {
                    var local0 = iLGenerator.DeclareLocal(currentComponentType);
                    iLGenerator.Emit(OpCodes.Stloc, local0);
                    iLGenerator.Emit(OpCodes.Ldloca, local0);
                }
                skipValueTypeVar = false;
                if (int.TryParse(field, out int idx))
                {
                    if (currentComponentType.IsArray)
                    {
                        iLGenerator.Emit(OpCodes.Ldc_I4, idx);
                        iLGenerator.Emit(OpCodes.Ldelem_I4);
                        currentComponentType = currentComponentType.GetElementType();
                    }
                    else if (currentComponentType.GetMethod("get_Item", new[] { typeof(int) }) is MethodInfo getItem && currentComponentType.GetInterfaces().FirstOrDefault(x =>
                       x.IsGenericType
                       && (x.GetGenericTypeDefinition() == typeof(IIndexable<>) || x.GetGenericTypeDefinition() == typeof(IList<>))
                    ) is Type listType)
                    {
                        iLGenerator.Emit(OpCodes.Ldc_I4, idx);
                        iLGenerator.Emit(OpCodes.Call, getItem);
                        currentComponentType = listType.GetGenericArguments()[0];
                    }
                    else
                    {
                        errorFmtArgs = new[] { field, currentComponentType.FullName };
                        return 10; // Member type {1} isn't a list nor array type (indexing {0})
                    }
                }
                else
                {
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

        public delegate int FormulaeSetter<T>(ref T material, string newFormulae, out string[] errorArgs) where T : unmanaged, IComponentData;
        public static void SetupOnFormulaeChangedAction<T>(WEWorldPickerController controller, FormulaeSetter<T> formulaeSetter, MultiUIValueBinding<string> formulaeStr, MultiUIValueBinding<int> formulaeCompileResult, MultiUIValueBinding<string[]> formulaeCompileResultErrorArgs) where T : unmanaged, IComponentData
           => formulaeStr.OnScreenValueChanged += (x) => controller.EnqueueModification<string, T>(x, (x, currentItem) =>
           {
               formulaeCompileResult.Value = formulaeSetter(ref currentItem, x, out string[] errorArgs);
               formulaeCompileResultErrorArgs.Value = errorArgs;
               return currentItem;
           });

        public static void ResetScreenFormulaeValue(string formulaeValue, MultiUIValueBinding<string> formulaeStrObj, MultiUIValueBinding<int> formulaeCompileResultObj, MultiUIValueBinding<string[]> formulaeCompileResultErrorArgsObj)
        {
            formulaeStrObj.Value = formulaeValue;
            formulaeCompileResultObj.Value = 0;
            formulaeCompileResultErrorArgsObj.Value = null;
        }
    }
}