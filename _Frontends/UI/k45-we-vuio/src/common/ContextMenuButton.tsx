import { PropsToolButton, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import classNames from "classnames";
import { Portal } from "cs2/ui";
import { CSSProperties, ReactNode, useEffect, useRef, useState } from "react";
import "style/contextMenu.scss"

export type ContextButtonMenuItemArray = ({
    label: string,
    action: () => any,
    disabled?: boolean
} | null)[]

export enum ContextMenuExpansion {
    BOTTOM_RIGHT,
    BOTTOM_LEFT,
    TOP_RIGHT,
    TOP_LEFT
}

export type ContextMenuButtonProps = {
    menuTitle?: ReactNode,
    menuItems: ContextButtonMenuItemArray,
    menuDirection?: ContextMenuExpansion
} & Omit<PropsToolButton, "onClick" | "onSelect" | "selected">
export const ContextMenuButton = (props: ContextMenuButtonProps) => {
    const btnRef = useRef(null as any as HTMLDivElement);
    const menuRef = useRef(null as any as HTMLDivElement);
    const Button = VanillaComponentResolver.instance.ToolButton;
    const ScrollPanel = VanillaWidgets.instance.EditorScrollable;

    const findFixedPosition = (el: HTMLElement) => {
        const result = { left: 0, top: 0 }
        if (el) {
            let nextParent = el;
            do {
                result.left += nextParent.offsetLeft;
                result.top += nextParent.offsetTop;
            } while (nextParent = nextParent.offsetParent as HTMLElement)
        }
        return result;
    }
    const menuPosition = findFixedPosition(btnRef.current)
    const [menuOpen, setMenuOpen] = useState(false);
    const findBetterDirection = () => {
        if (!btnRef.current) return;
        const btnCenterX = menuPosition.left + btnRef.current.offsetWidth / 2
        const btnCenterY = menuPosition.top + btnRef.current.offsetHeight / 2
        if (btnCenterX > window.innerWidth / 2) {//right - expand left
            if (btnCenterY > window.innerHeight / 2) {//bottom - expand top
                return ContextMenuExpansion.TOP_LEFT;
            } else {
                return ContextMenuExpansion.BOTTOM_LEFT;
            }
        } else {
            if (btnCenterY > window.innerHeight / 2) {//bottom - expand top
                return ContextMenuExpansion.TOP_RIGHT;
            } else {
                return ContextMenuExpansion.BOTTOM_RIGHT;
            }
        }
    }

    const [menuCss, setMenuCss] = useState({} as CSSProperties)
    useEffect(() => {
        const effectiveMenuDirection = props.menuDirection ?? findBetterDirection()
        switch (effectiveMenuDirection) {
            case ContextMenuExpansion.BOTTOM_LEFT:
                setMenuCss({ top: menuPosition.top + btnRef.current?.offsetHeight + 3, right: window.innerWidth - menuPosition.left - btnRef.current?.offsetWidth });
                break;
            case ContextMenuExpansion.TOP_RIGHT:
                setMenuCss({ bottom: window.innerHeight - menuPosition.top + 3, left: menuPosition.left });
                break;
            case ContextMenuExpansion.TOP_LEFT:
                setMenuCss({ bottom: window.innerHeight - menuPosition.top + 3, right: window.innerWidth - menuPosition.left - btnRef.current?.offsetWidth });
                break;
            case ContextMenuExpansion.BOTTOM_RIGHT:
            default:
                setMenuCss({ top: menuPosition.top + btnRef.current?.offsetHeight + 3, left: menuPosition.left });
                break;
        }
    }, [menuOpen])



    const handleClickOutside = (event: MouseEvent) => {
        if (btnRef.current && !btnRef.current?.contains(event.target as Node) && !menuRef.current?.contains(event.target as Node)) {
            setMenuOpen(false);
        }
    };

    useEffect(() => {
        document.addEventListener('mousedown', handleClickOutside, true);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside, true);
        };
    }, []);



    return <>
        <div ref={btnRef}>
            <Button {...props} selected={menuOpen} onSelect={() => { setMenuOpen(!menuOpen) }} />
        </div>
        {menuOpen && <Portal>
            <div className="k45_comm_contextMenu" style={menuCss} ref={menuRef}>
                {props.menuTitle && <div className="k45_comm_contextMenu_title">{props.menuTitle}</div>}
                <ScrollPanel>
                    {props.menuItems.map(x => x ? <button className={classNames("k45_comm_contextMenu_item", x.disabled ? "disabled" : "")} onClick={() => { setMenuOpen(false); x.action() }} disabled={x.disabled}>{x.label}</button> : <div className="k45_comm_contextMenu_separator" />)}
                </ScrollPanel>
            </div>
        </Portal>
        }
    </>
}