import { AmountValueSection, MetricUnitsEntries, MultiUIValueBinding, UnitSystem, VectorSection, translateUnitResult } from "@klyte45/vuio-commons";
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

let CurrentItemName: MultiUIValueBinding<string>
let CurrentItemIdx: MultiUIValueBinding<number>
let CurrentEntity: MultiUIValueBinding<Entity>
let CurrentScale: MultiUIValueBinding<number3>
let CurrentRotation: MultiUIValueBinding<number3>
let CurrentPosition: MultiUIValueBinding<number3>
let MouseSensibility: MultiUIValueBinding<number>
let CurrentPlaneMode: MultiUIValueBinding<number>

const isValidItem = async () => (await engine.call("k45::we.wpicker.isValidEditingItem")) as boolean
const getItemsAvailable = async () => (await engine.call("k45::we.wpicker.getItemsAvailable")) as string[]

function initBindings(x: Component) {
    CurrentItemName ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemName")
    CurrentItemIdx ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentItemIdx")
    CurrentEntity ??= new MultiUIValueBinding<Entity>("k45::we.wpicker.CurrentEntity")
    CurrentScale ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentScale")
    CurrentRotation ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentRotation")
    CurrentPosition ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentPosition")
    MouseSensibility ??= new MultiUIValueBinding<number>("k45::we.wpicker.MouseSensibility")
    CurrentPlaneMode ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentPlaneMode")
    const _ = [
        CurrentItemName,
        CurrentItemIdx,
        CurrentEntity,
        CurrentScale,
        CurrentRotation,
        CurrentPosition,
        MouseSensibility,
        CurrentPlaneMode
    ].map(y => y.subscribe(async () => x.setState({})))
}

function disposeBindings() {
    CurrentItemName?.dispose()
    CurrentItemIdx?.dispose()
    CurrentEntity?.dispose()
    CurrentScale?.dispose()
    CurrentRotation?.dispose()
    CurrentPosition?.dispose()
    MouseSensibility?.dispose()
    CurrentPlaneMode?.dispose()
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
        initBindings(this)
    }

    private isValid: boolean = false;
    private itemsAvailable: string[] = [];

    componentWillUnmount(): void {
        disposeBindings();
    }

    
    render() {
        return <>
            <AmountValueSection
                valueGetter={() => translateUnitResult([MetricUnitsEntries.distance.linear[UnitSystem.Metric][0], { VALUE: precisions[MouseSensibility.value] + "" }])}
                title="Mouse precision"
                up={{
                    tooltip: "Increment the strenght of the mouse moves when editing the text position/rotation",
                    onSelect: () => MouseSensibility.set(MouseSensibility.value - 1).then(x => this.setState({})),
                    disabledFn: () => MouseSensibility.value <= 0
                }}
                down={{
                    tooltip: "Decrease the strenght of the mouse moves when editing the text position/rotation",
                    onSelect: () => MouseSensibility.set(MouseSensibility.value + 1).then(x => this.setState({})),
                    disabledFn: () => MouseSensibility.value >= precisions.length - 1
                }}
            />
            <VectorSection title="Position" valueGetter={() => CurrentPosition.value?.map(x => translateUnitResult([MetricUnitsEntries.distance.linear[UnitSystem.Metric][0], { VALUE: x.toFixed(3) + "" }]))} />
            <VectorSection title="Rotation" valueGetter={() => CurrentRotation.value?.map(x => x.toFixed(3) + "°")} />
            <VectorSection title="Scale" valueGetter={() => CurrentScale.value?.map(x => x.toFixed(3))} />
            {/* <VanillaComponentResolver.instance.Section title={amountSection}>
            <VanillaComponentResolver.instance.ToolButton
                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}
                tooltip={amountDownTooltip}
                onSelect={() => handleClick(amountDownID)}
                src={arrowDownSrc}
                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            ></VanillaComponentResolver.instance.ToolButton>
            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{amountIsFlow ? AmountValue.toFixed(AmountScale) : AmountValue.toFixed(AmountScale) + " m"}</div>
            <VanillaComponentResolver.instance.ToolButton
                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton}
                tooltip={amountUpTooltip}
                onSelect={() => handleClick(amountUpID)}
                src={arrowUpSrc}
                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            ></VanillaComponentResolver.instance.ToolButton>
            <VanillaComponentResolver.instance.StepToolButton tooltip={amountStepTooltip} onSelect={() => handleClick(amountStepID)} values={defaultValues} selectedValue={AmountStep}></VanillaComponentResolver.instance.StepToolButton>
        </VanillaComponentResolver.instance.Section>

        {ShowMinDepth && ToolMode == WaterToolModes.PlaceWaterSource && (
            // This section is only shown if binding says so.
            <VanillaComponentResolver.instance.Section title={minDepthSection}>
                <VanillaComponentResolver.instance.ToolButton
                    className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}
                    tooltip={minDepthDownTooltip}
                    onSelect={() => handleClick(minDepthDownID)}
                    src={arrowDownSrc}
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                ></VanillaComponentResolver.instance.ToolButton>
                <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{MinDepthValue.toFixed(MinDepthScale) + " m"}</div>
                <VanillaComponentResolver.instance.ToolButton
                    className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton}
                    tooltip={minDepthUpTooltip}
                    onSelect={() => handleClick(minDepthUpID)}
                    src={arrowUpSrc}
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                ></VanillaComponentResolver.instance.ToolButton>
                <VanillaComponentResolver.instance.StepToolButton tooltip={minDepthStepTooltip} onSelect={() => handleClick(minDepthStepID)} values={defaultValues} selectedValue={MinDepthStep}></VanillaComponentResolver.instance.StepToolButton>
            </VanillaComponentResolver.instance.Section>
        )}
        {ToolMode == WaterToolModes.PlaceWaterSource && (
            <VanillaComponentResolver.instance.Section title={radiusSection}>
                <VanillaComponentResolver.instance.ToolButton
                    className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}
                    tooltip={radiusDownTooltip}
                    onSelect={() => handleClick(radiusDownID)}
                    src={arrowDownSrc}
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                ></VanillaComponentResolver.instance.ToolButton>
                <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{RadiusValue.toFixed(RadiusScale) + " m"}</div>
                <VanillaComponentResolver.instance.ToolButton
                    className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton}
                    tooltip={radiusUpTooltip}
                    onSelect={() => handleClick(radiusUpID)}
                    src={arrowUpSrc}
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                ></VanillaComponentResolver.instance.ToolButton>
                <VanillaComponentResolver.instance.StepToolButton tooltip={radiusStepTooltip} onSelect={() => handleClick(radiusStepID)} values={defaultValues} selectedValue={RadiusStep}></VanillaComponentResolver.instance.StepToolButton>
            </VanillaComponentResolver.instance.Section>
        )}
        <VanillaComponentResolver.instance.Section title={toolModeTitle}>
            <VanillaComponentResolver.instance.ToolButton selected={ToolMode == WaterToolModes.PlaceWaterSource} tooltip={descriptionTooltip(placeWaterSourceTitle, placeWaterSourceTooltip)} onSelect={() => changeToolMode(WaterToolModes.PlaceWaterSource)} src={placeWaterSourceSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
            <VanillaComponentResolver.instance.ToolButton selected={ToolMode == WaterToolModes.ElevationChange} tooltip={descriptionTooltip(elevationChangeTitle, elevationChangeTooltip)} onSelect={() => changeToolMode(WaterToolModes.ElevationChange)} src={elevationChangeSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
            <VanillaComponentResolver.instance.ToolButton selected={ToolMode == WaterToolModes.MoveWaterSource} tooltip={descriptionTooltip(moveWaterSourceTitle, moveWaterSourceTooltip)} onSelect={() => changeToolMode(WaterToolModes.MoveWaterSource)} src={moveWaterSourceSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
            <VanillaComponentResolver.instance.ToolButton selected={ToolMode == WaterToolModes.RadiusChange} tooltip={descriptionTooltip(radiusChangeTitle, radiusChangeTooltip)} onSelect={() => changeToolMode(WaterToolModes.RadiusChange)} src={radiusChangeSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
        </VanillaComponentResolver.instance.Section> */}
        </>
    }
}

