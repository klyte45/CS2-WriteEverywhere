import { LocElementType, replaceArgs, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useCallback, useEffect, useState } from "react";
import { TextureAtlasService } from "services/TextureAtlasService";
import { WESimulationTextType } from "services/WEFormulaeElement";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import i_formulae from "../images/Function.svg";
import "../style/floatingPanels.scss";
import { FormulaeEditorRowFloat, FormulaeEditRow } from "../common/FormulaeEditRow";
import { ObjectTyped } from "object-typed";
import { FocusDisabled } from "cs2/input";
import { CustomMeshService } from "services/CustomMeshService";
import { DropdownItem } from "cs2/bindings";

const i_focus = "coui://uil/Standard/Magnifier.svg";

export const WETextValueSettings = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("textValueSettings.title"); //"Appearance Settings"
    const T_fontFieldTitle = translate("textValueSettings.fontFieldTitle"); //
    const T_fixedText = translate("textValueSettings.fixedText"); //
    const T_contentType = translate("textValueSettings.contentType"); //
    const T_atlas = translate("textValueSettings.atlas"); //
    const T_mesh = translate("textValueSettings.mesh"); //
    const T_defaultPlane = translate("textValueSettings.defaultPlane"); //
    const T_image = translate("textValueSettings.image"); //
    const T_useAbsoluteSize = translate("textValueSettings.useAbsoluteSize"); //
    const T_resizeHeightOnTextOverflow = translate("textValueSettings.resizeHeightOnTextOverflow"); //
    const T_childrenRefersToFrontFace = translate("textValueSettings.childrenRefersToFrontFace"); //
    const T_heightWidthCm = translate("textValueSettings.heightWidthCm"); //
    const T_dimensionsCm = translate("textValueSettings.dimensions"); //
    const T_HeightCm = translate("textValueSettings.heightCm"); //
    const T_widthDistortion = translate("textValueSettings.widthDistortion"); //
    const T_maxWidth = translate("textValueSettings.maxWidth"); //     
    const T_decalAreaThickness = translate("textValueSettings.decalAreaThickness"); //     
    const L_itemNamePlaceholder = translate("toolOption.itemNamePlaceholder"); //"Text #"
    const T_offsetPosition = translate("textValueSettings.offsetPosition"); //
    const T_offsetRotation = translate("textValueSettings.offsetRotation"); //
    const T_scaleByAxis = translate("textValueSettings.scaleByAxis"); //
    const T_flipZ = translate("textValueSettings.flipZ"); //

    const mesh = WorldPickerService.instance.bindingList.mesh;
    const material = WorldPickerService.instance.bindingList.material;
    const transform = WorldPickerService.instance.bindingList.transform;
    const picker = WorldPickerService.instance.bindingList.picker;
    const [buildIdx, setBuild] = useState(0);

    useEffect(() => { WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1)) }, [buildIdx])
    useEffect(() => { TextureAtlasService.listAtlasImages(mesh.ImageAtlasName.value).then(x => setImgOptions(x ?? [])); }, [mesh.ImageAtlasName.value, picker.CurrentSubEntity.value])
    useEffect(() => {
        Promise.all([
            TextureAtlasService.listAvailableLibraries(),
            TextureAtlasService.listModAtlases(),
            CustomMeshService.listAvailableLibraries()
        ]).then(([libs, mods, customMeshList]) => {
            setAtlases([...Object.keys(libs ?? {}), ...mods.flatMap(x => x.Atlases).sort((a, b) => a.localeCompare(b))])
            console.log(customMeshList)
            setMeshes([{ displayName: { __Type: LocElementType.String, value: T_defaultPlane }, value: "" }].concat(Object.entries(customMeshList ?? {})
                .map(([k, v]) => ({
                    displayName: { __Type: LocElementType.String, value: k },
                    value: v
                }))) as DropdownItem<string>[]);
        })

    }, [picker.CurrentSubEntity.value])

    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const NumberDropdownField = VanillaWidgets.instance.DropdownField<number>();
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const ToggleField = VanillaWidgets.instance.ToggleField;
    const FloatInputField = VanillaWidgets.instance.FloatInputField;
    const Float2InputField = VanillaWidgets.instance.Float2InputField;
    const Float3InputField = VanillaWidgets.instance.Float3InputField;
    const FloatInputStandalone = VanillaWidgets.instance.FloatInputStandalone;
    const editorStyle = VanillaWidgets.instance.editorItemModule;


    const [fixedTextTyping, setFixedTextTyping] = useState(mesh.ValueText.value);
    const [usingFormulae, setUsingFormulae] = useState(!!mesh.ValueTextFormulaeStr.value);

    const [atlases, setAtlases] = useState([] as string[]);
    const [meshes, setMeshes] = useState([] as DropdownItem<string>[]);
    const [imgOptions, setImgOptions] = useState([] as string[]);

    const [height, setHeight] = useState(transform.CurrentScale.value[1]);
    const [widthDistortion, setWidthDistortion] = useState(transform.CurrentScale.value[0] / transform.CurrentScale.value[1]);


    const [decalAreaThickness, setDecalAreaThickness] = useState(Math.abs(transform.CurrentScale.value[2]));


    useEffect(() => {
        setHeight(transform.CurrentScale.value[1]);
        setWidthDistortion(transform.CurrentScale.value[0] / transform.CurrentScale.value[1]);
        setDecalAreaThickness(Math.abs(transform.CurrentScale.value[2]))
    }, [transform.CurrentScale.value, picker.CurrentSubEntity.value])


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

    const saveDecalThickness = (value: number) => {
        setDecalAreaThickness(value);
        transform.CurrentScale.set([transform.CurrentScale.value[0], transform.CurrentScale.value[1], (Math.sign(transform.CurrentScale.value[2] + .001) || 1) * value]);
    }

    const defaultPosition = props.initialPosition ?? { x: 1 - 400 / window.innerWidth, y: 1 - 180 / window.innerHeight }

    const alwaysBeAbsolute = [WESimulationTextType.Placeholder, WESimulationTextType.WhiteTexture].includes(mesh.TextSourceType.value);
    const alwaysBeRelative = mesh.TextSourceType.value == WESimulationTextType.Text;
    const mayBeAbsolute = mesh.TextSourceType.value == WESimulationTextType.Image;
    const is3dEditor = mesh.TextSourceType.value == WESimulationTextType.WhiteCube;


    const formulaeModule = "mesh";
    const formulaeField = "ValueText";
    const formulaeFieldOffsetPosition = "OffsetPosition";
    const formulaeFieldOffsetRotation = "OffsetRotation";
    const formulaeFieldScaler = "Scaler";

    return <>
        <Portal>
            <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
                <EditorItemRow label={T_contentType}>
                    <NumberDropdownField
                        value={mesh.TextSourceType.value}
                        items={[0, 1, 2, 4, 5, 6].map(x => { return { displayName: { __Type: LocElementType.String, value: translate(`textValueSettings.contentType.${x}`) }, value: x } })}
                        onChange={(x) => mesh.TextSourceType.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />
                </EditorItemRow>
                {!is3dEditor && mayBeAbsolute && <ToggleField label={T_useAbsoluteSize} value={transform.UseAbsoluteSizeEditing.value} onChange={(x) => transform.UseAbsoluteSizeEditing.set(x)} />}
                {!is3dEditor && (alwaysBeRelative || (!transform.UseAbsoluteSizeEditing.value && mayBeAbsolute)) && <>
                    <FloatInputField label={T_HeightCm} min={.001} max={10000000} value={height * 100} onChange={(x) => saveHeight(x * .01)} onChangeEnd={() => saveHeight(height)} />
                    <FloatInputField label={T_widthDistortion} min={.001} max={1000000} value={widthDistortion} onChange={setWidthDistortion} onChangeEnd={() => saveWidthDistortion(widthDistortion)} />
                </>}
                {
                    (alwaysBeAbsolute || (transform.UseAbsoluteSizeEditing.value && mayBeAbsolute)) && <Float2InputField
                        label={T_heightWidthCm}
                        value={{ x: transform.CurrentScale.value[0] * 100, y: transform.CurrentScale.value[1] * 100 }}
                        onChange={(x) => transform.CurrentScale.set([x.x * .01, x.y * .01, (Math.sign(transform.CurrentScale.value[2]) || 1) * decalAreaThickness])} />
                }
                {
                    is3dEditor && <Float3InputField
                        label={T_dimensionsCm}
                        value={{ x: transform.CurrentScale.value[0] * 100, y: transform.CurrentScale.value[1] * 100, z: Math.abs(transform.CurrentScale.value[2]) * 100 }}
                        onChange={(x) => transform.CurrentScale.set([x.x * .01, x.y * .01, (Math.sign(transform.CurrentScale.value[2]) || 1) * x.z * .01])} />
                }

                {!is3dEditor && material.ShaderType.value == 2 && <FloatInputField label={T_decalAreaThickness} min={.001} max={100} value={decalAreaThickness} onChange={saveDecalThickness} onChangeEnd={() => saveDecalThickness(decalAreaThickness)} />}
                {
                    mesh.TextSourceType.value == WESimulationTextType.WhiteCube &&
                    <ToggleField label={T_childrenRefersToFrontFace} value={mesh.ChildrenRefersToFrontFace.value} onChange={(x) => mesh.ChildrenRefersToFrontFace.set(x)} />
                }
                {mesh.TextSourceType.value == WESimulationTextType.Text && <>
                    <FormulaeEditRow formulaeField="MaxWidth" formulaeModule={formulaeModule} label={T_maxWidth}
                        defaultInputField={<FloatInputStandalone className={editorStyle.input} min={.0} max={1000000} value={mesh.MaxWidth.value} onChange={x => mesh.MaxWidth.set(x)} />}
                    />
                    <ToggleField label={T_resizeHeightOnTextOverflow} value={mesh.RescaleHeightOnTextOverflow.value} onChange={(x) => mesh.RescaleHeightOnTextOverflow.set(x)} />
                    <EditorItemRow label={T_fontFieldTitle} styleContent={{ paddingLeft: "34rem" }}>
                        <DropdownField
                            value={mesh.SelectedFont.value}
                            items={picker.FontList.value.map(x => { return { displayName: { __Type: LocElementType.String, value: x == "/DEFAULT/" ? "<DEFAULT>" : x }, value: x } })}
                            onChange={(x) => mesh.SelectedFont.set(x)}
                            style={{ flexGrow: 1, width: "inherit" }}
                        />
                    </EditorItemRow>
                    <FormulaeEditRow formulaeField={formulaeField} formulaeModule={formulaeModule} label={T_fixedText}
                        defaultInputField={<StringInputField
                            value={fixedTextTyping}
                            onChange={(x) => { setFixedTextTyping(x) }}
                            onChangeEnd={() => {
                                mesh.ValueText.set(fixedTextTyping.trim());
                                mesh.ValueTextFormulaeStr.set("");
                            }}
                            maxLength={400}
                        />} />
                </>}
                {mesh.TextSourceType.value == WESimulationTextType.Image && <>
                    <EditorItemRow label={T_mesh}>
                        <DropdownField
                            value={mesh.CustomMeshName.value}
                            items={meshes}
                            onChange={(x) => mesh.CustomMeshName.set(x)}
                            style={{ flexGrow: 1, width: "inherit" }}
                        />
                    </EditorItemRow>
                    <EditorItemRow label={T_atlas}>
                        <DropdownField
                            value={mesh.ImageAtlasName.value}
                            items={atlases?.map(x => { return { displayName: { __Type: LocElementType.String, value: x || "<DEFAULT>" }, value: x } })}
                            onChange={(x) => mesh.ImageAtlasName.set(x)}
                            style={{ flexGrow: 1, width: "inherit" }}
                        />
                    </EditorItemRow>
                    <FormulaeEditRow formulaeField={formulaeField} formulaeModule={formulaeModule} label={T_image} defaultInputField={<DropdownField
                        value={mesh.ValueText.value}
                        items={imgOptions?.map(x => { return { displayName: { __Type: LocElementType.String, value: x || "<DEFAULT>" }, value: x } })}
                        onChange={(x) => mesh.ValueText.set(x)}
                        style={{ flexGrow: 1, width: "inherit" }}
                    />} />
                </>}
                {mesh.TextSourceType.value == WESimulationTextType.Placeholder &&
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
                {mesh.TextSourceType.value != WESimulationTextType.MatrixTransform && <>
                    <ToggleField label={T_flipZ} value={transform.CurrentScale.value[2] < 0} onChange={(x) => transform.CurrentScale.set([transform.CurrentScale.value[0], transform.CurrentScale.value[1], -transform.CurrentScale.value[2]])} />
                </>}
                {mesh.TextSourceType.value == WESimulationTextType.MatrixTransform &&
                    <>
                        <FormulaeEditRow formulaeField={formulaeFieldOffsetPosition} formulaeModule={formulaeModule} label={T_offsetPosition} defaultInputField={
                            <FocusDisabled>
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} value={mesh.OffsetPosition.value[0]} onChange={(x) => mesh.OffsetPosition.set([x, mesh.OffsetPosition.value[1], mesh.OffsetPosition.value[2]])} />
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} value={mesh.OffsetPosition.value[1]} onChange={(x) => mesh.OffsetPosition.set([mesh.OffsetPosition.value[0], x, mesh.OffsetPosition.value[2]])} />
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} value={mesh.OffsetPosition.value[2]} onChange={(x) => mesh.OffsetPosition.set([mesh.OffsetPosition.value[0], mesh.OffsetPosition.value[1], x])} />
                            </FocusDisabled>} />
                        <FormulaeEditRow formulaeField={formulaeFieldOffsetRotation} formulaeModule={formulaeModule} label={T_offsetRotation} defaultInputField={
                            <FocusDisabled>
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} value={mesh.OffsetRotation.value[0]} onChange={(x) => mesh.OffsetRotation.set([x, mesh.OffsetRotation.value[1], mesh.OffsetRotation.value[2]])} />
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} value={mesh.OffsetRotation.value[1]} onChange={(x) => mesh.OffsetRotation.set([mesh.OffsetRotation.value[0], x, mesh.OffsetRotation.value[2]])} />
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} value={mesh.OffsetRotation.value[2]} onChange={(x) => mesh.OffsetRotation.set([mesh.OffsetRotation.value[0], mesh.OffsetRotation.value[1], x])} />
                            </FocusDisabled>} />
                        <FormulaeEditRow formulaeField={formulaeFieldScaler} formulaeModule={formulaeModule} label={T_scaleByAxis} defaultInputField={
                            <FocusDisabled>
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} min={0} max={999} value={mesh.Scaler.value[0]} onChange={(x) => mesh.Scaler.set([x, mesh.Scaler.value[1], mesh.Scaler.value[2]])} />
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} min={0} max={999} value={mesh.Scaler.value[1]} onChange={(x) => mesh.Scaler.set([mesh.Scaler.value[0], x, mesh.Scaler.value[2]])} />
                                <FloatInputStandalone style={{ flexGrow: 1, flexShrink: 1 }} className={editorStyle.input} min={0} max={999} value={mesh.Scaler.value[2]} onChange={(x) => mesh.Scaler.set([mesh.Scaler.value[0], mesh.Scaler.value[1], x])} />
                            </FocusDisabled>} />
                    </>}
            </Panel>
        </Portal>
    </>
}