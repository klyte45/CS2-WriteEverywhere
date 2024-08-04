import { Entity, HierarchyViewport, LocElementType, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { LayoutsService } from "services/LayoutsService";
import { WESimulationTextType, WETextItemResume } from "services/WEFormulaeElement";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";




export const WETextHierarchyView = ({ clipboard, setClipboard }: { clipboard: Entity | undefined | null, setClipboard: (c: Entity | undefined | null) => any }) => {
    /**/const i_cut = "coui://uil/Standard/DottedLinesMarkers.svg";
    const i_copy = "coui://uil/Standard/RectangleCopy.svg";
    const i_paste = "coui://uil/Standard/RectanglePaste.svg";
    /**/const i_pasteAtRoot = "coui://uil/Standard/ArrowUpTriangleNotch.svg";
    const i_delete = "coui://uil/Standard/Trash.svg";
    /**/const i_addRoot = "coui://uil/Standard/ArrowLeftTriangleNotch.svg";
    /**/const i_addChild = "coui://uil/Standard/Plus.svg";

    /**/const i_exportAsPrefabLayout = "coui://uil/Standard/Cube.svg";
    /**/const i_saveAsCityTemplate = "coui://uil/Standard/Building Themes.svg";

    /**/const i_exportLayout = "coui://uil/Standard/DiskSave.svg";
    /**/const i_importLayout = "coui://uil/Standard/Folder.svg";


    const i_typeText = "coui://uil/Standard/PencilPaper.svg";
    const i_typeImage = "coui://uil/Standard/Image.svg";
    /**/const i_typePlaceholder = "coui://uil/Standard/RotateAngleRelative.svg";


    const wps = WorldPickerService.instance;
    const T_title = translate("textHierarchyWindow.title"); //"Appearance Settings"
    const T_cut = translate("textHierarchyWindow.cut"); //"Appearance Settings"
    const T_copy = translate("textHierarchyWindow.copy"); //"Appearance Settings"
    const T_paste = translate("textHierarchyWindow.paste"); //"Appearance Settings"
    const T_delete = translate("textHierarchyWindow.delete"); //"Appearance Settings"
    const T_pasteAtRoot = translate("textHierarchyWindow.pasteAtRoot"); //"Appearance Settings"
    const T_copiedInfo = translate("textHierarchyWindow.copiedInfoSuffix"); //"Appearance Settings"
    const T_cuttedInfo = translate("textHierarchyWindow.cuttedInfoSuffix"); //"Appearance Settings"
    const T_addEmptyChild = translate("textHierarchyWindow.addEmptyChild"); //"Appearance Settings"
    const T_addEmptyRoot = translate("textHierarchyWindow.addEmptyRoot"); //"Appearance Settings"


    const T_exportLayout = translate("textHierarchyWindow.exportLayoutToLib"); //"Appearance Settings"
    const T_importLayoutAtRoot = translate("textHierarchyWindow.importLayout"); //"Appearance Settings"
    const T_exportLayoutAsPrefab = translate("textHierarchyWindow.exportLayoutAsDefault"); //"Appearance Settings"
    const T_saveAsCityTemplate = translate("textHierarchyWindow.saveAsCityTemplate"); //"Appearance Settings"

    const defaultPosition = { x: 20 / window.innerWidth, y: 100 / window.innerHeight }

    const HierarchyMenu = VanillaWidgets.instance.HierarchyMenu;
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const Button = VanillaComponentResolver.instance.ToolButton;
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;

    const [viewport, setViewport] = useState([] as (HierarchyViewport & WETextItemResume)[])
    const [expandedViewports, setExpandedViewports] = useState([] as Entity[])
    const [clipboardIsCut, setClipboardIsCut] = useState(false)

    const getDisplayName = (x: WETextItemResume) => x.id.Index == clipboard?.Index ? `${x.name} <${clipboardIsCut ? T_cuttedInfo : T_copiedInfo}>` : x.name

    function getIconForTextType(type: WESimulationTextType) {
        switch (type) {
            case WESimulationTextType.Image:
                return i_typeImage;
            case WESimulationTextType.Text:
                return i_typeText;
            case WESimulationTextType.Placeholder:
                return i_typePlaceholder;
        }
    }

    function ResumeToViewPort(x: WETextItemResume, level: number = 0): (HierarchyViewport & WETextItemResume)[] {
        const isExpanded = expandedViewports.some(y => y.Index == x.id.Index);
        return [{
            ...x,
            displayName: { value: getDisplayName(x), __Type: LocElementType.String },
            icon: getIconForTextType(x.type),
            level,
            expandable: x.children?.length > 0,
            expanded: isExpanded,
            selectable: true,
            selected: x.id.Index == wps.CurrentSubEntity.value?.Index
        } as HierarchyViewport & WETextItemResume].concat(isExpanded ? x.children.flatMap(x => ResumeToViewPort(x, level + 1)) : []);
    }

    useEffect(() => {
        setViewport(wps.CurrentTree.value.flatMap((x, i) => ResumeToViewPort(x)))
    }, [wps.CurrentEntity.value, wps.CurrentSubEntity.value, wps.CurrentTree.value, expandedViewports, clipboard, clipboardIsCut])

    const applyPasteAtRoot = () => doPaste(wps.CurrentEntity.value!)
    const applyPaste = () => doPaste(wps.CurrentSubEntity.value!)

    const doPaste = async (parent: Entity) => {
        if (clipboard) {
            if (clipboardIsCut) {
                if (await WorldPickerService.changeParent(clipboard, parent)) {
                    setClipboard(void 0);
                }
            } else {
                await WorldPickerService.cloneAsChild(clipboard, parent);
            }
        }
    }

    return <Portal>
        <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <HierarchyMenu
                viewport={viewport}
                visibleCount={12}
                flex={{ basis: 0, grow: 1, shrink: 1 }}
                onSelect={(i) => wps.CurrentSubEntity.set(viewport[i].id)}
                onSetExpanded={(x, b) => !b ? setExpandedViewports(expandedViewports.filter(y => y.Index != viewport[x].id.Index)) : setExpandedViewports(expandedViewports.concat([viewport[x].id]))}
            />
            <EditorItemRow>
                <Button onSelect={() => WorldPickerService.addEmpty()} src={i_addRoot} tooltip={T_addEmptyRoot} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => WorldPickerService.addEmpty(wps.CurrentSubEntity.value!)} src={i_addChild} tooltip={T_addEmptyChild} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ flexGrow: 1 }}></div>
                <Button onSelect={() => { WorldPickerService.dumpBris(); }} src={i_delete} tooltip={"DUMP!"} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { LayoutsService.exportComponentAsPrefabDefault(wps.CurrentSubEntity.value!, true); }} src={i_exportAsPrefabLayout} tooltip={T_exportLayoutAsPrefab} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { LayoutsService.saveAsCityTemplate(wps.CurrentSubEntity.value!, "teste"); }} src={i_saveAsCityTemplate} tooltip={T_saveAsCityTemplate} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { LayoutsService.exportComponentAsXml(wps.CurrentSubEntity.value!, "teste"); }} src={i_exportLayout} tooltip={T_exportLayout} focusKey={FocusDisabled} className={buttonClass} />
                <Button onSelect={() => { LayoutsService.loadAsChildFromXml(wps.CurrentEntity.value!, "teste"); }} src={i_importLayout} tooltip={T_importLayoutAtRoot} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ flexGrow: 1 }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { setClipboard(wps.CurrentSubEntity.value); setClipboardIsCut(true); }} src={i_cut} tooltip={T_cut} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { setClipboard(wps.CurrentSubEntity.value); setClipboardIsCut(false); }} src={i_copy} tooltip={T_copy} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!clipboard || !wps.CurrentSubEntity.value?.Index} onSelect={() => applyPaste()} src={i_paste} tooltip={T_paste} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!clipboard} onSelect={() => applyPasteAtRoot()} src={i_pasteAtRoot} tooltip={T_pasteAtRoot} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => WorldPickerService.removeItem()} src={i_delete} tooltip={T_delete} focusKey={FocusDisabled} className={buttonClass} />
            </EditorItemRow>
        </Panel>
    </Portal>;
};

