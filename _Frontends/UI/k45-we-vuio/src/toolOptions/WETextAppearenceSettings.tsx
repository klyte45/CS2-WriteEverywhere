import { VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import { FormulaeEditorRowColor, FormulaeEditorRowFloat, FormulaeEditorRowFloatLog10 } from "../common/FormulaeEditRow";
import "../style/floatingPanels.scss";
import "../style/formulaeEditorField.scss";


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

    const T_affectSmoothness = translate("appearenceSettings.affectSmoothness");
    const T_metallicOpacity = translate("appearenceSettings.metallicOpacity");
    const T_drawOrder = translate("appearenceSettings.drawOrder");


    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
    }, [buildIdx])

    const material = WorldPickerService.instance.bindingList.material;
    const mesh = WorldPickerService.instance.bindingList.mesh;

    const ToggleField = VanillaWidgets.instance.ToggleField;
    const IntInputField = VanillaWidgets.instance.IntInputField;

    const defaultPosition = props.initialPosition ?? { x: 1 - 200 / window.innerWidth, y: 200 / window.innerHeight }

    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            {/* <VanillaWidgets.instance.ColorPicker showAlpha={true} value={material.MainColor.value} onChange={(x) => { material.MainColor.set(x) }} label={T_mainColor} /> */}
            <FormulaeEditorRowColor showAlpha={true} formulaeField="MainColor" formulaeModule="material" label={T_mainColor} />
            {[0].includes(material.ShaderType.value) && <>
                {mesh.TextSourceType.value == WESimulationTextType.Image && <>
                    <FormulaeEditorRowColor formulaeModule="material" formulaeField="ColorMask1" label={T_colorMask1} />
                    <FormulaeEditorRowColor formulaeModule="material" formulaeField="ColorMask2" label={T_colorMask2} />
                    <FormulaeEditorRowColor formulaeModule="material" formulaeField="ColorMask3" label={T_colorMask3} />
                </>}
                <FormulaeEditorRowColor formulaeModule="material" formulaeField="EmissiveColor" label={T_emissiveColor} />
                <FormulaeEditorRowFloatLog10 formulaeField="EmissiveIntensity" formulaeModule="material" label={T_EmissiveIntensity} max={3} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="EmissiveExposureWeight" label={T_EmissiveExposureWeight} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Metallic" label={T_Metallic} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="CoatStrength" label={T_CoatStrength} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Smoothness" label={T_Smoothness} max={1} min={0} />
            </>}
            {[2].includes(material.ShaderType.value) && <>
                <FormulaeEditorRowColor showAlpha={true} formulaeModule="material" formulaeField="ColorMask1" label={T_colorMask1} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="CoatStrength" label={T_metallicOpacity} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Metallic" label={T_Metallic} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="NormalStrength" label={T_normalStrength} max={1} min={0} />
                <FormulaeEditorRowFloat formulaeModule="material" formulaeField="Smoothness" label={T_Smoothness} max={1} min={0} />
                <ToggleField label={T_affectSmoothness} value={material.AffectSmoothness.value} onChange={(x) => material.AffectSmoothness.set(x)} />
                <IntInputField label={T_drawOrder} value={material.DrawOrder.value} onChange={(x) => material.DrawOrder.set(x)} />
            </>}
            {material.ShaderType.value == 1 && <>
                <FormulaeEditorRowColor formulaeModule="material" formulaeField="GlassColor" label={T_glassColor} />
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
