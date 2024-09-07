import { LocElementType, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useCallback, useEffect, useState } from "react";
import { TextureAtlasService } from "services/TextureAtlasService";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import i_formulae from "../images/Function.svg";
import "../style/floatingPanels.scss";

const i_focus = "coui://uil/Standard/Magnifier.svg";

export const WETextValueSettings = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("textValueSettings.title"); //"Appearance Settings"
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
    const T_focusInFormulaePanel = translate("textValueSettings.focusInFormulaePanel"); //    
    const L_itemNamePlaceholder = translate("toolOption.itemNamePlaceholder"); //"Text #"

    const mesh = WorldPickerService.instance.bindingList.mesh;
    const transform = WorldPickerService.instance.bindingList.transform;
    const picker = WorldPickerService.instance.bindingList.picker;
    const [buildIdx, setBuild] = useState(0);

    useEffect(() => { WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1)) }, [buildIdx])
    useEffect(() => { TextureAtlasService.listAtlasImages(mesh.ImageAtlasName.value).then(x => setImgOptions(x ?? [])); }, [mesh.ImageAtlasName.value, picker.CurrentSubEntity.value])
    useEffect(() => { TextureAtlasService.listAvailableLibraries().then(x => setAtlases(Object.keys(x ?? {}))); }, [picker.CurrentSubEntity.value])

    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const NumberDropdownField = VanillaWidgets.instance.DropdownField<number>();
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const ToggleField = VanillaWidgets.instance.ToggleField;
    const FloatInputField = VanillaWidgets.instance.FloatInputField;
    const Float2InputField = VanillaWidgets.instance.Float2InputField;
    const editorTheme = VanillaWidgets.instance.editorItemModule;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const Button = VanillaComponentResolver.instance.ToolButton;


    const [formulaeTyping, setFormulaeTyping] = useState(mesh.ValueTextFormulaeStr.value);
    const [fixedTextTyping, setFixedTextTyping] = useState(mesh.ValueText.value);
    const [usingFormulae, setUsingFormulae] = useState(!!mesh.ValueTextFormulaeStr.value);

    const [atlases, setAtlases] = useState([] as string[]);
    const [imgOptions, setImgOptions] = useState([] as string[]);

    const [height, setHeight] = useState(transform.CurrentScale.value[1]);
    const [widthDistortion, setWidthDistortion] = useState(transform.CurrentScale.value[0] / transform.CurrentScale.value[1]);
    const [maxWidth, setMaxWidth] = useState(mesh.MaxWidth.value * 100);


    useEffect(() => {
        setHeight(transform.CurrentScale.value[1]);
        setWidthDistortion(transform.CurrentScale.value[0] / transform.CurrentScale.value[1]);
    }, [transform.CurrentScale.value, picker.CurrentSubEntity.value])


    useEffect(() => { setMaxWidth(mesh.MaxWidth.value * 100) }, [mesh.MaxWidth.value])
    useEffect(() => { setFormulaeTyping(mesh.ValueTextFormulaeStr.value); }, [mesh.ValueTextFormulaeStr.value, picker.CurrentSubEntity.value])
    useEffect(() => { setUsingFormulae(!!mesh.ValueTextFormulaeStr.value); }, [picker.CurrentSubEntity.value])

    useEffect(() => { setFixedTextTyping(mesh.ValueText.value); }, [mesh.ValueText.value, picker.CurrentSubEntity.value])
    useEffect(() => { if (usingFormulae) WorldPickerService.instance.setCurrentEditingFormulaeParam("mesh", "") }, [usingFormulae])

    const saveHeight = (height: number) => {
        const scale = transform.CurrentScale.value;
        if ([WESimulationTextType.Text, WESimulationTextType.Image].includes(mesh.TextSourceType.value)) {
            const proportion = transform.CurrentScale.value[0] / transform.CurrentScale.value[1];
            scale[0] = height * proportion;
        }
        scale[1] = height;
        transform.CurrentScale.set(scale);
    }
    const saveWidthDistortion = (proportion: number) => {
        const scale = transform.CurrentScale.value;
        scale[0] = scale[1] * proportion;
        transform.CurrentScale.set(scale);
    }

    const saveMaxWidth = (value: number) => mesh.MaxWidth.set(value > 0 ? value * .01 : 0)

    const defaultPosition = props.initialPosition ?? { x: 1 - 400 / window.innerWidth, y: 1 - 180 / window.innerHeight }

    const alwaysBeAbsolute = [WESimulationTextType.Placeholder, WESimulationTextType.WhiteTexture].includes(mesh.TextSourceType.value);
    const alwaysBeRelative = mesh.TextSourceType.value == WESimulationTextType.Text;
    const mayBeAbsolute = mesh.TextSourceType.value == WESimulationTextType.Image;

    const formulaeModule = "mesh";
    const formulaeField = "ValueText";

    const isCurrentlyFocused = useCallback(() =>
        WorldPickerService.instance.currentFormulaeModule == formulaeModule &&
        WorldPickerService.instance.currentFormulaeField == formulaeField
        , [
            WorldPickerService.instance.currentFormulaeField,
            WorldPickerService.instance.currentFormulaeModule,
        ])
    const setFocusToField = () => { WorldPickerService.instance.setCurrentEditingFormulaeParam(formulaeModule, formulaeField); setBuild(buildIdx + 1) }

    return <>
        <Portal>
            <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
                <EditorItemRow label={T_contentType}>
                    <NumberDropdownField
                        value={mesh.TextSourceType.value}
                        items={[0, 1, 2, 4].map(x => { return { displayName: { __Type: LocElementType.String, value: translate(`textValueSettings.contentType.${x}`) }, value: x } })}
                        onChange={(x) => mesh.TextSourceType.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                {mayBeAbsolute && <ToggleField label={T_useAbsoluteSize} value={transform.UseAbsoluteSizeEditing.value} onChange={(x) => transform.UseAbsoluteSizeEditing.set(x)} />}
                {(alwaysBeRelative || (!transform.UseAbsoluteSizeEditing.value && mayBeAbsolute)) && <>
                    <FloatInputField label={T_HeightCm} min={.001} max={10000000} value={height * 100} onChange={(x) => saveHeight(x * .01)} onChangeEnd={() => saveHeight(height)} />
                    <FloatInputField label={T_widthDistortion} min={.001} max={1000000} value={widthDistortion} onChange={setWidthDistortion} onChangeEnd={() => saveWidthDistortion(widthDistortion)} />
                </>}
                {(alwaysBeAbsolute || (transform.UseAbsoluteSizeEditing.value && mayBeAbsolute)) && <Float2InputField label={T_heightWidthCm} value={{ x: transform.CurrentScale.value[0] * 100, y: transform.CurrentScale.value[1] * 100 }} onChange={(x) => transform.CurrentScale.set([x.x * .01, x.y * .01, 1])} />}
                {mesh.TextSourceType.value == WESimulationTextType.Text && <>
                    <FloatInputField label={T_maxWidth} min={.001} max={1000000} value={maxWidth} onChange={setMaxWidth} onChangeEnd={() => saveMaxWidth(maxWidth)} />
                    <EditorItemRow label={T_fontFieldTitle} styleContent={{ paddingLeft: "34rem" }}>
                        <DropdownField
                            value={mesh.SelectedFont.value}
                            items={picker.FontList.value.map(x => { return { displayName: { __Type: LocElementType.String, value: x == "/DEFAULT/" ? "<DEFAULT>" : x }, value: x } })}
                            onChange={(x) => mesh.SelectedFont.set(x)}
                            style={{ flexGrow: 1, width: "inherit" }}
                        />
                    </EditorItemRow>
                    {usingFormulae ?
                        <EditorItemRow label={T_formulae} styleContent={{ paddingLeft: "56rem" }}>
                            <StringInputField
                                value={formulaeTyping}
                                onChange={(x) => { setFormulaeTyping(x.replaceAll(/\s/g, "")) }}
                                onChangeEnd={() => mesh.ValueTextFormulaeStr.set(formulaeTyping)}
                                className="we_formulaeInput"
                                maxLength={400}
                            />
                            <Button src={i_focus} tooltip={T_focusInFormulaePanel} selected={isCurrentlyFocused()} onSelect={() => setFocusToField()} focusKey={noFocus} />
                            <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
                        </EditorItemRow> :
                        <EditorItemRow label={T_fixedText} styleContent={{ paddingLeft: "28rem" }}>
                            <StringInputField
                                value={fixedTextTyping}
                                onChange={(x) => { setFixedTextTyping(x) }}
                                onChangeEnd={() => {
                                    mesh.ValueText.set(fixedTextTyping.trim());
                                    mesh.ValueTextFormulaeStr.set("");
                                }}
                                maxLength={400}
                            />
                            <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
                        </EditorItemRow>
                    }
                </>}
                {mesh.TextSourceType.value == WESimulationTextType.Image && <>
                    <EditorItemRow label={T_atlas}>
                        <DropdownField
                            value={mesh.ImageAtlasName.value}
                            items={atlases?.map(x => { return { displayName: { __Type: LocElementType.String, value: x || "<DEFAULT>" }, value: x } })}
                            onChange={(x) => mesh.ImageAtlasName.set(x)}
                            style={{ flexGrow: 1, width: "inherit" }}
                        />
                    </EditorItemRow>
                    {usingFormulae ?
                        <EditorItemRow label={T_formulae} styleContent={{ paddingLeft: "62rem" }}>
                            <StringInputField
                                value={formulaeTyping}
                                onChange={(x) => { setFormulaeTyping(x.replaceAll(/\s/g, "")) }}
                                onChangeEnd={() => mesh.ValueTextFormulaeStr.set(formulaeTyping)}
                                className="we_formulaeInput"
                                maxLength={400}
                            />
                            <Button src={i_focus} tooltip={T_focusInFormulaePanel} selected={isCurrentlyFocused()} onSelect={() => setFocusToField()} focusKey={noFocus} />
                            <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
                        </EditorItemRow> :
                        <EditorItemRow label={T_image} styleContent={{ paddingLeft: "28rem" }}>
                            <DropdownField
                                value={mesh.ValueText.value}
                                items={imgOptions?.map(x => { return { displayName: { __Type: LocElementType.String, value: x || "<DEFAULT>" }, value: x } })}
                                onChange={(x) => mesh.ValueText.set(x)}
                                style={{ flexGrow: 1, width: "inherit" }}
                            />
                            <Button src={i_formulae} tooltip={T_useFormulae} selected={usingFormulae} onSelect={() => setUsingFormulae(!usingFormulae)} focusKey={noFocus} />
                        </EditorItemRow>
                    }
                </>}{mesh.TextSourceType.value == WESimulationTextType.Placeholder &&
                    <>
                        <EditorItemRow label={L_itemNamePlaceholder}>
                            <StringInputField
                                value={fixedTextTyping}
                                onChange={(x) => { setFixedTextTyping(x) }}
                                onChangeEnd={() => {
                                    mesh.ValueText.set(fixedTextTyping.trim());
                                }}
                                maxLength={120}
                            />
                        </EditorItemRow>
                    </>}
            </Panel>
        </Portal>
    </>
} 
