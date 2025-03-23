import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Panel, Portal, Scrollable } from "cs2/ui";
import { ReactNode, useEffect, useState } from "react";
import { FormulaeService } from "services/FormulaeService";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/floatingPanels.scss";
import "../style/variablesWindow.scss";


const i_ExcludeBtnIcon = "coui://uil/Standard/XClose.svg";

export const WELayoutVariablesView = (props: { initialPosition?: { x: number, y: number } }) => {
    const T_title = translate("layoutVariables.title"); //"Appearance Settings"
    const T_key = translate("layoutVariables.key"); //"Appearance Settings"
    const T_value = translate("layoutVariables.value"); //"Appearance Settings"
    const T_addVariable = translate("layoutVariables.addVariable"); //"Appearance Settings"
    const T_noVariable = translate("layoutVariables.noVariablesLbl"); //"Appearance Settings"
    const [buildIdx, setBuildIdx] = useState(0);
    const [variableList, setVariableList] = useState<[string, string][]>([]);
    useEffect(() => {
        WorldPickerService.instance.bindingList.picker.CurrentSubEntity.subscribe(async () => setBuildIdx(buildIdx + 1))
        FormulaeService.listVariablesOnCurrentEntity().then((x) => setVariableList(x ?? []))
    }, [buildIdx, WorldPickerService.instance.bindingList.picker.CurrentSubEntity.value])

    const defaultPosition = props.initialPosition ?? { x: 600 / window.innerWidth, y: 100 / window.innerHeight }
    return <Portal>
        <Panel draggable header={T_title} className="k45_we_floatingSettingsPanel k45_contentFillPanel" initialPosition={defaultPosition} style={{ height: "400rem", display: "flex", flexDirection: "column" }}
            contentClassName="k45_variablesListWindow">
            {!!variableList?.length && <EditorRow keyCmp={T_key} value={T_value} />}
            <Scrollable style={{ flexGrow: 1, flexShrink: 1, padding: "5rem", position: 'relative' }}>
                {
                    variableList?.length
                        ? variableList.map((x, i) => <EditorRowKV idx={i} key={i} setVariableList={setVariableList} variableList={variableList} reload={() => setBuildIdx(buildIdx + 1)} />)
                        : <div className="k45_variablesEmpty">{T_noVariable}</div>
                }
            </Scrollable>
            <div className="k45_we_dialogBtns">
                <button className="positiveBtn" style={{ width: "100%" }} onClick={() => FormulaeService.setVariablesOnCurrentEntity([...variableList, ["newKey", "newValue"]]).then(() => setBuildIdx(buildIdx + 1))}>{T_addVariable}</button>
            </div>
        </Panel>
    </Portal>;
}

function EditorRow({ keyCmp, value }: { keyCmp: ReactNode, value: ReactNode }) {
    const EditorItemRowNoFocus = VanillaWidgets.instance.EditorItemRowNoFocus;
    return <EditorItemRowNoFocus className="k45_variablesRow">
        <div className="k45_varKey">{keyCmp}</div>
        <div className="k45_varValue">{value}</div>
    </EditorItemRowNoFocus>;
}

const EditorRowKV = ({ idx, variableList, setVariableList, reload }: {
    setVariableList: (newValue: [string, string][]) => any,
    variableList: [string, string][],
    idx: number,
    reload: () => any
}) => {
    const T_ExcludeVariablesBtn = translate("layoutVariables.removeVariable"); //"Appearance Settings"
    const TextInput = VanillaWidgets.instance.StringInputField;

    const [keyTyping, setKeyTyping] = useState<string>("")
    const [valueTyping, setValueTyping] = useState<string>("")

    useEffect(() => {
        setKeyTyping(variableList[idx][0])
        setValueTyping(variableList[idx][1])
    }, [variableList, idx])

    return <EditorRow
        key={idx}
        keyCmp={<TextInput onChange={setKeyTyping} onChangeEnd={x => {
            const newList = variableList;
            newList.splice(idx, 1, [(x.target as any).value, valueTyping])
            setVariableList(newList);
            FormulaeService.setVariablesOnCurrentEntity(newList).then(reload);
        }} value={keyTyping} maxLength={30} />}
        value={<><TextInput onChange={setValueTyping} onChangeEnd={x => {
            const newList = variableList;
            newList.splice(idx, 1, [keyTyping, (x.target as any).value])
            setVariableList(newList);
            FormulaeService.setVariablesOnCurrentEntity(newList).then(reload);
        }} value={valueTyping} maxLength={30}
        />{<VanillaComponentResolver.instance.ToolButton
            onSelect={() => {
                const newList = variableList;
                newList.splice(idx, 1)
                FormulaeService.setVariablesOnCurrentEntity(newList).then(reload);
            }} src={i_ExcludeBtnIcon}
            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            className={VanillaComponentResolver.instance.toolButtonTheme.button}
            tooltip={T_ExcludeVariablesBtn} />
            }</>}
    />;
}