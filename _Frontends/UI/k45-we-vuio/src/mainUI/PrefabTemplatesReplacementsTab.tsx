import { LocElementType, PropsDropdownField, replaceArgs, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import useAsyncMemo from "@klyte45/vuio-commons/src/utils/useAsyncMemo";
import { FilePickerDialog } from "common/FilePickerDialog";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { WEListWithContentTab } from "common/WEListWithContentTab";
import { ListActionTypeArray } from "common/WEListWithPreviewTab";
import { FocusDisabled } from "cs2/input";
import { ConfirmationDialog, Portal, Scrollable, Tooltip } from "cs2/ui";
import { ObjectTyped } from "object-typed";
import { CSSProperties, useEffect, useState } from "react";
import { FontService } from "services/FontService";
import { LayoutsService, ModReplacementData } from "services/LayoutsService";
import { TextureAtlasService } from "services/TextureAtlasService";
import { WEModuleOptionFieldTypes, WEModuleService } from "services/WEModulesService";
import "style/mainUi/layoutsReplacementsTab.scss";
import { translate } from "utils/translate";

const engine = (window as any).engine;

type Props = {}

enum SubTab {
    OPTIONS = "OPTIONS",
    FONTS = "FONTS",
    ATLASES = "ATLASES",
    SUBTEMPLATES = "SUBTEMPLATES"
}
type ModEntryOptions = {
    modName: string;
    itemList: string[];
};

type SelectableEntriesRecord = Record<string, ModEntryOptions>

export const PrefabTemplatesReplacementsTab = (props: Props) => {
    const i_saveItem = "coui://uil/Standard/DiskSave.svg";
    const i_loadItem = "coui://uil/Standard/Folder.svg";

    const T_saveBtn = translate("prefabTemplatesReplacementsTab.saveBtn")
    const T_loadBtn = translate("prefabTemplatesReplacementsTab.loadBtn")

    const T_fontsToReplace = translate("prefabTemplatesReplacementsTab.fontsToReplace")
    const T_fontsToReplaceTab = translate("prefabTemplatesReplacementsTab.fontsToReplaceTab")
    const T_atlasToReplace = translate("prefabTemplatesReplacementsTab.atlasToReplace")
    const T_atlasToReplaceTab = translate("prefabTemplatesReplacementsTab.atlasToReplaceTab")
    const T_subtemplatesToReplace = translate("prefabTemplatesReplacementsTab.subtemplatesToReplace")
    const T_subtemplatesToReplaceTab = translate("prefabTemplatesReplacementsTab.subtemplatesToReplaceTab")
    const T_weModuleOptionsTab = translate("prefabTemplatesReplacementsTab.weModuleOptionsTab")

    const T_currentCitySource = translate("prefabTemplatesReplacementsTab.currentCitySourceTitle")

    const T_saveSettingsDialogTitle = translate("prefabTemplatesReplacementsTab.saveSettingsDialogTitle")
    const T_saveSettingsDialogText = translate("prefabTemplatesReplacementsTab.saveSettingsDialogText")
    const T_confirmOverrideText = translate("prefabTemplatesReplacementsTab.confirmOverrideText")
    const T_savedAtDialogTemplate = translate("prefabTemplatesReplacementsTab.savedAtDialogTemplate")
    const T_goToFileFolder = translate("prefabTemplatesReplacementsTab.goToSavedReplacementsFolder")

    const T_loadSettingsDialogTitle = translate("prefabTemplatesReplacementsTab.loadSettingsDialogTitle")
    const T_loadSettingsDialogText = translate("prefabTemplatesReplacementsTab.loadSettingsDialogText")

    const M_tabTitle: Record<SubTab, string> = {
        [SubTab.OPTIONS]: T_weModuleOptionsTab,
        [SubTab.ATLASES]: T_atlasToReplaceTab,
        [SubTab.FONTS]: T_fontsToReplaceTab,
        [SubTab.SUBTEMPLATES]: T_subtemplatesToReplaceTab,
    }


    const [modsReplacementData, setModReplacementData] = useState({} as Record<string, ModReplacementData>)
    const [selectedMod, setSelectedMod] = useState(null as string | null)
    const [fontList, setFontList] = useState<SelectableEntriesRecord>({})
    const [atlasList, setAtlasList] = useState<SelectableEntriesRecord>({})
    const [selectableTemplatesList, setSelectableTemplatesList] = useState<SelectableEntriesRecord>({})
    const [modLayoutReplacementFolder, setModLayoutReplacementFolder] = useState("");
    const [extensionsImport, setExtensionsImport] = useState("")
    const [alertToDisplay, setAlertToDisplay] = useState(undefined as string | undefined)
    const [modsOptions, setModsOptions] = useState<Record<string, Record<string, WEModuleOptionFieldTypes>>>({})


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


    const getSettings = async () => {
        await LayoutsService.listModsReplacementData().then(replacemendData =>
            setModReplacementData(ObjectTyped.fromEntries(replacemendData.map(y => [y.modId, y])))
        );
        await WEModuleService.listAllOptions().then(x => setModsOptions(x));
    }
    useEffect(() => {
        getSettings();
        FontService.listCityFonts().then((x) => {
            setFontList({
                "": { modName: T_currentCitySource, itemList: [""].concat(Object.keys(x).sort((a, b) => a.localeCompare(b))) }
            })
        })
        Promise.all([
            TextureAtlasService.listAvailableLibraries(),
            TextureAtlasService.listModAtlases()
        ]).then(([x, y]) => {
            setAtlasList({
                "": { modName: T_currentCitySource, itemList: Object.keys(x).sort((a, b) => a.localeCompare(b)) },
                ...ObjectTyped.fromEntries(y.map(z => [z.ModId, { modName: z.ModName, itemList: z.Atlases.sort((a, b) => a.localeCompare(b)) }]))
            });
        });
        Promise.all([
            LayoutsService.listCityTemplates(),
            LayoutsService.listModSubtemplates()
        ]).then(([x, y]) => {
            setSelectableTemplatesList({
                "": { modName: T_currentCitySource, itemList: Object.keys(x).sort((a, b) => a.localeCompare(b)) },
                ...ObjectTyped.fromEntries(y.map(z => [z.ModId, { modName: z.ModName, itemList: z.Subtemplates.filter(x => !x.includes(":___")).sort((a, b) => a.localeCompare(b)) }] as [string, ModEntryOptions]).filter(x => x[1].itemList.length))
            });
        });
        LayoutsService.getLocationSavedReplacements().then(setModLayoutReplacementFolder)
        LayoutsService.getExtensionSavedReplacements().then((x) => setExtensionsImport("*." + x))
    }, [])
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

    const TabBar = VanillaComponentResolver.instance.TabBar;
    const Tab = VanillaComponentResolver.instance.Tab;
    const TabNav = VanillaComponentResolver.instance.TabNav;
    const [tabs, setTabs] = useState<SubTab[]>([])
    const [selectedEditorTab, setSelectedEditorTab] = useState<SubTab | null>(null)

    useEffect(() => {
        if (selectedMod) {
            const replacementData = modsReplacementData[selectedMod];
            const optionsData = modsOptions[selectedMod];
            const newTabs = [
                Object.keys(optionsData ?? {}).length && SubTab.OPTIONS,
                Object.keys(replacementData?.fonts ?? {}).length && SubTab.FONTS,
                Object.keys(replacementData?.atlases ?? {}).length && SubTab.ATLASES,
                Object.keys(replacementData?.subtemplates ?? {}).length && SubTab.SUBTEMPLATES,
            ].filter(x => x) as SubTab[]
            setTabs(newTabs);
            setSelectedEditorTab(newTabs[0]);
        }
    }, [selectedMod])


    useEffect(() => {
        if (selectedMod && selectedEditorTab) {
            switch (selectedEditorTab) {
                case SubTab.ATLASES:
                    setCurrentItemObj(modsReplacementData[selectedMod].atlases ?? {})
                    setCurrentTitle(T_atlasToReplace)
                    setCurrentOptionList(atlasList)
                    break;
                case SubTab.FONTS:
                    setCurrentItemObj(modsReplacementData[selectedMod].fonts ?? {})
                    setCurrentTitle(T_fontsToReplace)
                    setCurrentOptionList(fontList)
                    break;
                case SubTab.SUBTEMPLATES:
                    setCurrentItemObj(modsReplacementData[selectedMod].subtemplates ?? {})
                    setCurrentTitle(T_subtemplatesToReplace)
                    setCurrentOptionList(selectableTemplatesList)
                    break;
            }
        }
    }, [selectedMod, selectedEditorTab])


    const getDefaultValue = (key: string) => {
        switch (selectedEditorTab) {
            case SubTab.FONTS:
                return "";
            case SubTab.ATLASES:
            case SubTab.SUBTEMPLATES:
            default:
                return `${selectedMod}:${key}`;
        }
    }

    const setValue = async (key: string, newValue: string) => {
        if (selectedMod)
            switch (selectedEditorTab) {
                case SubTab.FONTS:
                    return await LayoutsService.setModFontReplacement(selectedMod, key, newValue).then(z => {
                        modsReplacementData[selectedMod].fonts[key] = z
                        setModReplacementData({ ...modsReplacementData });
                    });
                case SubTab.ATLASES:
                    return await LayoutsService.setModAtlasReplacement(selectedMod, key, newValue).then(z => {
                        modsReplacementData[selectedMod].atlases[key] = z
                        setModReplacementData({ ...modsReplacementData });
                    });
                case SubTab.SUBTEMPLATES:
                    return await LayoutsService.setModSubtemplateReplacement(selectedMod, key, newValue).then(z => {
                        modsReplacementData[selectedMod].subtemplates[key] = z
                        setModReplacementData({ ...modsReplacementData });
                    });
            }
    }


    const [currentItemObj, setCurrentItemObj] = useState<Record<string, string>>({})
    const [currentTitle, setCurrentTitle] = useState<string>()
    const [currentOptionList, setCurrentOptionList] = useState<SelectableEntriesRecord>({})

    const getCurrentTabContent = () => {
        if (selectedEditorTab == SubTab.OPTIONS) {
            const currentOptions = ObjectTyped.entries(modsOptions[selectedMod!] ?? {});
            return <Scrollable>
                <FocusDisabled>
                    {currentOptions.map(([i18n, optionObj], i) => {
                        return <OptionRow key={i} selectedModule={selectedMod!} i18n={i18n} optionObj={optionObj} />;
                    })}
                </FocusDisabled>
            </Scrollable>
        } else {
            return <>
                <div className="sectionTitle">{currentTitle}</div>
                <Scrollable>
                    {Object.entries(currentItemObj).map((x, i) => <RowData
                        currentOptionList={currentOptionList} getDefaultValue={getDefaultValue}
                        itemKey={x[0]} itemValue={x[1]} rowIndex={i} setValue={setValue}
                    />)}
                </Scrollable>
            </>
        }
    }

    return <>
        <WEListWithContentTab listActions={listActions}
            listItems={Object.values(modsReplacementData).map(x => { return { displayName: x.displayName, value: x.modId } }).sort((a, b) => a.displayName.localeCompare(b.displayName))}
            selectedKey={selectedMod!} onChangeSelection={setSelectedMod} bodyClasses="layoutReplacementTab">
            {selectedMod && <>
                <TabBar>{
                    tabs.map(x => <Tab id={x} selectedId={selectedEditorTab} onSelect={() => setSelectedEditorTab(x)}>{M_tabTitle[x]}</Tab>)
                }</TabBar>
                <TabNav tabs={tabs} selectedTab={selectedEditorTab}>
                    {getCurrentTabContent()}
                </TabNav>
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
            {alertToDisplay && <ConfirmationDialog onConfirm={() => { LayoutsService.openExportedReplacementSettingsFolder(); setAlertToDisplay(void 0) }}
                onCancel={() => setAlertToDisplay(void 0)} confirm={T_goToFileFolder} cancel={"OK"} message={alertToDisplay} />}
        </Portal>
    </>
};

const subcolumnStyle: CSSProperties = { display: "flex", flexDirection: 'column', flexGrow: 1, flexShrink: 1, position: "relative" };

type RowDataEditorProps = {
    getDefaultValue: (itemKey: string) => string,
    currentOptionList: SelectableEntriesRecord,
    setValue: (key: string, newValue: string) => Promise<any>
    rowIndex: number,
    itemKey: string,
    itemValue: string
}

const RowData = ({
    getDefaultValue,
    currentOptionList,
    setValue,
    rowIndex,
    itemKey,
    itemValue
}: RowDataEditorProps) => {
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const EditorRow = VanillaWidgets.instance.EditorItemRowNoFocus;

    const effectiveValue = (itemValue ?? getDefaultValue(itemKey)).split(":", 2);
    const [srcMod, setSrcMod] = useState(effectiveValue.length <= 1 ? "" : effectiveValue[0]);

    useEffect(() => {
        setSrcMod(effectiveValue.length <= 1 ? "" : effectiveValue[0])
    }, [currentOptionList])

    const currentModList = ObjectTyped.entries(currentOptionList) ?? [];
    const currentSelectionList = currentOptionList[srcMod]?.itemList ?? [];

    return <FocusDisabled>
        <EditorRow key={rowIndex} label={itemKey}>
            <DropdownField
                disabled={currentModList.length == 0 || (currentModList.length == 1 && currentModList[0][0] == srcMod)}
                value={srcMod}
                items={currentModList.map(x => ({ displayName: { __Type: LocElementType.String, value: x[1].modName }, value: x[0] }))}
                onChange={(y) => setSrcMod(y)}
                autoFocus={false}
            />
            <DropdownField
                disabled={currentSelectionList.length == 0}
                value={effectiveValue.join(":")}
                items={currentSelectionList.map(x => ({ displayName: { __Type: LocElementType.String, value: !x ? "--DEFAULT--" : x.split(":").reverse()[0].replace(/^__/g, "") }, value: x }))}
                onChange={(y) => setValue(itemKey, y)}
                autoFocus={false}
            />
        </EditorRow>
    </FocusDisabled>;
    ;
}

const OptionRow = ({ selectedModule, i18n, optionObj }: { selectedModule: string, i18n: string, optionObj: WEModuleOptionFieldTypes }) => {
    switch (optionObj) {
        case WEModuleOptionFieldTypes.BOOLEAN:
            return <BooleanOptionRow module={selectedModule} i18n={i18n} />;
        case WEModuleOptionFieldTypes.DROPDOWN:
            return <DropdownOptionRow module={selectedModule} i18n={i18n} />;
        default:
            return <>{engine.translate(i18n)} (TYPE = {optionObj} ????)</>;
    }
};

const BooleanOptionRow = ({ module, i18n }: { module: string, i18n: string }) => {
    const [buildIdx, setBuildIdx] = useState(0);

    const value = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<boolean>(module, i18n);
        return result;
    }, [buildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRowNoFocus;
    const BoooleanField = VanillaWidgets.instance.Checkbox;
    return <EditorRow label={engine.translate(i18n)}>
        <BoooleanField checked={value ?? false} onChange={x => WEModuleService.setFieldValue(module, i18n, x).then(() => setBuildIdx(buildIdx + 1))} />
    </EditorRow>;
};

const DropdownOptionRow = ({ module, i18n }: { module: string, i18n: string }) => {
    const [buildIdx, setBuildIdx] = useState(0);

    const value = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<string>(module, i18n);
        return result;
    }, [buildIdx]);

    const optionsData = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldOptions(module, i18n);
        return ObjectTyped.entries(result).map(x => ({ displayName: { __Type: LocElementType.String as any, value: x[1] }, value: x[0] }));
    }, [buildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRowNoFocus;
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    return <EditorRow label={engine.translate(i18n)}>
        <DropdownField value={value ?? ""} items={optionsData ?? []} onChange={x => WEModuleService.setFieldValue(module, i18n, x).then(() => setBuildIdx(buildIdx + 1))} />
    </EditorRow>;
};
