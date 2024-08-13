import { Portal } from "cs2/ui";
import { WEInputDialog } from "./WEInputDialog";

type StringInputDialogProps = {
    isActive: boolean,
    setIsActive: (x: boolean) => any,
    dialogTitle: string,
    dialogPromptText: string,
    initialValue?: string,
    actionOnSuccess: (x?: string) => any,
    validationFn?: (val: string) => boolean
    maxLength?: number
}

export const StringInputDialog = ({
    isActive, setIsActive, dialogTitle, dialogPromptText, initialValue, actionOnSuccess, validationFn, maxLength
}: StringInputDialogProps) => {
    const onConfirm = (x?: string) => {
        setIsActive(false);
        actionOnSuccess(x);
    };
    return <Portal>
        {isActive && <WEInputDialog callback={onConfirm} title={dialogTitle} promptText={dialogPromptText} initialValue={initialValue} validationFn={validationFn} maxLength={maxLength} />}
    </Portal>;
};
