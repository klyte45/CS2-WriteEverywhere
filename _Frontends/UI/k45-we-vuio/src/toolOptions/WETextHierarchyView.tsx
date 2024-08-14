import { Entity, HierarchyViewport, LocElementType, replaceArgs, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { ConfirmationDialog, Panel, Portal } from "cs2/ui";
import { forwardRef, ReactNode, useEffect, useRef, useState } from "react";
import { LayoutsService } from "services/LayoutsService";
import { WESimulationTextType, WETextItemResume } from "services/WEFormulaeElement";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import { WEInputDialog } from "../common/WEInputDialog";
import { getOverrideCheckFn } from "utils/getOverrideCheckFn";
import { ContextButtonMenuItemArray, ContextMenuButton } from "common/ContextMenuButton";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { StringInputDialog } from "common/StringInputDialog";




export const WETextHierarchyView = ({ clipboard, setClipboard }: { clipboard: Entity | undefined | null, setClipboard: (c: Entity | undefined | null) => any }) => {
    /**/const i_cut = "coui://uil/Standard/DottedLinesMarkers.svg";
    const i_copy = "coui://uil/Standard/RectangleCopy.svg";
    const i_paste = "coui://uil/Standard/RectanglePaste.svg";
    const i_delete = "coui://uil/Standard/Trash.svg";
    const i_addItem = "coui://uil/Standard/Plus.svg";

    const i_exportLayout = "coui://uil/Standard/DiskSave.svg";
    const i_importLayout = "coui://uil/Standard/Folder.svg";


    const i_typeText = "coui://uil/Standard/PencilPaper.svg";
    const i_typeImage = "coui://uil/Standard/Image.svg";
    /**/const i_typePlaceholder = "coui://uil/Standard/RotateAngleRelative.svg";


    const wps = WorldPickerService.instance;
    const T_title = translate("textHierarchyWindow.title"); //"Appearance Settings"
    const T_cut = translate("textHierarchyWindow.cut"); //"Appearance Settings"
    const T_copy = translate("textHierarchyWindow.copy"); //"Appearance Settings"
    const T_paste = translate("textHierarchyWindow.pasteAt"); //"Appearance Settings"
    const T_delete = translate("textHierarchyWindow.delete"); //"Appearance Settings"
    const T_pasteAtRoot = translate("textHierarchyWindow.pasteAtRoot"); //"Appearance Settings"
    const T_clearClipboard = translate("textHierarchyWindow.clearClipboard"); //"Appearance Settings"
    const T_pasteAsChildren = translate("textHierarchyWindow.pasteAsChildren"); //"Appearance Settings"
    const T_pasteAsSibling = translate("textHierarchyWindow.pasteAsSibling"); //"Appearance Settings"
    const T_copiedInfo = translate("textHierarchyWindow.copiedInfoSuffix"); //"Appearance Settings"
    const T_cuttedInfo = translate("textHierarchyWindow.cuttedInfoSuffix"); //"Appearance Settings"
    const T_addItem = translate("textHierarchyWindow.addItem"); //"Appearance Settings"
    const T_addEmptySibling = translate("textHierarchyWindow.addEmptySibling"); //"Appearance Settings"
    const T_addEmptyChild = translate("textHierarchyWindow.addEmptyChild"); //"Appearance Settings"
    const T_addEmptyRoot = translate("textHierarchyWindow.addEmptyRoot"); //"Appearance Settings"


    const T_exportSave = translate("textHierarchyWindow.exportOrSave"); //"Appearance Settings"
    const T_exportLayoutXml = translate("textHierarchyWindow.exportLayoutXml"); //"Appearance Settings"
    const T_exportLayoutAsPrefab = translate("textHierarchyWindow.exportLayoutAsDefault"); //"Appearance Settings"
    const T_saveAsCityTemplate = translate("textHierarchyWindow.saveAsCityTemplate"); //"Appearance Settings"

    const T_importOrLoad = translate("textHierarchyWindow.importOrLoad"); //"Appearance Settings"

    const T_confirmOverrideSaveAsCityTemplate = translate("textHierarchyWindow.confirmOverrideCityTemplateQuestion"); //"Appearance Settings"
    const T_addItemDialogTitle = translate("template.saveCityDialog.title")
    const T_addItemDialogPromptText = translate("template.saveCityDialog.dialogText")

    const T_loadingFromXmlDialogTitle = translate("template.loadXmlDialog.title")
    const T_loadingFromXmlDialogPromptText = translate("template.loadXmlDialog.dialogText")

    const T_importLayoutXmlToRoot = translate("textHierarchyWindow.importLayoutXml.toRoot");
    const T_importLayoutXmlAsSibling = translate("textHierarchyWindow.importLayoutXml.asSibling");
    const T_importLayoutXmlAsChild = translate("textHierarchyWindow.importLayoutXml.asChild");
    const T_loadFromCityTemplateToRoot = translate("textHierarchyWindow.loadFromCityTemplate.toRoot"); //"Appearance Settings"
    const T_loadFromCityTemplateAsSibling = translate("textHierarchyWindow.loadFromCityTemplate.asSibling"); //"Appearance Settings"
    const T_loadFromCityTemplateAsChild = translate("textHierarchyWindow.loadFromCityTemplate.asChild"); //"Appearance Settings"

    const defaultPosition = { x: 20 / window.innerWidth, y: 100 / window.innerHeight }

    const HierarchyMenu = VanillaWidgets.instance.HierarchyMenu;
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const Button = VanillaComponentResolver.instance.ToolButton;
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;

    const [clipboardIsCut, setClipboardIsCut] = useState(false)
    const doPaste = async (parent: Entity) => {
        if (clipboard) {
            if (clipboardIsCut) {
                if (await WorldPickerService.changeParent(clipboard, parent)) {
                    setClipboard(void 0);
                }
            } else {
                await WorldPickerService.cloneAsChild(clipboard, parent);
            }
            setExpandedViewports(expandedViewports.concat([parent]))
        }
    }



    const [viewport, setViewport] = useState([] as (HierarchyViewport & WETextItemResume)[])
    const [expandedViewports, setExpandedViewports] = useState([] as Entity[])

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
        const isExpanded = expandedViewports?.some(y => y.Index == x.id.Index);
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


    const [savingCityTemplate, setSavingCityTemplate] = useState(false)

    const onSaveTemplate = async (x: string) => {
        if (!await LayoutsService.saveAsCityTemplate(wps.CurrentSubEntity.value!, x!)) {
            setAlertToDisplay(translate("template.saveCityDialog.error.1"))
        }
    }

    const getParentNode = (search: Entity, treeNode: WETextItemResume): Entity | undefined => {
        if (treeNode.children?.some(x => x.id.Index == search.Index)) return treeNode.id
        return treeNode.children?.find(x => getParentNode(search, x))?.id
    }

    const [currentParentNode, setCurrentParentNode] = useState(null as Entity | null | undefined)

    useEffect(() => {
        setCurrentParentNode(wps.CurrentSubEntity.value && wps.CurrentTree.value.map(x => getParentNode(wps.CurrentSubEntity.value!, x)).find(x => x))
    }, [wps.CurrentSubEntity.value])


    const [alertToDisplay, setAlertToDisplay] = useState(void 0 as string | undefined);


    const [loadingFromXml, setLoadingFromXml] = useState(false)
    const [relativeParentToLoad, setRelativeParentToLoad] = useState(void 0 as Entity | undefined)

    const onLoadFromXml = async (x?: string) => {
        if (!x) return;
        if (!await LayoutsService.loadAsChildFromXml(relativeParentToLoad!, x)) {
            setAlertToDisplay(translate("template.loadXmlDialog.error.1"))
        } else {

        }
    }


    const [loadingFromCity, setLoadingFromCity] = useState(false)
    const onLoadFromCity = async (x?: string) => {
        if (!x) return;
        await LayoutsService.loadAsChildFromCityTemplate(relativeParentToLoad!, x)
    }

    const pasteMenuItems: ContextButtonMenuItemArray = [
        {
            label: T_pasteAtRoot,
            action: () => doPaste(wps.CurrentEntity.value!),
            disabled: !wps.CurrentEntity.value?.Index
        },
        {
            label: T_pasteAsChildren,
            action: () => doPaste(wps.CurrentSubEntity.value!),
            disabled: !wps.CurrentSubEntity.value?.Index
        },
        {
            label: T_pasteAsSibling,
            action: () => doPaste(currentParentNode!),
            disabled: !currentParentNode
        },
        null,
        {
            label: T_clearClipboard,
            action: () => setClipboard(void 0)
        }
    ]

    const addNodeMenu: ContextButtonMenuItemArray = [
        {
            label: T_addEmptyRoot,
            action: () => WorldPickerService.addEmpty()
        },
        {
            label: T_addEmptyChild,
            action: () => WorldPickerService.addEmpty(wps.CurrentSubEntity.value!),
            disabled: !wps.CurrentSubEntity.value?.Index
        },
        {
            label: T_addEmptySibling,
            action: () => WorldPickerService.addEmpty(currentParentNode!),
            disabled: !currentParentNode
        },
    ]

    const saveNodeMenu: ContextButtonMenuItemArray = [
        {
            label: T_saveAsCityTemplate,
            action: () => setSavingCityTemplate(true)
        },
        {
            label: T_exportLayoutXml,
            action: () => LayoutsService.exportComponentAsXml(wps.CurrentSubEntity.value!, "teste")
        },
        {
            label: T_exportLayoutAsPrefab,
            action: () => LayoutsService.exportComponentAsPrefabDefault(wps.CurrentSubEntity.value!, true)
        }
    ]

    const loadNodeMenu: ContextButtonMenuItemArray = [
        { label: T_importLayoutXmlToRoot, disabled: !wps.CurrentEntity.value?.Index, action: () => { setRelativeParentToLoad(wps.CurrentEntity.value!); setLoadingFromXml(true) }, },
        { label: T_importLayoutXmlAsChild, disabled: !wps.CurrentSubEntity.value?.Index, action: () => { setRelativeParentToLoad(wps.CurrentSubEntity.value!); setLoadingFromXml(true) }, },
        { label: T_importLayoutXmlAsSibling, disabled: !currentParentNode, action: () => { setRelativeParentToLoad(currentParentNode!); setLoadingFromXml(true) }, },
        null,
        { label: T_loadFromCityTemplateToRoot, disabled: true, action: () => { setRelativeParentToLoad(wps.CurrentEntity.value!); setLoadingFromCity(true) } },
        { label: T_loadFromCityTemplateAsChild, disabled: true, action: () => { setRelativeParentToLoad(wps.CurrentSubEntity.value!); setLoadingFromCity(true) } },
        { label: T_loadFromCityTemplateAsSibling, disabled: true, action: () => { setRelativeParentToLoad(currentParentNode!); setLoadingFromCity(true) } },
    ]

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
                <ContextMenuButton src={i_addItem} tooltip={T_addItem} focusKey={FocusDisabled} className={buttonClass} menuItems={addNodeMenu} menuTitle={T_addItem} />
                <div style={{ flexGrow: 1 }}></div>
                <ContextMenuButton disabled={!wps.CurrentSubEntity.value?.Index} src={i_exportLayout} tooltip={T_exportSave} focusKey={FocusDisabled} className={buttonClass} menuItems={saveNodeMenu} menuTitle={T_exportSave} />
                <ContextMenuButton src={i_importLayout} tooltip={T_importOrLoad} focusKey={FocusDisabled} className={buttonClass} menuItems={loadNodeMenu} menuTitle={T_importOrLoad} />
                <div style={{ flexGrow: 1 }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { setClipboard(wps.CurrentSubEntity.value); setClipboardIsCut(true); }} src={i_cut} tooltip={T_cut} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { setClipboard(wps.CurrentSubEntity.value); setClipboardIsCut(false); }} src={i_copy} tooltip={T_copy} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <ContextMenuButton disabled={!clipboard} tooltip={T_paste} src={i_paste} focusKey={FocusDisabled} className={buttonClass} menuItems={pasteMenuItems} menuTitle={T_paste} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => WorldPickerService.removeItem()} src={i_delete} tooltip={T_delete} focusKey={FocusDisabled} className={buttonClass} />
            </EditorItemRow>
        </Panel>
        {alertToDisplay && <ConfirmationDialog onConfirm={() => { setAlertToDisplay(void 0); }} cancellable={false} dismissable={false} message={alertToDisplay} confirm={"OK"} />}
        <StringInputWithOverrideDialog dialogTitle={T_addItemDialogTitle} dialogPromptText={T_addItemDialogPromptText} dialogOverrideText={T_confirmOverrideSaveAsCityTemplate}
            isActive={savingCityTemplate} setIsActive={setSavingCityTemplate}
            isShortCircuitCheckFn={(x) => !x || !wps.CurrentSubEntity.value}
            checkIfExistsFn={LayoutsService.checkCityTemplateExists}
            actionOnSuccess={onSaveTemplate}
        />
        <StringInputDialog dialogTitle={T_loadingFromXmlDialogTitle} dialogPromptText={T_loadingFromXmlDialogPromptText} isActive={loadingFromXml} setIsActive={setLoadingFromXml} actionOnSuccess={onLoadFromXml}
        />
    </Portal>;
};
