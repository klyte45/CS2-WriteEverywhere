using Belzont.Utils;
using BelzontWE;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Game.AssetPipeline;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;
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
        Texture main,
        Texture normal,
        Texture mask,
        Texture control,
        Texture emissive,
        ILocalAssetDatabase assetDatabase,
        string assetName,
        out SurfaceAsset defMat,
        out SurfaceAsset glsMat,
        out SurfaceAsset dclMat)
    {


        // 1. Create SurfaceAsset from Surface
        var assetControlMask = assetDatabase.AddAsset<TextureAsset, Texture>(AssetDataPath.Create($"{assetName}_ControlMask", EscapeStrategy.Filename), control);
        var assetEmissive = assetDatabase.AddAsset<TextureAsset, Texture>(AssetDataPath.Create($"{assetName}_Emissive", EscapeStrategy.Filename), emissive);
        var assetMaskMap = assetDatabase.AddAsset<TextureAsset, Texture>(AssetDataPath.Create($"{assetName}_MaskMap", EscapeStrategy.Filename), mask);
        var assetNormal = assetDatabase.AddAsset<TextureAsset, Texture>(AssetDataPath.Create($"{assetName}_Normal", EscapeStrategy.Filename), normal);
        var assetMain = assetDatabase.AddAsset<TextureAsset, Texture>(AssetDataPath.Create($"{assetName}_Main", EscapeStrategy.Filename), main);

        assetControlMask.Save();
        assetEmissive.Save();
        assetMaskMap.Save();
        assetNormal.Save();
        assetMain.Save();


        defMat = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_Def"), WERenderingHelper.GenerateMaterial(WEShader.Default, main, normal, mask, control, emissive));
        glsMat = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_Gls"), WERenderingHelper.GenerateMaterial(WEShader.Glass, main, normal, mask, control, emissive));
        dclMat = assetDatabase.AddAsset(AssetDataPath.Create($"{assetName}_Dcl"), WERenderingHelper.GenerateMaterial(WEShader.Decal, main, normal, mask, control, emissive));


        // 2. Save initial surface (non-VT)
        foreach (var surfaceAsset in new[] { defMat, glsMat, dclMat })
        {
            surfaceAsset.UpdateTextures(new Dictionary<string, TextureAsset>
            {
                { "_BaseColorMap", assetMain },
                { "_NormalMap", assetNormal },
                { "_MaskMap", assetMaskMap },
                { "_ControlMask", assetControlMask },
                { "_Emissive", assetEmissive }
            });
            surfaceAsset.Save(
                mipBias: 0,
                force: false,
                saveTextures: true,
                vt: false,  // Initially false
                virtualTexturingConfig: null,
                textureReferencesMap: null,
                tileSize: null,
                nbMidMipLevelsRequested: null
            );
            surfaceAsset.Load();
        }

        // 3. Convert to VT format
        ConvertToVT(assetDatabase, defMat, glsMat, dclMat);
    }

    private static void ConvertToVT(ILocalAssetDatabase assetDatabase, params SurfaceAsset[] surfaces)
    {
        // Get VT configuration
        VirtualTexturingConfig vtConfig = UnityEngine.Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");


        // Use AssetImportPipeline for VT conversion
        var report = new Colossal.AssetPipeline.Diagnostic.Report();

        using (var importStep = report.AddImportStep("Convert to VT"))
        {
            AssetImportPipeline.ConvertSurfacesToVT(
                surfacesToConvert: surfaces,
                allSurfaces: surfaces.Select(x => assetDatabase.GetAsset<SurfaceAsset>(x.id.guid)),
                force: false,
                tileSize: 512,
                midMipsCount: 3,
                mipBias: 20,
                writeVTSettings: false,
                report: importStep
            );
        }
        foreach (var surface in surfaces)
        {
            LogUtils.DoInfoLog($"VT Conversion completed for {surface.name}. Is VT Material: {surface.isVTMaterial}");
        }
    }

}
