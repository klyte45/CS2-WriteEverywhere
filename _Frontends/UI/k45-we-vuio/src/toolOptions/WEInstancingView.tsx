import { FormulaeEditRow } from "common/FormulaeEditRow";
import { Panel, Portal, Tooltip } from "cs2/ui";
import { useEffect, useState } from "react";
import { ArrayInstancingAxisOrder, WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import { WETextValueSettings } from "./WETextValueSettings";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { LocElementType, VanillaComponentResolver, VanillaWidgets, VectorSection } from "@klyte45/vuio-commons";
import { ObjectTyped } from "object-typed";



export const WEInstancingView = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("instancingSettings.title"); //"Appearance Settings"
    const T_showWhenLabel = translate("instancingSettings.showWhenLabel"); //"Appearance Settings"
    const T_showWhenTooltip = translate("instancingSettings.showWhenTooltip"); //"Appearance Settings"
    const T_showWhenAlways = translate("instancingSettings.showWhenAlways"); //"Appearance Settings"
    const T_arraysOnlyForTemplates = translate("instancingSettings.arraysOnlyForTemplates"); //"Appearance Settings"
    const T_arrayTitle = translate("instancingSettings.arrayTitle"); //"Appearance Settings"
    const T_arrayInstances = translate("instancingSettings.arrayInstances"); //"Appearance Settings"
    const T_arrayDistanceCm = translate("instancingSettings.arrayDistanceCm"); //"Appearance Settings"
    const T_arrayGrowthOrder = translate("instancingSettings.arrayGrowthOrder"); //"Appearance Settings"
    const T_arrayInfo = translate("instancingSettings.arrayInfo"); //"Appearance Settings"

    const [buildIdx, setBuildIdx] = useState(0);


    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<ArrayInstancingAxisOrder>();
    const wps = WorldPickerService.instance.bindingList;

    useEffect(() => {
        wps.picker.CurrentSubEntity.subscribe(async () => setBuildIdx(buildIdx + 1))
    }, [buildIdx, wps.picker.CurrentSubEntity.value])

    useEffect(() => {

    }, [buildIdx])

    const defaultPosition = props.initialPosition ?? { x: 600 / window.innerWidth, y: 100 / window.innerHeight }
    const arrayToXYZ = (x: number[]) => ({ x: x[0], y: x[1], z: x[2] })
    const xyzToArray = (x: { x: number, y: number, z: number }): [number, number, number] => [x.x, x.y, x.z]
    return <Portal>
        <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel k45_contentFillPanel" initialPosition={defaultPosition} style={{ height: "400rem", display: "flex", flexDirection: "column" }}
            contentClassName="k45_variablesListWindow">
            <FormulaeEditRow
                formulaeModule="transform" formulaeField="MustDrawFn" label={T_showWhenLabel} defaultInputField={T_showWhenAlways}
                isUsingFormulae={wps.transform.UseFormulaeToCheckIfDraw.value} onToggleFormulaeUse={(x) => wps.transform.UseFormulaeToCheckIfDraw.set(x)}
            />
            {wps.transform.UseFormulaeToCheckIfDraw.value && <div style={{ color: "var(--textColorDim)", fontSize: "var(--fontSizeXS)", textAlign: "center", display: "flex", alignContent: "center", justifyContent: "center" }}>{T_showWhenTooltip}</div>}
            <hr />
            <h4>{T_arrayTitle}</h4>
            {wps.mesh.TextSourceType.value != WESimulationTextType.Placeholder ? <>
                {T_arraysOnlyForTemplates}
            </> : <>
                <VanillaComponentResolver.instance.Int3Input
                    label={T_arrayInstances}
                    value={arrayToXYZ(wps.transform.ArrayInstancing.value)}
                    min={arrayToXYZ([1, 1, 1])}
                    max={arrayToXYZ([100, 100, 100])}
                    onChange={(v) => wps.transform.ArrayInstancing.set(xyzToArray(v))}
                />
                <VanillaComponentResolver.instance.Float3Input
                    label={T_arrayDistanceCm}
                    value={arrayToXYZ(wps.transform.ArrayInstancingGapMeters.value.map(x => x * 100))}
                    onChange={(v) => wps.transform.ArrayInstancingGapMeters.set(xyzToArray(v).map(x => x / 100) as any)}
                />
                <EditorItemRow label={T_arrayGrowthOrder}>
                    <DropdownField
                        value={wps.transform.ArrayAxisGrowthOrder.value}
                        items={ObjectTyped.entries(ArrayInstancingAxisOrder).filter(x => typeof x[1] == "number")?.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("instancingSettings.arrayGrowthOrder." + x[0]) || "<DEFAULT>" }, value: x[1] } })}
                        onChange={(x) => wps.transform.ArrayAxisGrowthOrder.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                <EditorItemRow>{T_arrayInfo}</EditorItemRow>
            </>}
        </Panel>
    </Portal>;
}
