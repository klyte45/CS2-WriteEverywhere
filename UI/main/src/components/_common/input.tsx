import { ColorUtils } from "#utility/ColorUtils";
import { CSSProperties, Component, KeyboardEvent } from "react";

interface InputProps {
    title: string;
    getValue: () => string;
    onValueChanged: (newVal: string) => string | Promise<string>;
    isValid?: (newVal: string) => boolean
    onTab?: (newVal: string, shiftDown: boolean) => string
    cssCustomOverrides?: {
        backgroundColor?: (value: string) => string,
        color?: (value: string) => string,
        fontWeight?: CSSProperties['fontWeight']
    }
    maxLength?: number,
    extraKeyPressFilter?: (x: KeyboardEvent<HTMLInputElement>) => void
}

export class Input extends Component<InputProps, {}> {
    constructor(props: InputProps) {
        super(props);
        this.state = {}
    }


    render() {
        return (
            <>
                <div className="cs2-fieldStyle cs2-fieldStyle2">
                    <div className="cs2-form-item-label cs2-form-item-label2">
                        {this.props.title}
                    </div>
                    <SimpleInput {...this.props} />
                </div>
            </>
        );
    }

}


export class SimpleInput extends Component<Omit<InputProps, 'title'>, { value: string, refValue: string }> {
    constructor(props: InputProps) {
        super(props);
        this.state = {
            value: this.props.getValue(),
            refValue: this.props.getValue()
        }
    }

    render() {
        const { onValueChanged } = this.props;
        const currentOuterValue = this.props.getValue();
        let targetValue = this.state.value;
        if (currentOuterValue != this.state.refValue) {
            this.setState({
                value: targetValue = this.props.getValue(),
                refValue: this.props.getValue()
            })
        }
        const overrideStyle: CSSProperties = { width: "50%" };
        if (this.props.cssCustomOverrides && (!this.props.isValid || this.props.isValid(this.state.value))) {
            overrideStyle.backgroundColor = this.props.cssCustomOverrides.backgroundColor?.(this.state.value);
            overrideStyle.color = this.props.cssCustomOverrides.color?.(this.state.value);
            overrideStyle.fontWeight = this.props.cssCustomOverrides.fontWeight;
        }
        return (
            <>
                <input style={overrideStyle}
                    value={targetValue}
                    className={"cs2-value-field cs2-form-value " + this.checkInvalidClasses()}
                    onChange={x => this.setState({ value: x.target.value })}
                    onKeyDown={(x) => this.onKeyDown(x)}
                    onBlur={async () => this.setState({ value: await onValueChanged(this.state.value) })}
                    maxLength={this.props.maxLength}
                    onKeyDownCapture={(x) => this.props.extraKeyPressFilter?.(x)}
                />
            </>
        );
    }
    onKeyDown(x: KeyboardEvent<HTMLInputElement>): void {
        if (x.key == "Escape") {
            const currentTarget = x.currentTarget;
            this.setState({ value: this.props.getValue() }, () => {
                currentTarget.value = this.state.value
                currentTarget.blur();
            })
        } else if (x.key == "Enter") {
            if (!this.props.isValid || this.props.isValid(x.currentTarget.value)) {
                x.currentTarget.blur();
            }
        } else if (x.key == "Tab") {
            if (this.props.onTab) {
                this.setState({ value: this.props.onTab(this.state.value, x.shiftKey) })
            }
        }
    }

    checkInvalidClasses() {
        if (this.props.isValid && !this.props.isValid(this.state.value)) {
            return "input_invalidValue"
        }
    }
}


export class ColorRgbInput extends Component<ColorInputProps, {}> {

    render() {
        return <Input {...this.props}
            cssCustomOverrides={{
                backgroundColor: (val) => ColorUtils.toRGB6(val),
                color: (val) => {
                    let rgb = ColorUtils.toRGB6(val)
                    return rgb ? ColorUtils.toRGBA(ColorUtils.getContrastColorFor(ColorUtils.toColor01(rgb))) : ""
                },
                fontWeight: 'bold'
            }}
            maxLength={7}
            isValid={x => !!ColorUtils.getHexRegexParts(x)}
            onValueChanged={(x) => this.props.onValueChanged(ColorUtils.toRGB6(x))}
        />
    }


}

interface ColorInputProps {
    title: string;
    getValue: () => `#${string}`;
    onValueChanged: (newVal: `#${string}`) => `#${string}` | Promise<`#${string}`>;
    onTab?: (newVal: `#${string}`, shiftDown: boolean) => `#${string}`;
}