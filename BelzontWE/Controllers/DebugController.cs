#if DEBUG
using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.IO;
using BelzontWE.Sprites;
using Colossal.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static BelzontWE.Utils.WEMaterialUtils;
namespace BelzontWE.Controllers
{
    public partial class DebugController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "debug.";
        private Action<string, object[]> eventCaller;
        public static CustomMeshRenderInformation testingMesh;


        public static uint Overlay { get; private set; }

        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            eventCaller($"{PREFIX}listShaderDatails", ListShadersDetails);
            eventCaller($"{PREFIX}listShader", ListShaders);
            eventCaller($"{PREFIX}setShader", SetShader);
            eventCaller($"{PREFIX}listCurrentMaterialSettings", ListCurrentMaterialSettings);
            eventCaller($"{PREFIX}setCurrentMaterialSettings", SetCurrentMaterialSettings);
            eventCaller($"{PREFIX}createSpecialMeshBRI", CreateSpecialMeshBRI);
        }



        private void CreateSpecialMeshBRI(Entity targetEntity, string meshLocation)
        {
            if (!EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData)) return;
            try
            {
                var parsedMesh = ObjFileHandler.ImportFromObj(meshLocation);
                WEAtlasesLibrary.Instance.TryGetAtlas(meshData.Atlas.ToString(), out var atlasInfo);
                var spriteInfo = atlasInfo.Sprites[meshData.Text];
                var dimensions = new float2(atlasInfo.Width, atlasInfo.Height);
                testingMesh = new CustomMeshRenderInformation(atlasInfo, parsedMesh, (float2)spriteInfo.Region.min / dimensions, (float2)spriteInfo.Region.max / dimensions);
                EntityManager.SetComponentData(targetEntity, meshData.UpdateBRI(testingMesh, null));
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"Error trying to set custom BRI: {e.Message}\n{e}");
            }

        }

        private List<PropertyDescriptor> ListCurrentMaterialSettings(Entity targetEntity)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
                && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData)
                && EntityManager.TryGetComponent<WETextDataMain>(targetEntity, out var mainData))
            {
                if (materialData.GetOwnMaterial(ref meshData, null, out var mat))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                    EntityManager.SetComponentData(targetEntity, mainData);
                }
                return ListPropertiesFromMaterial(mat[0]);
            }
            return null;
        }




        private string SetCurrentMaterialSettings(Entity targetEntity, string propertyIdxStr, string value)
        {

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
                && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData))
            {
                if (materialData.GetOwnMaterial(ref meshData, default, out var mats))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                }
                var mat = mats[0];
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
                                mat.SetFloat(nameID, math.asfloat(Convert.ToInt32(value[2..])));
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

        private void SetShader(Entity targetEntity, string shaderName)
        {
            if (Shader.Find(shaderName) is Shader sh
                && EntityManager.TryGetComponent<WETextDataMaterial>(targetEntity, out var materialData)
              && EntityManager.TryGetComponent<WETextDataMesh>(targetEntity, out var meshData))
            {
                if (materialData.GetOwnMaterial(ref meshData, null, out var mats))
                {
                    EntityManager.SetComponentData(targetEntity, materialData);
                    EntityManager.SetComponentData(targetEntity, meshData);
                }
                mats[0].shader = sh;
            }
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
            base.OnCreate();
        }
        protected override void OnUpdate()
        {
        }



    }
}
#endif
