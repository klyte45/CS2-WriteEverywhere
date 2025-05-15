import { AmountValueSection, Entity, VanillaComponentResolver, VanillaFnResolver, VectorSectionEditable } from "@klyte45/vuio-commons";
import { useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import { ModuleRegistryExtend } from "cs2/modding";
import { useCallback, useEffect, useState } from "react";
import { WEPlacementPivot, WorldPickerService } from "services/WorldPickerService";
import { translate } from "../utils/translate";
import { WETextAppearenceSettings } from "./WETextAppearenceSettings";
import { WETextValueSettings } from "./WETextValueSettings";
import { WETextHierarchyView } from "./WETextHierarchyView";
import { WETextShaderProperties } from "./WETextShaderProperties";
import { WEFormulaeEditor } from "./WEFormulaeEditor";
import i_debug from "../images/debug.svg"
import { WEDebugWindow } from "./WEDebugWindow";
import { ObjectTyped } from "object-typed";
import { WELayoutVariablesView } from "./WELayoutVariablesView";
import { WEInstancingView } from "./WEInstancingView";

const precisions = [1, 1 / 2, 1 / 4, 1 / 10, 1 / 20, 1 / 40, 1 / 100, 1 / 200, 1 / 400, 1 / 1000]

const i_ProjectionCube = "coui://uil/Standard/Cube.svg";
const i_XYplaneIcon = "coui://uil/Standard/BoxFront.svg";
const i_ZYplaneIcon = "coui://uil/Standard/BoxSide.svg";
const i_XZplaneIcon = "coui://uil/Standard/BoxTop.svg";
const i_UnselectCurrentIcon = "coui://uil/Standard/PickerPipette.svg";
const i_cameraIcon = "coui://uil/Standard/VideoCamera.svg";
const i_moveModeAll = "coui://uil/Standard/ArrowsMoveAll.svg";
const i_moveModeHorizontal = "coui://uil/Standard/ArrowsMoveLeftRight.svg";
const i_moveModeVertical = "coui://uil/Standard/ArrowsMoveUpDown.svg";
const i_AppearenceBtnIcon = "coui://uil/Standard/ColorPalette.svg";
const i_ShaderBtnIcon = "coui://uil/Standard/HouseAlternative.svg";
const i_VariablesBtnIcon = "coui://uil/Standard/ExclamationMark.svg";
const i_InstancingBtnIcon = "coui://uil/Standard/SameRhombus.svg";


const i_unselectedPivot = "coui://uil/Standard/Circle.svg";
const i_selectedPivot = "coui://uil/Standard/CircleXClose.svg";

const iarr_moveMode = [i_moveModeAll, i_moveModeHorizontal, i_moveModeVertical]


export const WriteEverywhereToolOptionsVisibility: ModuleRegistryExtend = (Component: any) => {
    return () => Component() || tool.activeTool$.value.id == "K45_WE_WEWorldPickerTool"
}

export const WriteEverywhereToolOptions: ModuleRegistryExtend = (Component: any) => {
    return () => {
        const toolActive = useValue(tool.activeTool$).id == "K45_WE_WEWorldPickerTool";

        const result = Component();
        if (toolActive) {
            result.props.children ??= []
            result.props.children.unshift(<WEWorldPickerToolPanel />);
        }
        return result;
    };
}
const WEWorldPickerToolPanel = () => {
    //Labels and tooltips
    const L_itemName = translate("toolOption.itemName"); //"Name";
    const L_mousePrecision = translate("toolOption.mousePrecision"); //"Mouse precision";
    const L_editingPlane = translate("toolOption.editingPlane"); //"Editing plane";
    const L_pivot = translate("toolOption.pivot"); //"Position"
    const L_position = translate("toolOption.position"); //"Position"
    const L_rotation = translate("toolOption.rotation"); //"Rotation"
    const L_actions = translate("toolOption.actions"); //"Actions"
    const L_selectItem = translate("toolOption.selectItem"); //"Select an Item"
    const T_mousePrecision_up = translate("toolOption.mousePrecision_up.tooltip"); //"Increment the strenght of the mouse moves when editing the text position/rotation";
    const T_mousePrecision_down = translate("toolOption.mousePrecision_down.tooltip"); //"Decrease the strenght of the mouse moves when editing the text position/rotation";
    const T_editingPlane_XY = translate("toolOption.editingPlane_XY.tooltip"); //"move in XY, rotate in Z (front)"
    const T_editingPlane_ZY = translate("toolOption.editingPlane_ZY.tooltip"); //"move in ZY, rotate in X (right)"
    const T_editingPlane_XZ = translate("toolOption.editingPlane_XZ.tooltip"); //"move in XZ, rotate in Y (top)"
    const T_ProjectionCube = translate("toolOption.decalProjectionCube.tooltip"); //"move in XZ, rotate in Y (top)"
    const T_picker = translate("toolOption.picker.tooltip"); //"Pick another object"
    const T_lockCamera = translate("toolOption.lockCamera.tooltip"); //"Lock camera to editing plane area and angle"
    const T_AppearenceBtn = translate("toolOption.AppearenceBtn.tooltip"); //"Appearance settings"
    const T_VariablesBtn = translate("toolOption.VariablesBtn.tooltip");
    const T_InstancingBtn = translate("toolOption.InstancingBtn.tooltip");
    const T_ShaderBtn = translate("toolOption.ShaderBtn.tooltip");//"Shader settings"
    const T_pivot_Left = translate("toolOption.pivot_Left.tooltip"); //"move in XY, rotate in Z (front)"
    const T_pivot_Center = translate("toolOption.pivot_Center.tooltip"); //"move in ZY, rotate in X (right)"
    const T_pivot_Right = translate("toolOption.pivot_Right.tooltip"); //"move in XZ, rotate in Y (top)"
    const T_pivot_Top = translate("toolOption.pivot_Top.tooltip"); //"move in XY, rotate in Z (front)"
    const T_pivot_Middle = translate("toolOption.pivot_Middle.tooltip"); //"move in ZY, rotate in X (right)"
    const T_pivot_Bottom = translate("toolOption.pivot_Bottom.tooltip"); //"move in XZ, rotate in Y (top)"

    const descriptionPivotPosition: Record<WEPlacementPivot, string> = {
        [WEPlacementPivot.TopLeft]: `${T_pivot_Top}, ${T_pivot_Left}`,
        [WEPlacementPivot.TopCenter]: `${T_pivot_Top}, ${T_pivot_Center}`,
        [WEPlacementPivot.TopRight]: `${T_pivot_Top}, ${T_pivot_Right}`,
        [WEPlacementPivot.MiddleLeft]: `${T_pivot_Middle}, ${T_pivot_Left}`,
        [WEPlacementPivot.MiddleCenter]: `${T_pivot_Middle}, ${T_pivot_Center}`,
        [WEPlacementPivot.MiddleRight]: `${T_pivot_Middle}, ${T_pivot_Right}`,
        [WEPlacementPivot.BottomLeft]: `${T_pivot_Bottom}, ${T_pivot_Left}`,
        [WEPlacementPivot.BottomCenter]: `${T_pivot_Bottom}, ${T_pivot_Center}`,
        [WEPlacementPivot.BottomRight]: `${T_pivot_Bottom}, ${T_pivot_Right}`,
    }


    const Tarr_moveMode = [
        `${translate("toolOption.moveMode.tooltip")} ${translate("toolOption.moveMode.descriptionBoth")}`,// "Toggle between modes to lock/unlock a axis in current plane. Currently: Move in any direction",
        `${translate("toolOption.moveMode.tooltip")} ${translate("toolOption.moveMode.descriptionHorizontal")}`,// "Toggle between modes to lock/unlock a axis in current plane. Currently: Move horizontally only",
        `${translate("toolOption.moveMode.tooltip")} ${translate("toolOption.moveMode.descriptionVertical")}`,// "Toggle between modes to lock/unlock a axis in current plane. Currently: Move vertically only"
    ]

    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setTimeout(() => setBuild(buildIdx + 1), 100))
        return () => WorldPickerService.instance.disposeBindings()
    }, [buildIdx])

    const [displayAppearenceWindow, setDisplayAppearenceWindow] = useState(false);
    const [displayShaderWindow, setDisplayShaderWindow] = useState(false);
    const [displayVariablesWindow, setDisplayVariablesWindow] = useState(false);
    const [displayInstancingWindow, setDisplayInstancingWindow] = useState(false);
    const [displayDebugWindow, setDisplayDebugWindow] = useState(false);
    const [debugAvailable, setDebugAvailable] = useState(false);

    useEffect(() => {
        WorldPickerService.debugAvailable().then(setDebugAvailable);
    }, []);

    const wps = WorldPickerService.instance.bindingList.picker;
    const main = WorldPickerService.instance.bindingList.main;
    const material = WorldPickerService.instance.bindingList.material;
    const transform = WorldPickerService.instance.bindingList.transform;
    const Locale = VanillaFnResolver.instance.localization.useCachedLocalization();
    const decimalsFormat = (value: number) => VanillaFnResolver.instance.localizedNumber.formatFloat(Locale, value, false, 3, true, false, Infinity);

    const currentItemIsValid = wps.CurrentSubEntity.value?.Index != 0;

    const [clipboard, setClipboard] = useState(undefined as Entity | undefined | null)

    const currentEditingFormulaeDefaultValue = useCallback(() => WorldPickerService.instance.getCurrentEditingFormulaeValueField(), [
        WorldPickerService.instance.currentFormulaeField,
        WorldPickerService.instance.currentFormulaeModule,
    ])
    const currentEditingFormulaeStr = useCallback(() => WorldPickerService.instance.getCurrentEditingFormulaeFn(), [
        WorldPickerService.instance.currentFormulaeField,
        WorldPickerService.instance.currentFormulaeModule,
    ])
    const currentEditingFormulaeResult = useCallback(() => WorldPickerService.instance.getCurrentEditingFormulaeFnResult(), [
        WorldPickerService.instance.currentFormulaeField,
        WorldPickerService.instance.currentFormulaeModule,
    ])
    const getCurrentEditingFormulaeType = useCallback(() => {
        switch (typeof currentEditingFormulaeDefaultValue()?.value) {
            case 'number': return "number";
            case 'string': return "string";
            case 'object': return "color";
            default: return null;
        }
    }, [currentEditingFormulaeDefaultValue()])


    return !wps.CurrentEntity.value?.Index ?
        <VanillaComponentResolver.instance.Section title={L_selectItem} children={[]} /> :
        <>

            {currentItemIsValid &&
                <>
                    <VectorSectionEditable title={L_itemName}
                        valueGetter={() => [main.CurrentItemName.value]}
                        valueGetterFormatted={() => [main.CurrentItemName.value]}
                        onValueChanged={(i, x) => {
                            main.CurrentItemName.set(x);
                        }} />

                    <AmountValueSection
                        widthContent={120}
                        valueGetter={() => `${decimalsFormat(precisions[wps.MouseSensibility.value])} m  | ${decimalsFormat(precisions[wps.MouseSensibility.value] * 10)}°`}
                        title={L_mousePrecision}
                        up={{
                            tooltip: T_mousePrecision_up,
                            onSelect: () => wps.MouseSensibility.set(wps.MouseSensibility.value - 1),
                            disabledFn: () => wps.MouseSensibility.value <= 0
                        }}
                        down={{
                            tooltip: T_mousePrecision_down,
                            onSelect: () => wps.MouseSensibility.set(wps.MouseSensibility.value + 1),
                            disabledFn: () => wps.MouseSensibility.value >= precisions.length - 1
                        }}
                    />
                    <VanillaComponentResolver.instance.Section title={L_editingPlane}>
                        {material.ShaderType.value == 2 && <>
                            <VanillaComponentResolver.instance.ToolButton selected={wps.ShowProjectionCube.value} onSelect={() => wps.ShowProjectionCube.set(!wps.ShowProjectionCube.value)} src={i_ProjectionCube} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_ProjectionCube}></VanillaComponentResolver.instance.ToolButton>
                            <div style={{ width: "10rem" }}></div>
                        </>}
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentPlaneMode.value == 0} onSelect={() => wps.CurrentPlaneMode.set(0)} src={i_XYplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_XY}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentPlaneMode.value == 1} onSelect={() => wps.CurrentPlaneMode.set(1)} src={i_ZYplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_ZY}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentPlaneMode.value == 2} onSelect={() => wps.CurrentPlaneMode.set(2)} src={i_XZplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_XZ}></VanillaComponentResolver.instance.ToolButton>
                        <div style={{ width: "10rem" }}></div>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentMoveMode.value > 0} onSelect={() => wps.CurrentMoveMode.set((wps.CurrentMoveMode.value + 1) % 3)} src={iarr_moveMode[wps.CurrentMoveMode.value]} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={Tarr_moveMode[wps.CurrentMoveMode.value]}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CameraLocked.value} onSelect={() => wps.CameraLocked.set(!wps.CameraLocked.value)} src={i_cameraIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_lockCamera}></VanillaComponentResolver.instance.ToolButton>
                    </VanillaComponentResolver.instance.Section>
                    <VanillaComponentResolver.instance.Section title={L_pivot}>
                        {ObjectTyped.values(WEPlacementPivot)
                            .filter(x => typeof x == "number")
                            .map(x => <>
                                <VanillaComponentResolver.instance.ToolButton
                                    key={x}
                                    tooltip={descriptionPivotPosition[x]}
                                    selected={transform.Pivot.value == x}
                                    onSelect={() => transform.Pivot.set(x)}
                                    src={transform.Pivot.value == x ? i_selectedPivot : i_unselectedPivot}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                    className={VanillaComponentResolver.instance.toolButtonTheme.button} />
                                {(x & 3) == 2 ? <div style={{ flexBasis: "100%" }} /> : <></>}
                            </>)
                        }

                    </VanillaComponentResolver.instance.Section>
                    <VectorSectionEditable title={L_position}
                        valueGetter={() => transform.CurrentPosition.value?.map(x => x.toFixed(3))}
                        valueGetterFormatted={() => transform.CurrentPosition.value?.map(x => decimalsFormat(x) + "m")}
                        onValueChanged={(i, x) => {
                            const newVal = transform.CurrentPosition.value;
                            newVal[i] = parseFloat(x);
                            if (isNaN(newVal[i])) return;
                            transform.CurrentPosition.set(newVal);
                        }} />
                    <VectorSectionEditable title={L_rotation}
                        valueGetter={() => transform.CurrentRotation.value?.map(x => x.toFixed(3))}
                        valueGetterFormatted={() => transform.CurrentRotation.value?.map(x => decimalsFormat(x) + "°")}
                        onValueChanged={(i, x) => {
                            const newVal = transform.CurrentRotation.value;
                            newVal[i] = parseFloat(x);
                            if (isNaN(newVal[i])) return;
                            transform.CurrentRotation.set(newVal);
                        }} />
                </>}


            <VanillaComponentResolver.instance.Section title={L_actions} >
                <>
                    {currentItemIsValid && <>
                        {debugAvailable && <>
                            <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayDebugWindow(!displayDebugWindow)} selected={displayDebugWindow} src={i_debug} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={"DEBUG"} />
                            <div style={{ width: "20rem", flexShrink: 1 }}></div>
                        </>
                        }
                        <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayInstancingWindow(!displayInstancingWindow)} selected={displayInstancingWindow} src={i_InstancingBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_InstancingBtn} />
                        <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayVariablesWindow(!displayVariablesWindow)} selected={displayVariablesWindow} src={i_VariablesBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_VariablesBtn} />
                        <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayShaderWindow(!displayShaderWindow)} selected={displayShaderWindow} src={i_ShaderBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_ShaderBtn} />
                        <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayAppearenceWindow(!displayAppearenceWindow)} selected={displayAppearenceWindow} src={i_AppearenceBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_AppearenceBtn} />
                        <div style={{ width: "10rem" }}></div>
                    </>
                    }
                    <VanillaComponentResolver.instance.ToolButton onSelect={() => wps.CurrentEntity.set(null)} src={i_UnselectCurrentIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_picker} />
                </>
            </VanillaComponentResolver.instance.Section >
            {currentItemIsValid && displayAppearenceWindow && <WETextAppearenceSettings />}
            {currentItemIsValid && displayShaderWindow && <WETextShaderProperties />}
            {currentItemIsValid && displayVariablesWindow && <WELayoutVariablesView />}
            {currentItemIsValid && displayInstancingWindow && <WEInstancingView />}
            {currentItemIsValid && <WETextValueSettings />}
            {debugAvailable && currentItemIsValid && displayDebugWindow && <WEDebugWindow />}
            {<WETextHierarchyView clipboard={clipboard} setClipboard={setClipboard} />}
            {currentEditingFormulaeStr() && <WEFormulaeEditor formulaeStr={currentEditingFormulaeStr()!} formulaeType={getCurrentEditingFormulaeType()!} lastCompileStatus={currentEditingFormulaeResult()!} />}
        </>

}



