import { Entity, LocElementType, replaceArgs, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { ContextButtonMenuItemArray, ContextMenuButton } from "common/ContextMenuButton";
import { DropdownDialog } from "common/DropdownDialog";
import { FilePickerDialog } from "common/FilePickerDialog";
import { StringInputDialog } from "common/StringInputDialog";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { ConfirmationDialog, Panel, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { FileService } from "services/FileService";
import { LayoutsService } from "services/LayoutsService";
import { WESimulationTextType, WETextItemResume } from "services/WEFormulaeElement";
import { BuildingTreeWe, NodeType, WorldPickerService } from "services/WorldPickerService";
import { ModFolder } from "utils/ModFolder";
import { translate } from "utils/translate";
import i_cut from "../images/Scissors.svg";
import { K45HierarchyMenu, K45HierarchyViewport } from "./K45HierarchyMenu";
import useAsyncMemo from "@klyte45/vuio-commons/src/utils/useAsyncMemo";
import '../style/textHierarchyView.scss';



export const WETextHierarchyView = ({ clipboard, setClipboard }: { clipboard: Entity | undefined | null, setClipboard: (c: Entity | undefined | null) => any }) => {

    const i_copy = "coui://uil/Standard/RectangleCopy.svg";
    const i_paste = "coui://uil/Standard/RectanglePaste.svg";
    const i_delete = "coui://uil/Standard/Trash.svg";
    const i_addItem = "coui://uil/Standard/Plus.svg";

    const i_exportLayout = "coui://uil/Standard/DiskSave.svg";
    const i_importLayout = "coui://uil/Standard/Folder.svg";


    const i_typeText = "coui://uil/Standard/PencilPaper.svg";
    const i_typeImage = "coui://uil/Standard/Image.svg";
    const i_typePlaceholder = "coui://uil/Standard/RotateAngleRelative.svg";
    const i_typeWhiteTexture = "coui://uil/Standard/SingleRhombus.svg";
    const i_typeWhiteCube = "coui://uil/Standard/BoxSide.svg";
    const i_typeMatrixTransform = "coui://uil/Standard/ArrowsMoveAll.svg";
    const i_bookmarkMods = "coui://uil/Standard/Puzzle.svg";


    const i_buildingModuleSelect = "coui://uil/Standard/HouseSmallSubElements.svg";
    const i_nodeType_Root = "coui://uil/Standard/UniqueBuilding.svg";
    const i_nodeType_Upgrade = "coui://uil/Standard/StarFilledSmallOutlined.svg";
    const i_nodeType_Attached = "coui://uil/Standard/BusShelter.svg";
    const i_nodeType_Subobject = "coui://uil/Standard/PropCommercial.svg";
    const i_nodeType_Unknown = "coui://uil/Standard/ExclamationMark.svg";

    const nodeTypeIcons = {
        [NodeType.ROOT]: i_nodeType_Root,
        [NodeType.UPGRADE]: i_nodeType_Upgrade,
        [NodeType.ATTACHMENT]: i_nodeType_Attached,
        [NodeType.SUBOBJECT]: i_nodeType_Subobject,
        [NodeType.UNKNOWN]: i_nodeType_Unknown
    }

    const wps = WorldPickerService.instance.bindingList.picker;
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



    const T_buildingModuleSelect = translate("textHierarchyWindow.buildingModuleSelect"); //"Appearance Settings"

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


    const T_exportXmlDialogTitle = translate("cityLayoutsTab.exportXml.title")
    const T_exportXmlDialogText = translate("cityLayoutsTab.exportXml.text")
    const T_successMessage = translate("cityLayoutsTab.exportXml.successMessage")
    const T_goToFileFolder = translate("cityLayoutsTab.exportXml.goToFileFolder")
    const T_back = translate("cityLayoutsTab.exportXml.back")

    const T_templateFromMods = translate("cityLayoutsTab.addTemplateXmlDialog.templateFromMods")

    const defaultPosition = { x: 20 / window.innerWidth, y: 100 / window.innerHeight }

    const HierarchyMenu = K45HierarchyMenu;
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const Button = VanillaComponentResolver.instance.ToolButton;
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;
    const ScrollView = VanillaWidgets.instance.EditorScrollable;


    const [layoutFolder, setLayoutFolder] = useState("")
    const [extensionsImport, setExtensionsImport] = useState("")
    const [modsLayoutsFolder, setModsLayoutsFolder] = useState([] as ModFolder[])
    useEffect(() => {
        FileService.getLayoutFolder().then(setLayoutFolder);
        (async () => {
            const prefabLayoutExt = await FileService.getPrefabLayoutExtension()
            const storedLayoutExt = await FileService.getStoredLayoutExtension()
            return [prefabLayoutExt, storedLayoutExt].map(x => "*." + x).join("|")
        })().then(setExtensionsImport)

        LayoutsService.listModsLoadableTemplates().then(setModsLayoutsFolder)
    }, [])

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



    const [viewport, setViewport] = useState([] as (K45HierarchyViewport & WETextItemResume)[])
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
            case WESimulationTextType.WhiteTexture:
                return i_typeWhiteTexture;
            case WESimulationTextType.MatrixTransform:
                return i_typeMatrixTransform;
            case WESimulationTextType.WhiteCube:
                return i_typeWhiteCube;
        }
    }

    function ResumeToViewPort(x: WETextItemResume, level: number = 0): (K45HierarchyViewport & WETextItemResume)[] {
        const isExpanded = expandedViewports?.some(y => y.Index == x.id.Index);
        return [{
            ...x,
            displayName: getDisplayName(x),
            icon: getIconForTextType(x.type),
            level,
            expandable: x.children?.length > 0,
            expanded: isExpanded,
            selectable: true,
            selected: x.id.Index == wps.CurrentSubEntity.value?.Index
        } as K45HierarchyViewport & WETextItemResume].concat(isExpanded ? x.children.flatMap(x => ResumeToViewPort(x, level + 1)) : []);
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
    const [exportingAsXml, setExportingAsXml] = useState(false)
    const [nameExportedAsXml, setNameExportedAsXml] = useState(null as string | null)

    const onExportAsXml = async (x?: string) => {
        if (!x) return;
        const saveName = await LayoutsService.exportComponentAsXml(wps.CurrentSubEntity.value!, x!);
        if (!saveName) {
            setAlertToDisplay(translate("template.saveCityDialog.error.1"))
        } else {
            setNameExportedAsXml(saveName);
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

    const [cityLayoutsAvailable, setCityLayoutsAvailable] = useState([] as string[])


    const [loadingFromCity, setLoadingFromCity] = useState(false)
    const onLoadFromCity = async (x?: string) => {
        if (!x) return;
        await LayoutsService.loadAsChildFromCityTemplate(relativeParentToLoad!, x);
    }

    useEffect(() => {
        if (loadingFromCity) {
            LayoutsService.listCityTemplates().then(x => setCityLayoutsAvailable(Object.keys(x).sort((a, b) => a.localeCompare(b))));
        }
    }, [loadingFromCity])

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
            action: () => setExportingAsXml(true)
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
        { label: T_loadFromCityTemplateToRoot, disabled: !wps.CurrentEntity.value?.Index, action: () => { setRelativeParentToLoad(wps.CurrentEntity.value!); setLoadingFromCity(true) } },
        { label: T_loadFromCityTemplateAsChild, disabled: !wps.CurrentSubEntity.value?.Index, action: () => { setRelativeParentToLoad(wps.CurrentSubEntity.value!); setLoadingFromCity(true) } },
        { label: T_loadFromCityTemplateAsSibling, disabled: !currentParentNode, action: () => { setRelativeParentToLoad(currentParentNode!); setLoadingFromCity(true) } },
    ]


    const currentBuildingTree = useAsyncMemo(async () => {
        function filterData(children: BuildingTreeWe[]): BuildingTreeWe[] {
            return children.filter(x => [NodeType.UPGRADE, NodeType.ATTACHMENT].includes(x.nodeType.value__)).map(x => {
                const newChildren = filterData(x.children);
                return {
                    ...x,
                    children: newChildren,
                };
            });
        }
        const data = await WorldPickerService.getBuildingTree(wps.CurrentEntity.value!);
        data.children = filterData(data.children);
        const generateOptions = (x: BuildingTreeWe, level: number): ContextButtonMenuItemArray => {
            const childrenMenus = x.children.flatMap(y => generateOptions(y, level + 1))
            const isSelected = wps.CurrentEntity.value?.Index == x.entity.Index;
            return [{
                label: <div className={["buildingHierarchyItem", isSelected ? "isSelected" : ""].join(" ")}>
                    <img src={nodeTypeIcons[x.nodeType.value__]} style={{ marginRight: (level * 8 + 4) + "rem" }} />
                    <div className="label">{x.name}</div>
                    <div className="prefabNameContextMenu">{x.prefabName}</div>
                </div>,
                action: () => {
                    wps.CurrentEntity.set(x.entity);
                },
                disabled: isSelected
            }].concat(childrenMenus as []);
        }
        return {
            selectedItem: [data].flatMap(x => [x, ...x.children]).find(x => x.entity.Index == wps.CurrentEntity.value?.Index),
            menuData: generateOptions(data, 0)
        };
    }, [wps.CurrentEntity.value])

    return <Portal>
        <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <div className="k45_we_hierarchyViewportTitle">
                <div className="k45_we_itemTitle">{currentBuildingTree?.selectedItem?.name}</div>
                <div className="k45_we_prefabName">{currentBuildingTree?.selectedItem?.prefabName}</div>
            </div>
            <ScrollView style={{ height: "400rem" }}>
                <HierarchyMenu
                    viewport={viewport}
                    onSelect={(i) => wps.CurrentSubEntity.set(viewport[i].id)}
                    onSetExpanded={(x, b) => !b ? setExpandedViewports(expandedViewports.filter(y => y.Index != viewport[x].id.Index)) : setExpandedViewports(expandedViewports.concat([viewport[x].id]))}
                />
            </ScrollView>
            <EditorItemRow>
                <ContextMenuButton src={i_addItem} tooltip={T_addItem} focusKey={FocusDisabled} className={buttonClass} menuItems={addNodeMenu} menuTitle={T_addItem} />
                <div style={{ flexGrow: 1 }}></div>
                {currentBuildingTree && currentBuildingTree.menuData.length > 1 && <>
                    <ContextMenuButton src={i_buildingModuleSelect} tooltip={T_buildingModuleSelect} focusKey={FocusDisabled} className={buttonClass} menuItems={currentBuildingTree.menuData} menuTitle={T_buildingModuleSelect} />
                    <div style={{ flexGrow: 2 }}></div>
                </>}
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
        {alertToDisplay && <ConfirmationDialog onConfirm={() => { setAlertToDisplay(void 0); }} cancellable={false} dismissible={false} message={alertToDisplay} confirm={"OK"} />}
        <StringInputWithOverrideDialog dialogTitle={T_addItemDialogTitle} dialogPromptText={T_addItemDialogPromptText} dialogOverrideText={T_confirmOverrideSaveAsCityTemplate}
            isActive={savingCityTemplate} setIsActive={setSavingCityTemplate}
            isShortCircuitCheckFn={(x) => !x || !wps.CurrentSubEntity.value}
            checkIfExistsFn={LayoutsService.checkCityTemplateExists}
            actionOnSuccess={onSaveTemplate}
        />
        <FilePickerDialog dialogTitle={T_loadingFromXmlDialogTitle} dialogPromptText={T_loadingFromXmlDialogPromptText} isActive={loadingFromXml} setIsActive={setLoadingFromXml} actionOnSuccess={onLoadFromXml} allowedExtensions={extensionsImport} initialFolder={layoutFolder}
            bookmarksTitle={T_templateFromMods} bookmarksIcon={i_bookmarkMods}
            bookmarks={modsLayoutsFolder.sort((a, b) => a.ModName.localeCompare(b.ModName)).map(x => {
                return {
                    name: x.ModName,
                    targetPath: x.Location
                }
            })} />

        <StringInputDialog dialogTitle={T_exportXmlDialogTitle} dialogPromptText={T_exportXmlDialogText} isActive={exportingAsXml} setIsActive={setExportingAsXml} actionOnSuccess={onExportAsXml} />
        {nameExportedAsXml &&
            <ConfirmationDialog onConfirm={() => { LayoutsService.openExportedFilesFolder(); setNameExportedAsXml(null) }} onCancel={() => setNameExportedAsXml(null)} confirm={T_goToFileFolder} cancel={T_back} message={replaceArgs(T_successMessage, { "name": nameExportedAsXml })} />
        }
        <DropdownDialog isActive={loadingFromCity} setIsActive={setLoadingFromCity} callback={onLoadFromCity} items={cityLayoutsAvailable.map(x => { return { displayName: { __Type: LocElementType.String, value: x }, value: x } })} title={T_loadingFromXmlDialogTitle} promptText={T_loadingFromXmlDialogPromptText} />
    </Portal>;
};

