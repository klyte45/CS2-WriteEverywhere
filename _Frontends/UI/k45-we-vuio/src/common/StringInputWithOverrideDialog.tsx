import { Portal, ConfirmationDialog } from "cs2/ui"
import { useState } from "react"
import { getOverrideCheckFn } from "utils/getOverrideCheckFn"
import { WEInputDialog } from "./WEInputDialog"

type StringInputWithOverrideDialogProps = {
    isActive: boolean,
    setIsActive: (x: boolean) => any,
    dialogTitle: string,
    dialogPromptText: string,
    dialogOverrideText: string,
    initialValue?: string,
    isShortCircuitCheckFn: (x?: string) => boolean,
    checkIfExistsFn: (x?: string) => Promise<boolean>,
    actionOnSuccess: (x: string) => any,
    validationFn?: (val: string) => boolean
    maxLength?: number
}
export const StringInputWithOverrideDialog = ({
    isActive,
    setIsActive,
    dialogTitle,
    dialogPromptText,
    dialogOverrideText,
    initialValue,
    isShortCircuitCheckFn,
    checkIfExistsFn,
    actionOnSuccess,
    validationFn,
    maxLength
}: StringInputWithOverrideDialogProps) => {
    const [confirmingOverride, setConfirmingOverride] = useState(false)
    const [actionOnConfirmOverride, setActionOnConfirmOverride] = useState(() => () => { })
    const callback = getOverrideCheckFn(
        setIsActive,
        isShortCircuitCheckFn,
        checkIfExistsFn,
        setActionOnConfirmOverride,
        setConfirmingOverride,
        actionOnSuccess as any
    );

    return <Portal>
        {isActive && <WEInputDialog callback={callback} title={dialogTitle} promptText={dialogPromptText} initialValue={initialValue} validationFn={validationFn} maxLength={maxLength} />}
        {confirmingOverride && <ConfirmationDialog onConfirm={() => { actionOnConfirmOverride(); }} title={dialogTitle} onCancel={() => setConfirmingOverride(false)} message={dialogOverrideText} />}
    </Portal>
}
