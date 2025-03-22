import { MultiUIValueBinding, UIColorRGBA, VanillaComponentResolver, VanillaWidgets, ColorUtils, VanillaFnResolver, ColorHSVA, replaceArgs } from "@klyte45/vuio-commons";
import { CSSProperties, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import i_formulae from "../images/Function.svg";
import classNames from "classnames";
import { Portal } from "cs2/ui";
import { ContextMenuExpansion } from "./ContextMenuButton";
import { FocusDisabled } from "cs2/input";

type Props = {
    label: string
    formulaeModule: keyof typeof WorldPickerService.instance.bindingList,
    formulaeField: string,
    defaultInputField: JSX.Element,
}

const i_focus = "coui://uil/Standard/Magnifier.svg";

export const FormulaeEditRow = ({ defaultInputField, label, formulaeModule, formulaeField }: Props) => {
    const T_focusInFormulaePanel = translate("textValueSettings.focusInFormulaePanel"); // 
    const T_useFormulae = translate("textValueSettings.useFormulae"); //  

    const [formulaeStrField, setFormulaeStrField] = useState<MultiUIValueBinding<string>>(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeStr" as never]);
    const [valueField, setValueField] = useState<MultiUIValueBinding<any>>(WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]);
    const [formulaeCompileResultField, setFormulaeCompileResultField] = useState<MultiUIValueBinding<string>>(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeCompileResult" as never]);
    const [formulaeCompileResultErrorArgs, setFormulaCompileResultErrorArgs] = useState<MultiUIValueBinding<string[]>>(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeCompileResultErrorArgs" as never]);

    const [formulaeTyping, setFormulaeTyping] = useState(formulaeStrField?.value);
    const [usingFormulae, setUsingFormulae] = useState(!!formulaeStrField?.value);

    useEffect(() => { setFormulaeTyping(formulaeStrField.value); }, [formulaeStrField?.value])
    useEffect(() => {
        setValueField(WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]);
        setFormulaeStrField(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeStr" as never]);
        setFormulaeCompileResultField(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeCompileResult" as never]);
        setFormulaCompileResultErrorArgs(WorldPickerService.instance.bindingList[formulaeModule][formulaeField + "FormulaeCompileResultErrorArgs" as never]);
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
        if (!usingFormulae) {
            formulaeStrField.set("");
            if (WorldPickerService.instance.currentFormulaeField == formulaeField
                && WorldPickerService.instance.currentFormulaeModule == formulaeModule) {
                WorldPickerService.instance.clearCurrentEditingFormulaeParam();
            }
        }

    }, [usingFormulae])
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const Button = VanillaComponentResolver.instance.ToolButton;

    return <FocusDisabled>
        {usingFormulae ?
            <>
                <EditorItemRow label={label} styleContent={{ flexDirection: 'column', alignItems: "flex-end" }}>
                    <div style={{ paddingLeft: "56rem", flexDirection: 'row', display: 'flex' }}>
                        <StringInputField
                            value={formulaeTyping}
                            onChange={(x) => { setFormulaeTyping(x.replaceAll(/\s/g, "")) }}
                            onChangeEnd={() => {
                                formulaeCompileResultErrorArgs.set([]);
                                formulaeStrField.set(formulaeTyping);
                            }
                            }
                            className="we_formulaeInput"
                            maxLength={400}
                        />
                        <Button src={i_focus} tooltip={T_focusInFormulaePanel} selected={isCurrentlyFocused()} onSelect={() => setFocusToField()} focusKey={noFocus} />
                        <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
                    </div>
                    {!!formulaeCompileResultField?.value && <div style={{ color: "var(--warningColor)", paddingTop: "5rem", paddingBottom: "5rem", width: "100%" }}>{replaceArgs(translate("formulaeError." + formulaeCompileResultField.value), formulaeCompileResultErrorArgs?.value ?? [])}</div>}
                </EditorItemRow>
            </> :
            <EditorItemRow label={label} styleContent={{ paddingLeft: "28rem" }}>
                {defaultInputField}
                <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
            </EditorItemRow>
        }
    </FocusDisabled>
}

type FloatFormulaeProps = {
    min: number,
    max: number,
    label: string
    formulaeField: string
    formulaeModule: keyof typeof WorldPickerService.instance.bindingList
}

export const FormulaeEditorRowFloat = ({ min, max, label, formulaeField, formulaeModule }: FloatFormulaeProps) => {
    const Slider = VanillaComponentResolver.instance.Slider;
    const FloatInput = VanillaComponentResolver.instance.FloatInput;
    const sliderTheme = VanillaComponentResolver.instance.sliderTheme;
    const editorItemTheme = VanillaComponentResolver.instance.editorItemTheme;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const valueBinding: MultiUIValueBinding<number> = WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]
    return <FormulaeEditRow formulaeField={formulaeField} formulaeModule={formulaeModule} label={label} defaultInputField={<>
        <div className="k45_we_formulaeEditorFieldContainer">
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
        <div className="k45_we_formulaeEditorFieldContainer">
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

type ColorFormulaeProps = {
    showAlpha?: boolean,
    label: string
    formulaeField: string
    formulaeModule: keyof typeof WorldPickerService.instance.bindingList
}

const formatColorCss = ({ r, g, b, a }: UIColorRGBA) => `rgba(${Math.round(255 * r)},${Math.round(255 * g)},${Math.round(255 * b)},${a.toString().replace(",", ".")})`
const formatColorRGB = ({ r, g, b }: UIColorRGBA) => `#${Math.round(255 * r).toString(16).padStart(2, '0')}${Math.round(255 * g).toString(16).padStart(2, '0')}${Math.round(255 * b).toString(16).padStart(2, '0')}`
const formatColorRGBA = ({ r, g, b, a }: UIColorRGBA) => `#${Math.round(255 * r).toString(16).padStart(2, '0')}${Math.round(255 * g).toString(16).padStart(2, '0')}${Math.round(255 * b).toString(16).padStart(2, '0')}${Math.round(255 * a).toString(16).padStart(2, '0')}`
export const FormulaeEditorRowColor = ({ showAlpha, label, formulaeField, formulaeModule }: ColorFormulaeProps) => {
    const Button = VanillaComponentResolver.instance.CommonButton;
    const ColorPicker = VanillaComponentResolver.instance.ColorPicker;
    const editorItemTheme = VanillaComponentResolver.instance.editorItemTheme;
    const sliderTheme = VanillaComponentResolver.instance.sliderTheme;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const valueBinding: MultiUIValueBinding<UIColorRGBA> = WorldPickerService.instance.bindingList[formulaeModule][formulaeField as never]


    const btnRef = useRef(null as any as HTMLDivElement);
    const menuRef = useRef(null as any as HTMLDivElement);
    const findFixedPosition = (el: HTMLElement) => {
        const result = { left: 0, top: 0 }
        if (el) {
            let nextParent = el;
            do {
                result.left += nextParent.offsetLeft;
                result.top += nextParent.offsetTop;
            } while ((nextParent = nextParent.offsetParent as HTMLElement) && !isNaN(nextParent.offsetLeft))
        }
        return result;
    }
    const menuPosition = findFixedPosition(btnRef.current)
    const [menuOpen, setMenuOpen] = useState(false);
    const findBetterDirection = () => {
        if (!btnRef.current) return ContextMenuExpansion.BOTTOM_RIGHT;
        const btnCenterX = menuPosition.left + btnRef.current.offsetWidth / 2
        const btnCenterY = menuPosition.top + btnRef.current.offsetHeight / 2
        if (btnCenterX > window.innerWidth / 2) {//right - expand left
            if (btnCenterY > window.innerHeight / 2) {//bottom - expand top
                return ContextMenuExpansion.TOP_LEFT;
            } else {
                return ContextMenuExpansion.BOTTOM_LEFT;
            }
        } else {
            if (btnCenterY > window.innerHeight / 2) {//bottom - expand top
                return ContextMenuExpansion.TOP_RIGHT;
            } else {
                return ContextMenuExpansion.BOTTOM_RIGHT;
            }
        }
    }

    const [menuCss, setMenuCss] = useState({} as CSSProperties)
    useEffect(() => {
        const effectiveMenuDirection = findBetterDirection()
        switch (effectiveMenuDirection) {
            case ContextMenuExpansion.BOTTOM_LEFT:
                setMenuCss({ top: menuPosition.top + btnRef.current?.offsetHeight + 3, right: window.innerWidth - menuPosition.left - btnRef.current?.offsetWidth });
                break;
            case ContextMenuExpansion.TOP_RIGHT:
                setMenuCss({ bottom: window.innerHeight - menuPosition.top + 3, left: menuPosition.left });
                break;
            case ContextMenuExpansion.TOP_LEFT:
                setMenuCss({ bottom: window.innerHeight - menuPosition.top + 3, right: window.innerWidth - menuPosition.left - btnRef.current?.offsetWidth });
                break;
            case ContextMenuExpansion.BOTTOM_RIGHT:
            default:
                setMenuCss({ top: menuPosition.top + btnRef.current?.offsetHeight + 3, left: menuPosition.left });
                break;
        }
    }, [menuOpen])
    const handleClickOutside = (event: MouseEvent) => {
        if (btnRef.current && !btnRef.current?.contains(event.target as Node) && !menuRef.current?.contains(event.target as Node)) {
            setMenuOpen(false);
        }
    };
    useEffect(() => {
        document.addEventListener('mousedown', handleClickOutside, true);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside, true);
        };
    }, []);
    var VanillaColorUtils = VanillaFnResolver.instance.color;
    const [prevHue, setPrevHue] = useState(0)
    const colorHsv = useMemo(() => {
        const n = VanillaColorUtils.rgbaToHsva(valueBinding.value, prevHue);
        if (0 === n.h && prevHue > .99)
            return {
                ...n,
                h: 1
            };
        return n
    }, [valueBinding.value, prevHue])
    const onChangeColorPicker = useCallback((e: ColorHSVA) => {
        setPrevHue(e.h);
        valueBinding.set(VanillaColorUtils.hsvaToRgba(e))
    }, [valueBinding])

    return <FormulaeEditRow formulaeField={formulaeField} formulaeModule={formulaeModule} label={label} defaultInputField={<>
        <div className="k45_we_formulaeEditorFieldContainer" ref={btnRef}>
            <Button className={classNames(editorItemTheme.swatch, valueBinding.value.a < 1 && editorItemTheme.alpha, "we_colorpicker_btn")}
                onClick={() => { setMenuOpen(!menuOpen) }}
                theme={sliderTheme}
            ><div style={{ backgroundColor: formatColorCss(valueBinding.value) }}><div>{(showAlpha ? formatColorRGBA : formatColorRGB)(valueBinding.value).toUpperCase()}</div></div></Button>
        </div>
        {menuOpen && <Portal><div className="k45_comm_contextMenu k45_we_colorPickerOverlay" style={menuCss} ref={menuRef}>
            <div className="k45_we_colorPickerTitle">{label}</div>
            <ColorPicker alpha={showAlpha} focusKey={noFocus} color={colorHsv} onChange={onChangeColorPicker} />
        </div></Portal>}
    </>} />
}