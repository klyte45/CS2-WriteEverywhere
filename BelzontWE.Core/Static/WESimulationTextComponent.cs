#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Serialization.Entities;
using MonoMod.Utils;
using System;
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

    public struct WESimulationTextComponent : IBufferElementData, ISerializable, IDisposable
    {
        public const uint CURRENT_VERSION = 4;
        public unsafe static int Size => sizeof(WESimulationTextComponent);

        public static WESimulationTextComponent CreateDefault()
        {
            return new WESimulationTextComponent
            {
                offsetPosition = new(0, 1, 0),
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

        public Entity targetEntity;
        public WEPropertyDescription targetProperty;

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

        public static WESimulationTextComponent From(WEWaitingRenderingComponent src, BasicRenderInformation bri, string text)
        {
            src.src.dirty = true;
            if (src.src.basicRenderInformation.IsAllocated) src.src.basicRenderInformation.Free();
            src.src.basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
            src.src.BriOffsetScaleX = bri.m_offsetScaleX;
            src.src.BriPixelDensity = bri.m_pixelDensityMeters;
            if (bri.m_isError)
            {
                src.src.LastErrorStr = text;
            }
            return src.src;
        }

        public void Dispose()
        {
            basicRenderInformation.Free();
            materialBlockPtr.Free();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(targetEntity);
            writer.Write((uint)targetProperty);
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
            reader.Read(out uint targetProperty);
            this.targetProperty = (WEPropertyDescription)targetProperty;
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
            if (version < 3)
            {
                reader.Read(out FixedString32Bytes _);
            }
            else
            {
                reader.Read(out font);
            }
            if (version >= 1)
            {
                reader.Read(out itemName);
            }
            if (version >= 2)
            {
                reader.Read(out coatStrength);
                reader.Read(out string formulae);
                Formulae = formulae.TrimToNull();
            }
            else
            {
                coatStrength = 0.5f;
            }
            if (version >= 4)
            {
                reader.Read(out short type);
                this.type = (WESimulationTextType)type;
                reader.Read(out string atlas);
                Atlas = atlas;
            }
        }

        internal void MarkDirty()
        {
            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
            if (materialBlockPtr.IsAllocated) materialBlockPtr.Free();
            dirty = true;
        }

        private static MethodInfo r_GetComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "GetComponentData" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));
        private Entity font;

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae == "")
            {
                Formulae = newFormulae;
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                errorFmtArgs = null;
                return 0;
            }
            // FormulaeExample: Game.Common.Owner
            var path = newFormulae.Split("/");
            DynamicMethodDefinition dynamicMethodDefinition = new(
                $"__WE_CS2_{nameof(WESimulationTextComponent)}_formulae_{new Regex("[^A-Za-z0-9_]").Replace(newFormulae, "_")}",
                typeof(string),
                new Type[] { typeof(EntityManager), typeof(Entity) }
                );
            ILGenerator iLGenerator = dynamicMethodDefinition.GetILGenerator();
            var typeList = TypeManager.AllTypes;
            try
            {
                errorFmtArgs = null;
                var entityLocalField = iLGenerator.DeclareLocal(typeof(Entity));
                var currentComponentType = typeof(Entity);
                LocalBuilder localVarEntity = null;
                for (int i = 0; i < path.Length; i++)
                {
                    if (currentComponentType != typeof(Entity))
                    {
                        return 4; // Each block on formulae path must result in an Entity, except last
                    }

                    var itemSplitted = path[i].Split(";");
                    if (itemSplitted.Length != 2)
                    {
                        return 6; // Each entity block must be a pair of component name and field navigation, separated by a semicolon
                    }
                    var entityTypeName = itemSplitted[0];
                    var fieldPath = itemSplitted[1].Split(".");

                    var itemComponentType = typeList.Where(x => x.Type?.FullName?.EndsWith(entityTypeName) ?? false).ToList();
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

                    if (i == 0)
                    {
                        iLGenerator.Emit(OpCodes.Ldarga_S, 0);
                        iLGenerator.Emit(OpCodes.Ldarg_1);
                    }
                    else
                    {
                        localVarEntity ??= iLGenerator.DeclareLocal(typeof(Entity));
                        iLGenerator.Emit(OpCodes.Stloc, localVarEntity);
                        iLGenerator.Emit(OpCodes.Ldarga_S, 0);
                        iLGenerator.Emit(OpCodes.Ldloc, localVarEntity);
                    }
                    iLGenerator.EmitCall(OpCodes.Call, r_GetComponent.MakeGenericMethod(itemComponentType[0].Type), null);


                    currentComponentType = itemComponentType[0].Type;
                    for (int j = 0; j < fieldPath.Length; j++)
                    {
                        string field = fieldPath[j];
                        if (currentComponentType.GetField(field, ReflectionUtils.allFlags) is FieldInfo targetField)
                        {
                            iLGenerator.Emit(OpCodes.Ldfld, targetField);
                            currentComponentType = targetField.FieldType;
                            continue;
                        }
                        else if (currentComponentType.GetProperty(field, ReflectionUtils.allFlags) is PropertyInfo targetProperty && targetProperty.GetMethod != null)
                        {
                            iLGenerator.EmitCall(targetProperty.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetProperty.GetMethod, null);
                            currentComponentType = targetProperty.GetMethod.ReturnType;
                            continue;
                        }
                        else if (currentComponentType.GetMethod(field, ReflectionUtils.allFlags, null, new Type[0], null) is MethodInfo targetMethod)
                        {
                            iLGenerator.EmitCall(targetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethod, null);
                            currentComponentType = targetMethod.ReturnType;
                            continue;
                        }
                        else
                        {
                            errorFmtArgs = new[] { field, currentComponentType.FullName, string.Join("\n", currentComponentType.GetMembers(ReflectionUtils.allFlags).Select(x => x.Name)) };
                            return 3; // Member {0} not found at component type {1}; Available Members: {2}
                        }
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
                    var local0 = iLGenerator.DeclareLocal(currentComponentType);
                    iLGenerator.Emit(OpCodes.Stloc, local0);
                    iLGenerator.Emit(OpCodes.Ldloca_S, local0);
                    iLGenerator.EmitCall(OpCodes.Call, toStringMethod, null);
                }
                iLGenerator.Emit(OpCodes.Ret);
                Formulae = newFormulae;
                if (formulaeHandlerFn.IsAllocated) formulaeHandlerFn.Free();
                var generatedMethod = dynamicMethodDefinition.Generate();
                Func<EntityManager, Entity, string> fn = (x, e) => generatedMethod.Invoke(null, new object[] { x, e }) as string;
                formulaeHandlerFn = GCHandle.Alloc(fn);
                if (BasicIMod.DebugMode) LogUtils.DoLog("FN => (" + string.Join(", ", dynamicMethodDefinition.Definition.Parameters.Select(x => $"{(x.IsIn ? "in " : x.IsOut ? "out " : "")}{x.ParameterType} {x.Name}")) + ")");
                if (BasicIMod.DebugMode) LogUtils.DoLog("FN => \n" + string.Join("\n", dynamicMethodDefinition.Definition.Body.Instructions.Select(x => x.ToString())));

                return 0;
            }
            finally
            {
                dynamicMethodDefinition.Dispose();
            }
        }

        public string GetEffectiveText(EntityManager em)
        {
            return formulaeHandlerFn.IsAllocated && formulaeHandlerFn.Target is Func<EntityManager, Entity, string> fn
                ? fn(em, targetEntity)?.ToString().Truncate(500) ?? ""
                : Text;
        }
    }

}