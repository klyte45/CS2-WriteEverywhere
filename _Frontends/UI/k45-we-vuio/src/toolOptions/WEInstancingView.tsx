import { LocElementType, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { FormulaeEditRow } from "common/FormulaeEditRow";
import { FocusDisabled } from "cs2/input";
import { Panel, Portal } from "cs2/ui";
import { ObjectTyped } from "object-typed";
import { useEffect, useState } from "react";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { ArrayInstancingAxisOrder, WEPlacementAlignment, WEZPlacementPivot, WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";



export const WEInstancingView = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("instancingSettings.title");
    const T_showWhenLabel = translate("instancingSettings.showWhenLabel");
    const T_showWhenTooltip = translate("instancingSettings.showWhenTooltip");
    const T_showWhenAlways = translate("instancingSettings.showWhenAlways");
    const T_arraysOnlyForTemplates = translate("instancingSettings.arraysOnlyForTemplates");
    const T_arrayTitle = translate("instancingSettings.arrayTitle");
    const T_arrayInstances = translate("instancingSettings.arrayInstances");
    const T_arrayDistanceCm = translate("instancingSettings.arrayDistanceCm");
    const T_arrayGrowthOrder = translate("instancingSettings.arrayGrowthOrder");
    const T_arrayInfo = translate("instancingSettings.arrayInfo");

    const T_totalInstancesCount = translate("instancingSettings.totalInstancesCount");
    const T_zPivot = translate("instancingSettings.zPivot");
    const T_alignmentByAxis = translate("instancingSettings.axisAlignment");


    const [buildIdx, setBuildIdx] = useState(0);


    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const IntInput = VanillaWidgets.instance.IntInputStandalone;
    const editorModule = VanillaWidgets.instance.editorItemModule;
    const DropdownFieldAxisOrder = VanillaWidgets.instance.DropdownField<ArrayInstancingAxisOrder>();
    const DropdownFieldPivotZ = VanillaWidgets.instance.DropdownField<WEZPlacementPivot>();
    const DropdownFieldAlignment = VanillaWidgets.instance.DropdownField<WEPlacementAlignment>();
    const wps = WorldPickerService.instance.bindingList;

    useEffect(() => {
        wps.picker.CurrentSubEntity.subscribe(async () => setBuildIdx(buildIdx + 1))
    }, [buildIdx, wps.picker.CurrentSubEntity.value])

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
                <FormulaeEditRow
                    formulaeModule="transform" formulaeField="InstanceCount" label={T_totalInstancesCount} defaultInputField={<IntInput
                        className={editorModule.input}
                        min={-1}
                        max={256}
                        value={wps.transform.InstanceCount.value}
                        onChange={(x) => { wps.transform.InstanceCount.set(x) }}
                    />}
                />
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
                    <DropdownFieldAxisOrder
                        value={wps.transform.ArrayAxisGrowthOrder.value}
                        items={ObjectTyped.entries(ArrayInstancingAxisOrder).filter(x => typeof x[1] == "number")?.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("instancingSettings.arrayGrowthOrder." + x[0]) || "<DEFAULT>" }, value: x[1] } })}
                        onChange={(x) => wps.transform.ArrayAxisGrowthOrder.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                <EditorItemRow label={T_zPivot}>
                    <DropdownFieldPivotZ
                        value={wps.transform.PivotZ.value}
                        items={ObjectTyped.entries(WEZPlacementPivot).filter(x => typeof x[1] == "number")?.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("instancingSettings.pivotZ." + x[0]) || "<DEFAULT>" }, value: x[1] } })}
                        onChange={(x) => wps.transform.PivotZ.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                <EditorItemRow label={T_alignmentByAxis} className="buttonShrink">
                    <FocusDisabled>
                        <DropdownFieldAlignment
                            value={wps.transform.AlignmentX.value}
                            items={ObjectTyped.entries(WEPlacementAlignment).filter(x => typeof x[1] == "number")?.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("instancingSettings.placingAlignment." + x[0]) || "<DEFAULT>" }, value: x[1] } })}
                            onChange={(x) => wps.transform.AlignmentX.set(x)}
                        />
                        <DropdownFieldAlignment
                            value={wps.transform.AlignmentY.value}
                            items={ObjectTyped.entries(WEPlacementAlignment).filter(x => typeof x[1] == "number")?.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("instancingSettings.placingAlignment." + x[0]) || "<DEFAULT>" }, value: x[1] } })}
                            onChange={(x) => wps.transform.AlignmentY.set(x)}
                        />
                        <DropdownFieldAlignment
                            value={wps.transform.AlignmentZ.value}
                            items={ObjectTyped.entries(WEPlacementAlignment).filter(x => typeof x[1] == "number")?.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("instancingSettings.placingAlignment." + x[0]) || "<DEFAULT>" }, value: x[1] } })}
                            onChange={(x) => wps.transform.AlignmentZ.set(x)}
                        />
                    </FocusDisabled>
                </EditorItemRow>
                <EditorItemRow>{T_arrayInfo}</EditorItemRow>
            </>}
        </Panel>
    </Portal>;
}
/*
    PivotZ: MultiUIValueBinding<WEZPlacementPivot>,
    AlignmentX: MultiUIValueBinding<WEPlacementAlignment>,
    AlignmentY: MultiUIValueBinding<WEPlacementAlignment>,
    AlignmentZ: MultiUIValueBinding<WEPlacementAlignment>,
    InstanceCount: MultiUIValueBinding<number>,
    InstanceCountFormulaeStr: MultiUIValueBinding<string>,
    InstanceCountFormulaeCompileResult: MultiUIValueBinding<number>,
    InstanceCountFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    */