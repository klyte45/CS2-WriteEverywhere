import { LocElementType, replaceArgs, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { FilePickerDialog } from "common/FilePickerDialog";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { WEListWithContentTab } from "common/WEListWithContentTab";
import { ListActionTypeArray, WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { FocusBoundary } from "cs2/input";
import { ConfirmationDialog, Portal, Scrollable, Tooltip } from "cs2/ui";
import { ObjectTyped } from "object-typed";
import { useEffect, useState } from "react";
import { FontService } from "services/FontService";
import { LayoutsService, ModReplacementData } from "services/LayoutsService";
import { TextureAtlasService } from "services/TextureAtlasService";
import "style/mainUi/layoutsReplacementsTab.scss";
import { translate } from "utils/translate";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
}


export const PrefabTemplatesReplacementsTab = (props: Props) => {
    const i_saveItem = "coui://uil/Standard/DiskSave.svg";
    const i_loadItem = "coui://uil/Standard/Folder.svg";

    const T_saveBtn = translate("prefabTemplatesReplacementsTab.saveBtn")
    const T_loadBtn = translate("prefabTemplatesReplacementsTab.loadBtn")

    const T_fontsToReplace = translate("prefabTemplatesReplacementsTab.fontsToReplace")
    const T_noFontsToReplace = translate("prefabTemplatesReplacementsTab.noFontsToReplace")
    const T_atlasToReplace = translate("prefabTemplatesReplacementsTab.atlasToReplace")
    const T_noAtlasToReplace = translate("prefabTemplatesReplacementsTab.noAtlasToReplace")

    const T_saveSettingsDialogTitle = translate("prefabTemplatesReplacementsTab.saveSettingsDialogTitle")
    const T_saveSettingsDialogText = translate("prefabTemplatesReplacementsTab.saveSettingsDialogText")
    const T_confirmOverrideText = translate("prefabTemplatesReplacementsTab.confirmOverrideText")
    const T_savedAtDialogTemplate = translate("prefabTemplatesReplacementsTab.savedAtDialogTemplate")
    const T_goToFileFolder = translate("prefabTemplatesReplacementsTab.goToSavedReplacementsFolder")

    const T_loadSettingsDialogTitle = translate("prefabTemplatesReplacementsTab.loadSettingsDialogTitle")
    const T_loadSettingsDialogText = translate("prefabTemplatesReplacementsTab.loadSettingsDialogText")


    const [modsReplacementData, setModReplacementData] = useState({} as Record<string, ModReplacementData>)
    const [selectedMod, setSelectedMod] = useState(null as string | null)
    const [fontList, setFontList] = useState([] as string[])
    const [atlasList, setAtlasList] = useState([] as string[])
    const [modLayoutReplacementFolder, setModLayoutReplacementFolder] = useState("");
    const [extensionsImport, setExtensionsImport] = useState("")
    const [alertToDisplay, setAlertToDisplay] = useState(undefined as string | undefined)


    const [isSavingReplacements, setIsSavingReplacements] = useState(false);

    const onSaveReplacements = async (x: string) => {
        const savedPath = await LayoutsService.saveReplacementSettings(x);
        setAlertToDisplay(replaceArgs(T_savedAtDialogTemplate, { path: savedPath.replaceAll("\\", "/").split("/").reverse()[0] }))
    }
    const [isLoadingSettings, setIsLoadingSettings] = useState(false);
    const onLoadSettings = async (x?: string) => {
        if (x && await LayoutsService.loadReplacementSettings(x)) {
            await getSettings();
        }
    }


    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const EditorRow = VanillaWidgets.instance.EditorItemRow;

    const getSettings = async () => {
        const replacemendData = await LayoutsService.listModsReplacementData();
        setModReplacementData(ObjectTyped.fromEntries(replacemendData.map(y => [y.modId, y])));
    }
    useEffect(() => {
        getSettings();
        FontService.listCityFonts().then((x) => {
            setFontList([""].concat(Object.keys(x).sort((a, b) => a.localeCompare(b))))
        })
        Promise.all([
            TextureAtlasService.listAvailableLibraries(),
            TextureAtlasService.listModAtlases()
        ]).then(([x, y]) => {
            setAtlasList([...Object.keys(x), ...y.flatMap(z => z.Atlases).sort((a, b) => a.localeCompare(b))]);
        });
        LayoutsService.getLocationSavedReplacements().then(setModLayoutReplacementFolder)
        LayoutsService.getExtensionSavedReplacements().then((x) => setExtensionsImport("*." + x))
    }, [])

    const currentFontList = (selectedMod && modsReplacementData[selectedMod]?.fonts) ?? {};
    const currentAtlasList = (selectedMod && modsReplacementData[selectedMod]?.atlases) ?? {};
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;
    const noFocus = VanillaComponentResolver.instance.FOCUS_DISABLED;

    const listActions: ListActionTypeArray = [
        {
            isContext: false,
            onSelect: () => setIsSavingReplacements(true),
            src: i_saveItem,
            tooltip: T_saveBtn,
            focusKey: noFocus,
            className: buttonClass
        },
        {
            isContext: false,
            onSelect: () => setIsLoadingSettings(true),
            src: i_loadItem,
            tooltip: T_loadBtn,
            focusKey: noFocus,
            className: buttonClass
        },
    ]

    return <>
        <WEListWithContentTab listActions={listActions}
            listItems={Object.values(modsReplacementData).map(x => { return { displayName: x.displayName, value: x.modId } }).sort((a, b) => a.displayName.localeCompare(b.displayName))}
            selectedKey={selectedMod!} onChangeSelection={setSelectedMod} bodyClasses="layoutReplacementTab" >
            {selectedMod && <>
                <div className="k45_we_layoutReplacementTab_fonts">
                    {Object.keys(currentFontList).length == 0 ? <div className="emptyDataContainer">{T_noFontsToReplace}</div> : <>
                        <div className="sectionTitle">{T_fontsToReplace}</div>
                        <Scrollable>
                            {Object.entries(currentFontList).map((x, i) => <Tooltip tooltip={x[0]}><EditorRow key={i} label={x[0]}>
                                <DropdownField
                                    value={x[1] ?? ""}
                                    items={(fontList ?? []).map(x => { return { displayName: { __Type: LocElementType.String, value: !x ? "--DEFAULT--" : x }, value: x } })}
                                    onChange={(y) => {
                                        LayoutsService.setModFontReplacement(selectedMod, x[0], y).then(z => {
                                            modsReplacementData[selectedMod].fonts[x[0]] = z
                                            setModReplacementData({ ...modsReplacementData });
                                        })
                                    }}
                                />
                            </EditorRow></Tooltip>)}
                        </Scrollable>
                    </>}
                </div>
                <div className="k45_we_layoutReplacementTab_atlases">
                    {Object.keys(currentAtlasList).length == 0 ? <div className="emptyDataContainer">{T_noAtlasToReplace}</div> : <>
                        <div className="sectionTitle">{T_atlasToReplace}</div>
                        <Scrollable >
                            {Object.entries(currentAtlasList).map((x, i) => <Tooltip tooltip={x[0]}><EditorRow key={i} label={x[0]}><DropdownField
                                value={x[1] ?? x[0]}
                                items={(atlasList ?? []).map(x => { return { displayName: { __Type: LocElementType.String, value: !x ? "--DEFAULT--" : x }, value: x } })}
                                onChange={(y) => {
                                    LayoutsService.setModAtlasReplacement(selectedMod, x[0], y).then(z => {
                                        modsReplacementData[selectedMod].atlases[x[0]] = z
                                        setModReplacementData({ ...modsReplacementData });
                                    })
                                }}
                            /></EditorRow>
                            </Tooltip>)}
                        </Scrollable>
                    </>}
                </div>
            </>}
        </WEListWithContentTab>
        <StringInputWithOverrideDialog
            dialogTitle={T_saveSettingsDialogTitle}
            dialogPromptText={T_saveSettingsDialogText}
            dialogOverrideText={T_confirmOverrideText}
            actionOnSuccess={onSaveReplacements}
            isActive={isSavingReplacements}
            setIsActive={setIsSavingReplacements}
            initialValue=""
            isShortCircuitCheckFn={(x) => !x}
            checkIfExistsFn={LayoutsService.checkReplacementSettingFileExists}
        />
        <FilePickerDialog
            dialogTitle={T_loadSettingsDialogTitle}
            dialogPromptText={T_loadSettingsDialogText}
            isActive={isLoadingSettings}
            setIsActive={setIsLoadingSettings}
            actionOnSuccess={onLoadSettings}
            allowedExtensions={extensionsImport}
            initialFolder={modLayoutReplacementFolder}
        />
        <Portal>
            {alertToDisplay && <ConfirmationDialog onConfirm={() => { LayoutsService.openExportedReplacementSettingsFolder(); setAlertToDisplay(void 0) }} onCancel={() => setAlertToDisplay(void 0)} confirm={T_goToFileFolder} cancel={"OK"} message={alertToDisplay} />}
        </Portal>
    </>
};

