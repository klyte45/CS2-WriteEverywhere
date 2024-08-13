import { Entity, replaceArgs, VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import classNames from "classnames";
import { NameInputWithOverrideDialog } from "common/NameInputWithOverrideDialog";
import { WEInputDialog } from "common/WEInputDialog";
import { WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { ConfirmationDialog, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { CityDetailResponse, LayoutsService } from "services/LayoutsService";
import "style/mainUi/tabStructure.scss"
import { getOverrideCheckFn } from "utils/getOverrideCheckFn";
import { translate } from "utils/translate";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
    EXPORTING_TEMPLATE,
    SUCCESS_EXPORTING_TEMPLATE,
}

export const CityLayoutsTab = (props: Props) => {
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

    const units = VanillaFnResolver.instance.unit.Unit;
    const formatInteger = VanillaFnResolver.instance.localizedNumber.useNumberFormat(units.Integer, false);

    const [selectedLayout, setSelectedLayout] = useState(null as null | string);
    const [layoutList, setLayoutList] = useState({} as Record<string, Entity>);
    const [selectedTemplateDetails, setSelectedTemplateDetails] = useState(null as null | CityDetailResponse);
    const [currentModal, setCurrentModal] = useState(Modals.NONE);
    const [lastXmlExportedLayoutName, setLastXmlExportedLayoutName] = useState("");


    useEffect(() => { LayoutsService.listCityTemplates().then(setLayoutList) }, [selectedLayout])
    useEffect(() => { LayoutsService.getCityTemplateDetail(selectedLayout!).then(setSelectedTemplateDetails) }, [selectedLayout])
    const actions = [
        { className: "negativeBtn", action() { setCurrentModal(Modals.CONFIRMING_DELETE) }, text: T_delete },
        { className: "neutralBtn", action() { setIsRenamingLayout(true) }, text: T_rename },
        { className: "neutralBtn", action() { setIsDuplicatingLayout(true) }, text: T_duplicate },
        null,
        { className: "neutralBtn", action() { setCurrentModal(Modals.EXPORTING_TEMPLATE) }, text: T_export },

    ]
    const detailsFields = selectedTemplateDetails ? [{ key: T_usages, value: formatInteger(selectedTemplateDetails.usages) }] : undefined
  
    const exportTemplateCallback = async (fileName?: string) => {
        if (!fileName || !selectedLayout) return;
        var filepath = await LayoutsService.exportCityLayoutAsXml(selectedLayout, fileName);
        setLastXmlExportedLayoutName(filepath)
        setCurrentModal(Modals.SUCCESS_EXPORTING_TEMPLATE);
    }

    const displayingModal = () => {
        switch (currentModal) {
            case Modals.CONFIRMING_DELETE: return <ConfirmationDialog onConfirm={() => { setCurrentModal(0); LayoutsService.deleteTemplate(selectedLayout!); setSelectedLayout(null) }} onCancel={() => setCurrentModal(0)} message={T_confirmDeleteText} />
            case Modals.EXPORTING_TEMPLATE: return <WEInputDialog callback={exportTemplateCallback} title={T_exportXmlDialogTitle} promptText={T_exportXmlDialogText} initialValue={selectedLayout!} />
            case Modals.SUCCESS_EXPORTING_TEMPLATE: return <ConfirmationDialog onConfirm={() => { LayoutsService.openExportedFilesFolder(); setCurrentModal(0) }} onCancel={() => setCurrentModal(0)} confirm={T_goToFileFolder} cancel={T_back} message={replaceArgs(T_successMessage, { "name": lastXmlExportedLayoutName })} />
        }
    }


    const [isRenamingLayout, setIsRenamingLayout] = useState(false);
    const onRenameLayout = (x: string) => {
        LayoutsService.renameCityTemplate(selectedLayout!, x!);
        setSelectedLayout(x!);
    }
    const [isDuplicatingLayout, setIsDuplicatingLayout] = useState(false);
    const onDuplicateLayout = (x: string) => {
        LayoutsService.duplicateCityTemplate(selectedLayout!, x!);
        setSelectedLayout(x!);
    }
    return <>
        <WEListWithPreviewTab itemActions={actions} detailsFields={detailsFields} listItems={Object.keys(layoutList)} selectedKey={selectedLayout!} onChangeSelection={setSelectedLayout} >
            <div className="k45_we_tabWithPreview_previewControls">

            </div>
        </WEListWithPreviewTab>
        <NameInputWithOverrideDialog dialogTitle={T_renameDialogTitle} dialogPromptText={T_renameDialogText} dialogOverrideText={T_confirmOverrideText} initialValue={selectedLayout!}
            isActive={isRenamingLayout} setIsActive={setIsRenamingLayout}
            isShortCircuitCheckFn={(x) => !x || x == selectedLayout}
            checkIfExistsFn={LayoutsService.checkCityTemplateExists}
            actionOnSuccess={onRenameLayout}
        />
        <NameInputWithOverrideDialog dialogTitle={T_duplicateDialogTitle} dialogPromptText={T_duplicateDialogText} dialogOverrideText={T_confirmOverrideText}
            isActive={isDuplicatingLayout} setIsActive={setIsDuplicatingLayout}
            isShortCircuitCheckFn={(x) => !x || x == selectedLayout}
            checkIfExistsFn={LayoutsService.checkCityTemplateExists}
            actionOnSuccess={onDuplicateLayout}
        />
        <Portal>{displayingModal()}</Portal>
    </>
};

