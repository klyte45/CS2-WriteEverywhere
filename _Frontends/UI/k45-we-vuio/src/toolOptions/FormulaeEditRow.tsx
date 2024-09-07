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