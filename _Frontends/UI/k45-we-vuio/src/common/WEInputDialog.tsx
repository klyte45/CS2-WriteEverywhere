import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { useState } from "react";
import "style/common.scss";
import { translate } from "utils/translate";

type Props = {
    callback: (nameSet?: string) => any
    title: string
    promptText: string
    initialValue?: string
    validationFn?: (val: string) => boolean
}

export const WEInputDialog = ({ callback, title, promptText, initialValue, validationFn }: Props) => {
    const Dialog = VanillaComponentResolver.instance.Dialog;
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const [name, setName] = useState(initialValue ?? "")

    return <Dialog
        onClose={() => callback()}
        wide={true}
        title={title}
        buttons={<div className="k45_we_dialogBtns">
            {<button className="positiveBtn" onClick={() => callback(name)} disabled={validationFn ? validationFn(name) : !name.trim()}>{translate("saveBtn")}</button>}
            <button className="negativeBtn" onClick={() => callback()}>{translate("cancelBtn")}</button>
        </div>}>
        <div className="k45_we_dialogMessage">
            <p>{promptText}</p>
            <StringInputField onChange={setName} value={name} />
        </div>
    </Dialog>

};
