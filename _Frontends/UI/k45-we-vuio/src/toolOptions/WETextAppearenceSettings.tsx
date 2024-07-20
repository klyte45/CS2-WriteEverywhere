import { VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";


const T_appearenceTitle = translate("appearenceSettings.appearenceTitle"); //"Appearance Settings"
const T_mainColor = translate("appearenceSettings.mainColor"); //"Main Color"
const T_emissiveColor = translate("appearenceSettings.emissiveColor"); //"Emission Color"
const T_Metallic = translate("appearenceSettings.Metallic"); //"Metallic"
const T_CoatStrength = translate("appearenceSettings.CoatStrength"); //"Coat Strength"
const T_Smoothness = translate("appearenceSettings.Smoothness"); //"Smoothness"
const T_EmissiveIntensity = translate("appearenceSettings.EmissiveIntensity"); //"Emissive Intensity"
const T_EmissiveExposureWeight = translate("appearenceSettings.EmissiveExposureWeight"); //"Emissive Exposure"

export const WETextAppearenceSettings = (props: { initialPosition?: { x: number, y: number } }) => {
    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
        return () => WorldPickerService.instance.disposeBindings()
    }, [buildIdx])

    const wps = WorldPickerService.instance;

    const defaultPosition = props.initialPosition ?? { x: 1 - 200 / window.innerWidth, y: 200 / window.innerHeight }

    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <VanillaWidgets.instance.ColorPicker value={wps.MainColor.value} onChange={(x) => { wps.MainColor.set(x) }} label={T_mainColor} />
            <VanillaWidgets.instance.ColorPicker value={wps.EmissiveColor.value} onChange={(x) => { wps.EmissiveColor.set(x) }} label={T_emissiveColor} />
            <VanillaWidgets.instance.FloatSlider value={wps.EmissiveIntensity.value} onChange={(x) => { wps.EmissiveIntensity.set(x) }} label={T_EmissiveIntensity} max={1} min={0} />
            <VanillaWidgets.instance.FloatSlider value={wps.EmissiveExposureWeight.value} onChange={(x) => { wps.EmissiveExposureWeight.set(x) }} label={T_EmissiveExposureWeight} max={1} min={0} />
            <VanillaWidgets.instance.FloatSlider value={wps.Metallic.value} onChange={(x) => { wps.Metallic.set(x) }} label={T_Metallic} max={1} min={0} />
            <VanillaWidgets.instance.FloatSlider value={wps.CoatStrength.value} onChange={(x) => { wps.CoatStrength.set(x) }} label={T_CoatStrength} max={1} min={0} />
            <VanillaWidgets.instance.FloatSlider value={wps.Smoothness.value} onChange={(x) => { wps.Smoothness.set(x) }} label={T_Smoothness} max={1} min={0} />
        </Panel>
    </Portal>;
} 
