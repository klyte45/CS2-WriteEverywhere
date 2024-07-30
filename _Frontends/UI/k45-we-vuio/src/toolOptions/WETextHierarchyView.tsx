import { Entity, HierarchyViewport, LocElementType, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Portal, Panel } from "cs2/ui";
import { useEffect, useState } from "react";
import { WorldPickerService } from "services/WorldPickerService";
import { WESimulationTextType, WETextItemResume } from "services/WEFormulaeElement";
import { translate } from "utils/translate";
import { FormulaeService } from "services/FormulaeService";

function getIconForTextType(type: WESimulationTextType) {
    switch (type) {
        case WESimulationTextType.Image:
            return "coui://uil/Standard/Image.svg";
        case WESimulationTextType.Text:
            return "coui://uil/Standard/PencilPaper.svg";
    }
}


export const WETextHierarchyView = ({ clipboard, setClipboard }: { clipboard: Entity | undefined | null, setClipboard: (c: Entity | undefined | null) => any }) => {
    const i_cut = "coui://uil/Standard/DottedLinesMarkers.svg";
    const i_copy = "coui://uil/Standard/RectangleCopy.svg";
    const i_paste = "coui://uil/Standard/RectanglePaste.svg";
    const i_pasteAtRoot = "coui://uil/Standard/ArrowUpTriangleNotch.svg";
    const i_delete = "coui://uil/Standard/Trash.svg";
    const i_addRoot = "coui://uil/Standard/ArrowLeftTriangleNotch.svg";
    const i_addChild = "coui://uil/Standard/Plus.svg";
    const wps = WorldPickerService.instance;
    const T_title = translate("textHierarchyWindow.title"); //"Appearance Settings"
    const T_cut = translate("textHierarchyWindow.cut"); //"Appearance Settings"
    const T_copy = translate("textHierarchyWindow.copy"); //"Appearance Settings"
    const T_paste = translate("textHierarchyWindow.paste"); //"Appearance Settings"
    const T_delete = translate("textHierarchyWindow.delete"); //"Appearance Settings"
    const T_pasteAtRoot = translate("textHierarchyWindow.pasteAtRoot"); //"Appearance Settings"
    const T_copiedInfo = translate("textHierarchyWindow.copiedInfoSuffix"); //"Appearance Settings"
    const T_cuttedInfo = translate("textHierarchyWindow.cuttedInfoSuffix"); //"Appearance Settings"
    const T_addEmptyChild = translate("textHierarchyWindow.addEmptyChild"); //"Appearance Settings"
    const T_addEmptyRoot = translate("textHierarchyWindow.addEmptyRoot"); //"Appearance Settings"
    const defaultPosition = { x: 20 / window.innerWidth, y: 100 / window.innerHeight }

    const HierarchyMenu = VanillaWidgets.instance.HierarchyMenu;
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const Button = VanillaComponentResolver.instance.ToolButton;
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;

    const [viewport, setViewport] = useState([] as (HierarchyViewport & WETextItemResume)[])
    const [expandedViewports, setExpandedViewports] = useState([] as Entity[])
    const [clipboardIsCut, setClipboardIsCut] = useState(false)

    const getDisplayName = (x: WETextItemResume) => x.id.Index == clipboard?.Index ? `${x.name} <${clipboardIsCut ? T_cuttedInfo : T_copiedInfo}>` : x.name

    function ResumeToViewPort(x: WETextItemResume, level: number = 0): (HierarchyViewport & WETextItemResume)[] {
        const isExpanded = expandedViewports.some(y => y.Index == x.id.Index);
        return [{
            ...x,
            displayName: { value: getDisplayName(x), __Type: LocElementType.String },
            icon: getIconForTextType(x.type),
            level,
            expandable: x.children.length > 0,
            expanded: isExpanded,
            selectable: true,
            selected: x.id.Index == wps.CurrentSubEntity.value?.Index
        } as HierarchyViewport & WETextItemResume].concat(isExpanded ? x.children.flatMap(x => ResumeToViewPort(x, level + 1)) : []);
    }

    useEffect(() => {
        setViewport(wps.CurrentTree.value.flatMap((x, i) => ResumeToViewPort(x)))
    }, [wps.CurrentEntity.value, wps.CurrentSubEntity.value, wps.CurrentTree.value, expandedViewports, clipboard, clipboardIsCut])

    const applyPasteAtRoot = () => doPaste(wps.CurrentEntity.value!)
    const applyPaste = () => doPaste(wps.CurrentSubEntity.value!)

    const doPaste = async (parent: Entity) => {
        if (clipboard) {
            if (clipboardIsCut) {
                if (await WorldPickerService.changeParent(clipboard, parent)) {
                    setClipboard(void 0);
                }
            } else {
                await WorldPickerService.cloneAsChild(clipboard, parent);
            }
        }
    }

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
                <Button onSelect={() => WorldPickerService.addEmpty()} src={i_addRoot} tooltip={T_addEmptyRoot} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => WorldPickerService.addEmpty(wps.CurrentSubEntity.value!)} src={i_addChild} tooltip={T_addEmptyChild} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ flexGrow: 1 }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { FormulaeService.exportComponentAsJson(wps.CurrentSubEntity.value!, "teste"); }} src={i_copy} tooltip={"????"} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { setClipboard(wps.CurrentSubEntity.value); setClipboardIsCut(true); }} src={i_cut} tooltip={T_cut} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => { setClipboard(wps.CurrentSubEntity.value); setClipboardIsCut(false); }} src={i_copy} tooltip={T_copy} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!clipboard || !wps.CurrentSubEntity.value?.Index} onSelect={() => applyPaste()} src={i_paste} tooltip={T_paste} focusKey={FocusDisabled} className={buttonClass} />
                <Button disabled={!clipboard} onSelect={() => applyPasteAtRoot()} src={i_pasteAtRoot} tooltip={T_pasteAtRoot} focusKey={FocusDisabled} className={buttonClass} />
                <div style={{ width: "10rem" }}></div>
                <Button disabled={!wps.CurrentSubEntity.value?.Index} onSelect={() => WorldPickerService.removeItem()} src={i_delete} tooltip={T_delete} focusKey={FocusDisabled} className={buttonClass} />
            </EditorItemRow>
        </Panel>
    </Portal>;
};

