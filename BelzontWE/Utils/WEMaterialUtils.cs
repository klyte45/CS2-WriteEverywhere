using Belzont.Interfaces;
using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BelzontWE.Utils
{
    public static class WEMaterialUtils
    {
        public class PropertyDescriptor
        {
            public string Name { get; set; }
            public int Idx { get; set; }
            public int Id { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }
        public static List<PropertyDescriptor> ListPropertiesFromMaterial(Material mat)
        {
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

        private static string GetFloatVal(Material mat, string name)
        {
            if (name != "colossal_DecalLayerMask")
            {
                return mat.GetFloat(name).ToString();
            }
            else
            {
                return "0x" + math.asint(mat.GetFloat(name)).ToString("X8");
            }
        }

        private static string ReadTexture(Material mat, string name)
        {
            try
            {
                var texture = mat.GetTexture(name);
                if (texture is Texture2D t2d)
                {
                    return Convert.ToBase64String(ImageConversion.EncodeToPNG(t2d));
                }
                else if (texture is Texture2DArray t2dArray)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Material '{mat}' has texture '{name}' of type {texture?.GetType()}. The dimension type is '{t2dArray.dimension}'. Dimensions = {t2dArray.width} x {t2dArray.height} x {t2dArray.depth}");
                    var output = new string[t2dArray.depth];
                    t2d = new Texture2D(t2dArray.width, t2dArray.height);
                    try
                    {
                        for (int i = 0; i < t2dArray.depth; i++)
                        {
                            t2d.SetPixels(t2dArray.GetPixels(i));
                            output[i] = Convert.ToBase64String(ImageConversion.EncodeToPNG(t2d));
                        }
                    }
                    finally
                    {
                        GameObject.Destroy(t2d);
                    }
                    return string.Join("|", output);
                }
                else
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Material '{mat}' has texture '{name}' of type {texture?.GetType()} which is not supported for reading.");
                    return null;
                }
            }
            catch (Exception e)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Error reading image '{name}' from material '{mat}': " + e);
                return null;
            }
        }
    }
}
