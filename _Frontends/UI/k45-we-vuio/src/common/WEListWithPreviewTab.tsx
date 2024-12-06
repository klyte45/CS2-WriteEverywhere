import { PropsToolButton, VanillaComponentResolver } from "@klyte45/vuio-commons";
import { ReactNode } from "react";
import "style/mainUi/tabStructure.scss";
import { ContextMenuButtonProps } from "./ContextMenuButton";
import { WEListWithContentTab } from "./WEListWithContentTab";

export type ListActionTypeArray = (({ isContext: false } & PropsToolButton) | ({ isContext: true } & ContextMenuButtonProps) | null)[];

type Props = {
    listItems: (string | { section?: string, emptyPlaceholder?: string, displayName?: undefined } | { displayName: string, value: string })[],
    detailsFields?: {
        key: ReactNode,
        value: ReactNode
    }[],
    listActions?: ListActionTypeArray,
    itemActions: ({
        className: string,
        action: () => any,
        text: string
    } | null)[]
    children: ReactNode,
    onChangeSelection: (x: string) => any,
    selectedKey: string | null,
    emptyListMsg?: ReactNode,
    noneSelectedMsg?: ReactNode,
}

export const WEListWithPreviewTab = ({ listItems, detailsFields, listActions, itemActions, onChangeSelection, selectedKey, children, emptyListMsg, noneSelectedMsg }: Props) => {

    return <WEListWithContentTab listItems={listItems} listActions={listActions} onChangeSelection={onChangeSelection} selectedKey={selectedKey} emptyListMsg={emptyListMsg} >
        {selectedKey ? <>
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
                    itemActions.map(x => x == null ? <div style={{ flexGrow: 1 }} /> : <button className={x.className} onClick={x.action}>{x.text}</button>)
                }
            </div>
        </> : <div className="k45_we_tabWithPreview_noneSelectedMsg">{noneSelectedMsg ?? "No item selected"}</div>}
    </WEListWithContentTab>
};
