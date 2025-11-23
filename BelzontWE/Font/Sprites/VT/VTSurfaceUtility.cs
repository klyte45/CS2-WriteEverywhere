using Belzont.Utils;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Game.AssetPipeline;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class VTSurfaceUtility
{
    /// <summary>
    /// Converts a Surface to a VT-enabled SurfaceAsset
    /// </summary>
    /// <param name="surface">Source Surface object</param>
    /// <param name="assetDatabase">Target asset database</param>
    /// <param name="assetName">Name for the asset</param>
    /// <returns>VT-enabled SurfaceAsset</returns>
    public static void CreateVTSurfaceAsset(
        Texture2D main,
        Texture2D normal,
        Texture2D mask,
        Texture2D control,
        Texture2D emissive,
        ILocalAssetDatabase assetDatabase,
        string assetName,
        out VTTextureAsset mainVt,
        out VTTextureAsset normalVt,
        out VTTextureAsset maskVt,
        out VTTextureAsset controlVt,
        out VTTextureAsset emissiveVt
    )
    {
        using var mainImporter = new TextureImporter.Texture($"{assetName}_Main", $"K45_we://{assetName}_Main", main);
        mainImporter.ComputeMips(true, true); 
        mainImporter.CompressBC(3);

        using var normalImporter = new TextureImporter.Texture($"{assetName}_Normal", $"K45_we://{assetName}_Normal", normal);
        normalImporter.ComputeMips(true, false);
        normalImporter.CompressBC(3);

        using var maskImporter = new TextureImporter.Texture($"{assetName}_MaskMap", $"K45_we://{assetName}_MaskMap", mask);
        maskImporter.ComputeMips(true, false);
        maskImporter.CompressBC(3);

        using var controlImporter = new TextureImporter.Texture($"{assetName}_ControlMask", $"K45_we://{assetName}_ControlMask", control);
        controlImporter.ComputeMips(true, false);
        controlImporter.CompressBC(3);

        using var emissiveImporter = new TextureImporter.Texture($"{assetName}_Emissive", $"K45_we://{assetName}_Emissive", emissive);
        emissiveImporter.ComputeMips(true, true);
        emissiveImporter.CompressBC(3);

        // 1. Create SurfaceAsset from Surface
        var assetControlMask = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_ControlMask", EscapeStrategy.Filename), controlImporter);
        var assetEmissive = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_Emissive", EscapeStrategy.Filename), emissiveImporter);
        var assetMaskMap = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_MaskMap", EscapeStrategy.Filename), maskImporter);
        var assetNormal = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_Normal", EscapeStrategy.Filename), normalImporter);
        var assetMain = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_Main", EscapeStrategy.Filename), mainImporter);

        VirtualTexturingConfig vtConfig = UnityEngine.Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");

        var nbReq = 0;

        mainVt = assetDatabase.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{assetName}_Main", EscapeStrategy.Filename));
        mainVt.Save(assetMain.mipBias, assetMain, 512, nbReq, vtConfig);

        normalVt = assetDatabase.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{assetName}_Normal", EscapeStrategy.Filename));
        normalVt.Save(assetNormal.mipBias, assetNormal, 512, nbReq, vtConfig);

        maskVt = assetDatabase.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{assetName}_MaskMap", EscapeStrategy.Filename));
        maskVt.Save(assetMaskMap.mipBias, assetMaskMap, 512, nbReq, vtConfig);

        controlVt = assetDatabase.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{assetName}_ControlMask", EscapeStrategy.Filename));
        controlVt.Save(assetControlMask.mipBias, assetControlMask, 512, nbReq, vtConfig);

        emissiveVt = assetDatabase.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{assetName}_Emissive", EscapeStrategy.Filename));
        emissiveVt.Save(assetEmissive.mipBias, assetEmissive, 512, nbReq, vtConfig);
    }

    /**
     * Default:
     *
     * _BaseColorMap
     * _NormalMap
     * _MaskMap
     * _ControlMask
     * _EmissiveColorMap
     *
     * Decal:
     * _BaseColorMap
     * _NormalMap
     * _MaskMap
     *
     */
    public static void AAAA(Material newMaterial,
        VTTextureAsset mainVt,
        VTTextureAsset normalVt,
        VTTextureAsset maskVt,
        VTTextureAsset controlVt,
        VTTextureAsset emissivVtd
    )
    {
        var templateHash = AssetDatabase.global.resources.materialLibrary.GetMaterialHash(newMaterial);
        var templateDescription = AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(templateHash);
        LogUtils.DoInfoLog($"Material Template: {templateDescription.m_Material} ({templateHash})");

        var vtSys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();


        NativeArray<byte> textureData = vtSys.GetTextureData(mainVt.id.guid);
    }

    private static void ConvertToVT(ILocalAssetDatabase assetDatabase, params SurfaceAsset[] surfaces)
    {
        // Use AssetImportPipeline for VT conversion
        var report = new Colossal.AssetPipeline.Diagnostic.Report();


        using var importStep = report.AddImportStep("Convert to VT");

        AssetImportPipeline.ConvertSurfacesToVT(
            surfacesToConvert: surfaces,
            allSurfaces: surfaces.Select(x => assetDatabase.GetAsset<SurfaceAsset>(x.id.guid)),
            force: true,
            report: importStep
        );

        AssetImportPipeline.BuildMidMipsCache(surfaces, 512, 3, assetDatabase);
        AssetImportPipeline.HideVTSourceTextures(surfaces);

        var vtSys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        foreach (var surface in surfaces)
        {
            surface.RegisterToVT(vtSys);
            surface.LoadVTAsync(vtSys, 3, 512, 0);
            LogUtils.DoInfoLog($"VT Conversion completed for {surface.name}. Is VT Material: {surface.isVTMaterial}");
        }
    }
}