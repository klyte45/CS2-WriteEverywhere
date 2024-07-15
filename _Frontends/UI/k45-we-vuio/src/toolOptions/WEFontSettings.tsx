import { VanillaComponentResolver } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { Component } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import "../style/floatingPanels.scss";
import { translate } from "utils/translate";

const T_appearenceTitle = translate("appearenceSettings.appearenceTitle"); //"Appearance Settings"
const T_mainColor = translate("appearenceSettings.mainColor"); //"Main Color"
const T_emissiveColor = translate("appearenceSettings.emissiveColor"); //"Emission Color"
const T_Metallic = translate("appearenceSettings.Metallic"); //"Metallic"
const T_CoatStrength = translate("appearenceSettings.CoatStrength"); //"Coat Strength"
const T_Smoothness = translate("appearenceSettings.Smoothness"); //"Smoothness"
const T_EmissiveIntensity = translate("appearenceSettings.EmissiveIntensity"); //"Emissive Intensity"
const T_EmissiveExposureWeight = translate("appearenceSettings.EmissiveExposureWeight"); //"Emissive Exposure"

export class WEFontSettings extends Component<{ initialPosition?: { x: number, y: number } }> {
    render() {
        const wps = WorldPickerService.instance
        return <Portal>
            <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={this.props.initialPosition ?? { x: 0.5, y: 0 }} >
                {/* <VanillaComponentResolver.instance.DropdownField props={{ value: 24, items: [{ displayName: "Teste 24", value: 24 }, { displayName: "AAAA", value: 22 }] }} group={"aaa"} parent={{ group: "bbbbb", path: "AAA" }} path="aasda" /> */}
            </Panel>
        </Portal>;
    }
}
