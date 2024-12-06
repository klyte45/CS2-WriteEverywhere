import { LocElementType, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { WEListWithContentTab } from "common/WEListWithContentTab";
import { ListActionTypeArray, WEListWithPreviewTab } from "common/WEListWithPreviewTab";
import { FocusBoundary } from "cs2/input";
import { Scrollable, Tooltip } from "cs2/ui";
import { ObjectTyped } from "object-typed";
import { useEffect, useState } from "react";
import { FontService } from "services/FontService";
import { LayoutsService, ModReplacementData } from "services/LayoutsService";
import { TextureAtlasService } from "services/TextureAtlasService";
import "style/mainUi/layoutsReplacementsTab.scss";
import { translate } from "utils/translate";

type Props = {}

enum Modals {
    NONE,
    CONFIRMING_DELETE,
}


export const PrefabTemplatesReplacementsTab = (props: Props) => {
    const i_addItem = "coui://uil/Standard/Plus.svg";

    const T_fontsToReplace = translate("prefabTemplatesReplacementsTab.fontsToReplace")
    const T_noFontsToReplace = translate("prefabTemplatesReplacementsTab.noFontsToReplace")
    const T_atlasToReplace = translate("prefabTemplatesReplacementsTab.atlasToReplace")
    const T_noAtlasToReplace = translate("prefabTemplatesReplacementsTab.noAtlasToReplace")

    const [modsReplacementData, setModReplacementData] = useState({} as Record<string, ModReplacementData>)
    const [selectedMod, setSelectedMod] = useState(null as string | null)
    const [fontList, setFontList] = useState([] as string[])
    const [atlasList, setAtlasList] = useState([] as string[])


    const DropdownField = VanillaWidgets.instance.DropdownField<string>();
    const EditorRow = VanillaWidgets.instance.EditorItemRow;

    useEffect(() => {
        LayoutsService.listModsReplacementData().then((x) => {
            return setModReplacementData(ObjectTyped.fromEntries(x.map(y => [y.modId, y])));
        });
        FontService.listCityFonts().then((x) => {
            setFontList([""].concat(Object.keys(x).sort((a, b) => a.localeCompare(b))))
        })
        Promise.all([
            TextureAtlasService.listAvailableLibraries(),
            TextureAtlasService.listModAtlases()
        ]).then(([x, y]) => {
            setAtlasList([...Object.keys(x), ...y.flatMap(z => z.Atlases)]);
        });
    }, [])

    const currentFontList = (selectedMod && modsReplacementData[selectedMod]?.fonts) ?? {};
    const currentAtlasList = (selectedMod && modsReplacementData[selectedMod]?.atlases) ?? {};

    const listActions: ListActionTypeArray = []

    return <>
        <WEListWithContentTab listActions={listActions}
            listItems={Object.values(modsReplacementData).map(x => { return { displayName: x.displayName, value: x.modId } }).sort((a, b) => a.displayName.localeCompare(b.displayName))}
            selectedKey={selectedMod!} onChangeSelection={setSelectedMod} bodyClasses="layoutReplacementTab" >
            {selectedMod && <>
                <div className="k45_we_layoutReplacementTab_fonts">
                    {Object.keys(currentFontList).length == 0 ? <div className="emptyDataContainer">{T_noFontsToReplace}</div> : <>
                        <div className="sectionTitle">{T_fontsToReplace}</div>
                        <Scrollable >
                            {Object.entries(currentFontList).map((x, i) => <Tooltip tooltip={x[0]}><EditorRow key={i} label={x[0]}>
                                <DropdownField
                                    value={x[1] ?? ""}
                                    items={(fontList ?? []).map(x => { return { displayName: { __Type: LocElementType.String, value: !x ? "--DEFAULT--" : x }, value: x } })}
                                    onChange={(y) => {
                                        LayoutsService.setModFontReplacement(selectedMod, x[0], y).then(z => {
                                            modsReplacementData[selectedMod].fonts[x[0]] = z
                                            setModReplacementData({ ...modsReplacementData });
                                        })
                                    }}
                                />
                            </EditorRow></Tooltip>)}
                        </Scrollable>
                    </>}
                </div>
                <div className="k45_we_layoutReplacementTab_atlases">
                    {Object.keys(currentAtlasList).length == 0 ? <div className="emptyDataContainer">{T_noAtlasToReplace}</div> : <>
                        <div className="sectionTitle">{T_atlasToReplace}</div>
                        <Scrollable >
                            {Object.entries(currentAtlasList).map((x, i) => <Tooltip tooltip={x[0]}><EditorRow key={i} label={x[0]}><DropdownField
                                value={x[1] ?? x[0]}
                                items={(atlasList ?? []).map(x => { return { displayName: { __Type: LocElementType.String, value: !x ? "--DEFAULT--" : x }, value: x } })}
                                onChange={(y) => {
                                    LayoutsService.setModAtlasReplacement(selectedMod, x[0], y).then(z => {
                                        modsReplacementData[selectedMod].atlases[x[0]] = z
                                        setModReplacementData({ ...modsReplacementData });
                                    })
                                }}
                            /></EditorRow>
                            </Tooltip>)}
                        </Scrollable>
                    </>}
                </div>
            </>}
        </WEListWithContentTab>
    </>
};

