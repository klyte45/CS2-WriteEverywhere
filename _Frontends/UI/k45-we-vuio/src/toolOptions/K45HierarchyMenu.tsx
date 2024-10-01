import classNames from "classnames";
import { ReactNode } from "react";
import '../style/hierarchyMenu.scss'


export type K45HierarchyViewport = {
    displayName: ReactNode;
    icon?: string;
    tooltip?: string;
    level: number;
    selectable?: boolean;
    selected?: boolean;
    expandable?: boolean;
    expanded?: boolean;
};
type PropsHierarchyMenu = {
    viewport: K45HierarchyViewport[];
    onSelect?: (viewportIndex: number, selected: boolean) => any;
    onSetExpanded?: (viewportIndex: number, expanded: boolean) => any;
    currentTreeOnlyMode?: boolean
};
export const K45HierarchyMenu = ({ viewport, onSelect, onSetExpanded, currentTreeOnlyMode }: PropsHierarchyMenu) => {
    const targetViewport = [] as { idx: number, item: K45HierarchyViewport }[]
    let startIdx = 0;
    let endIdx = viewport.length;
    const selectedIdx = viewport.map((x, i) => [x.selected, i]).find(x => x[0])?.[1]
    if (currentTreeOnlyMode) {
        if (typeof selectedIdx == 'number' && viewport[selectedIdx].level > 0) {
            let i = selectedIdx;
            let currentLvlSel = viewport[selectedIdx].level;
            do {
                startIdx = --i;
                if (viewport[i].level < currentLvlSel) {
                    currentLvlSel = viewport[i].level;
                    viewport[i].expanded = true;
                }
            } while (viewport[i].level != 0)
            i = selectedIdx;
            do {
                endIdx = ++i;
            } while (viewport[i].level != 0)
        }
    }
    let prevLevel = 0;
    let lastExpandedLevel = 0;
    for (let i = startIdx; i < endIdx; i++) {
        const item = viewport[i];
        if (item.level == 0 || item.level == prevLevel || item.level <= lastExpandedLevel + 1) {
            targetViewport.push({ idx: i, item });
            prevLevel = item.level;
            if (item.level < lastExpandedLevel || item.expanded) {
                lastExpandedLevel = item.level - (item.expanded ? 0 : 1);
            }
        }
    }
    return <div className="k45_hierarchyTree">{targetViewport.map(x =>
        <div className={classNames("k45_hierarchy_item", x.item.expanded && "expanded", x.item.selected && "selected")} style={{ paddingLeft: (4 + 10 * x.item.level) + "rem" }}>
            <div className={classNames("k45_hierarchy_connector", !x.item.level && "root")} onClick={() => x.item.selectable && onSelect?.(x.idx, !x.item.selected)} />
            <div className={classNames("k45_hierarchy_icon", x.item.expanded && "expanded", x.item.selected && "selected")} onClick={() => x.item.selectable && onSelect?.(x.idx, !x.item.selected)} style={{ backgroundImage: `url(${x.item.icon})` }} />
            <div className={classNames("k45_hierarchy_title", x.item.expanded && "expanded", x.item.selected && "selected")} onClick={() => x.item.selectable && onSelect?.(x.idx, !x.item.selected)} >{x.item.displayName}</div>
            {x.item.expandable && <div onClick={() => onSetExpanded?.(x.idx, !x.item.expanded)} className={classNames("k45_hierarchy_expandIcon", x.item.expanded && "expanded")} />}
        </div>)}</div>
};
