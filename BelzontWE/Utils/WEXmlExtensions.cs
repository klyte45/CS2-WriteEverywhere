using Belzont.Utils;
using Colossal.Entities;
using Unity.Entities;
using Unity.Mathematics;
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
                    case WEShader.Decal:
                        result.decalStyle = weMat.ToDecalXml();
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
                    case WESimulationTextType.WhiteCube:
                        result.whiteCubeMesh = weMesh.ToWhiteCubeXml();
                        break;
                    case WESimulationTextType.MatrixTransform:
                        result.matrixTransform = weMesh.ToScalerXml();
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
            em.AddComponent<WETextComponentValid>(result);
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
            else if (xml.decalStyle != null)
                material = WETextDataMaterial.ToComponent(xml.decalStyle);
            else material = default;

            mesh = xml.textMesh?.ToComponent()
                ?? xml.imageMesh?.ToComponent()
                ?? xml.layoutMesh?.ToComponent()
                ?? xml.whiteMesh?.ToComponent()
                ?? xml.matrixTransform?.ToComponent()
                ?? xml.whiteCubeMesh?.ToComponent()
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
                isAbsoluteScale = value.useAbsoluteSizeEditing,
                pivot = value.pivot,
                mustDraw = value.MustDrawFn.ToXml(),
                useFormulaeToCheckIfDraw = value.useFormulaeToCheckIfDraw,
                arrayAxisOrder = value.arrayAxisGrowthOrder,
                arrayInstances = (Vector3Xml)(float3)value.ArrayInstancing,
                arraySpacing = (Vector3Xml)value.arrayInstancingGapMeters,
                alignment = value.alignment,
                instanceCount = value.InstanceCountFn.ToXml(),
                pivotZ = value.pivotZ,

            };
        public static WETextDataTransform ToComponent(this WETextDataXml.TransformXml value)
            => value is null ? default : new()
            {
                offsetPosition = value.offsetPosition ?? default,
                offsetRotation = Quaternion.Euler(value.offsetRotation ?? default),
                scale = value.scale ?? Vector3.one,
                useAbsoluteSizeEditing = value.isAbsoluteScale,
                pivot = value.pivot,
                MustDrawFn = value.mustDraw.ToComponent(),
                useFormulaeToCheckIfDraw = value.useFormulaeToCheckIfDraw,
                ArrayInstancing = (uint3)(float3)value.arrayInstances,
                arrayInstancingGapMeters = value.arraySpacing,
                arrayAxisGrowthOrder = value.arrayAxisOrder,
                alignment = value.alignment,
                InstanceCountFn = value.instanceCount.ToComponent(),
                pivotZ = value.pivotZ
            };


        public static WETextDataXml.MeshDataTextXml ToTextMeshXml(this WETextDataMesh value)
            => new()
            {
                fontName = value.FontName.ToString(),
                MaxWidthMeters = value.MaxWidthMeters.ToXml(),
                rescaleHeightOnTextOverflow = value.RescaleHeightOnTextOverflow,
                text = value.ValueData.ToXml()
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataTextXml value)
            => value is null ? default : new()
            {
                FontName = value.fontName ?? "",
                originalName = $"{value.fontName}",
                MaxWidthMeters = value.MaxWidthMeters?.ToComponent() ?? default,
                ValueData = value.text?.ToComponent() ?? default,
                TextType = value.textType,
                RescaleHeightOnTextOverflow = value.rescaleHeightOnTextOverflow
            };
        public static WETextDataXml.MeshDataImageXml ToImageMeshXml(this WETextDataMesh value)
            => new()
            {
                atlas = value.Atlas.ToString(),
                mesh = value.CustomMeshName.ToXml(),
                image = value.ValueData.ToXml(),
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataImageXml value)
            => value is null ? default : new()
            {
                Atlas = value.atlas ?? "",
                originalName = $"{value.atlas}",
                CustomMeshName = value.mesh?.ToComponent() ?? default,
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
                TextType = value.textType,
                originalName = $"{value.layout.defaultValue}",
            };

        public static WETextDataXml.MeshDataWhiteTextureXml ToWhiteTextureXml(this WETextDataMesh value)
            => new() { };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataWhiteTextureXml value)
            => value is null ? default : new()
            {
                TextType = value.textType
            };

        public static WETextDataXml.MeshDataWhiteCubeXml ToWhiteCubeXml(this WETextDataMesh value)
            => new()
            {
                childrenRefersToFrontFace = value.childrenRefersToFrontFace
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataWhiteCubeXml value)
            => value is null ? default : new()
            {
                TextType = value.textType,
                childrenRefersToFrontFace = value.childrenRefersToFrontFace
            };

        public static WETextDataXml.MeshDataMatrixTransformXml ToScalerXml(this WETextDataMesh value)
            => new()
            {
                scale = value.ScaleFormulae.ToXml(),
                offsetPosition = value.OffsetPositionFormulae.ToXml(),
                offsetRotation = value.OffsetRotationFormulae.ToXml(),
            };
        public static WETextDataMesh ToComponent(this WETextDataXml.MeshDataMatrixTransformXml value)
            => new()
            {
                ScaleFormulae = value?.scale.ToComponent() ?? new WETextDataValueFloat3
                {
                    defaultValue = new float3(1, 1, 1)
                },
                OffsetPositionFormulae = value?.offsetPosition.ToComponent() ?? new WETextDataValueFloat3(),
                OffsetRotationFormulae = value?.offsetRotation.ToComponent() ?? new WETextDataValueFloat3(),
                TextType = value.textType
            };




        public static WETextDataXml.FormulaeStringXml ToXml(this WETextDataValueString value) => new()
        {
            defaultValue = value.DefaultValue,
            formulae = value.Formulae
        };
        public static WETextDataXml.FormulaeFloatXml ToXml(this WETextDataValueFloat value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.Formulae
        };
        public static WETextDataXml.FormulaeFloat3Xml ToXml(this WETextDataValueFloat3 value) => new()
        {
            defaultValue = (Vector3Xml)value.defaultValue,
            formulae = value.Formulae
        };
        public static WETextDataXml.FormulaeIntXml ToXml(this WETextDataValueInt value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.Formulae
        };
        public static WETextDataXml.FormulaeColorRgbaXml ToRgbaXml(this WETextDataValueColor value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.Formulae
        };
        public static WETextDataXml.FormulaeColorRgbXml ToRgbXml(this WETextDataValueColor value) => new()
        {
            defaultValue = value.defaultValue,
            formulae = value.Formulae
        };

        public static WETextDataValueString ToComponent(this WETextDataXml.FormulaeStringXml value) => value is null ? default : new()
        {
            DefaultValue = value.defaultValue ?? "",
            Formulae = value.formulae
        };
        public static WETextDataValueFloat ToComponent(this WETextDataXml.FormulaeFloatXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            Formulae = value.formulae
        };
        public static WETextDataValueFloat3 ToComponent(this WETextDataXml.FormulaeFloat3Xml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            Formulae = value.formulae
        };
        public static WETextDataValueInt ToComponent(this WETextDataXml.FormulaeIntXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            Formulae = value.formulae
        };
        public static WETextDataValueColor ToComponent(this WETextDataXml.FormulaeColorRgbaXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            Formulae = value.formulae
        };
        public static WETextDataValueColor ToComponent(this WETextDataXml.FormulaeColorRgbXml value) => value is null ? default : new()
        {
            defaultValue = value.defaultValue,
            Formulae = value.formulae
        };
    }
}
