using System.Runtime.InteropServices;
using UnityEngine;

namespace BelzontWE
{
    public partial struct WETextData
    {
        private struct WETextDataMaterial
        {
            public WETextDataValueColor color;
            public WETextDataValueColor emissiveColor;
            public WETextDataValueColor glassColor;
            public WETextDataValueFloat normalStrength;
            public WETextDataValueFloat glassRefraction;
            public WETextDataValueFloat metallic;
            public WETextDataValueFloat smoothness;
            public WETextDataValueFloat emissiveIntensity;
            public WETextDataValueFloat emissiveExposureWeight;
            public WETextDataValueFloat coatStrength;
            public WETextDataValueFloat glassThickness;
            public WETextDataValueColor colorMask1;
            public WETextDataValueColor colorMask2;
            public WETextDataValueColor colorMask3;

            public bool dirty;
            public GCHandle ownMaterial;
            public Colossal.Hash128 ownMaterialGuid;

            public readonly void UpdateDefaultMaterial(Material material, WESimulationTextType textType)
            {
                material.SetColor("_BaseColor", color.EffectiveValue);
                material.SetFloat("_Metallic", metallic.EffectiveValue);
                material.SetColor("_EmissiveColor", emissiveColor.EffectiveValue);
                material.SetFloat("_EmissiveIntensity", emissiveIntensity.EffectiveValue);
                material.SetFloat("_EmissiveExposureWeight", emissiveExposureWeight.EffectiveValue);
                material.SetFloat("_CoatStrength", coatStrength.EffectiveValue);
                material.SetFloat("_Smoothness", smoothness.EffectiveValue);
                if (textType == WESimulationTextType.Image)
                {
                    material.SetColor("colossal_ColorMask0", colorMask1.EffectiveValue);
                    material.SetColor("colossal_ColorMask1", colorMask2.EffectiveValue);
                    material.SetColor("colossal_ColorMask2", colorMask3.EffectiveValue);
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
                material.SetColor("_BaseColor", color.EffectiveValue);
                material.SetFloat("_Metallic", metallic.EffectiveValue);
                material.SetFloat("_Smoothness", smoothness.EffectiveValue);
                material.SetFloat(WERenderingHelper.IOR, glassRefraction.EffectiveValue);
                material.SetColor(WERenderingHelper.Transmittance, glassColor.EffectiveValue);
                material.SetFloat("_NormalStrength", normalStrength.EffectiveValue);
                material.SetFloat("_Thickness", glassThickness.EffectiveValue);
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