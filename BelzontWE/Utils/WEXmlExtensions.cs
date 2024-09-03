using Belzont.Utils;
using Colossal.Entities;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE.Utils
{
    public static class WEXmlExtensions
    {
        public static WETextDataXml ToXml(Entity e, EntityManager em)
        {
            var result = em.TryGetComponent<WETextDataMain>(e, out var weMain) ? weMain.ToXml() : null;
            if (result is null) return result;
            if (em.TryGetComponent<WETextDataMaterial>(e, out var weMat))
            {
                switch (result.shader)
                {
                    case WEShader.Default:
                        result.defaultStyle = weMat.ToDefaultXml();
                        break;
                    case WEShader.Glass:
                        result.glassStyle = weMat.ToGlassXml();
                        break;
                }
            }
            if (em.TryGetComponent<WETextDataMesh>(e, out var weMesh))
            {
                switch (weMain.TextType)
                {
                    case WESimulationTextType.Text:
                        result.textMesh = weMesh.ToTextMeshXml();
                        break;
                    case WESimulationTextType.Image:
                        result.imageMesh = weMesh.ToImageMeshXml();
                        break;
                    case WESimulationTextType.Placeholder:
                        result.layoutMesh = weMesh.ToPlaceholderXml();
                        break;
                }
            }
            result.transform = em.TryGetComponent<WETextDataTransform>(e, out var weTransf) ? weTransf.ToXml() : default;
            return result;
        }

        public static Entity ToEntity(this WETextDataXml xml, EntityManager em, out WETextDataMain main)
        {
            var result = em.CreateEntity();
            em.AddComponentData(result, main = xml.ToComponent());
            em.AddComponentData(result, xml.transform.ToComponent());
            switch (xml.shader)
            {
                case WEShader.Default:
                    em.AddComponentData(result, xml.defaultStyle.ToComponent());
                    break;
                case WEShader.Glass:
                    em.AddComponentData(result, xml.glassStyle.ToComponent());
                    break;
            }
            switch (xml.textType)
            {
                case WESimulationTextType.Text:
                    em.AddComponentData(result, xml.textMesh.ToComponent());
                    break;
                case WESimulationTextType.Image:
                    em.AddComponentData(result, xml.imageMesh.ToComponent());
                    break;
                case WESimulationTextType.Placeholder:
                    em.AddComponentData(result, xml.layoutMesh.ToComponent());
                    break;
            }
            return result;
        }

        public static WETextDataXml ToXml(this WETextDataMain value)
            => new()
            {
                decalFlags = value.decalFlags,
                shader = value.shader,
                textType = value.TextType,
                itemName = value.ItemName.ToString(),
            };
        public static WETextDataMain ToComponent(this WETextDataXml value)
            => new()
            {
                decalFlags = value.decalFlags,
                shader = value.shader,
                TextType = value.textType,
                ItemName = value.itemName ?? ""
            };

        public static WETextDataXml.TransformXml ToXml(this WETextDataTransform value)
            => new()
            {
                offsetPosition = (Vector3Xml)value.offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)value.offsetRotation).eulerAngles,
                scale = (Vector3Xml)value.scale
            };
        public static WETextDataTransform ToComponent(this WETextDataXml.TransformXml value)
            => new()
            {
                offsetPosition = value.offsetPosition,
                offsetRotation = Quaternion.Euler(value.offsetRotation),
                scale = value.scale
            };


        public static WETextDataXml.MeshDataTextXml ToTextMeshXml(this WETextDataMesh value)
            => new()
            {
                fontName = value.FontName.ToString(),
                maxWidthMeters = value.MaxWidthMeters,
                text = value.ValueData.ToXml()
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataTextXml value)
            => new()
            {
                FontName = value.fontName ?? "",
                MaxWidthMeters = value.maxWidthMeters,
                ValueData = value.text.ToComponent()
            };
        public static WETextDataXml.MeshDataImageXml ToImageMeshXml(this WETextDataMesh value)
            => new()
            {
                atlas = value.Atlas.ToString(),
                image = value.ValueData.ToXml()
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataImageXml value)
            => new()
            {
                Atlas = value.atlas.ToString(),
                ValueData = value.image.ToComponent()
            };
        public static WETextDataXml.MeshDataPlaceholderXml ToPlaceholderXml(this WETextDataMesh value)
            => new()
            {
                layout = value.ValueData.ToXml()
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataPlaceholderXml value)
            => new()
            {
                ValueData = value.layout.ToComponent()
            };


        public static WETextDataXml.DefaultStyleXml ToDefaultXml(this WETextDataMaterial value)
            => new()
            {
                color = value.color.ToRgbaXml(),
                emissiveColor = value.emissiveColor.ToRgbaXml(),
                metallic = value.metallic.ToXml(),
                smoothness = value.smoothness.ToXml(),
                emissiveIntensity = value.emissiveIntensity.ToXml(),
                emissiveExposureWeight = value.emissiveExposureWeight.ToXml(),
                coatStrength = value.coatStrength.ToXml(),
                colorMask1 = value.colorMask1.ToRgbXml(),
                colorMask2 = value.colorMask2.ToRgbXml(),
                colorMask3 = value.colorMask3.ToRgbXml(),

            };
        public static WETextDataMaterial ToComponent(this WETextDataXml.DefaultStyleXml value)
            => new()
            {
                color = value.color.ToComponent(),
                emissiveColor = value.emissiveColor.ToComponent(),
                metallic = value.metallic.ToComponent(),
                smoothness = value.smoothness.ToComponent(),
                emissiveIntensity = value.emissiveIntensity.ToComponent(),
                emissiveExposureWeight = value.emissiveExposureWeight.ToComponent(),
                coatStrength = value.coatStrength.ToComponent(),
                colorMask1 = value.colorMask1.ToComponent(),
                colorMask2 = value.colorMask2.ToComponent(),
                colorMask3 = value.colorMask3.ToComponent(),
            };
        public static WETextDataXml.GlassStyleXml ToGlassXml(this WETextDataMaterial value)
            => new()
            {
                color = value.color.ToRgbaXml(),
                glassColor = value.glassColor.ToRgbXml(),
                glassRefraction = value.glassRefraction.ToXml(),
                metallic = value.metallic.ToXml(),
                smoothness = value.smoothness.ToXml(),
                normalStrength = value.normalStrength.ToXml(),
                glassThickness = value.glassThickness.ToXml(),
            };
        public static WETextDataMaterial ToComponent(this WETextDataXml.GlassStyleXml value)
            => new()
            {
                color = value.color.ToComponent(),
                glassColor = value.glassColor.ToComponent(),
                glassRefraction = value.glassRefraction.ToComponent(),
                metallic = value.metallic.ToComponent(),
                smoothness = value.smoothness.ToComponent(),
                normalStrength = value.normalStrength.ToComponent(),
                glassThickness = value.glassThickness.ToComponent(),
            };



        public static WETextDataXml.FormulaeXml<string> ToXml(this WETextDataValueString value) => new()
        {
            defaultValue = value.defaultValue.ToString(),
            formulae = value.formulaeStr.ToString()
        };
        public static WETextDataXml.FormulaeXml<float> ToXml(this WETextDataValueFloat value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.formulaeStr.ToString()
        };
        public static WETextDataXml.FormulaeColorRgbaXml ToRgbaXml(this WETextDataValueColor value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.formulaeStr.ToString()
        };
        public static WETextDataXml.FormulaeColorRgbXml ToRgbXml(this WETextDataValueColor value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.formulaeStr.ToString()
        };

        public static WETextDataValueString ToComponent(this WETextDataXml.FormulaeXml<string> value) => new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae
        };
        public static WETextDataValueFloat ToComponent(this WETextDataXml.FormulaeXml<float> value) => new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae
        };
        public static WETextDataValueColor ToComponent(this WETextDataXml.FormulaeColorRgbaXml value) => new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae
        };
        public static WETextDataValueColor ToComponent(this WETextDataXml.FormulaeColorRgbXml value) => new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae
        };
    }
}
