#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Game;
using Game.Common;
using Game.Input;
using Game.Modding;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod, IMod
    {
        public override string SimpleName => "Write Everywhere";

        public override string SafeName => "WriteEverywhere";

        public override string Acronym => "WE";

        public override string Description => "Write Everywhere for Cities Skylines 2";

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateBefore<FontServer>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WETestTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WETestTooltip>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAfter<WERendererSystem>(SystemUpdatePhase.Rendering);
        }

        public override void OnDispose()
        {
        }

        public override void DoOnLoad()
        {
        }


        public override BasicModData CreateSettingsFile()
        {
            return new WEModData(this);
        }
    }

    public struct WESimulationTextComponent : IBufferElementData, IDisposable
    {
        public unsafe static int Size => sizeof(WESimulationTextComponent);

        public Guid parentSourceGuid;
        public Guid propertySourceGuid;
        public FixedString512Bytes FontName
        {
            get => fontName; set
            {
                fontName = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public FixedString512Bytes Text
        {
            get => text; set
            {
                text = value;
                if (basicRenderInformation.IsAllocated)
                {
                    basicRenderInformation.Free();
                    basicRenderInformation = default;
                }
            }
        }
        public Vector3 offsetPosition;
        public Quaternion offsetRotation;
        public Vector3 scale;
        public GCHandle basicRenderInformation;
        public GCHandle materialBlockPtr;
        public bool dirty;
        public Color32 color;
        public Color32 emissiveColor;
        public float metallic;
        public float emissiveIntensity;
        public FixedString512Bytes text;
        public FixedString512Bytes fontName;

        public float BriOffsetScaleX { get; private set; }
        public float BriPixelDensity { get; private set; }
        public MaterialPropertyBlock MaterialProperties
        {
            get
            {
                MaterialPropertyBlock block;
                if (!materialBlockPtr.IsAllocated)
                {
                    block = new MaterialPropertyBlock();
                    materialBlockPtr = GCHandle.Alloc(block, GCHandleType.Normal);
                }
                else
                {
                    block = materialBlockPtr.Target as MaterialPropertyBlock;
                }
                if (dirty)
                {
                    block.SetColor("_EmissiveColor", emissiveColor);
                    block.SetColor("_BaseColor", color);
                    block.SetFloat("_Metallic", metallic);
                    // block.SetFloat("_EmissiveIntensity", emissiveIntensity);


                    block.SetTexture("unity_Lightmaps", Texture2D.blackTexture);
                    block.SetTexture("unity_LightmapsInd", Texture2D.blackTexture);
                    block.SetTexture("unity_ShadowMasks", Texture2D.blackTexture);
                    dirty = false;
                }
                return block;
            }
        }

        public bool IsDirty() => dirty;
        public Color32 Color
        {
            get => color; set
            {
                color = value;
                dirty = true;
            }
        }
        public Color32 EmmissiveColor
        {
            get => emissiveColor; set
            {
                emissiveColor = value;
                dirty = true;
            }
        }
        public float Metallic
        {
            get => metallic; set
            {
                metallic = Mathf.Clamp01(value);
                dirty = true;
            }
        }

        internal static WESimulationTextComponent From(WEWaitingRenderingComponent src, DynamicSpriteFont font, BasicRenderInformation bri)
        {
            return new WESimulationTextComponent
            {
                propertySourceGuid = src.propertySourceGuid,
                FontName = src.fontName,
                Text = src.text,
                offsetPosition = src.offsetPosition,
                offsetRotation = src.offsetRotation,
                scale = src.scale,
                Color = src.color,
                EmmissiveColor = src.emmissiveColor,
                Metallic = src.metallic,
                dirty = true,
                basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak),
                BriOffsetScaleX = bri.m_offsetScaleX,
                BriPixelDensity = bri.m_pixelDensityMeters
            };
        }

        public void Dispose()
        {
            basicRenderInformation.Free();
        }
    }

    public struct WEWaitingRenderingComponent : IBufferElementData
    {
        public Guid propertySourceGuid;
        public FixedString512Bytes fontName;
        public FixedString512Bytes text;
        public Vector3 offsetPosition;
        public Quaternion offsetRotation;
        public Vector3 scale;
        public Color32 color;
        public Color32 emmissiveColor;
        public float metallic;

        internal static WEWaitingRenderingComponent From(WESimulationTextComponent src)
        {
            var result = new WEWaitingRenderingComponent
            {
                propertySourceGuid = src.propertySourceGuid,
                fontName = src.FontName,
                text = src.Text,
                offsetPosition = src.offsetPosition,
                offsetRotation = src.offsetRotation,
                scale = src.scale,
                color = src.Color,
                emmissiveColor = src.EmmissiveColor,
                metallic = src.Metallic,
            };
            return result;
        }
    }

    public partial class WETestTool : ToolBaseSystem
    {
        public override string toolID => $"K45_WE_{GetType().Name}";
        private Func<Entity, bool> callback;

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }


        private ProxyAction m_ApplyAction;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private WETestController m_Controller;

        protected override void OnCreate()
        {
            Enabled = false;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            LogUtils.DoLog("{MyTool.OnCreate} MyTool Created.");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_Controller = World.GetOrCreateSystemManaged<WETestController>();
            base.OnCreate();
        }
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
        }
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
        }
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.MovingObjects;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            bool flag = GetRaycastResult(out Entity e, out RaycastHit hit);
            if (flag)
            {
                if (m_ApplyAction.WasPressedThisFrame())
                {
                    if (flag && !(callback?.Invoke(e) ?? true))
                    {
                        RequestDisable();
                    }
                }

            }
            return inputDeps;
        }
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }
        public void Select()
        {
            m_ToolSystem.activeTool = this;
        }

        public void SetCallbackAndEnable(Func<Entity, bool> callback)
        {
            this.callback = callback;
            Select();
        }
    }

    public partial class WETestTooltip : TooltipSystemBase
    {
        private ToolSystem m_ToolSystem;
        private StringTooltip m_Tooltip;
        private WETestTool m_WETestTool;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            m_WETestTool = base.World.GetOrCreateSystemManaged<WETestTool>();
            m_Tooltip = new StringTooltip
            {
                path = "Tooltip.LABEL[XX.MyTool]"
            };
        }
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool != m_WETestTool)
            {
                return;
            }
            m_Tooltip.value = LocalizedString.IdWithFallback("Tooltip.LABEL[XX.MyTool]", "My Tool");
            AddMouseTooltip(m_Tooltip);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

    public class WETestController : ComponentSystemBase, IBelzontBindable
    {
        private Action<string, object[]> eventCaller;
        private WETestTool m_WETestTool;
        private FontServer m_FontServer;
        private EndFrameBarrier m_EndFrameBarrier;

        private Entity targetEntity;
        private string targetString;
        private string targetFont;

        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            eventCaller("test.enableTestTool", EnableTestTool);
            eventCaller("test.reloadFonts", ReloadFonts);
            eventCaller("test.listFonts", ListFonts);
            eventCaller("test.requestTextMesh", RequestTextMesh);
            eventCaller("test.listShaderDatails", ListShadersDetails);
            eventCaller("test.listShader", ListShaders);
            eventCaller("test.setShader", SetShader);
            eventCaller("test.getShader", GetShader);
            eventCaller("test.listCurrentMaterialSettings", ListCurrentMaterialSettings);
            eventCaller("test.setCurrentMaterialSettings", SetCurrentMaterialSettings);
        }

        public class PropertyDescriptor
        {
            public string Name { get; set; }
            public int Idx { get; set; }
            public int Id { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }

        private List<PropertyDescriptor> ListCurrentMaterialSettings(string fontName)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (m_FontServer[fontName] is DynamicSpriteFont df)
            {
                var mat = df.MainAtlas.Material;
                var propertyCount = mat.shader.GetPropertyCount();
                var listResult = new List<PropertyDescriptor>
                {
                    new()
                    {
                        Name = "<RenderQueue>",
                        Idx = -1,
                        Id= -1,
                        Description="Render queue index",
                        Type= "<RenderQueue>",
                        Value = mat.renderQueue.ToString()
                    }
                };
                for (int i = 0; i < propertyCount; i++)
                {
                    int nameID = mat.shader.GetPropertyNameId(i);
                    var name = mat.shader.GetPropertyName(i);
                    ShaderPropertyType shaderPropertyType = mat.shader.GetPropertyType(i);
                    listResult.Add(new()
                    {
                        Idx = i,
                        Name = name,
                        Id = nameID,
                        Description = mat.shader.GetPropertyDescription(i),
                        Type = shaderPropertyType.ToString(),
                        Value = shaderPropertyType switch
                        {
                            ShaderPropertyType.Color => mat.GetColor(name).ToRGBA(),
                            ShaderPropertyType.Vector => mat.GetVector(name).ToString()[1..^1].Trim(),
                            ShaderPropertyType.Float or ShaderPropertyType.Range => mat.GetFloat(name).ToString(),
                            ShaderPropertyType.Texture => mat.GetTexture(name) is Texture2D t2d ? Convert.ToBase64String(ImageConversion.EncodeToPNG(t2d)) : null,
                            ShaderPropertyType.Int => mat.GetInt(name).ToString(),
                            _ => null
                        }
                    });
                }
                foreach (var keyword in mat.shader.keywordSpace.keywords)
                {
                    listResult.Add(new()
                    {
                        Idx = -2,
                        Name = keyword.ToString(),
                        Id = -2,
                        Description = keyword.type.ToString(),
                        Type = "Keyword",
                        Value = mat.enabledKeywords.Any(x => x == keyword).ToString()
                    });
                }
                for (var i = 0; i < mat.passCount; i++)
                {
                    var passName = mat.GetPassName(i);
                    listResult.Add(new()
                    {
                        Idx = -3,
                        Name = passName,
                        Id = -3,
                        Description = passName,
                        Type = "ShaderPass",
                        Value = mat.GetShaderPassEnabled(passName).ToString()
                    });
                }
                return listResult;
            }
            return null;
        }

        private string SetCurrentMaterialSettings(string fontName, string propertyIdxStr, string value)
        {

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (m_FontServer[fontName] is DynamicSpriteFont df)
            {
                var mat = df.MainAtlas.Material;
                if (!int.TryParse(propertyIdxStr, out var propertyIdx))
                {
                    switch (propertyIdxStr)
                    {
                        case "<RenderQueue>":
                            return (mat.renderQueue = int.TryParse(value, out var val) ? val : mat.renderQueue).ToString();
                        default:
                            var propertyName = propertyIdxStr[1..];
                            switch (propertyIdxStr[0])
                            {
                                case 'k':
                                    {
                                        if (bool.TryParse(value, out var valBool) && valBool)
                                        {
                                            mat.EnableKeyword(propertyName);
                                        }
                                        else
                                        {
                                            mat.DisableKeyword(propertyName);
                                        }
                                        return mat.IsKeywordEnabled(propertyName).ToString();
                                    }
                                case 'p':
                                    {
                                        if (bool.TryParse(value, out var valBool))
                                        {
                                            mat.SetShaderPassEnabled(propertyName, valBool);
                                        }
                                        return mat.GetShaderPassEnabled(propertyName).ToString();
                                    }
                                default:
                                    return null;
                            }
                    }
                }
                else
                {
                    var oldRenderQueue = mat.renderQueue;
                    var nameID = mat.shader.GetPropertyNameId(propertyIdx);
                    ShaderPropertyType shaderPropertyType = mat.shader.GetPropertyType(propertyIdx);
                    switch (shaderPropertyType)
                    {
                        case ShaderPropertyType.Color:
                            try
                            {
                                mat.SetColor(nameID, ColorExtensions.FromRGBA(value));
                            }
                            catch { }
                            break;
                        case ShaderPropertyType.Vector:
                            var targVal = value.Split(",").Select(x => x.Trim()).ToArray();
                            if (targVal.Length > 1 && targVal.All(x => float.TryParse(x, out _)))
                            {
                                Vector4 vect4 = new Vector4(float.Parse(targVal[0]), float.Parse(targVal[1]), float.Parse(targVal.ElementAtOrDefault(2) ?? "0"), float.Parse(targVal.ElementAtOrDefault(3) ?? "0"));
                                mat.SetVector(nameID, vect4);
                            }
                            break;
                        case ShaderPropertyType.Range:
                        case ShaderPropertyType.Float:
                            if (float.TryParse(value, out var valFloat))
                            {
                                mat.SetFloat(nameID, valFloat);
                            }
                            break;
                        case ShaderPropertyType.Int:
                            if (float.TryParse(value, out var valInt))
                            {
                                mat.SetFloat(nameID, valInt);
                            }
                            break;
                        case ShaderPropertyType.Texture:
                            mat.SetTexture(nameID, value switch
                            {
                                "wh" => Texture2D.whiteTexture,
                                "bk" => Texture2D.blackTexture,
                                "gy" => Texture2D.grayTexture,
                                _ => null
                            });

                            break;
                    }
                    //    HDMaterial.ValidateMaterial(mat);
                    return shaderPropertyType switch
                    {
                        ShaderPropertyType.Color => mat.GetColor(nameID).ToRGBA(),
                        ShaderPropertyType.Vector => mat.GetVector(nameID).ToString()[1..^1].Trim(),
                        ShaderPropertyType.Float or ShaderPropertyType.Range => mat.GetFloat(nameID).ToString(),
                        ShaderPropertyType.Texture => mat.GetTexture(nameID) is Texture2D t2d ? Convert.ToBase64String(ImageConversion.EncodeToPNG(t2d)) : null,
                        ShaderPropertyType.Int => mat.GetInt(nameID).ToString(),
                        _ => null
                    };
                }
            }
            return null;
        }

        private Dictionary<string, Dictionary<string, object>> ListShadersDetails()
        {
            var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var shader in allShaders)
            {
                result[shader.name] = new();
                var count = shader.GetPropertyCount();
                result[shader.name]["<Keywords>"] = shader.keywordSpace.keywordNames;
                //result[shader.name]["<Keywords_Enabled>"] = shader.keywordSpace.keywordNames.Where(x=>shader.isK);
                for (int i = 0; i < count; i++)
                {
                    result[shader.name][shader.GetPropertyName(i)] = shader.GetPropertyType(i).ToString();
                }
            }
            return result;
        }
        private List<string> ListShaders()
        {
            return Resources.FindObjectsOfTypeAll<Shader>().Select(x => x.name).ToList();
        }

        private void SetShader(string shaderName)
        {
            m_FontServer.SetDefaultShader(shaderName);
        }
        private string GetShader()
        {
            return m_FontServer.GetDefaultShader();
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            this.eventCaller = eventCaller;
        }

        public void SetupEventBinder(Action<string, Delegate> eventCaller)
        {
        }
        protected override void OnCreate()
        {
            m_WETestTool = World.GetExistingSystemManaged<WETestTool>();
            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            m_EndFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_FontServer.OnFontsLoadedChanged += () => SendToFrontend("test.fontsChanged->", new object[] { ListFonts() });
            base.OnCreate();
        }
        public override void Update()
        {


        }

        private void SendToFrontend(string eventName, params object[] args) => eventCaller?.Invoke(eventName, args);

        private void EnableTestTool()
        {
            m_WETestTool.SetCallbackAndEnable((e) =>
            {
                SendToFrontend("test.enableTestTool->", e);
                var prevEntity = targetEntity;
                targetEntity = e;
                UpdateDataAtEntity();
                return false;
            });
        }

        private void ReloadFonts()
        {
            m_FontServer.ReloadFontsFromPath();
        }

        private void UpdateDataAtEntity()
        {
            if (targetEntity != Entity.Null && targetString != null && targetFont != null)
            {
                if (EntityManager.HasComponent<WESimulationTextComponent>(targetEntity))
                {
                    var compList = EntityManager.GetBuffer<WESimulationTextComponent>(targetEntity, false);
                    for (int i = 0; i < compList.Length; i++)
                    {
                        var x = compList[i];
                        x.FontName = targetFont;
                        x.Text = targetString;
                        x.Color = Color.red;
                        x.EmmissiveColor = Color.gray;
                        x.Metallic = .0f;
                        compList[i] = x;
                    }
                }
                else
                {
                    var newComponent = new WESimulationTextComponent
                    {
                        FontName = targetFont,
                        Text = targetString,
                        offsetPosition = Vector3.up * 2,
                        scale = Vector3.one * 4
                    };
                    EntityManager.AddBuffer<WESimulationTextComponent>(targetEntity).Add(newComponent);
                }
            }
        }

        private string[] ListFonts() => m_FontServer.GetAllFonts()?.ToArray();

        private string RequestTextMesh(string text, string fontName)
        {
            targetFont = fontName;
            targetString = text;
            UpdateDataAtEntity();
            var result = m_FontServer[fontName]?.DrawString(text, Vector2.one);
            return result is null ? null : XmlUtils.DefaultXmlSerialize(result);
        }
    }

}