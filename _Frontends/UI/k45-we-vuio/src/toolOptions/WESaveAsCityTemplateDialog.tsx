import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { useState } from "react";
import "style/common.scss";
import { translate } from "utils/translate";

type Props = {
    callback: (nameSet?: string) => any
}

export const WESaveAsCityTemplateDialog = ({ callback }: Props) => {
    const T_addItemDialogTitle = translate("template.saveCityDialog.title")
    const Dialog = VanillaComponentResolver.instance.Dialog;
    const StringInputField = VanillaWidgets.instance.StringInputField;
    const [name, setName] = useState("")

    return <Dialog
        onClose={() => callback()}
        wide={true}
        title={T_addItemDialogTitle}
        buttons={<div className="k45_we_dialogBtns">
            {<button className="positiveBtn" onClick={() => callback(name)} disabled={!name.trim()}>{translate("template.saveCityDialog.saveBtn")}</button>}
            <button className="negativeBtn" onClick={() => callback()}>{translate("template.saveCityDialog.cancelBtn")}</button>
        </div>}>
        <div className="k45_we_dialogMessage">
            <p>{translate("template.saveCityDialog.dialogText")}</p>
            <StringInputField onChange={setName} value={name} />
        </div>
    </Dialog>

};
