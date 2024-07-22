import { ModRegistrar } from "cs2/modding";
import { WriteEverywhereToolOptions, WriteEverywhereToolOptionsVisibility } from "toolOptions/WriteEverywhereToolOptions";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', WriteEverywhereToolOptionsVisibility);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', WriteEverywhereToolOptions);
}

export default register;
