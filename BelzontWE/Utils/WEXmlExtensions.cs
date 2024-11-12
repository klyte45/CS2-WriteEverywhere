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
                switch (weMat.Shader)
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
                switch (weMesh.TextType)
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
                    case WESimulationTextType.WhiteTexture:
                        result.whiteMesh = weMesh.ToWhiteTextureXml();
                        break;
                }
            }
            result.transform = em.TryGetComponent<WETextDataTransform>(e, out var weTransf) ? weTransf.ToXml() : default;
            return result;
        }

        public static Entity ToEntity(this WETextDataXml xml, EntityManager em, out WETextDataMain main)
        {
            var result = em.CreateEntity();
            ToComponents(xml, out main, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform);
            em.AddComponentData(result, main);
            em.AddComponentData(result, mesh);
            em.AddComponentData(result, material);
            em.AddComponentData(result, transform);
            return result;
        }

        public static void ToComponents(this WETextDataXml xml, out WETextDataMain main, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform)
        {
            if (xml is null)
            {
                main = new() { ItemName = "<EMPTY NODE>" };
                material = default;
                mesh = default;
                transform = default;
                return;
            }
            main = xml.ToComponent();
            transform = xml.transform.ToComponent();
            if (xml.defaultStyle != null)
                material = WETextDataMaterial.ToComponent(xml.defaultStyle);
            else if (xml.glassStyle != null)
                material = WETextDataMaterial.ToComponent(xml.glassStyle);
            else material = default;

            mesh = xml.textMesh?.ToComponent()
                ?? xml.imageMesh?.ToComponent()
                ?? xml.layoutMesh?.ToComponent()
                ?? xml.whiteMesh?.ToComponent()
                ?? new() { TextType = WESimulationTextType.Archetype };

        }


        public static WETextDataXml ToXml(this WETextDataMain value)
            => new()
            {
                itemName = value.ItemName.ToString(),
            };
        public static WETextDataMain ToComponent(this WETextDataXml value)
            => new()
            {
                ItemName = value.itemName ?? ""
            };

        public static WETextDataXml.TransformXml ToXml(this WETextDataTransform value)
            => new()
            {
                offsetPosition = (Vector3Xml)value.offsetPosition,
                offsetRotation = (Vector3Xml)((Quaternion)value.offsetRotation).eulerAngles,
                scale = (Vector3Xml)value.scale,
                isAbsoluteScale = value.useAbsoluteSizeEditing
            };
        public static WETextDataTransform ToComponent(this WETextDataXml.TransformXml value)
            => value is null ? default : new()
            {
                offsetPosition = value.offsetPosition ?? default,
                offsetRotation = Quaternion.Euler(value.offsetRotation ?? default),
                scale = value.scale ?? Vector3.one,
                useAbsoluteSizeEditing = value.isAbsoluteScale
            };


        public static WETextDataXml.MeshDataTextXml ToTextMeshXml(this WETextDataMesh value)
            => new()
            {
                fontName = value.FontName.ToString(),
                maxWidthMeters = value.MaxWidthMeters,
                text = value.ValueData.ToXml()
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataTextXml value)
            => value is null ? default : new()
            {
                FontName = value.fontName ?? "",
                MaxWidthMeters = value.maxWidthMeters,
                ValueData = value.text?.ToComponent() ?? default,
                TextType = value.textType
            };
        public static WETextDataXml.MeshDataImageXml ToImageMeshXml(this WETextDataMesh value)
            => new()
            {
                atlas = value.Atlas.ToString(),
                image = value.ValueData.ToXml(),                
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataImageXml value)
            => value is null ? default : new()
            {
                Atlas = value.atlas ?? "",
                ValueData = value.image?.ToComponent() ?? default,
                TextType = value.textType
            };
        public static WETextDataXml.MeshDataPlaceholderXml ToPlaceholderXml(this WETextDataMesh value)
            => new()
            {
                layout = value.ValueData.ToXml()
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataPlaceholderXml value)
            => value is null ? default : new()
            {
                ValueData = value.layout.ToComponent(),
                TextType = value.textType
            };
        public static WETextDataXml.MeshDataWhiteTextureXml ToWhiteTextureXml(this WETextDataMesh value)
            => new() { };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataWhiteTextureXml value)
            => value is null ? default : new()
            {
                TextType = value.textType
            };





        public static WETextDataXml.FormulaeStringXml ToXml(this WETextDataValueString value) => new()
        {
            defaultValue = value.defaultValue.ToString(),
            formulae = value.formulaeStr.ToString()
        };
        public static WETextDataXml.FormulaeFloatXml ToXml(this WETextDataValueFloat value) => new()
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

        public static WETextDataValueString ToComponent(this WETextDataXml.FormulaeStringXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue ?? "",
            formulaeStr = value.formulae ?? ""
        };
        public static WETextDataValueFloat ToComponent(this WETextDataXml.FormulaeFloatXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae ?? ""
        };
        public static WETextDataValueColor ToComponent(this WETextDataXml.FormulaeColorRgbaXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae ?? ""
        };
        public static WETextDataValueColor ToComponent(this WETextDataXml.FormulaeColorRgbXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            formulaeStr = value.formulae ?? ""
        };
    }
}
