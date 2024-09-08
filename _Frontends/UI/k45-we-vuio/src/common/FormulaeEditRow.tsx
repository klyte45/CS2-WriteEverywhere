import { MultiUIValueBinding, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { useCallback, useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import i_formulae from "../images/Function.svg";

type Props = {
    label: string
    formulaeModule: keyof typeof WorldPickerService.instance.bindingList,
    formulaeField: string,
    defaultInputField: JSX.Element
}

const i_focus = "coui://uil/Standard/Magnifier.svg";

export const FormulaeEditRow = ({ defaultInputField, label, formulaeModule, formulaeField }: Props) => {
    const T_focusInFormulaePanel = translate("textValueSettings.focusInFormulaePanel"); // 
    const T_useFormulae = translate("textValueSettings.useFormulae"); //  

    const [formulaeStrField, setFormulaeStrField] = useState<MultiUIValueBinding<string>>(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeStr" as never]);
    const [valueField, setValueField] = useState<MultiUIValueBinding<any>>(WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]);

    const [formulaeTyping, setFormulaeTyping] = useState(formulaeStrField?.value);
    const [usingFormulae, setUsingFormulae] = useState(!!formulaeStrField?.value);

    useEffect(() => { setFormulaeTyping(formulaeStrField.value); }, [formulaeStrField?.value])
    useEffect(() => {
        setFormulaeStrField(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeStr" as never]);
        setValueField(WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]);
    }, [
        WorldPickerService.instance.bindingList.picker.CurrentSubEntity.value,
        formulaeField,
        formulaeModule
    ])

    const setFocusToField = () => { WorldPickerService.instance.setCurrentEditingFormulaeParam(formulaeModule, formulaeField) }
    const isCurrentlyFocused = useCallback(() =>
        WorldPickerService.instance.getCurrentEditingFormulaeValueField() as any == valueField
        , [
            WorldPickerService.instance.currentFormulaeField,
            WorldPickerService.instance.currentFormulaeModule,
        ])
    useEffect(() => { setUsingFormulae(!!formulaeStrField.value); }, [WorldPickerService.instance.bindingList.picker.CurrentSubEntity.value])
    useEffect(() => {
        if (!usingFormulae
            && WorldPickerService.instance.currentFormulaeField == formulaeField
            && WorldPickerService.instance.currentFormulaeModule == formulaeModule) {
            WorldPickerService.instance.clearCurrentEditingFormulaeParam();
        }

    }, [usingFormulae])
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const Button = VanillaComponentResolver.instance.ToolButton;

    return <>
        {usingFormulae ?
            <EditorItemRow label={label} styleContent={{ paddingLeft: "56rem" }}>
                <StringInputField
                    value={formulaeTyping}
                    onChange={(x) => { setFormulaeTyping(x.replaceAll(/\s/g, "")) }}
                    onChangeEnd={() => formulaeStrField.set(formulaeTyping)}
                    className="we_formulaeInput"
                    maxLength={400}
                />
                <Button src={i_focus} tooltip={T_focusInFormulaePanel} selected={isCurrentlyFocused()} onSelect={() => setFocusToField()} focusKey={noFocus} />
                <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
            </EditorItemRow> :
            <EditorItemRow label={label} styleContent={{ paddingLeft: "28rem" }}>
                {defaultInputField}
                <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
            </EditorItemRow>
        }
    </>
}

type FloatFormulaeProps = {
    min: number,
    max: number,
    label: string
    formulaeField: string
    formulaeModule: keyof typeof WorldPickerService.instance.bindingList,
}

export const FormulaeEditorRowFloat = ({ min, max, label, formulaeField, formulaeModule }: FloatFormulaeProps) => {
    const Slider = VanillaComponentResolver.instance.Slider;
    const FloatInput = VanillaComponentResolver.instance.FloatInput;
    const sliderTheme = VanillaComponentResolver.instance.sliderTheme;
    const editorItemTheme = VanillaComponentResolver.instance.editorItemTheme;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const valueBinding: MultiUIValueBinding<number> = WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]
    return <FormulaeEditRow formulaeField={formulaeField} formulaeModule={formulaeModule} label={label} defaultInputField={<>
        <div className="k45_we_formulaeFloatFieldContainer">
            <Slider start={min} end={max}
                value={valueBinding.value}
                onChange={(x) => valueBinding.set(x)}
                theme={sliderTheme}
            />
            <FloatInput focusKey={noFocus} min={min} max={max} onChange={(x) => valueBinding.set(x)} value={valueBinding.value} className={editorItemTheme.sliderInput} />
        </div>
    </>} />
}


export const FormulaeEditorRowFloatLog10 = ({ min, max, label, formulaeField, formulaeModule }: FloatFormulaeProps) => {
    const Slider = VanillaComponentResolver.instance.Slider;
    const FloatInput = VanillaComponentResolver.instance.FloatInput;
    const sliderTheme = VanillaComponentResolver.instance.sliderTheme;
    const editorItemTheme = VanillaComponentResolver.instance.editorItemTheme;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const valueBinding: MultiUIValueBinding<number> = WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]
    return <FormulaeEditRow formulaeField={formulaeField} formulaeModule={formulaeModule} label={label} defaultInputField={<>
        <div className="k45_we_formulaeFloatFieldContainer">
            <Slider start={min} end={max}
                value={Math.log10(valueBinding.value + 1)}
                onChange={(x) => valueBinding.set(Math.pow(10, x) - 1)}
                theme={sliderTheme}
            /><div style={{
                fontSize: "75%",
                alignSelf: "flex-end",
                paddingLeft: "5rem",
                marginRight: "-5rem"
            }}>10^</div>
            <FloatInput focusKey={noFocus} min={min} max={max}
                onChange={(x) => valueBinding.set(Math.pow(10, x) - 1)}
                value={Math.log10(valueBinding.value + 1)}
                className={editorItemTheme.sliderInput} />
        </div>
    </>} />
}