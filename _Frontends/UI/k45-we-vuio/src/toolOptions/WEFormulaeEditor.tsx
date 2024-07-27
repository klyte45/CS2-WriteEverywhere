import { VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Portal } from "cs2/ui";
import { useEffect, useState } from "react";
import { WEComponentMemberDesc, WEComponentTypeDesc, WEDescType, WEFormulaeElement, WEFormulaeMethodDesc, WEMemberType, WEMethodSource, WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/formulaeEditor.scss"

const T_title = translate("formulaeEditor.title"); //Formulae stages
const T_implicitConversionWarning = translate("formulaeEditor.implicitConversionWarning"); //Implicit conversion to String
const T_finalPipelineAlwaysStringInfo = translate("formulaeEditor.finalPipelineAlwaysStringInfo"); //The final text will always have type String
const T_descType_staticMethod = translate("formulaeEditor.descType.staticMethod"); //Static method call
const T_descType_fieldGetter = translate("formulaeEditor.descType.fieldGetter"); //Load field
const T_descType_propertyGetter = translate("formulaeEditor.descType.propertyGetter"); //Get property
const T_descType_parameterlessInstanceMethodCall = translate("formulaeEditor.descType.parameterlessInstanceMethodCall"); //Call instance method
const T_descType_componentGetter = translate("formulaeEditor.descType.componentGetter"); //Get component
const T_addStageEnd = translate("formulaeEditor.addStageEnd"); //Get component
const T_removeLastStage = translate("formulaeEditor.removeLastStage"); //Get component
const T_editorFootnote = translate("formulaeEditor.editorFootnote"); //Get component

export const WEFormulaeEditor = () => {

    const wps = WorldPickerService.instance;
    const [formulaeSteps, setFormulaeSteps] = useState([] as WEFormulaeElement[])

    useEffect(() => {
        WorldPickerService.formulaeToPathObjects(wps.FormulaeStr.value).then(x => setFormulaeSteps(x))
        return;
    }, [wps.FormulaeStr.value])

    const pathObjectsToFormulae = (arr: WEFormulaeElement[]) => {
        let output = "";
        for (let item of arr) {
            switch (item?.WEDescType) {
                case WEDescType.COMPONENT:
                    if (output.length > 0) output += '/';
                    output += `${item.className};`
                    break;
                case WEDescType.MEMBER:
                    output += `${output.endsWith(";") ? "" : "."}${item.memberName}`
                    break;
                case WEDescType.STATIC_METHOD:
                    if (output.length > 0) output += '/';
                    output += `${item.FormulaeString}`
                    break;
            }
        }
        return output;
    }

    const requiresConvert = (x: WEFormulaeElement) => {
        switch (x?.WEDescType) {
            case WEDescType.COMPONENT:
                return true
            case WEDescType.MEMBER:
                return x.memberTypeClassName != "System.String"
            case WEDescType.STATIC_METHOD:
                return x.returnType != "System.String"
            default:
                return true;
        }
    }

    const EditorScrollable = VanillaWidgets.instance.EditorScrollable;

    const removeLastStage = () => {
        formulaeSteps.pop();
        wps.FormulaeStr.set(pathObjectsToFormulae(formulaeSteps))
    }

    return <Portal>
        <div className="k45_we_formulaeEditor">
            <div className="k45_we_formulaeEditor_title">{T_title}</div>
            <EditorScrollable className="k45_we_formulaeEditor_content">
                <div className="k45_we_formulaeEditor_initial_dot" >Entity</div>
                <div className="k45_we_formulaeEditor_downArrow" />
                {formulaeSteps.map((x, i) => {
                    switch (x.WEDescType) {
                        case WEDescType.COMPONENT:
                            return <WEComponentGetterBlock {...x} i={i} />
                        case WEDescType.MEMBER:
                            return <WEComponentMemberBlock {...x} i={i} />
                        case WEDescType.STATIC_METHOD:
                            return <WEMethodCallBlock {...x} i={i} />
                    }
                })}
                {requiresConvert(formulaeSteps[formulaeSteps.length - 1]) && <>
                    <div className="k45_we_formulaeEditor_implicitConversion" >{T_implicitConversionWarning}</div>
                    <div className="k45_we_formulaeEditor_downArrow" />
                </>}
                <div className="k45_we_formulaeEditor_pipelineResult" >{T_finalPipelineAlwaysStringInfo}</div>
            </EditorScrollable>
            <div className="k45_we_formulaeEditor_actions">
                <button className="positiveBtn">{T_addStageEnd}</button>
                <button className="negativeBtn" onClick={() => removeLastStage()} disabled={formulaeSteps.length <= 0}>{T_removeLastStage}</button>
                <div className="k45_we_formulaeEditor_footnote">{T_editorFootnote}</div>
            </div>
        </div>
    </Portal>;
};

const WEMethodCallBlock = (data: WEFormulaeMethodDesc & { i: number }) => {
    return <>
        <div className="k45_we_formulaeEditor_methodCall">
            <div className="k45_we_formulaeEditor_dotTitle">{T_descType_staticMethod}</div>
            <div className="k45_we_formulaeEditor_assembly">{`${data.dllName} [${WEMethodSource[data.source.value__]}]`}</div>
            <div className="k45_we_formulaeEditor_class">{data.className}</div>
            <div className="k45_we_formulaeEditor_methodName">{data.methodName}</div>
            <WEReturnType>{data.returnType}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}

const WEComponentGetterBlock = (data: WEComponentTypeDesc & { i: number }) => {
    return <>
        <div className="k45_we_formulaeEditor_componentGet">
            <div className="k45_we_formulaeEditor_dotTitle">{T_descType_componentGetter}</div>
            <WEReturnType>{data.className}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}
const WEComponentMemberBlock = (data: WEComponentMemberDesc & { i: number }) => {
    let title: string;
    let className: string;
    switch (data.type.value__) {
        case WEMemberType.Field: title = T_descType_fieldGetter; className = "k45_we_formulaeEditor_componentField"; break;
        case WEMemberType.ParameterlessMethod: title = T_descType_parameterlessInstanceMethodCall; className = "k45_we_formulaeEditor_componentMethod"; break;
        case WEMemberType.Property: title = T_descType_propertyGetter; className = "k45_we_formulaeEditor_componentProperty"; break;
    }
    return <>
        <div className={className}>
            <div className="k45_we_formulaeEditor_dotTitle">{title}</div>
            {data.type.value__ == WEMemberType.Field && <div className="k45_we_formulaeEditor_fieldName">{data.memberName}</div>}
            {data.type.value__ == WEMemberType.Property && <div className="k45_we_formulaeEditor_propertyName">{data.memberName}</div>}
            {data.type.value__ == WEMemberType.ParameterlessMethod && <div className="k45_we_formulaeEditor_methodName">{data.memberName}</div>}
            <WEReturnType>{data.memberTypeClassName}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}

const regexFullType = /(?<!`[0-9])\[([^,`[]+)[^[\]]+\]/g;
const regexGenType = /\[([a-zA-Z\._]+)`[0-9]+\[([^\]]+)\][^\]]*\]/g;
const regexGenTypeSt = /^([a-zA-Z\._]+)`[0-9]+\[([^\]]+)\][^\]]*$/g;

const WEReturnType = ({ children }: { children: string }) => {
    while (regexFullType.exec(children)) {
        children = children.replaceAll(regexFullType, "$1")
    }
    while (regexGenType.exec(children)) {
        children = children.replaceAll(regexGenType, "$1<$2>")
    }
    while (regexGenTypeSt.exec(children)) {
        children = children.replaceAll(regexGenTypeSt, "$1<$2>")
    }
    return <VanillaComponentResolver.instance.Tooltip tooltip={children.split("<").map((x, i) => <div>{(i > 0 ? " <" : "") + x}</div>)}>
        <div className="k45_we_formulaeEditor_returnType" >
            {children.split("<").map((x, i) => x.split(",").map(y => y.split(".").reverse()[0]).join(", ")).join(" <")}
        </div>
    </VanillaComponentResolver.instance.Tooltip>
}
