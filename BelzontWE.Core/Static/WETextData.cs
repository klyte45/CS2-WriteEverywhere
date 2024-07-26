#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextData : ISerializable, IDisposable, IComponentData
    {
        public const uint CURRENT_VERSION = 1;
        public unsafe static int Size => sizeof(WETextData);

        public static WETextData CreateDefault(Entity target, Entity? parent = null)
        {
            return new WETextData
            {
                TargetEntity = target,
                ParentEntity = parent ?? target,
                offsetPosition = new(0, (parent ?? target) == target ? 1 : 0, 0),
                offsetRotation = new(),
                scale = new(1, 1, 1),
                dirty = true,
                color = new(0xff, 0xff, 0xff, 0xff),
                emissiveColor = new(0xff, 0xff, 0xff, 0xff),
                metallic = 0,
                smoothness = 0,
                emissiveIntensity = 0,
                coatStrength = 0.5f,
                text = GCHandle.Alloc("NEW TEXT"),
                itemName = "New item",
                shader = WEShader.Default
            };
        }

        public bool SetNewParent(Entity e, EntityManager em)
        {
            if (e != targetEntity && (!em.TryGetComponent<WETextData>(e, out var weData) || weData.targetEntity != targetEntity))
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"NOPE: e = {e}; weData = {weData}; targetEntity = {targetEntity}; weData.targetEntity = {weData.targetEntity}");
                return false;
            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"YEP: e = {e};  targetEntity = {targetEntity}");
            parentEntity = e;
            return true;
        }

        public Entity TargetEntity { readonly get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { readonly get => parentEntity; private set => parentEntity = value; }

        public Entity Font
        {
            get => font; set
            {
                font = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public string Text
        {
            readonly get => text.IsAllocated ? text.Target as string ?? "" : ""; set
            {
                if (text.IsAllocated) text.Free();
                text = GCHandle.Alloc(value);
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public string Atlas
        {
            readonly get => atlas.IsAllocated ? atlas.Target as string ?? "" : ""; set
            {
                if (atlas.IsAllocated) atlas.Free();
                atlas = GCHandle.Alloc(value);
                if (type == WESimulationTextType.Image && basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public WESimulationTextType TextType
        {
            readonly get => type; set
            {
                if (type != value)
                {
                    type = value;
                    if (basicRenderInformation.IsAllocated)
                    {
                        basicRenderInformation.Free();
                        basicRenderInformation = default;
                    }
                }
            }
        }
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        private GCHandle basicRenderInformation;
        private GCHandle materialBlockPtr;
        private bool dirty;
        private Color32 color;
        private Color32 emissiveColor;
        private float metallic;
        private float smoothness;
        private float emissiveIntensity;
        private float emissiveExposureWeight;
        private float coatStrength;
        private WESimulationTextType type;
        private GCHandle atlas;
        private GCHandle text;
        public FixedString32Bytes itemName;
        public WEShader shader;
        public FixedString512Bytes LastErrorStr { get; private set; }
        private int lastEvaluationFrame;
        private FixedString512Bytes formulaeHandlerStr;
        private GCHandle formulaeHandlerFn;
        public float BriOffsetScaleX { get; private set; }
        public float BriPixelDensity { get; private set; }

        private bool loadingFnDone;

        public BasicRenderInformation RenderInformation => basicRenderInformation.IsAllocated ? basicRenderInformation.Target as BasicRenderInformation : null;
        public MaterialPropertyBlock MaterialProperties
        {
            get
            {
                MaterialPropertyBlock block;
                if (!materialBlockPtr.IsAllocated)
                {
                    block = new MaterialPropertyBlock();
                    materialBlockPtr = GCHandle.Alloc(block, GCHandleType.Normal);
                    dirty = true;
                }
                else
                {
                    block = materialBlockPtr.Target as MaterialPropertyBlock;
                }
                if (dirty)
                {
                    block.SetColor("_BaseColor", color);
                    block.SetFloat("_Metallic", metallic);
                    block.SetColor("_EmissiveColor", emissiveColor);
                    block.SetFloat("_EmissiveIntensity", emissiveIntensity);
                    block.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight);
                    block.SetFloat("_CoatStrength", coatStrength);
                    block.SetFloat("_Smoothness", smoothness);

                    dirty = false;
                }
                return block;
            }
        }

        public readonly bool IsDirty() => dirty;
        public Color32 Color
        {
            readonly get => color; set
            {
                color = value;
                dirty = true;
            }
        }
        public Color32 EmissiveColor
        {
            readonly get => emissiveColor; set
            {
                emissiveColor = value;
                dirty = true;
            }
        }
        public float Metallic
        {
            readonly get => metallic; set
            {
                metallic = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float Smoothness
        {
            readonly get => smoothness; set
            {
                smoothness = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float EmissiveIntensity
        {
            readonly get => emissiveIntensity; set
            {
                emissiveIntensity = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float EmissiveExposureWeight
        {
            readonly get => emissiveExposureWeight; set
            {
                emissiveExposureWeight = Mathf.Clamp01(value);
                dirty = true;
            }
        }
        public float CoatStrength
        {
            readonly get => coatStrength; set
            {
                coatStrength = Mathf.Clamp01(value);
                dirty = true;
            }
        }

        public string Formulae
        {
            get => formulaeHandlerStr.ToString();
            private set
            {
                formulaeHandlerStr = value ?? "";
            }
        }

        public bool HasFormulae => formulaeHandlerFn.IsAllocated;

        public WETextData UpdateBRI(BasicRenderInformation bri, string text)
        {
            dirty = true;
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            basicRenderInformation = default;
            basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
            BriOffsetScaleX = bri.m_offsetScaleX;
            BriPixelDensity = bri.m_pixelDensityMeters;
            if (bri.m_isError)
            {
                LastErrorStr = text;
            }
            return this;
        }

        public void Dispose()
        {
            basicRenderInformation.Free();
            materialBlockPtr.Free();
            formulaeHandlerFn.Free();
            atlas.Free();
            text.Free();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(TargetEntity);
            writer.Write((byte)shader);
            writer.Write(offsetPosition);
            writer.Write(offsetRotation);
            writer.Write(scale);
            writer.Write(color);
            writer.Write(emissiveColor);
            writer.Write(emissiveIntensity);
            writer.Write(metallic);
            writer.Write(smoothness);
            writer.Write(text.IsAllocated ? new FixedString512Bytes(text.Target as string ?? "") : "");
            writer.Write(font);
            writer.Write(itemName);
            writer.Write(coatStrength);
            writer.Write(Formulae ?? "");
            writer.Write((ushort)TextType);
            writer.Write(Atlas);
            writer.Write(parentEntity);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out targetEntity);
            reader.Read(out byte shader);
            this.shader = (WEShader)shader;
            reader.Read(out offsetPosition);
            reader.Read(out offsetRotation);
            reader.Read(out scale);
            reader.Read(out color);
            reader.Read(out emissiveColor);
            reader.Read(out emissiveIntensity);
            reader.Read(out metallic);
            reader.Read(out smoothness);
            reader.Read(out FixedString512Bytes txt);
            if (text.IsAllocated) text.Free();
            text = GCHandle.Alloc(txt.ToString());
            reader.Read(out font);
            reader.Read(out itemName);
            reader.Read(out coatStrength);
            reader.Read(out string formulae);
            Formulae = formulae.TrimToNull();
            reader.Read(out short type);
            this.type = (WESimulationTextType)type;
            reader.Read(out string atlas);
            Atlas = atlas;
            if (version >= 1)
            {
                reader.Read(out parentEntity);
            }
            else
            {
                parentEntity = targetEntity;
            }

        }

        private static MethodInfo r_GetComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "GetComponentData" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private static MethodInfo r_HasComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "HasComponent" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private Entity font;
        private Entity targetEntity;
        private Entity parentEntity;



        private static readonly Dictionary<string, GCHandle> cachedFns = new();
        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae.TrimToNull() is null)
            {
                Formulae = "";
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                errorFmtArgs = null;
                return 0;
            }
            if (cachedFns.TryGetValue(newFormulae, out var handle) && handle.IsAllocated && handle.Target != null)
            {
                Formulae = newFormulae;
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                formulaeHandlerFn = GCHandle.Alloc(handle.Target);
                errorFmtArgs = null;
                return 0;
            }


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
                        if (candidateMethods.Count() == 0)
                        {
                            return 8; // Class or method not found.
                        }
                        if (candidateMethods.Count() > 1)
                        {
                            return 9; // Class or method matches more than one result. Please be more specific.
                        }
                        var methodInfo = candidateMethods.First();
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
                Formulae = newFormulae;
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                var generatedMethod = dynamicMethodDefinition.Generate();
                Func<EntityManager, Entity, string> fn = (x, e) => generatedMethod.Invoke(null, new object[] { x, e }) as string;
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                formulaeHandlerFn = GCHandle.Alloc(fn);
                cachedFns[newFormulae] = GCHandle.Alloc(fn, GCHandleType.Weak);
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

        public static IEnumerable<MethodInfo> FilterAvailableMethodsForFormulae(Type currentComponentType, string className = null, string method = null) => AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(assembly => assembly.GetTypes())
                                                 .Where(t => className is null || t.Name == className || t.Name.EndsWith($".{className}"))
                                                 .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
                                                 .Where(m => (method is null || m.Name == method)
                                                    && m.GetParameters() is ParameterInfo[] p
                                                    && p.Length == 1
                                                    && p[0].ParameterType == currentComponentType
                                                    && !p[0].ParameterType.IsByRefLike
                                                    && m.ReturnType != typeof(void)
                                                  );

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
                if (currentComponentType.GetField(field, ReflectionUtils.allFlags) is FieldInfo targetField)
                {
                    iLGenerator.Emit(OpCodes.Ldfld, targetField);
                    currentComponentType = targetField.FieldType;
                    continue;
                }
                else if (currentComponentType.GetProperty(field, ReflectionUtils.allFlags & ~BindingFlags.Static & ~BindingFlags.NonPublic & ~BindingFlags.DeclaredOnly) is PropertyInfo targetProperty && targetProperty.GetMethod != null)
                {
                    if (currentComponentType.IsValueType && (targetProperty.GetMethod.IsVirtual || currentComponentType.IsGenericType))
                    {
                        iLGenerator.Emit(OpCodes.Constrained, currentComponentType);
                    }
                    iLGenerator.EmitCall(targetProperty.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetProperty.GetMethod, null);
                    currentComponentType = targetProperty.GetMethod.ReturnType;
                    continue;
                }
                else if (currentComponentType.GetMethod(field, ReflectionUtils.allFlags & ~BindingFlags.Static & ~BindingFlags.NonPublic & ~BindingFlags.DeclaredOnly, null, new Type[0], null) is MethodInfo targetMethod && targetMethod.ReturnType != typeof(void))
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
            fieldPath = itemSplitted[1].Split(".");
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

        public string GetEffectiveText(EntityManager em)
        {
            if (!loadingFnDone)
            {
                if (formulaeHandlerStr.Length > 0) SetFormulae(formulaeHandlerStr.ToString(), out _);
                loadingFnDone = true;
            }
            return formulaeHandlerFn.IsAllocated && formulaeHandlerFn.Target is Func<EntityManager, Entity, string> fn
                ? fn(em, TargetEntity)?.ToString().Truncate(500) ?? "<InvlidFn>"
                : formulaeHandlerStr.Length > 0 ? "<InvalidFn>" : Text;
        }

        public void OnPostInstantiate()
        {
            formulaeHandlerFn = formulaeHandlerFn.IsAllocated ? GCHandle.Alloc(formulaeHandlerFn.Target) : default;
            text = text.IsAllocated ? GCHandle.Alloc(text.Target) : default;
            atlas = atlas.IsAllocated ? GCHandle.Alloc(atlas.Target) : default;
            basicRenderInformation = default;
            materialBlockPtr = default;
        }
    }

}