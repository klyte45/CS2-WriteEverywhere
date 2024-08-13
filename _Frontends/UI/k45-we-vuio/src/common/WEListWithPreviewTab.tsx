import { PropsToolButton, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import classNames from "classnames";
import { ReactNode } from "react";
import "style/mainUi/tabStructure.scss";
import { ContextMenuButton, ContextMenuButtonProps } from "./ContextMenuButton";

export type ListActionTypeArray = (({ isContext: false } & PropsToolButton) | ({ isContext: true } & ContextMenuButtonProps) | null)[];

type Props = {
    listItems: string[],
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

    const Button = VanillaComponentResolver.instance.ToolButton;

    return <div className="k45_we_tabWithPreview_content">
        <div className="k45_we_tabWithPreview_list">
            <div className="k45_we_tabWithPreview_listActions">
                {listActions?.map(x =>
                    x == null ? <div style={{ flexGrow: 1 }} />
                        : x.isContext ? <ContextMenuButton {...x} />
                            : <Button {...x} />
                )}
            </div>
            {listItems.length ? <VanillaWidgets.instance.EditorScrollable className="k45_we_tabWithPreview_listContent">
                {listItems.map(x => <button onClick={() => onChangeSelection(x)} className={classNames(x == selectedKey ? "selected" : "")}>{x}</button>)}
            </VanillaWidgets.instance.EditorScrollable> : <div className="k45_we_tabWithPreview_emptyListMsg">{emptyListMsg ?? "List is empty"}</div>}
        </div>
        <div className="k45_we_tabWithPreview_body">
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
        </div>
    </div >;
};
