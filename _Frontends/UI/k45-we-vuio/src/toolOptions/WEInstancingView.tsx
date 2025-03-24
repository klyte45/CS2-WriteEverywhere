import { FormulaeEditRow } from "common/FormulaeEditRow";
import { Panel, Portal, Tooltip } from "cs2/ui";
import { useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";



export const WEInstancingView = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("instancingSettings.title"); //"Appearance Settings"
    const T_showWhenLabel = translate("instancingSettings.showWhenLabel"); //"Appearance Settings"
    const T_showWhenTooltip = translate("instancingSettings.showWhenTooltip"); //"Appearance Settings"
    const T_showWhenAlways = translate("instancingSettings.showWhenAlways"); //"Appearance Settings"

    const [buildIdx, setBuildIdx] = useState(0);


    const wps = WorldPickerService.instance.bindingList;

    useEffect(() => {
        wps.picker.CurrentSubEntity.subscribe(async () => setBuildIdx(buildIdx + 1))
    }, [buildIdx, wps.picker.CurrentSubEntity.value])

    useEffect(() => {

    }, [buildIdx])

    const defaultPosition = props.initialPosition ?? { x: 600 / window.innerWidth, y: 100 / window.innerHeight }
    return <Portal>
        <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel k45_contentFillPanel" initialPosition={defaultPosition} style={{ height: "400rem", display: "flex", flexDirection: "column" }}
            contentClassName="k45_variablesListWindow">
            <FormulaeEditRow
                formulaeModule="transform" formulaeField="MustDrawFn" label={T_showWhenLabel} defaultInputField={T_showWhenAlways}
                isUsingFormulae={wps.transform.UseFormulaeToCheckIfDraw.value} onToggleFormulaeUse={(x) => wps.transform.UseFormulaeToCheckIfDraw.set(x)}
            />
            {wps.transform.UseFormulaeToCheckIfDraw.value && <div style={{ color: "var(--textColorDim)", fontSize: "var(--fontSizeXS)", textAlign: "center", display: "flex", alignContent: "center", justifyContent: "center" }}>{T_showWhenTooltip}</div>}
        </Panel>
    </Portal>;
}
