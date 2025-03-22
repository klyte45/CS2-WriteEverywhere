import { replaceArgs, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import classNames from "classnames";
import { FocusDisabled } from "cs2/input";
import { useEffect, useState } from "react";
import { FormulaeService } from "services/FormulaeService";
import { getClassNameFrom, getDllNameFrom, WEArrayIndexingDesc, WEComponentTypeDesc, WEDescType, WEFormulaeElement, WEMemberType } from "services/WEFormulaeElement";
import { IndexedComponentListing, IndexedStaticMethodsListing, WorldPickerService } from "services/WorldPickerService";
import { breakIntoFlexComponents } from "utils/breakIntoFlexComponents";
import { translate } from "utils/translate";

type Props = {
    callback: (appendResult?: WEFormulaeElement | WEArrayIndexingDesc) => any,
    referenceElement: WEFormulaeElement,
    formulaeStr: string
}

enum WEDescTypeUI {
    CURRENT_COMPONENT = "CURRENT_COMPONENT"
}


export const WEAddFormulaeStageDialog = ({ callback, referenceElement, formulaeStr }: Props) => {
    const T_addItemDialogTitle = translate("formulaeEditor.addDialog.title")
    const T_loading = translate("formulaeEditor.addDialog.loading")//Loading data... Please wait
    const T_noWay = translate("formulaeEditor.addDialog.noWay")//There are no way to go from here!
    const T_navigate = translate("formulaeEditor.addDialog.navigate")//Navigate through
    const T_runStatic = translate("formulaeEditor.addDialog.runStatic")//Run static method
    const T_pickAnyComponent = translate("formulaeEditor.addDialog.pickAnyComponent")//Pick component
    const T_pickCurrentComponent = translate("formulaeEditor.addDialog.pickFromCurrent")//Pick component
    const T_arrayIndexing = translate("formulaeEditor.addDialog.arrayIndexing")//Array indexing
    const T_arrayIndexingFieldLabel = translate("formulaeEditor.addDialog.arrayIndexingFieldLabel")//Array indexing




    const Dialog = VanillaComponentResolver.instance.Dialog;
    const EditorScrollable = VanillaWidgets.instance.EditorScrollable;
    const [ready, setReady] = useState(false)
    const [optionsMembers, setOptionsMembers] = useState({} as WEFormulaeElement[])
    const [optionsStaticMethods, setOptionsStaticMethods] = useState({} as IndexedStaticMethodsListing)
    const [optionsComponentGetter, setOptionsComponentGetter] = useState({} as IndexedComponentListing | null)
    const [selectedTab, setSelectedTab] = useState(undefined as WEDescType | WEDescTypeUI | undefined)
    const [selectedElement, setSelectedElement] = useState(undefined as WEFormulaeElement | undefined)
    const [selectedPath, setSelectedPath] = useState([] as (number | string)[])
    const [currentEntityComponent, setCurrentEntityComponent] = useState([] as WEComponentTypeDesc[])
    const [supportIndexing, setSupportIndexing] = useState(false);
    const [indexSet, setIndexSet] = useState(0);

    useEffect(() => {
        setReady(false);
        const runners = [];
        const effectiveRef = referenceElement ?? { WEDescType: WEDescType.MEMBER, memberTypeClassName: "Unity.Entities.Entity", memberTypeDllName: "Unity.Entities" }
        if (!referenceElement || (effectiveRef.WEDescType == WEDescType.STATIC_METHOD && effectiveRef.returnType == "Unity.Entities.Entity")
            || (effectiveRef.WEDescType == WEDescType.MEMBER && effectiveRef.memberTypeClassName == "Unity.Entities.Entity")) {
            runners.push(FormulaeService.listAvailableComponents().then(x => setOptionsComponentGetter(x)));
            runners.push(FormulaeService.listComponentsOnCurrentEntity(formulaeStr).then(x => setCurrentEntityComponent(x)));
        } else {
            setOptionsComponentGetter(null);
        }
        if (referenceElement) {
            runners.push(FormulaeService.listAvailableMembersForType(getDllNameFrom(effectiveRef), getClassNameFrom(effectiveRef)).then(x => setOptionsMembers(x)));
        } else {
            setOptionsMembers([]);
        }
        runners.push(FormulaeService.listAvailableMethodsForType(getDllNameFrom(effectiveRef), getClassNameFrom(effectiveRef)).then(x => setOptionsStaticMethods(x)));
        FormulaeService.isTypeIndexable(getDllNameFrom(effectiveRef), getClassNameFrom(effectiveRef)).then(setSupportIndexing)
        Promise.all(runners).then(() => setReady(true))
    }, [referenceElement?.WEDescType])

    useEffect(() => setSelectedElement(undefined), [selectedTab])

    const EditorRow = VanillaWidgets.instance.EditorItemRowNoFocus;
    const IntInput = VanillaComponentResolver.instance.IntInput;

    return <Dialog
        onClose={() => callback()}
        wide={true}
        title={T_addItemDialogTitle}
        buttons={<div className="k45_we_dialogBtns">
            {(optionsMembers || optionsStaticMethods || optionsComponentGetter) && <button className="positiveBtn" onClick={() => callback(selectedTab == WEDescType.ARRAY_INDEXING ? {
                WEDescType: WEDescType.ARRAY_INDEXING,
                index: indexSet
            } : selectedElement)} disabled={selectedTab != WEDescType.ARRAY_INDEXING && !selectedElement}>Select</button>}
            <button className="negativeBtn" onClick={() => callback()}>Back</button>
        </div>}
    >
        {!ready
            ? T_loading
            : !optionsMembers && !optionsStaticMethods && !optionsComponentGetter && !currentEntityComponent
                ? <div className="k45_we_formulaeDialog"><div className="k45_we_formulaeDialog_content">{T_noWay}</div></div>
                : <div className="k45_we_formulaeDialog">
                    <div className="k45_we_formulaeDialog_tabRow">
                        {!!optionsMembers?.length && <button onClick={() => setSelectedTab(WEDescType.MEMBER)} className={["tabBtn", selectedTab == WEDescType.MEMBER ? "selected" : ""].join(" ").trim()}>{T_navigate}</button>}
                        {!!Object.keys(optionsStaticMethods || {}).length && <button onClick={() => setSelectedTab(WEDescType.STATIC_METHOD)} className={["tabBtn", selectedTab == WEDescType.STATIC_METHOD ? "selected" : ""].join(" ").trim()}>{T_runStatic}</button>}
                        {currentEntityComponent && !!currentEntityComponent.length && <button onClick={() => setSelectedTab(WEDescTypeUI.CURRENT_COMPONENT)} className={["tabBtn", selectedTab == WEDescTypeUI.CURRENT_COMPONENT ? "selected" : ""].join(" ").trim()}>{T_pickCurrentComponent}</button>}
                        {optionsComponentGetter && !!Object.keys(optionsComponentGetter ?? {}).length && <button onClick={() => setSelectedTab(WEDescType.COMPONENT)} className={["tabBtn", selectedTab == WEDescType.COMPONENT ? "selected" : ""].join(" ").trim()}>{T_pickAnyComponent}</button>}
                        {supportIndexing && <button onClick={() => setSelectedTab(WEDescType.ARRAY_INDEXING)} className={["tabBtn", selectedTab == WEDescType.ARRAY_INDEXING ? "selected" : ""].join(" ").trim()}>{T_arrayIndexing}</button>}
                    </div>
                    {selectedTab && [WEDescType.COMPONENT, WEDescType.STATIC_METHOD].includes(selectedTab as WEDescType) && <div className="k45_we_formulaeDialog_navRow">
                        <button className="k45_we_formulaeDialog_tabRow_resetNavigation" onClick={() => setSelectedPath([])} />
                        {selectedPath.map((x, i, a) => <>
                            <div className="k45_we_formulaeDialog_tabRow_navSeparator" />
                            <button className={"k45_we_formulaeDialog_tabRow_navigationPathPart_" + selectClassname(i, x, selectedTab == WEDescType.COMPONENT)} onClick={() => setSelectedPath(a.slice(0, i + 1) as any)} >{i == 0 ? translate("formulaeEditor.addDialog.WEMemberSource_" + x) : x}</button>
                        </>)}
                    </div>}
                    <EditorScrollable className={classNames("k45_we_formulaeDialog_content", selectedElement && "anySelected")}>
                        <FocusDisabled>
                            {
                                selectedTab == WEDescType.ARRAY_INDEXING ?
                                    <EditorRow label={T_arrayIndexingFieldLabel}>
                                        <IntInput min={0} onChange={(x) => setIndexSet(x)} value={indexSet} />
                                    </EditorRow>
                                    : selectedTab == WEDescType.MEMBER
                                        ? printWithTooltip(optionsMembers, selectedElement, setSelectedElement)
                                        : selectedTab == WEDescTypeUI.CURRENT_COMPONENT
                                            ? printWithTooltip(currentEntityComponent.sort((a, b) => a.className.localeCompare(b.className)), selectedElement, setSelectedElement)
                                            : selectedTab && [WEDescType.COMPONENT, WEDescType.STATIC_METHOD].includes(selectedTab)
                                            && <WEPaginateOverResultObj lastLevelIsPackage={selectedTab == WEDescType.COMPONENT} selectedElement={selectedElement} source={selectedTab == WEDescType.COMPONENT ? optionsComponentGetter! : optionsStaticMethods} currentNavigation={selectedPath} setNavigation={setSelectedPath} setElement={setSelectedElement} />
                            }
                        </FocusDisabled>
                    </EditorScrollable>
                </div>}
    </Dialog>

};

function printWithTooltip(fnList: WEFormulaeElement[], selectedElement: WEFormulaeElement | undefined, setSelectedElement: (x: WEFormulaeElement) => any) {
    return fnList.map(x => <VanillaComponentResolver.instance.Tooltip tooltip={getTooltipFor(x)}><WEFormulaeItemBtn item={x} isSelected={x == selectedElement} onSelect={(x) => setSelectedElement(x)} /></VanillaComponentResolver.instance.Tooltip>)
}

type PropsFormulaeItemBtn<T extends WEFormulaeElement | string | number> = {
    item: T,
    stringClassName?: string
    displayName?: string
    isSelected: boolean
    onSelect: (el: T) => any
}

function getTooltipFor(item: WEFormulaeElement) {
    switch (item.WEDescType) {
        case WEDescType.COMPONENT:
            return replaceArgs(translate("formulaeEditor.addDialog.tooltip_component"), [item.className])
        case WEDescType.MEMBER:
            switch (item.type.value__) {
                case WEMemberType.Field:
                    return replaceArgs(translate("formulaeEditor.addDialog.tooltip_field"), [item.memberTypeClassName, item.memberName])
                case WEMemberType.ParameterlessMethod:
                    return replaceArgs(translate("formulaeEditor.addDialog.tooltip_instanceMethod"), [item.memberTypeClassName, item.memberName])
                case WEMemberType.Property:
                    return replaceArgs(translate("formulaeEditor.addDialog.tooltip_property"), [item.memberTypeClassName, item.memberName])
                default:
                    return null;
            }
        case WEDescType.STATIC_METHOD:
            return replaceArgs(translate("formulaeEditor.addDialog.tooltip_staticMethod"), [item.className, item.methodName, item.returnType])
    }
}

const WEFormulaeItemBtn: <T extends string | WEFormulaeElement | number>(x: PropsFormulaeItemBtn<T>) => JSX.Element = ({ item, onSelect, isSelected, stringClassName, displayName }) => {
    if (typeof item == "string" || typeof item == "number") {
        return <div className={["k45_we_formulaeDialog_btn_" + stringClassName, isSelected ? "selected" : ""].join(" ").trim()} onClick={() => onSelect(item)} >{displayName ?? item}</div>
    }
    switch (item.WEDescType) {
        case WEDescType.COMPONENT:
            return <div className={["k45_we_formulaeDialog_btn_component", isSelected ? "selected" : ""].join(" ").trim()} onClick={() => onSelect(item)} >{breakIntoFlexComponents(displayName ?? item.className)}</div>
        case WEDescType.MEMBER:
            switch (item.type.value__) {
                case WEMemberType.Field:
                    return <div className={["k45_we_formulaeDialog_btn_field", isSelected ? "selected" : ""].join(" ").trim()} onClick={() => onSelect(item)}>{displayName ?? item.memberName}</div>
                case WEMemberType.ParameterlessMethod:
                    return <div className={["k45_we_formulaeDialog_btn_instanceMethod", isSelected ? "selected" : ""].join(" ").trim()} onClick={() => onSelect(item)} >{displayName ?? item.memberName}</div>
                case WEMemberType.Property:
                    return <div className={["k45_we_formulaeDialog_btn_property", isSelected ? "selected" : ""].join(" ").trim()} onClick={() => onSelect(item)}>{displayName ?? item.memberName}</div>
                default:
                    return <></>;
            }
        case WEDescType.STATIC_METHOD:
            return <div className={["k45_we_formulaeDialog_btn_staticMethod", isSelected ? "selected" : ""].join(" ").trim()} onClick={() => onSelect(item)} >{displayName ?? item.methodName}</div>
    }
}

interface RecursiveFormulaeElementPage {
    [key: string | number]: RecursiveFormulaeElementPage | WEFormulaeElement[];
}

type PaginateOverResultProps = {
    source: RecursiveFormulaeElementPage,
    currentNavigation: (string | number)[]
    setNavigation: (arr: (string | number)[]) => any,
    selectedElement?: WEFormulaeElement,
    setElement: (el: WEFormulaeElement) => any
    lastLevelIsPackage: boolean
}
const selectClassname = (level: number, value: number | string, isPackage: boolean) => {
    switch (level) {
        case 1: return "assembly"
        case 2: return isPackage ? "package" : "component"
        case 0: switch ("" + value) {
            case "0": return "Game"
            case "1": return "Unity"
            case "2": return "CoUI"
            case "3": return "System"
            case "4": return "Mod"
        }
    }
}
const WEPaginateOverResultObj = ({ source, currentNavigation, selectedElement, setNavigation, setElement, lastLevelIsPackage }: PaginateOverResultProps) => {
    let targetList: RecursiveFormulaeElementPage | WEFormulaeElement[] = source;
    let level = 0;
    for (let navPart of currentNavigation) {
        if (!navPart || !targetList || Array.isArray(targetList)) break;
        targetList = targetList[navPart]
        level++
    }
    if (!targetList) return <></>;
    if (Array.isArray(targetList)) return <>{printWithTooltip(targetList, selectedElement, setElement)}</>;

    return <>{Object.keys(targetList).map(x => <WEFormulaeItemBtn item={x}
        isSelected={false}
        onSelect={(x) => setNavigation(currentNavigation.concat([x]))}
        displayName={level == 0 ? translate("formulaeEditor.addDialog.WEMemberSource_" + x) : x}
        stringClassName={selectClassname(level, x, lastLevelIsPackage)}
    />)}</>
}