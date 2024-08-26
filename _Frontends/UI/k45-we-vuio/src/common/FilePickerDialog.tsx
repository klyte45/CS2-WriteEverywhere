import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Portal } from "cs2/ui";
import { MutableRefObject, useEffect, useRef, useState } from "react";
import { DataProvider, FileService } from "services/FileService";
import { translate } from "utils/translate";
import "../style/filePickerDialog.scss"
import classNames from "classnames";

type FilePickerDialogProps = {
    isActive: boolean,
    setIsActive: (x: boolean) => any,
    dialogTitle: string,
    dialogPromptText: string,
    allowedExtensions: string,
    actionOnSuccess: (x?: string) => any,
    initialFolder: string,
}

export const FilePickerDialog = ({
    isActive, setIsActive, dialogTitle, dialogPromptText, allowedExtensions, actionOnSuccess, initialFolder
}: FilePickerDialogProps) => {
    const onConfirm = (x?: string) => {
        setIsActive(false);
        actionOnSuccess(x);
    };
    return <Portal>
        {isActive && <BaseFilePickerDialog onConfirm={onConfirm} dialogTitle={dialogTitle} allowedExtensions={allowedExtensions} dialogPromptText={dialogPromptText} initialFolder={initialFolder} />}
    </Portal>;
};

type BaseFilePickerDialogProps = {
    onConfirm: (nameSet?: string) => any
    dialogTitle: string
    dialogPromptText: string
    initialFolder: string
    allowedExtensions: string,
}
const BaseFilePickerDialog = ({ onConfirm: callback, dialogTitle: title, dialogPromptText: promptText, allowedExtensions, initialFolder }: BaseFilePickerDialogProps) => {
    const T_parentFolder = "PARENT FOLDER"
    const i_dirIcon = "coui://uil/Standard/Folder.svg";
    const i_fileIcon = "coui://uil/Standard/PaperWithArrow.svg";
    const i_parentDirIcon = "coui://uil/Standard/ArrowUp.svg";


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
        setCurrentFolderTyping(currentFolder);
        setIsEditingPath(false);
    }, [allowedExtensions, currentFolder])

    useEffect(() => {
        if (isEditingPath && refInput.current) {
            var input = (refInput.current?.firstChild as HTMLInputElement);
            if (input) {
                input.focus()
                input.setSelectionRange(input.value.length, input.value.length)
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
                {isEditingPath
                    ? <StringInputField value={currentFolderTyping} onChange={setCurrentFolderTyping} onChangeEnd={() => { setIsEditingPath(false); setCurrentFolder(currentFolderTyping + (currentFolderTyping.endsWith("/") ? "" : "/")) }} />
                    : <div className="k45_we_currentPath" onClick={() => setIsEditingPath(true)}>{currentFolder.split(/[\\\/]/).filter(x => x).slice(-1)[0] || "/"}</div>
                }
                <VanillaComponentResolver.instance.ToolButton onSelect={navigateFolderUp} src={i_parentDirIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={toolButtonTheme.button} tooltip={T_parentFolder} />
            </div>
            <Scrollable className="k45_we_fileItemsListing">
                {currentData?.map((x, i) => <div className={classNames("k45_we_fileItemsListing_item", value == i ? "selected" : null)} key={i} onClick={() => onItemSet(i)}><img className="k45_we_fileItemIcon" src={x.directory ? i_dirIcon : i_fileIcon} />{x.displayName}</div>)}
            </Scrollable>
        </div>
    </Dialog>
};