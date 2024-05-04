import { ModRegistrar } from "cs2/modding";
import { WriteEverywhereToolOptions, WriteEverywhereToolOptionsVisibility } from "toolOptions/WriteEverywhereToolOptions";
import { WriteEverywhereActionSectionExtension, WriteEverywhereSectionRegistering } from "toolOptions/WriteEverywhereActionSectionExtension";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', WriteEverywhereToolOptionsVisibility);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', WriteEverywhereToolOptions);
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/actions-section.tsx", 'ActionsSection', WriteEverywhereActionSectionExtension);
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', WriteEverywhereSectionRegistering)
}

export default register;
