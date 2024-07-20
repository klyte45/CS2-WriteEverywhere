import { VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { Component, useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import "../style/floatingPanels.scss";
import { translate } from "utils/translate";


enum LocElementType {
    Bounds = "Game.UI.Localization.LocalizedBounds",
    Fraction = "Game.UI.Localization.LocalizedFraction",
    Number = "Game.UI.Localization.LocalizedNumber",
    String = "Game.UI.Localization.LocalizedString"
}

const i_addFont = "coui://uil/Colored/Folder.svg";

const T_title = translate("textValueSettings.title"); //"Appearance Settings"
const T_uploadNewFont = translate("textValueSettings.importNewFont"); //
const T_fontFieldTitle = translate("textValueSettings.fontFieldTitle"); //
const T_useFormulae = translate("textValueSettings.useFormulae"); //
const T_formulae = translate("textValueSettings.formulae"); //
const T_fixedText = translate("textValueSettings.fixedText"); //
const T_contentType = translate("textValueSettings.contentType"); //
const T_atlas = translate("textValueSettings.atlas"); //
const T_image = translate("textValueSettings.image"); //
const T_Height = translate("textValueSettings.height"); //
const T_widthDistortion = translate("textValueSettings.widthDistortion"); //

export const WETextValueSettings = (props: { initialPosition?: { x: number, y: number } }) => {
    const wps = WorldPickerService.instance;
    const [buildIdx, setBuild] = useState(0);

    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
    }, [buildIdx])

    useEffect(() => {
        WorldPickerService.listAtlasImages(wps.ImageAtlasName.value).then(x => setImgOptions(x ?? []));
    }, [wps.ImageAtlasName.value, wps.CurrentItemIdx.value])

    useEffect(() => {
        WorldPickerService.listAvailableLibraries().then(x => setAtlases(x ?? []));
    }, [wps.CurrentItemIdx.value])

    const Locale = VanillaFnResolver.instance.localization.useCachedLocalization();
    const decimalsFormat = (value: number) => VanillaFnResolver.instance.localizedNumber.formatFloat(Locale, value, false, 3, true, false, Infinity);

    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const NumberDropdownField = VanillaWidgets.instance.DropdownField<number>();
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const CommonButton = VanillaComponentResolver.instance.CommonButton;
    const Tooltip = VanillaComponentResolver.instance.Tooltip;
    const ToggleField = VanillaWidgets.instance.ToggleField;
    const FloatInputField = VanillaWidgets.instance.FloatInputField;
    const editorTheme = VanillaWidgets.instance.editorItemModule;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const onFontSelectWindow = async () => wps.SelectedFont.set(await WorldPickerService.requireFontInstallation(""))

    const [formulaeTyping, setFormulaeTyping] = useState(wps.FormulaeStr.value);
    const [fixedTextTyping, setFixedTextTyping] = useState(wps.CurrentItemText.value);
    const [usingFormulae, setUsingFormulae] = useState(!!wps.FormulaeStr.value);

    const [atlases, setAtlases] = useState([]);
    const [imgOptions, setImgOptions] = useState([]);

    const [height, setHeight] = useState(wps.CurrentScale.value[1]);
    const [widthDistortion, setWidthDistortion] = useState(wps.CurrentScale.value[0] / wps.CurrentScale.value[1]);


    useEffect(() => {
        setHeight(wps.CurrentScale.value[1]);
        setWidthDistortion(wps.CurrentScale.value[0] / wps.CurrentScale.value[1]);
    }, [wps.CurrentScale.value, wps.CurrentItemIdx.value])

    useEffect(() => {
        setFormulaeTyping(wps.FormulaeStr.value);
        setUsingFormulae(!!wps.FormulaeStr.value);
    }, [wps.FormulaeStr.value, wps.CurrentItemIdx.value])
    useEffect(() => { setFixedTextTyping(wps.CurrentItemText.value); }, [wps.CurrentItemText.value, wps.CurrentItemIdx.value])

    const saveHeight = (height: number) => {
        const scale = wps.CurrentScale.value;
        const proportion = wps.CurrentScale.value[0] / wps.CurrentScale.value[1];
        scale[1] = height;
        scale[0] = height * proportion;
        wps.CurrentScale.set(scale);
    }
    const saveWidthDistortion = (proportion: number) => {
        const scale = wps.CurrentScale.value;
        scale[0] = scale[1] * proportion;
        wps.CurrentScale.set(scale);
    }

    const defaultPosition = props.initialPosition ?? { x: 200 / window.innerWidth, y: 200 / window.innerHeight }

    return <Portal>
        <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <EditorItemRow label={T_contentType}>
                <NumberDropdownField
                    value={wps.TextSourceType.value}
                    items={[0, 1].map(x => { return { displayName: { __Type: LocElementType.String, value: translate(`textValueSettings.contentType.${x}`) }, value: x } })}
                    onChange={(x) => wps.TextSourceType.set(x)}
                    style={{ flexGrow: 1, width: "inherit" }}
                />
            </EditorItemRow>
            <FloatInputField label={T_Height} min={.001} max={10000000} value={height} onChange={saveHeight} onChangeEnd={() => saveHeight(height)} />
            <FloatInputField label={T_widthDistortion} min={.001} max={1000000} value={widthDistortion} onChange={setWidthDistortion} onChangeEnd={() => saveWidthDistortion(widthDistortion)} />
            {wps.TextSourceType.value == 0 && <>
                <EditorItemRow label={T_fontFieldTitle} styleContent={{ paddingLeft: "34rem" }}>
                    <DropdownField
                        value={wps.FontList.value.includes(wps.SelectedFont.value) ? wps.SelectedFont.value : wps.FontList.value[0]}
                        items={wps.FontList.value.map(x => { return { displayName: { __Type: LocElementType.String, value: x }, value: x } })}
                        onChange={(x) => wps.SelectedFont.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                    <Tooltip tooltip={T_uploadNewFont}>
                        <CommonButton onClick={onFontSelectWindow} className={editorTheme.pickerToggle} style={{ width: "34rem" }} focusKey={noFocus}>
                            <img src={i_addFont} className={editorTheme.directoryIcon} />
                        </CommonButton>
                    </Tooltip>
                </EditorItemRow>
                <ToggleField label={T_useFormulae} value={usingFormulae} onChange={(x) => setUsingFormulae(x)} />
                {usingFormulae ?
                    <EditorItemRow label={T_formulae} styleContent={{ paddingLeft: "34rem" }}>
                        <StringInputField
                            value={formulaeTyping}
                            onChange={(x) => { setFormulaeTyping(x.replaceAll(/\s/g, "")) }}
                            onChangeEnd={() => wps.FormulaeStr.set(formulaeTyping)}
                            className="we_formulaeInput"
                            maxLength={400}
                        />
                        <CommonButton className={editorTheme.pickerToggle} style={{ width: "34rem" }} focusKey={noFocus}>
                            {wps.FormulaeCompileResult.value}
                        </CommonButton>
                    </EditorItemRow> :
                    <EditorItemRow label={T_fixedText}>
                        <StringInputField
                            value={fixedTextTyping}
                            onChange={(x) => { setFixedTextTyping(x) }}
                            onChangeEnd={() => {
                                wps.CurrentItemText.set(fixedTextTyping.trim());
                                wps.FormulaeStr.set("");
                            }}
                            maxLength={400}
                        />
                    </EditorItemRow>
                }
            </>}
            {wps.TextSourceType.value == 1 && <>
                <EditorItemRow label={T_atlas}>
                    <DropdownField
                        value={wps.ImageAtlasName.value}
                        items={atlases?.map(x => { return { displayName: { __Type: LocElementType.String, value: x || "<DEFAULT>" }, value: x } })}
                        onChange={(x) => wps.ImageAtlasName.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                <ToggleField label={T_useFormulae} value={usingFormulae} onChange={(x) => setUsingFormulae(x)} />
                {usingFormulae ?
                    <EditorItemRow label={T_formulae} styleContent={{ paddingLeft: "34rem" }}>
                        <StringInputField
                            value={formulaeTyping}
                            onChange={(x) => { setFormulaeTyping(x.replaceAll(/\s/g, "")) }}
                            onChangeEnd={() => wps.FormulaeStr.set(formulaeTyping)}
                            className="we_formulaeInput"
                            maxLength={400}
                        />
                        <CommonButton className={editorTheme.pickerToggle} style={{ width: "34rem" }} focusKey={noFocus}>
                            {wps.FormulaeCompileResult.value}
                        </CommonButton>
                    </EditorItemRow> :
                    <EditorItemRow label={T_image}>
                        <DropdownField
                            value={wps.CurrentItemText.value}
                            items={imgOptions?.map(x => { return { displayName: { __Type: LocElementType.String, value: x || "<DEFAULT>" }, value: x } })}
                            onChange={(x) => wps.CurrentItemText.set(x)}
                            style={{ flexGrow: 1, width: "inherit" }}
                        />
                    </EditorItemRow>
                }
            </>}
        </Panel>
    </Portal>;
} 