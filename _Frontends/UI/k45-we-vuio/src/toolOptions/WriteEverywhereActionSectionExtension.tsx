import { ModuleRegistryExtend } from "cs2/modding";
import "../style/floatingPanels.scss";
import { VanillaComponentResolver, toEntityTyped } from "@klyte45/vuio-commons";
import weIcon from "../../../images/WE.svg"
import { selectedInfo } from "cs2/bindings";

const enableTool = function () {
    engine.call("k45::we.wpicker.enableTool", toEntityTyped(selectedInfo.selectedEntity$.value))
}

export const WriteEverywhereActionSectionExtension: ModuleRegistryExtend = (Component: any) => {
    return (props) => {
        const result = Component(props);


        const theme = VanillaComponentResolver.instance.actionsSectionTheme
        const divBtn = <div className="k45_we_actionSectionButton">
            <VanillaComponentResolver.instance.DescriptionTooltip title="Write Everywhere" description="ADASDASDASD">
                <VanillaComponentResolver.instance.IconButton src={weIcon} onClick={() => enableTool()} theme={VanillaComponentResolver.instance.actionButtonTheme} className={theme.button} />
            </VanillaComponentResolver.instance.DescriptionTooltip>
        </div>
        const childrenArr = (result.props.children.props.children as any[]);
        childrenArr.splice(childrenArr.length - childrenArr.filter(x => x.type == "div").length, 0, divBtn)


        return result;
    };
};
export const WriteEverywhereSectionRegistering = (componentList: any): any => {
    componentList["Game.UI.InGame.ActionsSection"] = WriteEverywhereActionSectionExtension(componentList["Game.UI.InGame.ActionsSection"])
    return componentList as any;
}

