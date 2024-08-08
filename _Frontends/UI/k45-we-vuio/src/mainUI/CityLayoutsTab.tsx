import { Entity, replaceArgs, VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import classNames from "classnames";
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
    RENAMING_TEMPLATE,
    OVERRIDE_CONFIRM,
    DUPLICATING_TEMPLATE,
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
    const [actionOnConfirmOverride, setActionOnConfirmOverride] = useState(() => () => { })
    const [lastXmlExportedLayoutName, setLastXmlExportedLayoutName] = useState("");


    useEffect(() => { LayoutsService.listCityTemplates().then(setLayoutList) }, [selectedLayout])
    useEffect(() => { LayoutsService.getCityTemplateDetail(selectedLayout!).then(setSelectedTemplateDetails) }, [selectedLayout])
    const actions = [
        { className: "negativeBtn", action() { setCurrentModal(Modals.CONFIRMING_DELETE) }, text: T_delete },
        { className: "neutralBtn", action() { setCurrentModal(Modals.RENAMING_TEMPLATE) }, text: T_rename },
        { className: "neutralBtn", action() { setCurrentModal(Modals.DUPLICATING_TEMPLATE) }, text: T_duplicate },
        null,
        { className: "neutralBtn", action() { setCurrentModal(Modals.EXPORTING_TEMPLATE) }, text: T_export },

    ]
    const detailsFields = selectedTemplateDetails ? [{ key: T_usages, value: formatInteger(selectedTemplateDetails.usages) }] : undefined

    const renameTemplateCallback = getOverrideCheckFn(
        (x: boolean) => setCurrentModal(x ? Modals.RENAMING_TEMPLATE : 0),
        (x) => !x || x == selectedLayout,
        LayoutsService.checkCityTemplateExists,
        setActionOnConfirmOverride,
        (x: boolean) => setCurrentModal(x ? Modals.OVERRIDE_CONFIRM : 0),
        (x) => {
            LayoutsService.renameCityTemplate(selectedLayout!, x!);
            setSelectedLayout(x!);
        })


    const duplicateTemplateCallback = getOverrideCheckFn(
        (x: boolean) => setCurrentModal(x ? Modals.DUPLICATING_TEMPLATE : 0),
        (x) => !x || x == selectedLayout,
        LayoutsService.checkCityTemplateExists,
        setActionOnConfirmOverride,
        (x: boolean) => setCurrentModal(x ? Modals.OVERRIDE_CONFIRM : 0),
        (x) => {
            LayoutsService.duplicateCityTemplate(selectedLayout!, x!);
            setSelectedLayout(x!);
        })

    const exportTemplateCallback = async (fileName?: string) => {
        if (!fileName || !selectedLayout) return;
        var filepath = await LayoutsService.exportCityLayoutAsXml(selectedLayout, fileName);
        setLastXmlExportedLayoutName(filepath)
        setCurrentModal(Modals.SUCCESS_EXPORTING_TEMPLATE);
    }

    const displayingModal = () => {
        switch (currentModal) {
            case Modals.CONFIRMING_DELETE: return <ConfirmationDialog onConfirm={() => { setCurrentModal(0); LayoutsService.deleteTemplate(selectedLayout!); setSelectedLayout(null) }} onCancel={() => setCurrentModal(0)} message={T_confirmDeleteText} />
            case Modals.RENAMING_TEMPLATE: return <WEInputDialog callback={renameTemplateCallback} title={T_renameDialogTitle} promptText={T_renameDialogText} initialValue={selectedLayout!} />
            case Modals.OVERRIDE_CONFIRM: return <ConfirmationDialog onConfirm={actionOnConfirmOverride} onCancel={() => setCurrentModal(0)} message={T_confirmOverrideText} />
            case Modals.DUPLICATING_TEMPLATE: return <WEInputDialog callback={duplicateTemplateCallback} title={T_duplicateDialogTitle} promptText={T_duplicateDialogText} validationFn={(x) => !x?.trim() && x != selectedLayout} />
            case Modals.EXPORTING_TEMPLATE: return <WEInputDialog callback={exportTemplateCallback} title={T_exportXmlDialogTitle} promptText={T_exportXmlDialogText} initialValue={selectedLayout!} />
            case Modals.SUCCESS_EXPORTING_TEMPLATE: return <ConfirmationDialog onConfirm={() => { LayoutsService.openExportedFilesFolder(); setCurrentModal(0) }} onCancel={() => setCurrentModal(0)} confirm={T_goToFileFolder} cancel={T_back} message={replaceArgs(T_successMessage, { "name": lastXmlExportedLayoutName })} />
        }
    }

    return <>
        <WEListWithPreviewTab actions={actions} detailsFields={detailsFields} listItems={Object.keys(layoutList)} selectedKey={selectedLayout!} onChangeSelection={setSelectedLayout} >
            <div className="k45_we_tabWithPreview_previewControls">

            </div>
        </WEListWithPreviewTab>
        <Portal>{displayingModal()}</Portal>
    </>
};

