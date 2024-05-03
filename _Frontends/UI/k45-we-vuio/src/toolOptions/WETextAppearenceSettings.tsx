import { VanillaComponentResolver } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { Component } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import "../style/floatingPanels.scss";
import { translate } from "utils/translate";

const T_appearenceTitle = translate("appearenceSettings.appearenceTitle.tooltip"); //"Appearance Settings"
const T_mainColor = translate("appearenceSettings.mainColor.tooltip"); //"Main Color"
const T_emissiveColor = translate("appearenceSettings.emissiveColor.tooltip"); //"Emission Color"
const T_Metallic = translate("appearenceSettings.Metallic.tooltip"); //"Metallic"
const T_CoatStrength = translate("appearenceSettings.CoatStrength.tooltip"); //"Coat Strength"
const T_Smoothness = translate("appearenceSettings.Smoothness.tooltip"); //"Smoothness"
const T_EmissiveIntensity = translate("appearenceSettings.EmissiveIntensity.tooltip"); //"Emissive Intensity"
const T_EmissiveExposureWeight = translate("appearenceSettings.EmissiveExposureWeight.tooltip"); //"Emissive Exposure"

export class WETextAppearenceSettings extends Component<{ initialPosition?: { x: number, y: number } }> {
    render() {
        const wps = WorldPickerService.instance
        return <Portal>
            <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={this.props.initialPosition ?? { x: 0.5, y: 0 }} >
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
