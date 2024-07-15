#define BURST
//#define VERBOSE 
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
        public const uint CURRENT_VERSION = 3;
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
                text = "NEW TEXT",
                itemName = "New item",
                shader = WEShader.Default
            };
        }

        public Entity targetEntity;
        public WEPropertyDescription targetProperty;

        public Entity Font;
        public FixedString512Bytes Text
        {
            readonly get => text; set
            {
                text = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
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
        private FixedString512Bytes text;
        public FixedString32Bytes itemName;
        public WEShader shader;

        private int lastEvaluationFrame;
        private GCHandle formulaeHandlerStr;
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
            get => formulaeHandlerStr.IsAllocated ? formulaeHandlerStr.Target as string : null;
            set
            {
                if (value.TrimToNull() is string checkedStr) formulaeHandlerStr.Target = checkedStr;
                else if (formulaeHandlerStr.IsAllocated) formulaeHandlerStr.Free();
            }
        }

        public static WESimulationTextComponent From(WEWaitingRenderingComponent src, BasicRenderInformation bri)
        {
            src.src.dirty = true;
            if (src.src.basicRenderInformation.IsAllocated) src.src.basicRenderInformation.Free();
            src.src.basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
            src.src.BriOffsetScaleX = bri.m_offsetScaleX;
            src.src.BriPixelDensity = bri.m_pixelDensityMeters;

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
            writer.Write(text);
            writer.Write(Font);
            writer.Write(itemName);
            writer.Write(coatStrength);
            writer.Write(Formulae ?? "");
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
            reader.Read(out text);
            if (version < 3)
            {
                reader.Read(out FixedString32Bytes _);
            }
            else
            {
                reader.Read(out Font);
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
        }

        internal void MarkDirty()
        {
            basicRenderInformation.Free();
            materialBlockPtr.Free();
            dirty = true;
        }

        private static MethodInfo r_GetComponent = typeof(EntityManager).GetMethods(ReflectionUtils.allFlags)
            .First(x => x.Name == "GetComponentData" && x.GetParameters() is ParameterInfo[] pi && pi.Length == 1 && pi[0].ParameterType == typeof(Entity));


        public readonly byte SetFormulae(EntityManager em, string newFormulae)
        {
            if (targetEntity == Entity.Null) return 2; // Must define a targetEntity before compiling formulae

            var path = newFormulae.Split("/");
            DynamicMethodDefinition dynamicMethodDefinition = new DynamicMethodDefinition($"__WE_CS2_{nameof(WESimulationTextComponent)}_formulae_{targetEntity.Index}_{targetEntity.Version}_{new Regex("[A-Za-z0-9_]").Replace(newFormulae, "_")}", typeof(string), new Type[] { typeof(EntityManager), typeof(Entity) });
            ILGenerator iLGenerator = dynamicMethodDefinition.GetILGenerator();
            var entityLocalField = iLGenerator.DeclareLocal(typeof(Entity));
            var currentComponentType = typeof(Entity);
            for (int i = 0; i < path.Length; i++)
            {
                if (currentComponentType != typeof(Entity))
                {
                    return 4; // Each block on formulae path must result in an Entity
                }

                var itemSplitted = path[i].Split(";");
                var entityTypeName = itemSplitted[0];
                var fieldPath = itemSplitted[1].Split(".");

                Type itemComponentType = Type.GetType(entityTypeName, throwOnError: false);
                if (itemComponentType is null)
                {
                    return 1; // Component type not found for {0}
                }
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Call, r_GetComponent.MakeGenericMethod(itemComponentType));


                currentComponentType = itemComponentType;
                for (int j = 0; j < fieldPath.Length; j++)
                {
                    string field = fieldPath[j];
                    if (itemComponentType.GetField(field, ReflectionUtils.allFlags) is FieldInfo targetField)
                    {
                        iLGenerator.Emit(OpCodes.Ldfld, targetField);
                        currentComponentType = targetField.DeclaringType;
                        continue;
                    }
                    else if (itemComponentType.GetProperty(field, ReflectionUtils.allFlags) is PropertyInfo targetProperty && targetProperty.GetMethod != null)
                    {
                        iLGenerator.Emit(targetProperty.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetProperty.GetMethod);
                        currentComponentType = targetProperty.GetMethod.ReturnType;
                        continue;
                    }
                    else if (itemComponentType.GetMethod(field, ReflectionUtils.allFlags, null, new Type[0], null) is MethodInfo targetMethod)
                    {
                        iLGenerator.Emit(targetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethod);
                        currentComponentType = targetMethod.ReturnType;
                        continue;
                    }
                    else
                    {
                        return 3; // Member {0} not found at component type {1}
                    }
                }

            }


            return 0;
        }

        public void UpdateFormulaeValue(EntityManager em)
        {
            if (formulaeHandlerFn.IsAllocated && formulaeHandlerFn.Target is Func<EntityManager, string> fn)
            {
                Text = fn(em).Truncate(500);
            }
        }
    }

}