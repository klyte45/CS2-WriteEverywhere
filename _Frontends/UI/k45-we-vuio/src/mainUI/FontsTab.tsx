import { VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { WEInputDialog } from "common/WEInputDialog";
import { WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { ConfirmationDialog, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { FontDetailResponse, FontService } from "services/FontService";
import "style/mainUi/fontsTab.scss";
import { getOverrideCheckFn } from "utils/getOverrideCheckFn";
import { translate } from "utils/translate";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
    RENAMING_FONT,
    OVERRIDE_CONFIRM,
    DUPLICATING_FONT
}

const WE_DYNAMIC_CSS_ID = "_K45_WE_DYNAMIC_CSS_DATA_"
function removeCssElement(cssEl: Element) {
    cssEl.parentNode?.removeChild(cssEl);
    [...(document.styleSheets as any)].forEach(
        (x: CSSStyleSheet) => {
            if ((x.ownerNode as any) == cssEl) {
                do {
                    x.deleteRule(0);
                } while (x.cssRules.length > 0);
            }
        }
    );
    cssEl.id = "";
}

export const FontsTab = (props: Props) => {
    const T_rename = translate("cityFontsTab.rename")
    const T_duplicate = translate("cityFontsTab.duplicate")
    const T_delete = translate("cityFontsTab.delete")
    const T_confirmDeleteText = translate("cityFontsTab.confirmDeleteText")
    const T_renameDialogTitle = translate("cityFontsTab.renameDialog.title")
    const T_renameDialogText = translate("cityFontsTab.renameDialog.text")
    const T_confirmOverrideText = translate("cityFontsTab.confirmOverrideOnRename")
    const T_duplicateDialogTitle = translate("cityFontsTab.duplicateDialog.title")
    const T_duplicateDialogText = translate("cityFontsTab.duplicateDialog.text")
    const T_typeAboveToPreviewThisFont = translate("cityFontsTab.typeAboveToPreview")

    const units = VanillaFnResolver.instance.unit.Unit;

    const [selectedFont, setSelectedFont] = useState(null as null | string);
    const [fontList, setFontList] = useState({} as Record<string, boolean>);
    const [fontDetail, setFontDetail] = useState(null as FontDetailResponse | null);
    const [currentModal, setCurrentModal] = useState(Modals.NONE);
    const [actionOnConfirmOverride, setActionOnConfirmOverride] = useState(() => () => { })
    const [stylesheetToRemove, setStylesheetToRemove] = useState(-1);

    async function loadFontFace() {
        const oldStyle = document.getElementById(WE_DYNAMIC_CSS_ID)
        if (oldStyle) {
            removeCssElement(oldStyle)
        }
        if (stylesheetToRemove >= 0) document.styleSheets[stylesheetToRemove] = null as any;
        const cssNode = document.createElement("link");
        cssNode.id = WE_DYNAMIC_CSS_ID;
        cssNode.rel = "stylesheet";
        cssNode.href = "coui://we.k45/_css/" + fontDetail?.name;
        document.querySelector("head")?.appendChild(cssNode);
    }


    useEffect(() => { fontDetail && loadFontFace() }, [fontDetail]);
    useEffect(() => { FontService.listCityFonts().then(setFontList) }, [selectedFont])
    useEffect(() => {
        FontService.getFontDetail(selectedFont!).then(setFontDetail);
        return () => {
            const oldStyle = document.getElementById(WE_DYNAMIC_CSS_ID)
            if (oldStyle) {
                removeCssElement(oldStyle)
            }
        }
    }, [selectedFont])

    const actions = [
        { className: "negativeBtn", action() { setCurrentModal(Modals.CONFIRMING_DELETE) }, text: T_delete },
        { className: "neutralBtn", action() { setCurrentModal(Modals.RENAMING_FONT) }, text: T_rename },
        { className: "neutralBtn", action() { setCurrentModal(Modals.DUPLICATING_FONT) }, text: T_duplicate },

    ]
    const detailsFields = [] as any[]

    const renameFontCallback = getOverrideCheckFn(
        (x: boolean) => setCurrentModal(x ? Modals.RENAMING_FONT : 0),
        (x) => !x || x == selectedFont,
        FontService.checkFontExists,
        setActionOnConfirmOverride,
        (x: boolean) => setCurrentModal(x ? Modals.OVERRIDE_CONFIRM : 0),
        (x) => {
            FontService.renameCityFont(selectedFont!, x!);
            setSelectedFont(x!);
        })


    const duplicateTemplateCallback = getOverrideCheckFn(
        (x: boolean) => setCurrentModal(x ? Modals.DUPLICATING_FONT : 0),
        (x) => !x || x == selectedFont,
        FontService.checkFontExists,
        setActionOnConfirmOverride,
        (x: boolean) => setCurrentModal(x ? Modals.OVERRIDE_CONFIRM : 0),
        (x) => {
            FontService.duplicateCityFont(selectedFont!, x!);
            setSelectedFont(x!);
        })


    const StringInputField = VanillaWidgets.instance.StringInputField;
    const IntSlider = VanillaWidgets.instance.IntSlider;
    const FocusableEditorItem = VanillaWidgets.instance.FocusableEditorItem;
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const [previewText, setPreviewText] = useState("");
    const [fontSize, setFontSize] = useState(30);
    const validateName = (x: string) => x.match(/^[A-Za-z0-9_]{2,30}$/g) != null;
    const displayingModal = () => {
        switch (currentModal) {
            case Modals.CONFIRMING_DELETE: return <ConfirmationDialog onConfirm={() => { setCurrentModal(0); FontService.deleteCityFont(selectedFont!); setSelectedFont(null) }} onCancel={() => setCurrentModal(0)} message={T_confirmDeleteText} />
            case Modals.RENAMING_FONT: return <WEInputDialog callback={renameFontCallback} title={T_renameDialogTitle} promptText={T_renameDialogText} validationFn={validateName} initialValue={selectedFont!} maxLength={30} />
            case Modals.OVERRIDE_CONFIRM: return <ConfirmationDialog onConfirm={actionOnConfirmOverride} onCancel={() => setCurrentModal(0)} message={T_confirmOverrideText} />
            case Modals.DUPLICATING_FONT: return <WEInputDialog callback={duplicateTemplateCallback} title={T_duplicateDialogTitle} promptText={T_duplicateDialogText} validationFn={(x) => validateName(x) && x != selectedFont} />
        }
    }
    return <>
        <WEListWithPreviewTab actions={actions} detailsFields={detailsFields} listItems={Object.entries(fontList).filter(x => !x[1]).map(x => x[0]).sort((a, b) => a.localeCompare(b))} selectedKey={selectedFont!} onChangeSelection={setSelectedFont} >
            {fontDetail && <>
                <div className="k45_we_fontTab_previewControls">
                    <FocusableEditorItem focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}><StringInputField value={previewText} onChange={setPreviewText} /></FocusableEditorItem>
                    <FocusableEditorItem focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}><IntSlider min={5} max={200} value={fontSize} onChange={setFontSize} /></FocusableEditorItem>
                </div>
                <div className="k45_we_fontTab_previewText" style={{ fontFamily: "K45WE_" + fontDetail.index, fontSize: fontSize + "rem" }}>
                    {previewText || T_typeAboveToPreviewThisFont}
                </div></>}
        </WEListWithPreviewTab>
        <Portal>{displayingModal()}</Portal>
    </>
};

