export const getOverrideCheckFn = (
    unsetStateFn: (x: boolean) => any,
    isShortCircuitCheckFn: (x?: string) => boolean,
    checkIfExistsFn: (x?: string) => Promise<boolean>,
    confirmationOverrideFnSetter: (fn: () => any) => any,
    confirmationOverrideEnablerSetter: (newVal: boolean) => any,
    actionOnSuccess: (x?: string) => any
) => async (newName?: string) => {
    unsetStateFn(false);
    newName = newName?.trim();
    if (isShortCircuitCheckFn(newName)) return;
    if (await checkIfExistsFn(newName)) {
        confirmationOverrideFnSetter(() => () => {
            actionOnSuccess(newName);
            confirmationOverrideEnablerSetter(false)
        });
        confirmationOverrideEnablerSetter(true);
    } else {
        actionOnSuccess(newName);
    }
};
