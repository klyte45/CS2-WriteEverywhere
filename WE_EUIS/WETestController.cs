//#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Entities;
using Game.Common;
using Game.Creatures;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace BelzontWE
{

    public class WETestController : ComponentSystemBase, IBelzontBindable
    {
        private Action<string, object[]> eventCaller;
        private WEWorldPickerTool m_WETestTool;
        private FontServer m_FontServer;

        internal Entity targetEntity;

        public static uint Overlay { get; private set; }

        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            eventCaller("test.enableTestTool", EnableTestTool);
            eventCaller("test.listShaderDatails", ListShadersDetails);
            eventCaller("test.listShader", ListShaders);
            eventCaller("test.setShader", SetShader);
            eventCaller("test.getShader", GetShader);
            eventCaller("test.listCurrentMaterialSettings", ListCurrentMaterialSettings);
            eventCaller("test.setCurrentMaterialSettings", SetCurrentMaterialSettings);
            eventCaller("test.setOverlay", SetOverlay);
            eventCaller("test.getOverlay", GetOverlay);
            eventCaller("test.getEntity", GetEntity);
            eventCaller("test.setEntity", SetEntity);
        }

        public uint SetOverlay(int newVal)
        {
            Overlay = (uint)newVal;
            try
            {
                //if (EntityManager.TryGetBuffer<WETextData>(targetEntity, false, out var buff))
                //{
                //    var x = buff[0];
                //    x.MarkDirty();
                //    buff[0] = x;
                //}
            }
            catch { }
            return Overlay;
        }

        public uint GetOverlay() => Overlay;
        public Entity GetEntity() => targetEntity;
        public void SetEntity(int index, int version) => targetEntity = new Entity { Index = index, Version = version };

        public class PropertyDescriptor
        {
            public string Name { get; set; }
            public int Idx { get; set; }
            public int Id { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }

        private List<PropertyDescriptor> ListCurrentMaterialSettings()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
                && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData)
                && EntityManager.TryGetComponent<WETextDataMain>(targetEntity, out var mainData))
            {
                if (materialData.GetOwnMaterial(ref meshData, out var mat))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                    EntityManager.SetComponentData(targetEntity, mainData);
                }
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
                            ShaderPropertyType.Float or ShaderPropertyType.Range => GetFloatVal(mat, name),
                            ShaderPropertyType.Texture => ReadTexture(mat, name),
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

        private static string GetFloatVal(Material mat, string name)
        {
            if (name != "colossal_DecalLayerMask")
            {
                return mat.GetFloat(name).ToString();
            }
            else
            {
                return mat.GetFloat(name).ToHexString();
            }
        }

        private static string ReadTexture(Material mat, string name)
        {
            try
            {
                return mat.GetTexture(name) is Texture2D t2d ? Convert.ToBase64String(ImageConversion.EncodeToPNG(t2d)) : null;
            }
            catch
            {
                return "Unreadable";
            }
        }

        private string SetCurrentMaterialSettings(string propertyIdxStr, string value)
        {

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
                && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData))
            {
                if (materialData.GetOwnMaterial(ref meshData,  out var mat))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                }
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
                            if (value.StartsWith("0x"))
                            {
                                mat.SetFloat(nameID, NumberExtensions.FromHexString(value));
                            }
                            else if (float.TryParse(value, out var valFloat))
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
            return Resources.FindObjectsOfTypeAll<Shader>().Select(x =>
            {
                var count = x.GetPropertyCount();
                var idx = -1;
                for (int i = 0; i < count; i++)
                {
                    if (x.GetPropertyName(i) == "_TileOffset")
                    {
                        idx = i;
                        break;
                    }
                }
                LogUtils.DoLog($"Shader _TileOffset idx = {idx} @ {x.name}");
                return x.name;
            }).ToList();
        }

        private void SetShader(string shaderName)
        {
            if (Shader.Find(shaderName) is Shader sh
                && EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
              && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData))
            {
                if (materialData.GetOwnMaterial(ref meshData, out var mat))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                }
                mat.shader = sh;
            }
        }
        private string GetShader()
        {
            if (EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
             && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData))
            {
                if (materialData.GetOwnMaterial(ref meshData,  out var mat))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                }
                return mat.shader.name;
            }
            return null;
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
            m_WETestTool = World.GetExistingSystemManaged<WEWorldPickerTool>();
            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            base.OnCreate();
        }
        public override void Update()
        {


        }

        private void SendToFrontend(string eventName, params object[] args) => eventCaller?.Invoke(eventName, args);

        private void EnableTestTool()
        {
            EntityManager.AddComponent<Deleted>(GetEntityQuery(new EntityQueryDesc[]
           {
                new() {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Pet>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
           }));
        }
    }

}