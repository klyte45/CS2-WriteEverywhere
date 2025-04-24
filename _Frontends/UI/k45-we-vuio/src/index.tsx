import engine from "cohtml/cohtml";
import { bindValue } from "cs2/api";
import { ModRegistrar } from "cs2/modding";
import { Panel, Portal } from "cs2/ui";
import { WeMainPanelId, WEButton, WEMainPanel } from "mainUI/WEMainUI";
import { Component, ComponentType, FunctionComponent, useEffect, useState } from "react";
import { WriteEverywhereToolOptions, WriteEverywhereToolOptionsVisibility } from "toolOptions/WriteEverywhereToolOptions";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', WriteEverywhereToolOptionsVisibility);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', WriteEverywhereToolOptions);
    moduleRegistry.extend("game-ui/game/data-binding/game-bindings.ts", 'GamePanelType', RegisterWePanelType);
    moduleRegistry.extend("game-ui/game/components/game-panel-renderer.tsx", 'gamePanelComponents', RegisterWePanel);
    moduleRegistry.extend("game-ui/editor/components/toolbar/toolbar.tsx", 'Toolbar', WePanelEditor);
    moduleRegistry.append('GameTopLeft', WEButton);
}

const RegisterWePanelType = (input: any) => {
    input["K45_WE"] = WeMainPanelId
    return input;
}

const RegisterWePanel = (input: any) => {
    input[WeMainPanelId] = WEMainPanel
    return input;
}

const WePanelEditor = (input: any) => {
    const editorGroup = "editorTool"
    const editorSelection = "activeTool"
    return (args: any) => {
        const bindResult = bindValue(editorGroup, editorSelection);

        const [tabActive, setTabActive] = useState(0)
        engine.on("k45::we.main.setTabActive", setTabActive)

        useEffect(() => () => engine.off("k45::we.main.setTabActive", setTabActive))
        return <>
            {input(args)}
            {bindResult.value === "k45__we_MainWindow" && <Portal>
                <WEMainPanel selectedTab={tabActive} noClose moveable />
            </Portal>}
        </>
    }
}

export default register;
