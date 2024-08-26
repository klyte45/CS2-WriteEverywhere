import { Entity, replaceArgs, VanillaComponentResolver, VanillaFnResolver } from "@klyte45/vuio-commons";
import { StringInputDialog } from "common/StringInputDialog";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { BaseStringInputDialog } from "common/BaseStringInputDialog";
import { ListActionTypeArray, WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { ConfirmationDialog, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { CityDetailResponse, LayoutsService } from "services/LayoutsService";
import "style/mainUi/tabStructure.scss";
import { translate } from "utils/translate";
import { FilePickerDialog } from "common/FilePickerDialog";
import { FileService } from "services/FileService";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
    EXPORTING_TEMPLATE,
    SUCCESS_EXPORTING_TEMPLATE,
}

export const CityLayoutsTab = (props: Props) => {
    const i_addItem = "coui://uil/Standard/Plus.svg";

    const T_addItem = translate("cityLayoutsTab.addTemplateFromXml")
    const T_usages = translate("cityLayoutsTab.usages")
    const T_rename = translate("cityLayoutsTab.rename")
    const T_duplicate = translate("cityLayoutsTab.duplicate")
    const T_delete = translate("cityLayoutsTab.delete")
    const T_export = translate("cityLayoutsTab.exportXml")
    const T_confirmDeleteText = translate("cityLayoutsTab.confirmDeleteText")
    const T_renameDialogTitle = translate("cityLayoutsTab.renameDialog.title")
    const T_renameDialogText = translate("cityLayoutsTab.renameDialog.text")
    const T_confirmOverrideText = translate("cityLayoutsTab.confirmOverrideOnRename")
    const T_duplicateDialogTitle = translate("cityLayoutsTab.duplicateDialog.title")
    const T_duplicateDialogText = translate("cityLayoutsTab.duplicateDialog.text")
    const T_exportXmlDialogTitle = translate("cityLayoutsTab.exportXml.title")
    const T_exportXmlDialogText = translate("cityLayoutsTab.exportXml.text")
    const T_successMessage = translate("cityLayoutsTab.exportXml.successMessage")
    const T_goToFileFolder = translate("cityLayoutsTab.exportXml.goToFileFolder")
    const T_back = translate("cityLayoutsTab.exportXml.back")


    const T_addDialogTitle = translate("cityLayoutsTab.addTemplateXmlDialog.title")
    const T_addDialogText = translate("cityLayoutsTab.addTemplateXmlDialog.text")
    const T_addDialogErrorGeneric = translate("cityLayoutsTab.addTemplateXmlDialog.errorLoadingFontMsg")

    const units = VanillaFnResolver.instance.unit.Unit;
    const formatInteger = VanillaFnResolver.instance.localizedNumber.useNumberFormat(units.Integer, false);
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;

    const [selectedTemplate, setSelectedTemplate] = useState(null as null | string);
    const [layoutList, setLayoutList] = useState({} as Record<string, any>);
    const [selectedTemplateDetails, setSelectedTemplateDetails] = useState(null as null | CityDetailResponse);
    const [currentModal, setCurrentModal] = useState(Modals.NONE);
    const [lastXmlExportedLayoutName, setLastXmlExportedLayoutName] = useState("");
    const [buildIdx, setBuildIdx] = useState(0);


    const [layoutFolder, setLayoutFolder] = useState("")
    const [extensionsImport, setExtensionsImport] = useState("")
    useEffect(() => {
        FileService.getLayoutFolder().then(setLayoutFolder);
        (async () => {
            const storedLayoutExt = await FileService.getStoredLayoutExtension()
            return "*." + storedLayoutExt
        })().then(setExtensionsImport)
    }, [])

    useEffect(() => { LayoutsService.listCityTemplates().then(setLayoutList) }, [selectedTemplate])
    useEffect(() => {
        LayoutsService.getCityTemplateDetail(selectedTemplate!).then((x) => {
            setSelectedTemplateDetails(x)
            if (selectedTemplate) setTimeout(() => setBuildIdx(buildIdx + 1), 600);
        })
    }, [selectedTemplate, buildIdx])



    const actions = [
        { className: "negativeBtn", action() { setCurrentModal(Modals.CONFIRMING_DELETE) }, text: T_delete },
        { className: "neutralBtn", action() { setIsRenamingLayout(true) }, text: T_rename },
        { className: "neutralBtn", action() { setIsDuplicatingLayout(true) }, text: T_duplicate },
        null,
        { className: "neutralBtn", action() { setCurrentModal(Modals.EXPORTING_TEMPLATE) }, text: T_export },

    ]
    const detailsFields = selectedTemplateDetails ? [{ key: T_usages, value: formatInteger(selectedTemplateDetails.usages) }, { key: "GUID", value: layoutList[selectedTemplate!] }] : undefined

    const exportTemplateCallback = async (fileName?: string) => {
        if (!fileName || !selectedTemplate) return;
        var filepath = await LayoutsService.exportCityLayoutAsXml(selectedTemplate, fileName);
        setLastXmlExportedLayoutName(filepath)
        setCurrentModal(Modals.SUCCESS_EXPORTING_TEMPLATE);
    }

    const displayingModal = () => {
        switch (currentModal) {
            case Modals.CONFIRMING_DELETE: return <ConfirmationDialog onConfirm={() => { setCurrentModal(0); LayoutsService.deleteTemplate(selectedTemplate!); setSelectedTemplate(null) }} onCancel={() => setCurrentModal(0)} message={T_confirmDeleteText} />
            case Modals.EXPORTING_TEMPLATE: return <BaseStringInputDialog onConfirm={exportTemplateCallback} dialogTitle={T_exportXmlDialogTitle} dialogPromptText={T_exportXmlDialogText} initialValue={selectedTemplate!} />
            case Modals.SUCCESS_EXPORTING_TEMPLATE: return <ConfirmationDialog onConfirm={() => { LayoutsService.openExportedFilesFolder(); setCurrentModal(0) }} onCancel={() => setCurrentModal(0)} confirm={T_goToFileFolder} cancel={T_back} message={replaceArgs(T_successMessage, { "name": lastXmlExportedLayoutName })} />
        }
    }


    const [isRenamingLayout, setIsRenamingLayout] = useState(false);
    const onRenameLayout = (x: string) => {
        LayoutsService.renameCityTemplate(selectedTemplate!, x!);
        setSelectedTemplate(x!);
    }
    const [isDuplicatingLayout, setIsDuplicatingLayout] = useState(false);
    const onDuplicateLayout = (x: string) => {
        LayoutsService.duplicateCityTemplate(selectedTemplate!, x!);
        setSelectedTemplate(x!);
    }

    const [alertToDisplay, setAlertToDisplay] = useState(undefined as string | undefined)
    const [isAskingPathAdd, setIsAskingPathAdd] = useState(false);
    const onAskPathAdd = async (x?: string) => {
        if (!x) return;
        const newTemplateName = await LayoutsService.importAsCityTemplateFromXml(x);
        if (newTemplateName) {
            setSelectedTemplate(newTemplateName);
        } else {
            setAlertToDisplay(T_addDialogErrorGeneric)
        }
    }

    const listActions: ListActionTypeArray = [
        {
            isContext: false,
            onSelect: () => setIsAskingPathAdd(true),
            src: i_addItem,
            tooltip: T_addItem,
            focusKey: FocusDisabled,
            className: buttonClass
        }
    ]
    return <>
        <WEListWithPreviewTab listActions={listActions} itemActions={actions} detailsFields={detailsFields} listItems={Object.keys(layoutList ?? {})} selectedKey={selectedTemplate!} onChangeSelection={setSelectedTemplate} >
            <div className="k45_we_tabWithPreview_previewControls">

            </div>
        </WEListWithPreviewTab>
        <StringInputWithOverrideDialog dialogTitle={T_renameDialogTitle} dialogPromptText={T_renameDialogText} dialogOverrideText={T_confirmOverrideText} initialValue={selectedTemplate!}
            isActive={isRenamingLayout} setIsActive={setIsRenamingLayout}
            isShortCircuitCheckFn={(x) => !x || x == selectedTemplate}
            checkIfExistsFn={LayoutsService.checkCityTemplateExists}
            actionOnSuccess={onRenameLayout}
        />
        <StringInputWithOverrideDialog dialogTitle={T_duplicateDialogTitle} dialogPromptText={T_duplicateDialogText} dialogOverrideText={T_confirmOverrideText} initialValue={selectedTemplate!}
            isActive={isDuplicatingLayout} setIsActive={setIsDuplicatingLayout}
            isShortCircuitCheckFn={(x) => !x || x == selectedTemplate}
            checkIfExistsFn={LayoutsService.checkCityTemplateExists}
            actionOnSuccess={onDuplicateLayout}
        />
        <FilePickerDialog dialogTitle={T_addDialogTitle} dialogPromptText={T_addDialogText} isActive={isAskingPathAdd} setIsActive={setIsAskingPathAdd}
            actionOnSuccess={onAskPathAdd} allowedExtensions={extensionsImport} initialFolder={layoutFolder} />

        <Portal>
            {alertToDisplay && <ConfirmationDialog onConfirm={() => { setAlertToDisplay(void 0); }} cancellable={false} dismissable={false} message={alertToDisplay} confirm={"OK"} />}
            {displayingModal()}
        </Portal>
    </>
};

