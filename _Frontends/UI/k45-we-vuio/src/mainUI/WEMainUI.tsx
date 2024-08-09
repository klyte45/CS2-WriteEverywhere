
import icon from "images/WE-White.svg"
import { Button, Panel, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "@klyte45/vuio-commons";
import classNames from "classnames";
import "style/mainUi/mainUi.scss"
import { translate } from "utils/translate";
import { CityLayoutsTab } from "./CityLayoutsTab";
import { FontsTab } from "./FontsTab";

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

type MainPanelProps = { selectedTab: number, onClose: () => any }
enum Tabs {
    CityLayouts = "CityLayouts",
    CityFonts = "CityFonts",
    CityAtlases = "CityAtlases",
    PrefabTemplates = "PrefabTemplates"
}
export const WEMainPanel = (props: MainPanelProps) => {
    const PanelTitleBar = VanillaComponentResolver.instance.PanelTitleBar;
    const Tab = VanillaComponentResolver.instance.Tab;
    const TabBar = VanillaComponentResolver.instance.TabBar;
    const TabNav = VanillaComponentResolver.instance.TabNav;
    const tabs = Object.values(Tabs);

    const onSelect = (i: string) => { engine.trigger("k45::we.main.setTabActive", tabs.indexOf(i as any)) }
    const selectedTab = tabs[props.selectedTab]

    const header = <>
        <PanelTitleBar className="k45_we_mainPanel_title" onCloseOverride={() => VanillaComponentResolver.instance.toggleGamePanel(WeMainPanelId)}>Write Everywhere</PanelTitleBar>
        <TabBar className="k45_we_mainPanel_tabBar">{
            tabs.map(x => <Tab id={x} selectedId={selectedTab} onSelect={onSelect} >{translate(`mainUi.tab.${x}`)}</Tab>)
        }</TabBar>
    </>

    return <div className={classNames(VanillaComponentResolver.instance.gameMainScreenModule.centerPanelLayout, "k45_we_mainPanel")}>
        <Panel header={header} className="k45_we_mainPanel_content">
            <TabNav tabs={tabs} selectedTab={selectedTab}>
                {selectedTab == Tabs.CityLayouts && <CityLayoutsTab />}
                {selectedTab == Tabs.CityFonts && <FontsTab />}
            </TabNav>
        </Panel>
    </div >;
}

