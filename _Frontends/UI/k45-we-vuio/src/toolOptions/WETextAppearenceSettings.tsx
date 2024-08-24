import { VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import { WESimulationTextType } from "services/WEFormulaeElement";


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

    const wps = WorldPickerService.instance;

    const defaultPosition = props.initialPosition ?? { x: 1 - 200 / window.innerWidth, y: 200 / window.innerHeight }

    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <VanillaWidgets.instance.ColorPicker showAlpha={true} value={wps.MainColor.value} onChange={(x) => { wps.MainColor.set(x) }} label={T_mainColor} />
            {wps.ShaderType.value == 0 && <>
                {wps.TextSourceType.value == WESimulationTextType.Image && <>
                    <VanillaWidgets.instance.ColorPicker value={wps.ColorMask1.value} onChange={(x) => { x.a = 1, wps.ColorMask1.set(x) }} label={T_colorMask1} />
                    <VanillaWidgets.instance.ColorPicker value={wps.ColorMask2.value} onChange={(x) => { x.a = 1, wps.ColorMask2.set(x) }} label={T_colorMask2} />
                    <VanillaWidgets.instance.ColorPicker value={wps.ColorMask3.value} onChange={(x) => { x.a = 1, wps.ColorMask3.set(x) }} label={T_colorMask3} />
                </>}
                <VanillaWidgets.instance.ColorPicker value={wps.EmissiveColor.value} onChange={(x) => { x.a = 1, wps.EmissiveColor.set(x) }} label={T_emissiveColor} />
                <VanillaWidgets.instance.FloatSlider value={wps.EmissiveIntensity.value} onChange={(x) => { wps.EmissiveIntensity.set(x) }} label={T_EmissiveIntensity} max={1} min={0} />
                <VanillaWidgets.instance.FloatSlider value={wps.EmissiveExposureWeight.value} onChange={(x) => { wps.EmissiveExposureWeight.set(x) }} label={T_EmissiveExposureWeight} max={1} min={0} />
                <VanillaWidgets.instance.FloatSlider value={wps.Metallic.value} onChange={(x) => { wps.Metallic.set(x) }} label={T_Metallic} max={1} min={0} />
                <VanillaWidgets.instance.FloatSlider value={wps.CoatStrength.value} onChange={(x) => { wps.CoatStrength.set(x) }} label={T_CoatStrength} max={1} min={0} />
                <VanillaWidgets.instance.FloatSlider value={wps.Smoothness.value} onChange={(x) => { wps.Smoothness.set(x) }} label={T_Smoothness} max={1} min={0} />
            </>}
            {wps.ShaderType.value == 1 && <>
                <VanillaWidgets.instance.ColorPicker value={wps.GlassColor.value} onChange={(x) => { x.a = 1, wps.GlassColor.set(x) }} label={T_glassColor} />
                <VanillaWidgets.instance.FloatSlider value={(wps.GlassRefraction.value - 1) * 10} onChange={(x) => { wps.GlassRefraction.set(1 + x / 10) }} label={T_glassRefraction} max={100} min={0} />
                {[WESimulationTextType.Image, WESimulationTextType.Text].includes(wps.TextSourceType.value) && <>
                    <VanillaWidgets.instance.FloatSlider value={wps.NormalStrength.value * 10} onChange={(x) => { wps.NormalStrength.set(x / 10) }} label={T_normalStrength} max={100} min={0} />
                </>}
                <VanillaWidgets.instance.FloatSlider value={wps.Metallic.value} onChange={(x) => { wps.Metallic.set(x) }} label={T_glassTint} max={1} min={0} />
                <VanillaWidgets.instance.FloatSlider value={wps.Smoothness.value} onChange={(x) => { wps.Smoothness.set(x) }} label={T_glassClearness} max={1} min={0} />
                <VanillaWidgets.instance.FloatSlider value={wps.GlassThickness.value} onChange={(x) => { wps.GlassThickness.set(x) }} label={T_glassThickness} max={100} min={0} />
            </>}
        </Panel>
    </Portal>;
} 
