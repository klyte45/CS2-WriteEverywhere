import { Component } from "react";
import { Cs2FormLine } from "./Cs2FormLine";

export interface CheckboxProps {
    title: string;
    isChecked: () => boolean;
    onValueToggle: (newVal: boolean) => void;
}

export class CheckboxWithLine extends Component<CheckboxProps, {}> {
    constructor(props: CheckboxProps) {
        super(props);
        this.state = {}
    }
    render() {
        const { title, onValueToggle } = this.props;
        const isCehcked = this.props.isChecked();
        return (
            <Cs2FormLine title={title} onClick={() => onValueToggle(!isCehcked)}>
                <Checkbox isChecked={this.props.isChecked} onValueToggle={(x) => onValueToggle(x)} />
            </Cs2FormLine>
        );
    }
}

interface CheckboxTitlelessProps {
    isChecked: () => boolean;
    onValueToggle: (newVal: boolean) => void;
}

export class Checkbox extends Component<CheckboxTitlelessProps, { checked: () => boolean }> {
    constructor(props: CheckboxProps) {
        super(props);
        this.state = {
            checked: props.isChecked
        }
    }
    render() {
        const { onValueToggle } = this.props;
        const isChecked = this.state.checked();
        return (<><div className={`cs2-toggle cs2-item-mouse-states cs2-toggle2 ${isChecked ? "checked" : "unchecked"}`} onClick={() => onValueToggle(!isChecked)}>
            <div className={`cs2-checkmark ${isChecked ? "checked" : ""}`} ></div>
        </div>
        </>);
    }
}


