import { ColorHSVA, LocElementType, PropsDropdownField, replaceArgs, UIColorRGBA, VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import useAsyncMemo from "@klyte45/vuio-commons/src/utils/useAsyncMemo";
import classNames from "classnames";
import { FilePickerDialog } from "common/FilePickerDialog";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { WEListWithContentTab } from "common/WEListWithContentTab";
import { ListActionTypeArray } from "common/WEListWithPreviewTab";
import { FocusBoundary, FocusDisabled } from "cs2/input";
import { ConfirmationDialog, Portal, Scrollable, Tooltip } from "cs2/ui";
import { ObjectTyped } from "object-typed";
import { CSSProperties, useCallback, useEffect, useMemo, useRef, useState } from "react";
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

    const [tabBuildIdx, setTabBuildIdx] = useState(0);
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
        engine.on("k45::we.modules.reloadOptions!", () => setTabBuildIdx(z => z + 1));
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
            return <Scrollable className="k45_we_moduleOptionsTab">
                <FocusDisabled>
                    {currentOptions.map(([i18n, optionObj], i) => {
                        return <OptionRow key={i} selectedModule={selectedMod!} i18n={i18n} optionObj={optionObj} tabBuildIdx={tabBuildIdx} />;
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

const OptionRow = ({ selectedModule, i18n, optionObj, tabBuildIdx }: { selectedModule: string, i18n: string, optionObj: WEModuleOptionFieldTypes, tabBuildIdx: number }) => {
    const tooltipKey = i18n.replace(/(]?)$/, ".tooltip$1");
    let tooltip = engine.translate(tooltipKey);
    if (tooltip === tooltipKey) tooltip = null;
    const content = ((x: WEModuleOptionFieldTypes) => {
        switch (x) {
            case WEModuleOptionFieldTypes.BOOLEAN:
                return <BooleanOptionRow module={selectedModule} i18n={i18n} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.DROPDOWN:
                return <DropdownOptionRow module={selectedModule} i18n={i18n} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.SECTION_TITLE: return <h4>{engine.translate(i18n)}</h4>;
            case WEModuleOptionFieldTypes.BUTTON_ROW: return <ButtonRowOptionsRow module={selectedModule} i18n={i18n} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.SLIDER: return <SliderOptionsRow module={selectedModule} i18n={i18n} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.FILE_PICKER: return <FilePickerOptionsRow module={selectedModule} i18n={i18n} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.COLOR_PICKER: return <ColorPickerOptionsRow module={selectedModule} i18n={i18n} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.SPACER: return <div style={{ height: "16px" }} />;
            case WEModuleOptionFieldTypes.TEXT_INPUT: return <TextInputOptionsRow module={selectedModule} i18n={i18n} multiline={false} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.MULTILINE_TEXT_INPUT: return <TextInputOptionsRow module={selectedModule} i18n={i18n} multiline={true} tabBuildIdx={tabBuildIdx} />;
            case WEModuleOptionFieldTypes.RADIO_BUTTON: return <></>;
            case WEModuleOptionFieldTypes.MULTISELECT: return <></>;
            case WEModuleOptionFieldTypes.VECTOR2: return <></>;
            case WEModuleOptionFieldTypes.VECTOR3: return <></>;
            case WEModuleOptionFieldTypes.VECTOR4: return <></>;
            case WEModuleOptionFieldTypes.INT_INPUT: return <></>;
            case WEModuleOptionFieldTypes.FLOAT_INPUT: return <></>;
            case WEModuleOptionFieldTypes.RANGE_INPUT: return <></>;
            default:
                return <>{engine.translate(i18n)} (TYPE = {optionObj} ????)</>;
        }
    })(optionObj);
    return tooltip ? <Tooltip tooltip={tooltip}>{content}</Tooltip> : content;
};

const BooleanOptionRow = ({ module, i18n, tabBuildIdx }: { module: string, i18n: string, tabBuildIdx: number }) => {
    const [buildIdx, setBuildIdx] = useState(0);

    const value = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<boolean>(module, i18n);
        return result;
    }, [buildIdx, tabBuildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRow;
    const BoooleanField = VanillaWidgets.instance.Checkbox;
    return <EditorRow label={engine.translate(i18n)}>
        <BoooleanField checked={value ?? false} onChange={x => WEModuleService.setFieldValue(module, i18n, x).then(() => setBuildIdx(buildIdx + 1))} />
    </EditorRow>;
};

const DropdownOptionRow = ({ module, i18n, tabBuildIdx }: { module: string, i18n: string, tabBuildIdx: number }) => {
    const [buildIdx, setBuildIdx] = useState(0);

    const value = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<string>(module, i18n);
        return result;
    }, [buildIdx, tabBuildIdx]);

    const optionsData = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldOptions(module, i18n);
        return ObjectTyped.entries(result).map(x => ({ displayName: { __Type: LocElementType.String as any, value: x[1].startsWith("__") ? x[1].replace(/^__/g, "").trim() : engine.translate(x[1]) }, value: x[0] }));
    }, [buildIdx, tabBuildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    return <EditorRow label={engine.translate(i18n)}>
        <DropdownField value={value ?? ""} items={optionsData ?? []} onChange={x => WEModuleService.setFieldValue(module, i18n, x).then(() => setBuildIdx(buildIdx + 1))} />
    </EditorRow>;
};

const ButtonRowOptionsRow = ({ module, i18n, tabBuildIdx }: { module: string, i18n: string, tabBuildIdx: number }) => {
    const optionsData = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldOptions(module, i18n);
        return ObjectTyped.entries(result);
    }, [tabBuildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRowNoFocus;
    const Button = VanillaComponentResolver.instance.CommonButton;
    return <EditorRow styleContent={{ flexDirection: "row-reverse" }} label={i18n.startsWith("__") ? i18n.replace(/^__/g, "").trim() : engine.translate(i18n)}>
        {optionsData?.map(([key, value]) => {
            const tooltipKey = i18n.replace(/(]?)$/, ".tooltip$1");
            let tooltip = engine.translate(tooltipKey);
            if (tooltip === tooltipKey) tooltip = null;
            const content = <Button key={key} className="k45_we neutralBtn" onClick={() => WEModuleService.setFieldValue(module, i18n, key)}>{value.startsWith("__") ? value.replace(/^__/g, "").trim() : engine.translate(value)}</Button>;
            return tooltip ? <Tooltip tooltip={tooltip}>{content}</Tooltip> : content;
        }
        )}
    </EditorRow>;
};

const SliderOptionsRow = ({ module, i18n, tabBuildIdx }: { module: string, i18n: string, tabBuildIdx: number }) => {
    const [buildIdx, setBuildIdx] = useState(0);

    const value = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<number>(module, i18n);
        return result;
    }, [buildIdx, tabBuildIdx]);

    const range = useAsyncMemo(async () => {
        const result = await WEModuleService.getMinMax(module, i18n);
        return { min: result[0][0], max: result[1][0] };
    }, [buildIdx, tabBuildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRow;
    const SliderField = VanillaWidgets.instance.FloatSlider;
    return <EditorRow label={engine.translate(i18n)}>
        {range && <SliderField value={value ?? 0} fractionDigits={3} min={range?.min} max={range?.max} onChange={x => WEModuleService.setFieldValue(module, i18n, x).then(() => setBuildIdx(buildIdx + 1))} />}
    </EditorRow>;
};

const FilePickerOptionsRow = ({ module, i18n, tabBuildIdx }: { module: string, i18n: string, tabBuildIdx: number }) => {
    const [buildIdx, setBuildIdx] = useState(0);
    const [showPicker, setShowPicker] = useState(false);

    const value = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<string>(module, i18n);
        return result;
    }, [buildIdx, tabBuildIdx]);

    const filePickerOptions = useAsyncMemo(async () => {
        const result = await WEModuleService.getFilePickerOptions(module, i18n);
        return result;
    }, [buildIdx, tabBuildIdx]);

    const EditorRow = VanillaWidgets.instance.EditorItemRow;
    const Button = VanillaComponentResolver.instance.CommonButton;
    return <><EditorRow label={engine.translate(i18n)}>
        <Button onClick={() => setShowPicker(true)} className="k45_we neutralBtn">{engine.translate("Common.Browse")}</Button>
    </EditorRow>
        {filePickerOptions && <FilePickerDialog
            dialogTitle={engine.translate(i18n)}
            dialogPromptText={filePickerOptions.promptText}
            isActive={showPicker}
            setIsActive={setShowPicker}
            actionOnSuccess={(x) => WEModuleService.setFieldValue(module, i18n, x ?? "").then(() => setBuildIdx(buildIdx + 1))}
            allowedExtensions={filePickerOptions.fileExtensionFilter}
            initialFolder={value?.split("/").slice(0, -1).join("/") ?? filePickerOptions.initialFolder}
        />
        }
    </>;
};

const ColorPickerOptionsRow = ({ module, i18n, tabBuildIdx }: { module: string, i18n: string, tabBuildIdx: number }) => {
    const [buildIdx, setBuildIdx] = useState(0);

    const [prevHue, setPrevHue] = useState(0)
    const [menuOpen, setMenuOpen] = useState(false)

    const colorStringRGB = useAsyncMemo(async () => {
        const result = await WEModuleService.getFieldValue<string>(module, i18n);
        return result;
    }, [buildIdx, tabBuildIdx]);

    const colorHsv = useMemo(() => {
        return colorStringRGB ? VanillaColorUtils.rgbaToHsva(VanillaColorUtils.parseRgba(colorStringRGB), prevHue) : undefined;
    }, [colorStringRGB, prevHue]);

    const EditorRow = VanillaWidgets.instance.EditorItemRow;
    const ColorPicker = VanillaComponentResolver.instance.ColorPicker;

    const VanillaColorUtils = VanillaFnResolver.instance.color;

    const formatColorCss = ({ r, g, b, a }: UIColorRGBA) => `rgba(${Math.round(255 * r)},${Math.round(255 * g)},${Math.round(255 * b)},${a.toString().replace(",", ".")})`
    const onChangeColorPicker = useCallback((e: ColorHSVA) => {
        setPrevHue(e.h);
        WEModuleService.setFieldValue(module, i18n, VanillaColorUtils.formatHexColor(VanillaColorUtils.hsvaToRgba(e))).then(() => setBuildIdx(buildIdx + 1))
    }, [])
    const Button = VanillaComponentResolver.instance.CommonButton;
    const sliderTheme = VanillaComponentResolver.instance.sliderTheme;
    const editorItemTheme = VanillaComponentResolver.instance.editorItemTheme;
    const label = engine.translate(i18n);
    const btnRef = useRef(null as any as HTMLDivElement);
    const menuRef = useRef(null as any as HTMLDivElement);
    const findFixedPosition = (el: HTMLElement) => {
        const result = { left: 0, top: 0 }
        if (el) {
            let nextParent = el;
            do {
                result.left += nextParent.offsetLeft;
                result.top += nextParent.offsetTop;
            } while ((nextParent = nextParent.offsetParent as HTMLElement) && !isNaN(nextParent.offsetLeft))
        }
        return result;
    }
    const menuPosition = findFixedPosition(btnRef.current)
    const menuCss = { bottom: window.innerHeight - menuPosition.top + 3, right: window.innerWidth - menuPosition.left - btnRef.current?.offsetWidth };
    return <EditorRow label={label}>

        <div className="k45_we_formulaeEditorFieldContainer" ref={btnRef}>
            <Button className={classNames(editorItemTheme.swatch, "we_colorpicker_btn")}
                onClick={() => { setMenuOpen(!menuOpen) }}
                theme={sliderTheme}
            ><div style={{ backgroundColor: formatColorCss(VanillaColorUtils.hsvaToRgba(colorHsv!)) }}><div>{VanillaColorUtils.formatHexColor(VanillaColorUtils.hsvaToRgba(colorHsv!)).toUpperCase()}</div></div></Button>
        </div>
        {(menuOpen && colorHsv) &&
            <Portal>
                <div className="k45_comm_contextMenu k45_we_colorPickerOverlay" style={menuCss} ref={menuRef}>
                    <div className="k45_we_colorPickerTitle">{label}</div>
                    <ColorPicker alpha={false} color={colorHsv} onChange={onChangeColorPicker} />
                </div>
            </Portal>
        }
    </EditorRow>;
};

const TextInputOptionsRow = ({ module, i18n, multiline, tabBuildIdx }: { module: string, i18n: string, multiline: boolean, tabBuildIdx: number }) => {
    const EditorRow = VanillaWidgets.instance.EditorItemRow;
    const TextInput = VanillaWidgets.instance.StringInputField;
    const [buildIdx, setBuildIdx] = useState(0);
    const [typingValue, setTypingValue] = useState("")
    useEffect(() => {
        WEModuleService.getFieldValue<string>(module, i18n).then(setTypingValue)
    }, [buildIdx, tabBuildIdx]);

    return <EditorRow label={engine.translate(i18n)}>
        <TextInput value={typingValue} multiline={multiline} onChangeEnd={_ => WEModuleService.setFieldValue(module, i18n, typingValue)} onChange={setTypingValue} />
    </EditorRow>;
};