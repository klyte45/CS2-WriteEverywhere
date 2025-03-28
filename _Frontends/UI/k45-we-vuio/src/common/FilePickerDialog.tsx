import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Portal } from "cs2/ui";
import { MutableRefObject, useEffect, useRef, useState } from "react";
import { DataProvider, FileService } from "services/FileService";
import { translate } from "utils/translate";
import "../style/filePickerDialog.scss"
import classNames from "classnames";
import { ContextMenuButton } from "./ContextMenuButton";
import engine from "cohtml/cohtml";
import { FocusDisabled } from "cs2/input";

type FilePickerDialogProps = {
    isActive: boolean,
    setIsActive: (x: boolean) => any,
    dialogTitle: string,
    dialogPromptText: string,
    allowedExtensions: string,
    actionOnSuccess: (x?: string) => any,
    initialFolder: string,
    bookmarks?: { name: string, targetPath: string }[]
    bookmarksTitle?: string,
    bookmarksIcon?: string
}

export const FilePickerDialog = ({
    isActive, setIsActive, dialogTitle, dialogPromptText, allowedExtensions, actionOnSuccess, initialFolder, bookmarks, bookmarksTitle, bookmarksIcon
}: FilePickerDialogProps) => {
    const onConfirm = (x?: string) => {
        setIsActive(false);
        actionOnSuccess(x);
    };
    return <Portal>
        {isActive &&
            <BaseFilePickerDialog onConfirm={onConfirm} dialogTitle={dialogTitle} allowedExtensions={allowedExtensions}
                dialogPromptText={dialogPromptText} initialFolder={initialFolder} bookmarks={bookmarks} bookmarksTitle={bookmarksTitle}
                bookmarksIcon={bookmarksIcon} />
        }
    </Portal>;
};

type BaseFilePickerDialogProps = {
    onConfirm: (nameSet?: string) => any
    dialogTitle: string
    dialogPromptText: string
    initialFolder: string
    allowedExtensions: string,
    bookmarks?: { name: string, targetPath: string }[]
    bookmarksTitle?: string,
    bookmarksIcon?: string
}
const BaseFilePickerDialog = ({ onConfirm: callback, dialogTitle: title, dialogPromptText: promptText,
    allowedExtensions, initialFolder, bookmarks, bookmarksTitle, bookmarksIcon }: BaseFilePickerDialogProps) => {
    const T_parentFolder = "Parent folder"
    const T_initialFolder = "Initial folder"
    const i_dirIcon = "coui://uil/Standard/Folder.svg";
    const i_fileIcon = "coui://uil/Standard/PaperWithArrow.svg";
    const i_parentDirIcon = "coui://uil/Standard/ArrowUp.svg";
    const i_bookmarks = "coui://uil/Standard/StarFilledSmall.svg";
    const i_homeIcon = "coui://uil/Standard/Home.svg";


    const Dialog = VanillaComponentResolver.instance.Dialog;
    const Tooltip = VanillaComponentResolver.instance.Tooltip;
    const Scrollable = VanillaWidgets.instance.EditorScrollable;
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const toolButtonTheme = VanillaComponentResolver.instance.toolButtonTheme;
    const [value, setValue] = useState(-1)
    const [currentFolder, setCurrentFolder] = useState(initialFolder);
    const [currentFolderTyping, setCurrentFolderTyping] = useState(initialFolder);
    const [currentData, setCurrentData] = useState([] as DataProvider);
    const [isEditingPath, setIsEditingPath] = useState(false);

    var refInput = useRef(null as any as HTMLDivElement);

    useEffect(() => {
        FileService.generateDataProvider(currentFolder, allowedExtensions).then(setCurrentData);
        setIsEditingPath(false);
    }, [allowedExtensions, currentFolder])

    useEffect(() => {
        if (isEditingPath && refInput.current) {
            var input = [...(refInput.current?.children ?? [])].find(x => x.tagName == "INPUT") as HTMLInputElement;
            if (input) {
                setCurrentFolderTyping(currentFolder)
                input.focus()
                input.setSelectionRange(currentFolder.length, currentFolder.length)
            }
        }
    }, [isEditingPath])

    const onItemSet = (i: number) => {
        const selectedItem = currentData[i];
        if (!selectedItem) return;
        if (selectedItem.directory) setCurrentFolder(selectedItem.fullPath);
        else setValue(i);
    }

    const navigateFolderUp = () => {
        setCurrentFolder(currentFolder.replaceAll("\\", "/").split("/").slice(0, currentFolder.endsWith("/") ? -2 : -1).join("/") + "/")
    }

    const menuItems: Parameters<typeof ContextMenuButton>[0]['menuItems'] | null = bookmarks ? bookmarks.map(x => {
        return {
            label: x.name,
            action: () => setCurrentFolder(x.targetPath)
        }
    }) : null;


    const getCurrentFolderTitle = () => {
        const bookmarkName = bookmarks?.find(x => x.targetPath == currentFolder);
        const folderName = currentFolder.split(/[\\\/]/).filter(x => x).slice(-1)[0] || "/";
        return bookmarkName ? <><b className="bookmark">{bookmarkName.name}</b> ({folderName})</> : folderName;
    }
    return <Dialog
        onClose={() => callback()}
        wide={true}
        title={title}
        buttons={<div className="k45_we_dialogBtns">
            <button className="positiveBtn" disabled={value < 0} onClick={() => callback(currentData[value].fullPath)}>{engine.translate("Common.OK")}</button>
            <button className="negativeBtn" onClick={() => callback()}>{translate("cancelBtn")}</button>
        </div>}>
        <div className="k45_we_dialogMessage">
            <p>{promptText}</p>
            <div ref={refInput} className="k45_we_currentFolder">
                <FocusDisabled>
                    <VanillaComponentResolver.instance.ToolButton onSelect={() => setCurrentFolder(initialFolder)} src={i_homeIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={classNames(toolButtonTheme.button, "home")} tooltip={T_initialFolder} />
                    {menuItems ? <Tooltip tooltip={bookmarksTitle}><ContextMenuButton menuTitle={bookmarksTitle} className="bookmarks" menuItems={menuItems} src={bookmarksIcon ?? i_bookmarks} /></Tooltip> : <div style={{ marginLeft: "5rem" }} />}
                    <StringInputField className={isEditingPath ? "" : "hidden"} value={currentFolderTyping} onChange={setCurrentFolderTyping} onChangeEnd={() => { setIsEditingPath(false); setCurrentFolder(currentFolderTyping + (currentFolderTyping.endsWith("/") ? "" : "/")); }} />
                    <div className={classNames("k45_we_currentPath", isEditingPath ? "hidden" : "")} onClick={() => setIsEditingPath(true)}>{getCurrentFolderTitle()}</div>
                    <VanillaComponentResolver.instance.ToolButton onSelect={navigateFolderUp} src={i_parentDirIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={classNames(toolButtonTheme.button, "above")} tooltip={T_parentFolder} />
                </FocusDisabled>
            </div>
            <Scrollable className="k45_we_fileItemsListing">
                {currentData?.map((x, i) => <Tooltip tooltip={x.displayName}>
                    <div className={classNames("k45_we_fileItemsListing_item", value == i ? "selected" : null)} key={i} onClick={() => onItemSet(i)}><img className="k45_we_fileItemIcon" src={x.directory ? i_dirIcon : i_fileIcon} />{x.displayName}</div>
                </Tooltip>)}
            </Scrollable>
        </div>
    </Dialog>

};