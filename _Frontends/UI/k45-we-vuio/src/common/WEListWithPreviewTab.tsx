import { Entity, VanillaWidgets } from "@klyte45/vuio-commons";
import classNames from "classnames";
import { ReactNode, useEffect, useState } from "react";
import { CityDetailResponse, LayoutsService } from "services/LayoutsService";
import "style/mainUi/tabStructure.scss"
import { translate } from "utils/translate";

type Props = {
    listItems: string[],
    detailsFields?: {
        key: ReactNode,
        value: ReactNode
    }[],
    actions: ({
        className: string,
        action: () => any,
        text: string
    } | null)[]
    children: ReactNode,
    onChangeSelection: (x: string) => any,
    selectedKey: string | null
}

export const WEListWithPreviewTab = ({ listItems, detailsFields, actions, onChangeSelection, selectedKey, children }: Props) => {

    return <div className="k45_we_tabWithPreview_content">
        <VanillaWidgets.instance.EditorScrollable className="k45_we_tabWithPreview_list">
            {listItems.map(x => <button onClick={() => onChangeSelection(x)} className={classNames(x == selectedKey ? "selected" : "")}>{x}</button>)}
        </VanillaWidgets.instance.EditorScrollable>
        <div className="k45_we_tabWithPreview_body">
            <div className="k45_we_tabWithPreview_preview">
                {children}
            </div>
            <div className="k45_we_tabWithPreview_details">
                {selectedKey && detailsFields &&
                    detailsFields.map(x =>
                        <div className="k45_we_keyValueContent">
                            <div className="key">{x.key}</div>
                            <div className="value">{x.value}</div>
                        </div>)
                }
            </div>
            <div className="k45_we_tabWithPreview_actions">
                {selectedKey &&
                    actions.map(x => x == null ? <div style={{ flexGrow: 1 }} /> : <button className={x.className} onClick={x.action}>{x.text}</button>)
                }
            </div>
        </div>
    </div >;
};
