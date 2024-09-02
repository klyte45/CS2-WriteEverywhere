import { LocElementType, VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { Component, useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import "../style/floatingPanels.scss";
import { translate } from "utils/translate";
import { WEFormulaeEditor } from "./WEFormulaeEditor";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { FontService } from "services/FontService";
import { TextureAtlasService } from "services/TextureAtlasService";


const i_addFont = "coui://uil/Colored/Folder.svg";

export const WETextValueSettings = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("textValueSettings.title"); //"Appearance Settings"
    const T_uploadNewFont = translate("textValueSettings.importNewFont"); //
    const T_fontFieldTitle = translate("textValueSettings.fontFieldTitle"); //
    const T_useFormulae = translate("textValueSettings.useFormulae"); //
    const T_formulae = translate("textValueSettings.formulae"); //
    const T_fixedText = translate("textValueSettings.fixedText"); //
    const T_contentType = translate("textValueSettings.contentType"); //
    const T_atlas = translate("textValueSettings.atlas"); //
    const T_image = translate("textValueSettings.image"); //
    const T_useAbsoluteSize = translate("textValueSettings.useAbsoluteSize"); //
    const T_heightWidthCm = translate("textValueSettings.heightWidthCm"); //
    const T_HeightCm = translate("textValueSettings.heightCm"); //
    const T_widthDistortion = translate("textValueSettings.widthDistortion"); //
    const T_maxWidth = translate("textValueSettings.maxWidth"); //    
    const L_itemNamePlaceholder = translate("toolOption.itemNamePlaceholder"); //"Text #"

    const wps = WorldPickerService.instance;
    const [buildIdx, setBuild] = useState(0);

    useEffect(() => { WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1)) }, [buildIdx])
    useEffect(() => { TextureAtlasService.listAtlasImages(wps.ImageAtlasName.value).then(x => setImgOptions(x ?? [])); }, [wps.ImageAtlasName.value, wps.CurrentSubEntity.value])
    useEffect(() => { TextureAtlasService.listAvailableLibraries().then(x => setAtlases(Object.keys(x ?? {}))); }, [wps.CurrentSubEntity.value])

    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const NumberDropdownField = VanillaWidgets.instance.DropdownField<number>();
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const CommonButton = VanillaComponentResolver.instance.CommonButton;
    const ToggleField = VanillaWidgets.instance.ToggleField;
    const FloatInputField = VanillaWidgets.instance.FloatInputField;
    const Float2InputField = VanillaWidgets.instance.Float2InputField;
    const editorTheme = VanillaWidgets.instance.editorItemModule;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;

    const [formulaeTyping, setFormulaeTyping] = useState(wps.FormulaeStr.value);
    const [fixedTextTyping, setFixedTextTyping] = useState(wps.CurrentItemText.value);
    const [usingFormulae, setUsingFormulae] = useState(!!wps.FormulaeStr.value);

    const [atlases, setAtlases] = useState([] as string[]);
    const [imgOptions, setImgOptions] = useState([] as string[]);

    const [height, setHeight] = useState(wps.CurrentScale.value[1]);
    const [widthDistortion, setWidthDistortion] = useState(wps.CurrentScale.value[0] / wps.CurrentScale.value[1]);
    const [width, setWidth] = useState(wps.CurrentScale.value[0]);
    const [maxWidth, setMaxWidth] = useState(wps.MaxWidth.value * 100);


    useEffect(() => {
        setHeight(wps.CurrentScale.value[1]);
        setWidthDistortion(wps.CurrentScale.value[0] / wps.CurrentScale.value[1]);
        setWidth(wps.CurrentScale.value[0]);
    }, [wps.CurrentScale.value, wps.CurrentSubEntity.value])


    useEffect(() => { setMaxWidth(wps.MaxWidth.value * 100) }, [wps.MaxWidth.value])
    useEffect(() => { setFormulaeTyping(wps.FormulaeStr.value); }, [wps.FormulaeStr.value, wps.CurrentSubEntity.value])
    useEffect(() => { setUsingFormulae(!!wps.FormulaeStr.value); }, [wps.CurrentSubEntity.value])

    useEffect(() => { setFixedTextTyping(wps.CurrentItemText.value); }, [wps.CurrentItemText.value, wps.CurrentSubEntity.value])

    const saveHeight = (height: number) => {
        const scale = wps.CurrentScale.value;
        if ([WESimulationTextType.Text, WESimulationTextType.Image].includes(wps.TextSourceType.value)) {
            const proportion = wps.CurrentScale.value[0] / wps.CurrentScale.value[1];
            scale[0] = height * proportion;
        }
        scale[1] = height;
        wps.CurrentScale.set(scale);
    }
    const saveWidthDistortion = (proportion: number) => {
        const scale = wps.CurrentScale.value;
        scale[0] = scale[1] * proportion;
        wps.CurrentScale.set(scale);
    }
    const saveWidth = (width: number) => {
        const scale = wps.CurrentScale.value;
        scale[0] = width;
        wps.CurrentScale.set(scale);
    }

    const saveMaxWidth = (value: number) => wps.MaxWidth.set(value > 0 ? value * .01 : 0)

    const defaultPosition = props.initialPosition ?? { x: 1 - 400 / window.innerWidth, y: 1 - 180 / window.innerHeight }

    const alwaysBeAbsolute = [WESimulationTextType.Placeholder, WESimulationTextType.WhiteTexture].includes(wps.TextSourceType.value);
    const alwaysBeRelative = wps.TextSourceType.value == WESimulationTextType.Text;
    const mayBeAbsolute = wps.TextSourceType.value == WESimulationTextType.Image;

    return <>
        <Portal>
            <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
                <EditorItemRow label={T_contentType}>
                    <NumberDropdownField
                        value={wps.TextSourceType.value}
                        items={[0, 1, 2, 4].map(x => { return { displayName: { __Type: LocElementType.String, value: translate(`textValueSettings.contentType.${x}`) }, value: x } })}
                        onChange={(x) => wps.TextSourceType.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                {mayBeAbsolute && <ToggleField label={T_useAbsoluteSize} value={wps.UseAbsoluteSizeEditing.value} onChange={(x) => wps.UseAbsoluteSizeEditing.set(x)} />}
                {(alwaysBeRelative || (!wps.UseAbsoluteSizeEditing.value && mayBeAbsolute)) && <>
                    <FloatInputField label={T_HeightCm} min={.001} max={10000000} value={height * 100} onChange={(x) => saveHeight(x * .01)} onChangeEnd={() => saveHeight(height)} />
                    <FloatInputField label={T_widthDistortion} min={.001} max={1000000} value={widthDistortion} onChange={setWidthDistortion} onChangeEnd={() => saveWidthDistortion(widthDistortion)} />
                </>}
                {(alwaysBeAbsolute || (wps.UseAbsoluteSizeEditing.value && mayBeAbsolute)) && <Float2InputField label={T_heightWidthCm} value={{ x: wps.CurrentScale.value[0] * 100, y: wps.CurrentScale.value[1] * 100 }} onChange={(x) => wps.CurrentScale.set([x.x * .01, x.y * .01, 1])} />}
                {wps.TextSourceType.value == WESimulationTextType.Text && <>
                    <FloatInputField label={T_maxWidth} min={.001} max={1000000} value={maxWidth} onChange={setMaxWidth} onChangeEnd={() => saveMaxWidth(maxWidth)} />
                    <EditorItemRow label={T_fontFieldTitle} styleContent={{ paddingLeft: "34rem" }}>
                        <DropdownField
                            value={wps.SelectedFont.value}
                            items={wps.FontList.value.map(x => { return { displayName: { __Type: LocElementType.String, value: x == "/DEFAULT/" ? "<DEFAULT>" : x }, value: x } })}
                            onChange={(x) => wps.SelectedFont.set(x)}
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
                {wps.TextSourceType.value == WESimulationTextType.Image && <>
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
                </>} {wps.TextSourceType.value == WESimulationTextType.Placeholder &&
                    <>
                        <EditorItemRow label={L_itemNamePlaceholder}>
                            <StringInputField
                                value={fixedTextTyping}
                                onChange={(x) => { setFixedTextTyping(x) }}
                                onChangeEnd={() => {
                                    wps.CurrentItemText.set(fixedTextTyping.trim());
                                }}
                                maxLength={120}
                            />
                        </EditorItemRow>
                    </>}
            </Panel>
        </Portal>
        {usingFormulae && <WEFormulaeEditor />}
    </>
} 
