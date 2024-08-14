import { VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import { useState, useEffect } from "react";


export const WETextShaderProperties = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_appearenceTitle = translate("shaderProperties.title"); //"Appearance Settings"
    const T_dynamicObjectsDecalFilter = translate("shaderProperties.dynamicObjectsDecalFilter"); //"Main Color"
    const T_supportDecals = translate("shaderProperties.supportBuildingDecals"); //"Emission Color"
    const T_supportRoadDecals = translate("shaderProperties.supportRoadDecals"); //"Emission Color"
    const T_supportTerrainDecals = translate("shaderProperties.supportTerrainDecals"); //"Emission Color"
    const T_supportCreatureDecals = translate("shaderProperties.supportCreatureDecals"); //"Emission Color"
    const T_supportOtherDecals = translate("shaderProperties.supportOtherDecals"); //"Emission Color"
    const wps = WorldPickerService.instance;

    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
    }, [buildIdx])

    const defaultPosition = props.initialPosition ?? { x: 1 - 600 / window.innerWidth, y: 100 / window.innerHeight }
    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 8) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~8 : wps.DecalFlags.value | 8) }} label={T_dynamicObjectsDecalFilter} />
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 4) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~4 : wps.DecalFlags.value | 4) }} label={T_supportDecals} />
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 2) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~2 : wps.DecalFlags.value | 2) }} label={T_supportRoadDecals} />
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 1) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~1 : wps.DecalFlags.value | 1) }} label={T_supportTerrainDecals} />
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 16) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~16 : wps.DecalFlags.value | 16) }} label={T_supportCreatureDecals} />
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 32) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~32 : wps.DecalFlags.value | 32) }} label={T_supportOtherDecals} />
        </Panel>
    </Portal>;
} 
