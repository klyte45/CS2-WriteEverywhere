import { AmountValueSection, VanillaComponentResolver, VanillaFnResolver, VectorSectionEditable } from "@klyte45/vuio-commons";
import { useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import { getModule, ModuleRegistryExtend } from "cs2/modding";
import { useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "../utils/translate";
import { WETextAppearenceSettings } from "./WETextAppearenceSettings";


const precisions = [1, 1 / 2, 1 / 4, 1 / 10, 1 / 20, 1 / 40, 1 / 100, 1 / 200, 1 / 400, 1 / 1000]

const i_XYplaneIcon = "coui://uil/Standard/BoxFront.svg";
const i_ZYplaneIcon = "coui://uil/Standard/BoxSide.svg";
const i_XZplaneIcon = "coui://uil/Standard/BoxTop.svg";
const i_UnselectCurrentIcon = "coui://uil/Standard/PickerPipette.svg";
const i_AddItemIcon = "coui://uil/Standard/Plus.svg";
const i_removeItemIcon = "coui://uil/Standard/Minus.svg";
const i_cameraIcon = "coui://uil/Standard/VideoCamera.svg";
const i_moveModeAll = "coui://uil/Standard/ArrowsMoveAll.svg";
const i_moveModeHorizontal = "coui://uil/Standard/ArrowsMoveLeftRight.svg";
const i_moveModeVertical = "coui://uil/Standard/ArrowsMoveUpDown.svg";
const i_lockRotationView = "coui://uil/Standard/ArrowCircularLeft.svg";
const i_AppearenceBtnIcon = "coui://uil/Standard/ColorPalette.svg";

const iarr_moveMode = [i_moveModeAll, i_moveModeHorizontal, i_moveModeVertical]



const addItem = () => engine.call("k45::we.wpicker.addItem");
const removeItem = () => engine.call("k45::we.wpicker.removeItem");


const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");

export const WriteEverywhereToolOptionsVisibility: ModuleRegistryExtend = (Component: any) => {
    return () => Component() || tool.activeTool$.value.id == "K45_WE_WEWorldPickerTool"
}

export const WriteEverywhereToolOptions: ModuleRegistryExtend = (Component: any) => {
    return (props) => {
        const { children, ...otherProps } = props || {};
        const toolActive = useValue(tool.activeTool$).id == "K45_WE_WEWorldPickerTool";

        var result = Component();
        if (toolActive) {
            result.props.children ??= []
            result.props.children.unshift(<WEWorldPickerToolPanel />);
        }
        return result;
    };
}
//Labels and tooltips
const L_itemTitle = translate("toolOption.itemTitle"); //"Text #"
const L_itemName = translate("toolOption.itemName"); //"Name";
const L_mousePrecision = translate("toolOption.mousePrecision"); //"Mouse precision";
const L_editingPlane = translate("toolOption.editingPlane"); //"Editing plane";
const L_position = translate("toolOption.position"); //"Position"
const L_rotation = translate("toolOption.rotation"); //"Rotation"
const L_scale = translate("toolOption.scale"); //"Scale"
const L_text = translate("toolOption.text"); //"Text"
const L_actions = translate("toolOption.actions"); //"Actions"
const L_selectItem = translate("toolOption.selectItem"); //"Select an Item"
const T_mousePrecision_up = translate("toolOption.mousePrecision_up.tooltip"); //"Increment the strenght of the mouse moves when editing the text position/rotation";
const T_mousePrecision_down = translate("toolOption.mousePrecision_down.tooltip"); //"Decrease the strenght of the mouse moves when editing the text position/rotation";
const T_editingPlane_XY = translate("toolOption.editingPlane_XY.tooltip"); //"move in XY, rotate in Z (front)"
const T_editingPlane_ZY = translate("toolOption.editingPlane_ZY.tooltip"); //"move in ZY, rotate in X (right)"
const T_editingPlane_XZ = translate("toolOption.editingPlane_XZ.tooltip"); //"move in XZ, rotate in Y (top)"
const T_picker = translate("toolOption.picker.tooltip"); //"Pick another object"
const T_addText = translate("toolOption.addText.tooltip"); //"Add text"
const T_removeText = translate("toolOption.removeText.tooltip"); //"Remove text"
const T_lockCamera = translate("toolOption.lockCamera.tooltip"); //"Lock camera to editing plane area and angle"
const T_lockRotationView = translate("toolOption.lockRotationView.tooltip"); //"Do not rotate camera along the text"
const T_AppearenceBtn = translate("toolOption.AppearenceBtn.tooltip"); //"Appearance settings"

const Tarr_moveMode = [
    `${translate("toolOption.moveMode.tooltip")} ${translate("toolOption.moveMode.descriptionBoth")}`,// "Toggle between modes to lock/unlock a axis in current plane. Currently: Move in any direction",
    `${translate("toolOption.moveMode.tooltip")} ${translate("toolOption.moveMode.descriptionHorizontal")}`,// "Toggle between modes to lock/unlock a axis in current plane. Currently: Move horizontally only",
    `${translate("toolOption.moveMode.tooltip")} ${translate("toolOption.moveMode.descriptionVertical")}`,// "Toggle between modes to lock/unlock a axis in current plane. Currently: Move vertically only"
]


const WEWorldPickerToolPanel = () => {

    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setTimeout(() => setBuild(buildIdx + 1),100))
        return () => WorldPickerService.instance.disposeBindings()
    }, [buildIdx])

    const [displayAppearenceWindow, setDisplayAppearenceWindow] = useState(false);


    const wps = WorldPickerService.instance;
    const Locale = VanillaFnResolver.instance.localization.useCachedLocalization();
    const decimalsFormat = (value: number) => VanillaFnResolver.instance.localizedNumber.formatFloat(Locale, value, false, 3, true, false, Infinity);


    return !wps.CurrentEntity.value?.Index ?
        <VanillaComponentResolver.instance.Section title={L_selectItem} children={[]} /> :
        <>
            <AmountValueSection
                title={L_itemTitle}
                valueGetter={() => `${wps.CurrentItemIdx.value + 1}/${wps.CurrentItemCount.value}`}
                up={{
                    onSelect: () => wps.CurrentItemIdx.set((wps.CurrentItemIdx.value + 1) % wps.CurrentItemCount.value),
                    disabledFn: () => !wps.CurrentItemCount.value
                }}
                down={{
                    onSelect: () => wps.CurrentItemIdx.set((wps.CurrentItemIdx.value + wps.CurrentItemCount.value - 1) % wps.CurrentItemCount.value),
                    disabledFn: () => !wps.CurrentItemCount.value
                }}
                actions={[
                    {
                        icon: i_removeItemIcon,
                        tooltip: T_removeText,
                        onSelect: () => removeItem(),
                        disabledFn: () => wps.CurrentItemIdx.value >= wps.CurrentItemCount.value
                    },
                    {
                        icon: i_AddItemIcon,
                        tooltip: T_addText,
                        onSelect: () => addItem()
                    },
                ]}
            />

            {wps.CurrentItemIdx.value < wps.CurrentItemCount.value &&
                <>
                    <VectorSectionEditable title={L_itemName}
                        valueGetter={() => [wps.CurrentItemName.value]}
                        valueGetterFormatted={() => [wps.CurrentItemName.value]}
                        onValueChanged={(i, x) => {
                            wps.CurrentItemName.set(x);
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
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentPlaneMode.value == 0} onSelect={() => wps.CurrentPlaneMode.set(0)} src={i_XYplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_XY}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentPlaneMode.value == 1} onSelect={() => wps.CurrentPlaneMode.set(1)} src={i_ZYplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_ZY}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentPlaneMode.value == 2} onSelect={() => wps.CurrentPlaneMode.set(2)} src={i_XZplaneIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_editingPlane_XZ}></VanillaComponentResolver.instance.ToolButton>
                        <div style={{ width: "10rem" }}></div>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CurrentMoveMode.value > 0} onSelect={() => wps.CurrentMoveMode.set((wps.CurrentMoveMode.value + 1) % 3)} src={iarr_moveMode[wps.CurrentMoveMode.value]} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={Tarr_moveMode[wps.CurrentMoveMode.value]}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton selected={wps.CameraLocked.value} onSelect={() => wps.CameraLocked.set(!wps.CameraLocked.value)} src={i_cameraIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_lockCamera}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton disabled={!wps.CameraLocked.value} selected={wps.CameraLocked.value && wps.CameraRotationLocked.value} onSelect={() => wps.CameraRotationLocked.set(!wps.CameraRotationLocked.value)} src={i_lockRotationView} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_lockRotationView}></VanillaComponentResolver.instance.ToolButton>
                    </VanillaComponentResolver.instance.Section>
                    <VectorSectionEditable title={L_position}
                        valueGetter={() => wps.CurrentPosition.value?.map(x => x.toFixed(3))}
                        valueGetterFormatted={() => wps.CurrentPosition.value?.map(x => decimalsFormat(x) + "m")}
                        onValueChanged={(i, x) => {
                            const newVal = wps.CurrentPosition.value;
                            newVal[i] = parseFloat(x);
                            if (isNaN(newVal[i])) return;
                            wps.CurrentPosition.set(newVal);
                        }} />
                    <VectorSectionEditable title={L_rotation}
                        valueGetter={() => wps.CurrentRotation.value?.map(x => x.toFixed(3))}
                        valueGetterFormatted={() => wps.CurrentRotation.value?.map(x => decimalsFormat(x) + "°")}
                        onValueChanged={(i, x) => {
                            const newVal = wps.CurrentRotation.value;
                            newVal[i] = parseFloat(x);
                            if (isNaN(newVal[i])) return;
                            wps.CurrentRotation.set(newVal);
                        }} />
                    <VectorSectionEditable title={L_scale}
                        valueGetter={() => wps.CurrentScale.value?.map(x => x.toFixed(3))}
                        valueGetterFormatted={() => wps.CurrentScale.value?.map(x => decimalsFormat(x))}
                        onValueChanged={(i, x) => {
                            const newVal = wps.CurrentScale.value;
                            newVal[i] = parseFloat(x);
                            if (isNaN(newVal[i])) return;
                            wps.CurrentScale.set(newVal);
                        }} />
                    <VectorSectionEditable title={L_text}
                        valueGetter={() => [wps.CurrentItemText.value]}
                        valueGetterFormatted={() => [wps.CurrentItemText.value]}
                        onValueChanged={(i, x) => {
                            wps.CurrentItemText.set(x);
                        }} /></>}


            <VanillaComponentResolver.instance.Section title={L_actions}>
                <>
                    {wps.CurrentItemIdx.value < wps.CurrentItemCount.value && <>
                        <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayAppearenceWindow(!displayAppearenceWindow)} selected={displayAppearenceWindow} src={i_AppearenceBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_AppearenceBtn} />
                        <div style={{ width: "10rem" }}></div>
                    </>
                    }
                    <VanillaComponentResolver.instance.ToolButton onSelect={() => wps.CurrentEntity.set(null)} src={i_UnselectCurrentIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_picker} />
                </>
            </VanillaComponentResolver.instance.Section>
            {wps.CurrentItemIdx.value < wps.CurrentItemCount.value && <>
                {displayAppearenceWindow && <WETextAppearenceSettings />}
            </>}
        </>

}



