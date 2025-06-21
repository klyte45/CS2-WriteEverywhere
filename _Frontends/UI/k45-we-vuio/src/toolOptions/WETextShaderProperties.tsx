import { LocElementType, replaceArgs, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal, Scrollable } from "cs2/ui";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import { useState, useEffect } from "react";
import { ExpandConsumer } from "cs2/input";
import { WESimulationTextType } from "services/WEFormulaeElement";


export const WETextShaderProperties = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_appearenceTitle = translate("shaderProperties.title"); //"Appearance Settings"
    const T_dynamicObjectsDecalFilter = translate("shaderProperties.dynamicObjectsDecalFilter"); //"Main Color"
    const T_supportDecals = translate("shaderProperties.supportBuildingDecals"); //"Emission Color"
    const T_supportRoadDecals = translate("shaderProperties.supportRoadDecals"); //"Emission Color"
    const T_supportTerrainDecals = translate("shaderProperties.supportTerrainDecals"); //"Emission Color"
    const T_supportCreatureDecals = translate("shaderProperties.supportCreatureDecals"); //"Emission Color"
    const T_supportOtherDecals = translate("shaderProperties.supportOtherDecals"); //"Emission Color"
    const T_shaderType = translate("shaderProperties.shaderType"); //"Emission Color"



    const T_dynamicObjectsDecalFilter_useOn = translate("shaderProperties.useOn.dynamicObjectsDecalFilter"); //"Main Color"
    const T_supportDecals_useOn = translate("shaderProperties.useOn.supportBuildingDecals"); //"Emission Color"
    const T_supportRoadDecals_useOn = translate("shaderProperties.useOn.supportRoadDecals"); //"Emission Color"
    const T_supportTerrainDecals_useOn = translate("shaderProperties.useOn.supportTerrainDecals"); //"Emission Color"
    const T_supportCreatureDecals_useOn = translate("shaderProperties.useOn.supportCreatureDecals"); //"Emission Color"
    const T_supportOtherDecals_useOn = translate("shaderProperties.useOn.supportOtherDecals"); //"Emission Color"
    const T_shaderType_useOn = translate("shaderProperties.useOn.shaderType"); //"Emission Color"

    const material = WorldPickerService.instance.bindingList.material;
    const mesh = WorldPickerService.instance.bindingList.mesh;
    const EditorItemRow = VanillaWidgets.instance.EditorItemRow;
    const DropdownField = VanillaWidgets.instance.DropdownField<number>();

    const availableShadersIdx = [WESimulationTextType.WhiteCube].includes(mesh.TextSourceType.value) ? [0, 1] : [0, 1, 2];
    const isDecalShader = material.ShaderType.value == 2;

    const [buildIdx, setBuild] = useState(0);
    useEffect(() => {
        WorldPickerService.instance.registerBindings(() => setBuild(buildIdx + 1))
    }, [buildIdx])

    const defaultPosition = props.initialPosition ?? { x: 1 - 600 / window.innerWidth, y: 100 / window.innerHeight }
    return <Portal>
        <Panel draggable header={T_appearenceTitle} className="k45_we_floatingSettingsPanel" initialPosition={defaultPosition} >
            <EditorItemRow label={T_shaderType}>
                <DropdownField
                    value={material.ShaderType.value}
                    items={availableShadersIdx.map(x => { return { displayName: { __Type: LocElementType.String, value: translate("shaderProperties.shaderType." + x) }, value: x } })}
                    onChange={(x) => material.ShaderType.set(x)}
                    style={{ flexGrow: 1, width: "inherit" }}
                />
            </EditorItemRow>
            <Scrollable style={{ maxHeight: "225rem" }}>
                <VanillaWidgets.instance.ToggleField value={(material.DecalFlags.value & 8) != 0} onChange={(x) => { material.DecalFlags.set(!x ? material.DecalFlags.value & ~8 : material.DecalFlags.value | 8) }} label={isDecalShader ? T_dynamicObjectsDecalFilter_useOn : T_dynamicObjectsDecalFilter} />
                <VanillaWidgets.instance.ToggleField value={(material.DecalFlags.value & 4) != 0} onChange={(x) => { material.DecalFlags.set(!x ? material.DecalFlags.value & ~4 : material.DecalFlags.value | 4) }} label={isDecalShader ? T_supportDecals_useOn : T_supportDecals} />
                <VanillaWidgets.instance.ToggleField value={(material.DecalFlags.value & 2) != 0} onChange={(x) => { material.DecalFlags.set(!x ? material.DecalFlags.value & ~2 : material.DecalFlags.value | 2) }} label={isDecalShader ? T_supportRoadDecals_useOn : T_supportRoadDecals} />
                <VanillaWidgets.instance.ToggleField value={(material.DecalFlags.value & 1) != 0} onChange={(x) => { material.DecalFlags.set(!x ? material.DecalFlags.value & ~1 : material.DecalFlags.value | 1) }} label={isDecalShader ? T_supportTerrainDecals_useOn : T_supportTerrainDecals} />
                <VanillaWidgets.instance.ToggleField value={(material.DecalFlags.value & 16) != 0} onChange={(x) => { material.DecalFlags.set(!x ? material.DecalFlags.value & ~16 : material.DecalFlags.value | 16) }} label={isDecalShader ? T_supportCreatureDecals_useOn : T_supportCreatureDecals} />
                <VanillaWidgets.instance.ToggleField value={(material.DecalFlags.value & 32) != 0} onChange={(x) => { material.DecalFlags.set(!x ? material.DecalFlags.value & ~32 : material.DecalFlags.value | 32) }} label={isDecalShader ? T_supportOtherDecals_useOn : T_supportOtherDecals} />
            </Scrollable>
        </Panel>
    </Portal>;
} 
