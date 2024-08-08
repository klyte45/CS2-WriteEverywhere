import { ModRegistrar } from "cs2/modding";
import { WeMainPanelId, WEButton, WEMainPanel } from "mainUI/WEMainUI";
import { WriteEverywhereToolOptions, WriteEverywhereToolOptionsVisibility } from "toolOptions/WriteEverywhereToolOptions";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', WriteEverywhereToolOptionsVisibility);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', WriteEverywhereToolOptions);
    moduleRegistry.extend("game-ui/game/data-binding/game-bindings.ts", 'GamePanelType', RegisterWePanelType);
    moduleRegistry.extend("game-ui/game/components/game-panel-renderer.tsx", 'gamePanelComponents', RegisterWePanel);
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
export default register;
