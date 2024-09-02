import { replaceArgs, VanillaComponentResolver, VanillaFnResolver } from "@klyte45/vuio-commons";
import { BaseStringInputDialog } from "common/BaseStringInputDialog";
import { ListActionTypeArray, WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { ConfirmationDialog, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { AtlasCityDetailResponse, TextureAtlasService } from "services/TextureAtlasService";
import "style/mainUi/tabStructure.scss";
import { translate } from "utils/translate";
import "../style/cityAtlasesTab.scss";
import { StringInputDialog } from "common/StringInputDialog";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
    EXPORTING_ATLAS,
    SUCCESS_EXPORTING_TEMPLATE,
}

export const CityAtlasesTab = (props: Props) => {
    const T_usages = translate("cityAtlasesTab.usages")
    const T_imageCount = translate("cityAtlasesTab.imageCount")
    const T_textureSize = translate("cityAtlasesTab.textureSize")
    const T_source = translate("cityAtlasesTab.source")
    const T_sourceSaveGame = translate("cityAtlasesTab.source.saveGame")
    const T_sourceLibraryFolder = translate("cityAtlasesTab.source.libraryFolder")
    const T_delete = translate("cityAtlasesTab.delete")
    const T_export = translate("cityAtlasesTab.export")
    const T_addToSaveGame = translate("cityAtlasesTab.addToSavegame")
    const T_exportDialogTitle = translate("cityAtlasesTab.export.title")
    const T_exportDialogText = translate("cityAtlasesTab.export.text")
    const T_successMessage = translate("cityAtlasesTab.export.successMessage")
    const T_goToFileFolder = translate("cityAtlasesTab.export.goToFileFolder")
    const T_back = translate("cityAtlasesTab.export.back")
    const T_addToCityDialogTitle = translate("cityAtlasesTab.addToCityDialog.title")
    const T_addToCityDialogText = translate("cityAtlasesTab.addToCityDialog.text")
    const T_confirmDeleteText = translate("cityAtlasesTab.confirmDeleteText")
    const T_cityAtlasesSection = translate("cityAtlasesTab.cityAtlasesSection")
    const T_localAtlasesSection = translate("cityAtlasesTab.localAtlasesSection")
    const T_noCityAtlases = translate("cityAtlasesTab.noCityAtlases")
    const T_noLocalAtlases = translate("cityAtlasesTab.noLocalAtlases")


    const units = VanillaFnResolver.instance.unit.Unit;
    const formatInteger = VanillaFnResolver.instance.localizedNumber.useNumberFormat(units.Integer, false);
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;

    const [selectedAtlas, setSelectedAtlas] = useState(null as null | string);
    const [atlasList, setAtlasList] = useState({} as Record<string, boolean>);
    const [selectedTemplateDetails, setSelectedTemplateDetails] = useState(null as null | AtlasCityDetailResponse);
    const [currentModal, setCurrentModal] = useState(Modals.NONE);
    const [lastExportedAtlasFolder, setLastExportedAtlasFolder] = useState("");
    const [buildIdx, setBuildIdx] = useState(0);

    useEffect(() => { TextureAtlasService.listAvailableLibraries().then(setAtlasList) }, [selectedAtlas])
    useEffect(() => {
        TextureAtlasService.getCityAtlasDetail(selectedAtlas!).then((x) => {
            setSelectedTemplateDetails(x)
            if (selectedAtlas) setTimeout(() => setBuildIdx(buildIdx + 1), 600);
        })
    }, [selectedAtlas, buildIdx])



    const actions = atlasList[selectedAtlas!] ? [
        { className: "negativeBtn", action() { setCurrentModal(Modals.CONFIRMING_DELETE) }, text: T_delete },
        null,
        { className: "neutralBtn", action() { setCurrentModal(Modals.EXPORTING_ATLAS) }, text: T_export },

    ] : [{ className: "positiveBtn", action() { setIsCopyingToCity(true) }, text: T_addToSaveGame }]

    const detailsFields = selectedTemplateDetails ? [
        { key: T_usages, value: formatInteger(selectedTemplateDetails.usages) },
        { key: T_source, value: atlasList[selectedAtlas!] ? T_sourceSaveGame : T_sourceLibraryFolder },
        { key: T_imageCount, value: formatInteger(selectedTemplateDetails.imageCount) },
        { key: T_textureSize, value: formatInteger(selectedTemplateDetails.textureSize) }
    ] : undefined

    const exportTemplateCallback = async (fileName?: string) => {
        if (!fileName || !selectedAtlas) return;
        var filepath = await TextureAtlasService.exportCityAtlas(selectedAtlas, fileName);
        setLastExportedAtlasFolder(filepath)
        setCurrentModal(Modals.SUCCESS_EXPORTING_TEMPLATE);
    }

    const displayingModal = () => {
        switch (currentModal) {
            case Modals.CONFIRMING_DELETE: return <ConfirmationDialog onConfirm={() => { setCurrentModal(0); TextureAtlasService.removeFromCity(selectedAtlas!); setSelectedAtlas(null) }} onCancel={() => setCurrentModal(0)} message={replaceArgs(T_confirmDeleteText, { "name": selectedAtlas ?? "?????" })} />
            case Modals.EXPORTING_ATLAS: return <BaseStringInputDialog onConfirm={exportTemplateCallback} dialogTitle={T_exportDialogTitle} dialogPromptText={T_exportDialogText} initialValue={selectedAtlas!} />
            case Modals.SUCCESS_EXPORTING_TEMPLATE: return <ConfirmationDialog onConfirm={() => { TextureAtlasService.openExportFolder(lastExportedAtlasFolder); setCurrentModal(0) }} onCancel={() => setCurrentModal(0)} confirm={T_goToFileFolder} cancel={T_back} message={replaceArgs(T_successMessage, { "name": lastExportedAtlasFolder ?? "?????" })} />
        }
    }

    const [isCopyingToCity, setIsCopyingToCity] = useState(false);
    const onCopyToCity = (x?: string) => {
        if (x) {
            TextureAtlasService.copyToCity(selectedAtlas!, x);
            setSelectedAtlas(null);
            TextureAtlasService.listAvailableLibraries().then(y => { setAtlasList(y); setSelectedAtlas(x!) })
        }
    }

    const [alertToDisplay, setAlertToDisplay] = useState(undefined as string | undefined)
    const listActions: ListActionTypeArray = []

    function getItems(atlasList: Record<string, boolean>): Parameters<typeof WEListWithPreviewTab>[0]['listItems'] {
        const cityAtlases = Object.entries(atlasList ?? {}).filter(x => x[1]).map(x => x[0]).sort((a, b) => a.toLowerCase().normalize("NFKD").localeCompare(b.toLowerCase().normalize("NFKD")))
        const localAtlases = Object.entries(atlasList ?? {}).filter(x => !x[1]).map(x => x[0]).sort((a, b) => a.toLowerCase().normalize("NFKD").localeCompare(b.toLowerCase().normalize("NFKD")))
        return [
            { section: T_cityAtlasesSection },
            ...(cityAtlases.length ? cityAtlases : [{ emptyPlaceholder: T_noCityAtlases }]),
            { section: T_localAtlasesSection },
            ...(localAtlases.length ? localAtlases : [{ emptyPlaceholder: T_noLocalAtlases }]),
        ]
    }

    return <>
        <WEListWithPreviewTab listActions={listActions} itemActions={actions} detailsFields={detailsFields} listItems={getItems(atlasList)} selectedKey={selectedAtlas!} onChangeSelection={setSelectedAtlas} >
            {selectedAtlas && <div className="k45_we_atlasPreviewImg" style={{ backgroundImage: `url(coui://we.k45/_textureAtlas/${selectedAtlas})` }} />}
        </WEListWithPreviewTab>
        <StringInputDialog dialogTitle={T_addToCityDialogTitle} dialogPromptText={T_addToCityDialogText} validationFn={(x) => !!x && !atlasList[x] && x.length <= 30} initialValue={selectedAtlas!}
            maxLength={30} isActive={isCopyingToCity} setIsActive={setIsCopyingToCity} actionOnSuccess={onCopyToCity}
        />
        <Portal>
            {alertToDisplay && <ConfirmationDialog onConfirm={() => { setAlertToDisplay(void 0); }} cancellable={false} dismissable={false} message={alertToDisplay} confirm={"OK"} />}
            {displayingModal()}
        </Portal>
    </>
};

