import { VanillaWidgets } from "@klyte45/vuio-commons";
import { Button, Panel, Portal, Scrollable, Tooltip } from "cs2/ui";
import { ReactNode, useEffect, useState } from "react";
import { DebugService, ShaderPropertyType, WEDebugPropertyDescriptor } from "services/DebugService";
import { WorldPickerService } from "services/WorldPickerService";
import "../style/floatingPanels.scss";
import "../style/formulaeEditorField.scss";
import { FocusDisabled } from "cs2/input";


export const WEDebugWindow = (props: { initialPosition?: { x: number, y: number } }) => {

    const defaultPosition = props.initialPosition ?? { x: 1 - 700 / window.innerWidth, y: 200 / window.innerHeight }
    const wps = WorldPickerService.instance.bindingList.picker;

    const [currentMaterialSettings, setCurrentMaterialSettings] = useState<WEDebugPropertyDescriptor[] | null>(null)
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;

    useEffect(() => {
        DebugService.listCurrentMaterialSettings(wps.CurrentSubEntity.value!).then((x) => {
            return setCurrentMaterialSettings(x);
        })
    }, [wps.CurrentSubEntity.value])

    function getValue(x: WEDebugPropertyDescriptor): ReactNode {
        switch (x.Type) {
            case ShaderPropertyType.Int:
                return parseInt(x.Value).toFixed(0);
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                return x.Value.startsWith("0x") ? x.Value : parseFloat(x.Value).toFixed(4);
            case ShaderPropertyType.Color:
                return `#${x.Value}`;
            case ShaderPropertyType.Texture:
                return x.Value ? "TEXTURE" : "null"
            case ShaderPropertyType.Vector:
                return x.Value;
        }

        return x.Value;
    }

    return <Portal>
        <Panel draggable header={"DEBUG"} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <EditorItemRow label="" >{wps.CurrentEntity.value?.Index + ""} ({wps.CurrentSubEntity.value?.Index + ""})</EditorItemRow>
            <EditorItemRow label="" >Current selection material values ({currentMaterialSettings?.length + ""})</EditorItemRow>
            <Scrollable style={{ maxHeight: "225rem" }}>
                {
                    currentMaterialSettings
                        ?.sort((a, b) => a.Name.localeCompare(b.Name))
                        .map(x => <Tooltip tooltip={`${x.Description} - ${x.Type}`}><EditorItemRow key={x.Id} label={x.Name}>{getValue(x)}</EditorItemRow></Tooltip>)
                }
            </Scrollable>
            <EditorItemRow label="" >
                <FocusDisabled>
                    <Button className="btn neutralBtn" onClick={() => { console.log(currentMaterialSettings) }}>Dump to console</Button>
                    <Button className="btn neutralBtn" onClick={() => { DebugService.listShader().then(console.log) }}>Dump shaders available</Button>
                    <Button className="btn negativeBtn" onClick={() => { DebugService.createSpecialMeshBRI(wps.CurrentSubEntity.value!) }}>Import mesh!!!</Button>
                </FocusDisabled>
            </EditorItemRow>
        </Panel>
    </Portal>;
} 
