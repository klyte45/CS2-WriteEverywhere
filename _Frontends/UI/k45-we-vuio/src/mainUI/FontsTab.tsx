import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { StringInputDialog } from "common/StringInputDialog";
import { StringInputWithOverrideDialog } from "common/StringInputWithOverrideDialog";
import { ListActionTypeArray, WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { ConfirmationDialog, Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { FontDetailResponse, FontService } from "services/FontService";
import "style/mainUi/fontsTab.scss";
import { translate } from "utils/translate";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
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
    const i_addItem = "coui://uil/Standard/Plus.svg";

    const T_addItem = translate("cityFontsTab.addFont")
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
    const T_addDialogTitle = translate("cityFontsTab.addFontDialog.title")
    const T_addDialogText = translate("cityFontsTab.addFontDialog.text")
    const T_addDialogErrorGeneric = translate("cityFontsTab.addFontDialog.errorLoadingFontMsg")


    const [selectedFont, setSelectedFont] = useState(null as null | string);
    const [fontList, setFontList] = useState({} as Record<string, boolean>);
    const [fontDetail, setFontDetail] = useState(null as FontDetailResponse | null);
    const [currentModal, setCurrentModal] = useState(Modals.NONE);
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
        setStylesheetToRemove(document.styleSheets.length - 1);
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
        { className: "neutralBtn", action() { setIsRenamingLayout(true) }, text: T_rename },
        { className: "neutralBtn", action() { setIsDuplicatingLayout(true) }, text: T_duplicate },

    ]
    const detailsFields = [] as any[]

    const StringInputField = VanillaWidgets.instance.StringInputField;
    const IntSlider = VanillaWidgets.instance.IntSlider;
    const FocusableEditorItem = VanillaWidgets.instance.FocusableEditorItem;
    const FocusDisabled = VanillaComponentResolver.instance.FOCUS_DISABLED;
    const buttonClass = VanillaComponentResolver.instance.toolButtonTheme.button;

    const [alertToDisplay, setAlertToDisplay] = useState(undefined as string | undefined)

    const [previewText, setPreviewText] = useState("");
    const [fontSize, setFontSize] = useState(30);
    const validateName = (x: string) => x.match(/^[A-Za-z0-9_]{2,30}$/g) != null;
    const displayingModal = () => {
        switch (currentModal) {
            case Modals.CONFIRMING_DELETE: return <ConfirmationDialog onConfirm={() => { setCurrentModal(0); FontService.deleteCityFont(selectedFont!); setSelectedFont(null) }} onCancel={() => setCurrentModal(0)} message={T_confirmDeleteText} />

        }
    }
    const [isRenamingLayout, setIsRenamingLayout] = useState(false);
    const onRenameLayout = (x: string) => {
        FontService.renameCityFont(selectedFont!, x!);
        setSelectedFont(x!);
    }
    const [isDuplicatingLayout, setIsDuplicatingLayout] = useState(false);
    const onDuplicateLayout = (x: string) => {
        FontService.duplicateCityFont(selectedFont!, x!);
        setSelectedFont(x!);
    }

    const [isAskingPathAdd, setIsAskingPathAdd] = useState(false);
    const onAskPathAdd = async (x?: string) => {
        if (!x) return;
        const newFontName = await FontService.requireFontInstallation(x);
        if (newFontName) {
            setSelectedFont(newFontName);
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
        <WEListWithPreviewTab listActions={listActions} itemActions={actions} detailsFields={detailsFields} listItems={Object.entries(fontList).filter(x => !x[1]).map(x => x[0]).sort((a, b) => a.localeCompare(b))} selectedKey={selectedFont!} onChangeSelection={setSelectedFont} >
            {fontDetail && <>
                <div className="k45_we_fontTab_previewControls">
                    <FocusableEditorItem focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}><StringInputField value={previewText} onChange={setPreviewText} /></FocusableEditorItem>
                    <FocusableEditorItem focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}><IntSlider min={5} max={200} value={fontSize} onChange={setFontSize} /></FocusableEditorItem>
                </div>
                <div className="k45_we_fontTab_previewText" style={{ fontFamily: "K45WE_" + fontDetail.guid, fontSize: fontSize + "rem" }}>
                    {previewText || T_typeAboveToPreviewThisFont}
                </div></>}
        </WEListWithPreviewTab>

        <StringInputWithOverrideDialog dialogTitle={T_renameDialogTitle} dialogPromptText={T_renameDialogText} dialogOverrideText={T_confirmOverrideText} validationFn={validateName} initialValue={selectedFont!}
            maxLength={30}
            isActive={isRenamingLayout} setIsActive={setIsRenamingLayout}
            isShortCircuitCheckFn={(x) => !x || x == selectedFont}
            checkIfExistsFn={FontService.checkFontExists}
            actionOnSuccess={onRenameLayout}
        />
        <StringInputWithOverrideDialog dialogTitle={T_duplicateDialogTitle} dialogPromptText={T_duplicateDialogText} dialogOverrideText={T_confirmOverrideText} validationFn={validateName}
            maxLength={30}
            isActive={isDuplicatingLayout} setIsActive={setIsDuplicatingLayout}
            isShortCircuitCheckFn={(x) => !x || x == selectedFont}
            checkIfExistsFn={FontService.checkFontExists}
            actionOnSuccess={onDuplicateLayout}
        />
        <StringInputDialog dialogTitle={T_addDialogTitle} dialogPromptText={T_addDialogText}
            isActive={isAskingPathAdd} setIsActive={setIsAskingPathAdd}
            actionOnSuccess={onAskPathAdd}
        />
        <Portal>
            {alertToDisplay && <ConfirmationDialog onConfirm={() => { setAlertToDisplay(void 0); }} cancellable={false} dismissable={false} message={alertToDisplay} confirm={"OK"} />}
            {displayingModal()}
        </Portal>
    </>
};

