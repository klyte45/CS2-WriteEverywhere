
import icon from "images/WE-White.svg"
import { Button, Panel, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "@klyte45/vuio-commons";
import classNames from "classnames";
import "style/mainUi/mainUi.scss"
import { translate } from "utils/translate";
import { CityLayoutsTab } from "./CityLayoutsTab";
import { FontsTab } from "./FontsTab";
import { CityAtlasesTab } from "./CityAtlasesTab";
import { PrefabTemplatesReplacementsTab } from "./PrefabTemplatesReplacementsTab";
import engine from "cohtml/cohtml";

export const WeMainPanelId = "BelzontWE.UI.WEMainPanel";

export const WEButton = () => {
    return (
        <Tooltip tooltip="Write Everywhere">
            <Button
                src={icon}
                variant="floating"
                onSelect={() => VanillaComponentResolver.instance.toggleGamePanel(WeMainPanelId)}
            />
        </Tooltip>
    );
}

type MainPanelProps = { selectedTab?: number, noClose?: boolean, moveable?: boolean }
enum Tabs {
    CityLayouts = "CityLayouts",
    CityFonts = "CityFonts",
    CityAtlases = "CityAtlases",
    PrefabTemplatesReplacements = "PrefabTemplatesReplacements"
}
export const WEMainPanel = ({ selectedTab = 0, noClose, moveable }: MainPanelProps) => {
    const PanelTitleBar = VanillaComponentResolver.instance.PanelTitleBar;
    const Tab = VanillaComponentResolver.instance.Tab;
    const TabBar = VanillaComponentResolver.instance.TabBar;
    const TabNav = VanillaComponentResolver.instance.TabNav;
    const tabs = Object.values(Tabs);

    const onSelect = (i: string) => { engine.trigger("k45::we.main.setTabActive", tabs.indexOf(i as any)) }
    const selectedTabId = tabs[selectedTab]

    const header = <>
        <PanelTitleBar className="k45_we_mainPanel_title" onCloseOverride={noClose ? undefined : (() => VanillaComponentResolver.instance.toggleGamePanel(WeMainPanelId))}>Write Everywhere</PanelTitleBar>
    </>

    return <div className={classNames(VanillaComponentResolver.instance.gameMainScreenModule.centerPanelLayout, "k45_we_mainPanel")} style={{}}>
        <Panel header={header} draggable={moveable}>
            <TabBar className="k45_we_mainPanel_tabBar">{
                tabs.map(x => <Tab id={x} selectedId={selectedTabId} onSelect={onSelect} >{translate(`mainUi.tab.${x}`)}</Tab>)
            }</TabBar>
            <TabNav tabs={tabs} selectedTab={selectedTabId}>
                <div className="k45_we_mainPanel_content">
                    {selectedTabId == Tabs.CityLayouts && <CityLayoutsTab />}
                    {selectedTabId == Tabs.CityFonts && <FontsTab />}
                    {selectedTabId == Tabs.CityAtlases && <CityAtlasesTab />}
                    {selectedTabId == Tabs.PrefabTemplatesReplacements && <PrefabTemplatesReplacementsTab />}
                </div>
            </TabNav>
        </Panel>
    </div>;
}

