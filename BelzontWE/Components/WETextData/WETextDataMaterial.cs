using System.Runtime.InteropServices;
using UnityEngine;

namespace BelzontWE
{
    public partial struct WETextData
    {
        private struct WETextDataMaterial
        {
            public Color32 color;
            public Color32 emissiveColor;
            public Color32 glassColor;
            public float normalStrength;
            public bool dirty;
            public float glassRefraction;
            public float metallic;
            public float smoothness;
            public float emissiveIntensity;
            public float emissiveExposureWeight;
            public float coatStrength;
            public float glassThickness;
            public Color colorMask1;
            public Color colorMask2;
            public Color colorMask3;
            public GCHandle ownMaterial;
            public Colossal.Hash128 ownMaterialGuid;

            public readonly void UpdateDefaultMaterial(Material material, WESimulationTextType textType)
            {
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", metallic);
                material.SetColor("_EmissiveColor", emissiveColor);
                material.SetFloat("_EmissiveIntensity", emissiveIntensity);
                material.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight);
                material.SetFloat("_CoatStrength", coatStrength);
                material.SetFloat("_Smoothness", smoothness);
                if (textType == WESimulationTextType.Image)
                {
                    material.SetColor("colossal_ColorMask0", colorMask1);
                    material.SetColor("colossal_ColorMask1", colorMask2);
                    material.SetColor("colossal_ColorMask2", colorMask3);
                }
                else
                {
                    material.SetColor("colossal_ColorMask0", UnityEngine.Color.white);
                    material.SetColor("colossal_ColorMask1", UnityEngine.Color.white);
                    material.SetColor("colossal_ColorMask2", UnityEngine.Color.white);
                }
            }

            public readonly void UpdateGlassMaterial(Material material)
            {
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat(WERenderingHelper.IOR, glassRefraction);
                material.SetColor(WERenderingHelper.Transmittance, glassColor);
                material.SetFloat("_NormalStrength", normalStrength);
                material.SetFloat("_Thickness", glassThickness);
            }
            public void ResetMaterial()
            {
                if (ownMaterial.IsAllocated)
                {
                    GameObject.Destroy(ownMaterial.Target as Material);
                    ownMaterial.Free();
                }
            }
        }

    }
}