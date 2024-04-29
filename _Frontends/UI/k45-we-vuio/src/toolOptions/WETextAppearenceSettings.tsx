import { VanillaComponentResolver } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { Component } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import "../style/floatingPanels.scss";

const T_appearenceTitle = "Appearance Settings"
const T_mainColor = "Main Color"
const T_emissiveColor = "Emission Color"
const T_Metallic = "Metallic"
const T_CoatStrength = "Coat Strength"
const T_Smoothness = "Smoothness"
const T_EmissiveIntensity = "Emissive Intensity"
const T_EmissiveExposureWeight = "Emissive Exposure"

export class WETextAppearenceSettings extends Component {
    render() {
        const wps = WorldPickerService.instance
        return <Portal>
            <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={{ x: 0.5, y: 0 }}>
                <VanillaComponentResolver.instance.ColorPicker value={wps.MainColor.value} onChange={(x) => { wps.MainColor.set(x) }} label={T_mainColor} />
                <VanillaComponentResolver.instance.ColorPicker value={wps.EmissiveColor.value} onChange={(x) => { wps.EmissiveColor.set(x) }} label={T_emissiveColor} />
                <VanillaComponentResolver.instance.FloatSlider value={wps.EmissiveIntensity.value} onChange={(x) => { wps.EmissiveIntensity.set(x) }} label={T_EmissiveIntensity} max={1} min={0} />
                <VanillaComponentResolver.instance.FloatSlider value={wps.EmissiveExposureWeight.value} onChange={(x) => { wps.EmissiveExposureWeight.set(x) }} label={T_EmissiveExposureWeight} max={1} min={0} />
                <VanillaComponentResolver.instance.FloatSlider value={wps.Metallic.value} onChange={(x) => { wps.Metallic.set(x) }} label={T_Metallic} max={1} min={0} />
                <VanillaComponentResolver.instance.FloatSlider value={wps.CoatStrength.value} onChange={(x) => { wps.CoatStrength.set(x) }} label={T_CoatStrength} max={1} min={0} />
                <VanillaComponentResolver.instance.FloatSlider value={wps.Smoothness.value} onChange={(x) => { wps.Smoothness.set(x) }} label={T_Smoothness} max={1} min={0} />
            </Panel>
        </Portal>;
    }
} 
