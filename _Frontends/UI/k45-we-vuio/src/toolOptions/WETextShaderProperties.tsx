import { VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import { useState, useEffect } from "react";


export const WETextShaderProperties = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_appearenceTitle = translate("shaderProperties.title"); //"Appearance Settings"
    const T_supportSurfaceAreas = translate("shaderProperties.supportSurfaceAreas"); //"Main Color"
    const T_supportDecals = translate("shaderProperties.supportDecals"); //"Emission Color"
    const wps = WorldPickerService.instance;

    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
    }, [buildIdx])

    const defaultPosition = props.initialPosition ?? { x: 1 - 500 / window.innerWidth, y: 100 / window.innerHeight }
    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 8) == 0} onChange={(x) => { wps.DecalFlags.set(x ? wps.DecalFlags.value & ~8 : wps.DecalFlags.value | 8) }} label={T_supportSurfaceAreas} />
            <VanillaWidgets.instance.ToggleField value={(wps.DecalFlags.value & 4) != 0} onChange={(x) => { wps.DecalFlags.set(!x ? wps.DecalFlags.value & ~4 : wps.DecalFlags.value | 4) }} label={T_supportDecals} />
        </Panel>
    </Portal>;
} 
