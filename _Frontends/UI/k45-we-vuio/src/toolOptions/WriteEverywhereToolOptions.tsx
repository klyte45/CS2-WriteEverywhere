import { AmountValueSection, MetricUnitsEntries, MultiUIValueBinding, UnitSystem, VanillaComponentResolver, VectorSection, VectorSectionEditable, translateUnitResult } from "@klyte45/vuio-commons";
import { useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import { ModuleRegistryExtend, getModule } from "cs2/modding";
import { Component, useEffect, useState } from "react";

type number3 = [number, number, number]

type Entity = {
    Index: number,
    Version: number
}

const precisions = [1, 1 / 2, 1 / 4, 1 / 10, 1 / 20, 1 / 40, 1 / 100, 1 / 200, 1 / 400, 1 / 1000]

const i_XYplaneIcon = "coui://uil/Standard/BoxFront.svg";
const i_ZYplaneIcon = "coui://uil/Standard/BoxSide.svg";
const i_XZplaneIcon = "coui://uil/Standard/BoxTop.svg";
const i_UnselectCurrentIcon = "coui://uil/Standard/PickerPipette.svg";
const i_AddItemIcon = "coui://uil/Standard/Plus.svg";
const i_removeItemIcon = "coui://uil/Standard/Minus.svg";
const i_cameraIcon = "coui://uil/Standard/VideoCamera.svg";
const i_moveModeAll = "coui://uil/Standard/Plus.svg";
const i_moveModeHorizontal = "coui://uil/Standard/ArrowRight.svg";
const i_moveModeVertical = "coui://uil/Standard/ArrowUp.svg";

const iarr_moveMode = [i_moveModeAll, i_moveModeHorizontal, i_moveModeVertical]



const addItem = () => engine.call("k45::we.wpicker.addItem");
const removeItem = () => engine.call("k45::we.wpicker.removeItem");

let CurrentItemName: MultiUIValueBinding<string>
let CurrentItemIdx: MultiUIValueBinding<number>
let CurrentEntity: MultiUIValueBinding<Entity | null>
let CurrentScale: MultiUIValueBinding<number3>
let CurrentRotation: MultiUIValueBinding<number3>
let CurrentPosition: MultiUIValueBinding<number3>
let MouseSensibility: MultiUIValueBinding<number>
let CurrentPlaneMode: MultiUIValueBinding<number>
let CurrentItemCount: MultiUIValueBinding<number>
let CurrentItemText: MultiUIValueBinding<string>
let CurrentItemIsValid: MultiUIValueBinding<string>
let CameraLocked: MultiUIValueBinding<boolean>
let CurrentMoveMode: MultiUIValueBinding<number>
const Bindings: MultiUIValueBinding<any>[] = []

function initBindings(x: Component) {
    CurrentItemName ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemName")
    CurrentItemIdx ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentItemIdx")
    CurrentEntity ??= new MultiUIValueBinding<Entity | null>("k45::we.wpicker.CurrentEntity")
    CurrentScale ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentScale")
    CurrentRotation ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentRotation")
    CurrentPosition ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentPosition")
    MouseSensibility ??= new MultiUIValueBinding<number>("k45::we.wpicker.MouseSensibility")
    CurrentPlaneMode ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentPlaneMode")
    CurrentItemText ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemText")
    CurrentItemIsValid ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemIsValid")
    CurrentItemCount ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentItemCount")
    CameraLocked ??= new MultiUIValueBinding<boolean>("k45::we.wpicker.CameraLocked")
    CurrentMoveMode ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentMoveMode")
    Bindings.length = 0;
    Bindings.push(
        CurrentItemIdx,
        CurrentScale,
        CurrentRotation,
        CurrentPosition,
        MouseSensibility,
        CurrentPlaneMode,
        CurrentItemText,
        CurrentItemIsValid,
        CurrentEntity,
        CurrentItemName,
        CurrentItemCount,
        CameraLocked,
        CurrentMoveMode
    );

    Bindings.map(y => {
        y.reactivate();
        y.subscribe(async () => x.setState({}));
    })
}

function disposeBindings() {
    Bindings.map(y => {
        y.dispose();
    })
}


const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");

// This is working, but it's possible a better solution is possible.
export function descriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null): JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}
export const WriteEverywhereToolOptionsVisibility: ModuleRegistryExtend = (Component: any) => {
    return () => Component() || tool.activeTool$.value.id == "K45_WE_WEWorldPickerTool"
}

export const WriteEverywhereToolOptions: ModuleRegistryExtend = (Component: any) => {
    return (props) => {
        const { children, ...otherProps } = props || {};
        //  console.log("AAAAAAAA");

        // These get the value of the bindings.
        const toolActive = useValue(tool.activeTool$).id == "K45_WE_WEWorldPickerTool";

        var result = Component();
        if (toolActive) {
            result.props.children ??= []
            result.props.children.unshift(<WEWorldPickerToolPanel />);
        }
        return result;
    };
}


class WEWorldPickerToolPanel extends Component {

    constructor(props: any) {
        super(props);
        this.state = {
            itemsAvailable: []
        }
        initBindings(this)
    }

    componentWillUnmount(): void {
        disposeBindings()
    }

    render() {

        //Labels and tooltips
        const L_itemTitle = "Text #"
        const L_itemName = "Name";
        const L_mousePrecision = "Mouse precision";
        const L_editingPlane = "Editing plane";
        const L_position = "Position"
        const L_rotation = "Rotation"
        const L_scale = "Scale"
        const L_text = "Text"
        const L_actions = "Actions"
        const L_selectItem = "Select an Item"

        const T_mousePrecision_up = "Increment the strenght of the mouse moves when editing the text position/rotation";
        const T_mousePrecision_down = "Decrease the strenght of the mouse moves when editing the text position/rotation";
        const T_editingPlane_XY = "move in XY, rotate in Z (front)"
        const T_editingPlane_ZY = "move in ZY, rotate in X (right)"
        const T_editingPlane_XZ = "move in XZ, rotate in Y (top)"
        const T_picker = "Pick another object"
        const T_addText = "Add text"
        const T_removeText = "Remove text"
        const T_lockCamera = "Lock camera to editing plane area and angle"

        const Tarr_moveMode = [
            "Toggle between modes to lock/unlock a axis in current plane.\nCurrently: Move in any direction",
            "Toggle between modes to lock/unlock a axis in current plane.\nCurrently: Move horizontally only",
            "Toggle between modes to lock/unlock a axis in current plane.\nCurrently: Move vertically only"
        ]

        return !CurrentEntity.value?.Index ?
            <VanillaComponentResolver.instance.Section title={L_selectItem} children={[]} /> :
            <>
                <AmountValueSection
                    title={L_itemTitle}
                    valueGetter={() => `${CurrentItemIdx.value + 1}/${CurrentItemCount.value}`}
                    up={{
                        onSelect: () => CurrentItemIdx.set((CurrentItemIdx.value + 1) % CurrentItemCount.value),
                        disabledFn: () => !CurrentItemCount.value
                    }}
                    down={{
                        onSelect: () => CurrentItemIdx.set((CurrentItemIdx.value + CurrentItemCount.value - 1) % CurrentItemCount.value),
                        disabledFn: () => !CurrentItemCount.value
                    }}
                    actions={[
                        {
                            icon: i_removeItemIcon,
                            tooltip: T_removeText,
                            onSelect: () => removeItem(),
                            disabledFn: () => CurrentItemIdx.value >= CurrentItemCount.value
                        },
                        {
                            icon: i_AddItemIcon,
                            tooltip: T_addText,
                            onSelect: () => addItem()
                        },
                    ]}
                />

                {CurrentItemIdx.value < CurrentItemCount.value &&
                    <>
                        <VectorSectionEditable title={L_itemName}
                            valueGetter={() => [CurrentItemName.value]}
                            valueGetterFormatted={() => [CurrentItemName.value]}
                            onValueChanged={(i, x) => {
                                CurrentItemName.set(x);
                            }} />

                        <AmountValueSection
                            widthContent={120}
                            valueGetter={() => translateUnitResult([MetricUnitsEntries.distance.linear[UnitSystem.Metric][0], { VALUE: precisions[MouseSensibility.value] + "" }]) + ` | ${(precisions[MouseSensibility.value] * 10).toFixed(2)}°`}
                            title={L_mousePrecision}
                            up={{
                                tooltip: T_mousePrecision_up,
                                onSelect: () => MouseSensibility.set(MouseSensibility.value - 1),
                                disabledFn: () => MouseSensibility.value <= 0
                            }}
                            down={{
                                tooltip: T_mousePrecision_down,
                                onSelect: () => MouseSensibility.set(MouseSensibility.value + 1),
                                disabledFn: () => MouseSensibility.value >= precisions.length - 1
                            }}
                        />
                        <VanillaComponentResolver.instance.Section title={L_editingPlane}>
                            <VanillaComponentResolver.instance.ToolButton selected={CurrentPlaneMode.value == 0} onSelect={() => CurrentPlaneMode.set(0)} src={i_XYplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_XY}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton selected={CurrentPlaneMode.value == 1} onSelect={() => CurrentPlaneMode.set(1)} src={i_ZYplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_ZY}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton selected={CurrentPlaneMode.value == 2} onSelect={() => CurrentPlaneMode.set(2)} src={i_XZplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_XZ}></VanillaComponentResolver.instance.ToolButton>
                            <div style={{ width: "10rem" }}></div>
                            <VanillaComponentResolver.instance.ToolButton selected={CurrentMoveMode.value > 0} onSelect={() => CurrentMoveMode.set((CurrentMoveMode.value + 1) % 3)} src={iarr_moveMode[CurrentMoveMode.value]} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={Tarr_moveMode[CurrentMoveMode.value]}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton selected={CameraLocked.value} onSelect={() => CameraLocked.set(!CameraLocked.value)} src={i_cameraIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_lockCamera}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                        <VectorSectionEditable title={L_position}
                            valueGetter={() => CurrentPosition.value?.map(x => x.toFixed(3))}
                            valueGetterFormatted={() => CurrentPosition.value?.map(x => translateUnitResult([MetricUnitsEntries.distance.linear[UnitSystem.Metric][0], { VALUE: x.toFixed(3) + "" }]))}
                            onValueChanged={(i, x) => {
                                const newVal = CurrentPosition.value;
                                newVal[i] = parseFloat(x);
                                if (isNaN(newVal[i])) return;
                                CurrentPosition.set(newVal);
                            }} />
                        <VectorSectionEditable title={L_rotation}
                            valueGetter={() => CurrentRotation.value?.map(x => x.toFixed(3))}
                            valueGetterFormatted={() => CurrentRotation.value?.map(x => x.toFixed(3) + "°")}
                            onValueChanged={(i, x) => {
                                const newVal = CurrentRotation.value;
                                newVal[i] = parseFloat(x);
                                if (isNaN(newVal[i])) return;
                                CurrentRotation.set(newVal);
                            }} />
                        <VectorSectionEditable title={L_scale}
                            valueGetter={() => CurrentScale.value?.map(x => x.toFixed(3))}
                            valueGetterFormatted={() => CurrentScale.value?.map(x => x.toFixed(3))}
                            onValueChanged={(i, x) => {
                                const newVal = CurrentScale.value;
                                newVal[i] = parseFloat(x);
                                if (isNaN(newVal[i])) return;
                                CurrentScale.set(newVal);
                            }} />
                        <VectorSectionEditable title={L_text}
                            valueGetter={() => [CurrentItemText.value]}
                            valueGetterFormatted={() => [CurrentItemText.value]}
                            onValueChanged={(i, x) => {
                                CurrentItemText.set(x);
                            }} /></>}


                <VanillaComponentResolver.instance.Section title={L_actions}>
                    <VanillaComponentResolver.instance.ToolButton onSelect={() => CurrentEntity.set(null)} src={i_UnselectCurrentIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_picker} />
                </VanillaComponentResolver.instance.Section>
            </>
    }
}

