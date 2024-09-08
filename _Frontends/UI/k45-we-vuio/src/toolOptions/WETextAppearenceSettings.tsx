import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import "../style/formulaeEditorField.scss";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { FormulaeEditorRowFloat, FormulaeEditorRowFloatLog10, FormulaeEditRow } from "../common/FormulaeEditRow";


export const WETextAppearenceSettings = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_appearenceTitle = translate("appearenceSettings.appearenceTitle"); //"Appearance Settings"
    const T_mainColor = translate("appearenceSettings.mainColor"); //"Main Color"
    const T_glassColor = translate("appearenceSettings.glassColor"); //"Glass Color"
    const T_glassRefraction = translate("appearenceSettings.glassRefraction"); //"Glass Color"
    const T_emissiveColor = translate("appearenceSettings.emissiveColor"); //"Emission Color"
    const T_Metallic = translate("appearenceSettings.Metallic"); //"Metallic"
    const T_CoatStrength = translate("appearenceSettings.CoatStrength"); //"Coat Strength"
    const T_Smoothness = translate("appearenceSettings.Smoothness"); //"Smoothness"
    const T_EmissiveIntensity = translate("appearenceSettings.EmissiveIntensity"); //"Emissive Intensity"
    const T_EmissiveExposureWeight = translate("appearenceSettings.EmissiveExposureWeight"); //"Emissive Exposure"
    const T_glassTint = translate("appearenceSettings.glassTint"); //"Emissive Intensity"
    const T_glassClearness = translate("appearenceSettings.glassClearness"); //"Emissive Exposure"
    const T_colorMask1 = translate("appearenceSettings.colorMask1"); //"Emissive Exposure"
    const T_colorMask2 = translate("appearenceSettings.colorMask2"); //"Emissive Exposure"
    const T_colorMask3 = translate("appearenceSettings.colorMask3"); //"Emissive Exposure"
    const T_normalStrength = translate("appearenceSettings.normalStrength"); //"Emissive Exposure"
    const T_glassThickness = translate("appearenceSettings.glassThickness"); //"Emissive Exposure"

    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
    }, [buildIdx])

    const material = WorldPickerService.instance.bindingList.material;
    const mesh = WorldPickerService.instance.bindingList.mesh;

    const defaultPosition = props.initialPosition ?? { x: 1 - 200 / window.innerWidth, y: 200 / window.innerHeight }
    
    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <VanillaWidgets.instance.ColorPicker showAlpha={true} value={material.MainColor.value} onChange={(x) => { material.MainColor.set(x) }} label={T_mainColor} />
            {material.ShaderType.value == 0 && <>              
                {mesh.TextSourceType.value == WESimulationTextType.Image && <>
                    <VanillaWidgets.instance.ColorPicker value={material.ColorMask1.value} onChange={(x) => { x.a = 1, material.ColorMask1.set(x) }} label={T_colorMask1} />
                    <VanillaWidgets.instance.ColorPicker value={material.ColorMask2.value} onChange={(x) => { x.a = 1, material.ColorMask2.set(x) }} label={T_colorMask2} />
                    <VanillaWidgets.instance.ColorPicker value={material.ColorMask3.value} onChange={(x) => { x.a = 1, material.ColorMask3.set(x) }} label={T_colorMask3} />
                </>}
                <VanillaWidgets.instance.ColorPicker value={material.EmissiveColor.value} onChange={(x) => { material.EmissiveColor.set(x) }} label={T_emissiveColor} />
                <FormulaeEditorRowFloatLog10 formulaeField="EmissiveIntensity" formulaeModule="material" label={T_EmissiveIntensity} max={3} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="EmissiveExposureWeight" label={T_EmissiveExposureWeight} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Metallic" label={T_Metallic} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="CoatStrength" label={T_CoatStrength} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Smoothness" label={T_Smoothness} max={1} min={0} />
            </>}
            {material.ShaderType.value == 1 && <>
                <VanillaWidgets.instance.ColorPicker value={material.GlassColor.value} onChange={(x) => { x.a = 1, material.GlassColor.set(x) }} label={T_glassColor} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="GlassRefraction" label={T_glassRefraction} max={100} min={0} />
                {[WESimulationTextType.Image, WESimulationTextType.Text].includes(mesh.TextSourceType.value) && <>
                    <FormulaeEditorRowFloat formulaeModule="material" formulaeField="NormalStrength" label={T_normalStrength} max={100} min={0} />
                </>}
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Metallic" label={T_glassTint} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Smoothness" label={T_glassClearness} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="GlassThickness" label={T_glassThickness} max={100} min={0} />
            </>}
        </Panel>
    </Portal>;
} 
