import { MultiUIValueBinding, UIColorRGBA, VanillaComponentResolver, VanillaFnResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Portal, Tooltip } from "cs2/ui";
import { useCallback, useEffect, useState } from "react";
import { FormulaeService } from "services/FormulaeService";
import { EnforceType, toFormulae, WEArrayIndexingDesc, WEComponentTypeDesc, WEDescType, WEFormulaeElement, WEFormulaeMathOperation, WEMathOperationDesc, WEMemberType, WEMethodSource, WEStaticMethodDesc, WETypeMemberDesc } from "services/WEFormulaeElement";
import { WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";
import "../style/formulaeEditor.scss";
import { WEAddFormulaeStageDialog } from "./WEAddFormulaeStageDialog";

type Props = {
    formulaeStr: MultiUIValueBinding<string>,
    formulaeType: 'string' | 'number' | 'color' | 'float3',
    lastCompileStatus: MultiUIValueBinding<number>
}

export const WEFormulaeEditor = ({ formulaeStr, formulaeType, lastCompileStatus }: Props) => {
    const T_title = translate("formulaeEditor.title"); //Formulae stages
    const T_finalPipelineAlwaysStringInfo = translate("formulaeEditor.finalPipelineAlwaysStringInfo"); //The final text will always have type String
    const T_finalPipelineAlwaysColorInfo = translate("formulaeEditor.finalPipelineAlwaysColorInfo"); //The final text will always have type String
    const T_finalPipelineAlwaysFloatInfo = translate("formulaeEditor.finalPipelineAlwaysFloatInfo"); //The final text will always have type String
    const T_finalPipelineAlwaysFloat3Info = translate("formulaeEditor.finalPipelineAlwaysFloat3Info"); //The final text will always have type String
    const T_implicitConversionWarningString = translate("formulaeEditor.implicitConversionWarningString"); //Implicit conversion to String
    const T_implicitConversionWarningColor = translate("formulaeEditor.implicitConversionWarningColor"); //Implicit conversion to String
    const T_implicitConversionWarningFloat = translate("formulaeEditor.implicitConversionWarningFloat"); //Implicit conversion to String
    const T_implicitConversionWarningColorFail = translate("formulaeEditor.implicitConversionWarningColorFail"); //Implicit conversion to String
    const T_implicitConversionWarningFloatFail = translate("formulaeEditor.implicitConversionWarningFloatFail"); //Implicit conversion to String
    const T_implicitConversionWarningFloat3Fail = translate("formulaeEditor.implicitConversionWarningFloat3Fail"); //Implicit conversion to String
    const T_addStageEnd = translate("formulaeEditor.addStageEnd"); //Get component
    const T_removeLastStage = translate("formulaeEditor.removeLastStage"); //Get component
    const T_editorFootnote = translate("formulaeEditor.editorFootnote"); //Get component
    const VanillaColorUtils = VanillaFnResolver.instance.color;

    const getImplicitConversionWarning = useCallback(() => {
        switch (formulaeType) {
            default:
            case 'string':
                return T_implicitConversionWarningString;
            case 'float3':
                return T_implicitConversionWarningFloat3Fail;
            case 'color':
                return lastCompileStatus.value == 0 ? T_implicitConversionWarningColor : T_implicitConversionWarningColorFail;
            case 'number':
                return lastCompileStatus.value == 0 ? T_implicitConversionWarningFloat : T_implicitConversionWarningFloatFail;
        }
    }, [formulaeType])
    const getTargetClass = useCallback(
        () => {
            switch (formulaeType) {
                default:
                case 'string':
                    return "System.String";
                case 'color':
                    return "UnityEngine.Color";
                case 'number':
                    return "System.Float";
                case 'float3':
                    return "UnityEngine.float3"
            }
        }, [formulaeType, lastCompileStatus])
    const getAlwaysConversionText = useCallback(
        () => {
            switch (formulaeType) {
                default:
                case 'string':
                    return T_finalPipelineAlwaysStringInfo;
                case 'color':
                    return T_finalPipelineAlwaysColorInfo;
                case 'number':
                    return T_finalPipelineAlwaysFloatInfo
                case 'float3':
                    return T_finalPipelineAlwaysFloat3Info;
            }
        }, [formulaeType, lastCompileStatus])

    const [formulaeSteps, setFormulaeSteps] = useState([] as (WEFormulaeElement | WEArrayIndexingDesc | WEMathOperationDesc)[])


    useEffect(() => {
        FormulaeService.formulaeToPathObjects(formulaeStr.value).then(x => setFormulaeSteps(x))
    }, [formulaeStr.value])

    const pathObjectsToFormulae = (arr: (WEFormulaeElement | WEArrayIndexingDesc | WEMathOperationDesc)[]) => {
        let output = "";
        for (let item of arr) {
            switch (item?.WEDescType) {
                case WEDescType.COMPONENT:
                    if (output.length > 0) output += '/';
                    output += `${item.className};`
                    break;
                case WEDescType.MATH_OPERATION:
                    output += `${output.endsWith(";") ? "" : "."}${toFormulae(item)}`
                    break;
                case WEDescType.MEMBER:
                    output += `${output.endsWith(";") ? "" : "."}${item.memberName}`
                    break;
                case WEDescType.STATIC_METHOD:
                    if (output.length > 0) output += '/';
                    output += `${item.FormulaeString}`
                    break;
                case WEDescType.ARRAY_INDEXING:
                    output += `${output.endsWith(";") ? "" : "."}${item.index}`
                    break;
            }
        }
        return output;
    }

    const requiresConvert = (x: (WEFormulaeElement | WEArrayIndexingDesc | WEMathOperationDesc)) => {
        switch (x?.WEDescType) {
            case WEDescType.COMPONENT:
                return true
            case WEDescType.MEMBER:
                return x.memberTypeClassName != getTargetClass()
            case WEDescType.STATIC_METHOD:
                return x.returnType != getTargetClass()
            default:
                return true;
        }
    }

    const EditorScrollable = VanillaWidgets.instance.EditorScrollable;

    const removeLastStage = () => {
        formulaeSteps.pop();
        formulaeStr.set(pathObjectsToFormulae(formulaeSteps))
    }

    const onAppend = (appendItem?: WEFormulaeElement | WEArrayIndexingDesc | WEMathOperationDesc) => {
        setAddingItem(false);
        if (appendItem) {
            formulaeSteps.push(appendItem);
            formulaeStr.set(pathObjectsToFormulae(formulaeSteps))
        }
    }

    const [addingItem, setAddingItem] = useState(false)

    const valueToString = (x: number | string | UIColorRGBA | number[] | null | undefined) => {
        if (!x && x !== 0) return "<EMPTY>";
        switch (typeof x) {
            case "number":
                return x.toFixed(3);
            case "string":
                return x;
            case "object":
                if (Array.isArray(x))
                    return `[${x[0].toFixed(3)},${x[1].toFixed(3)},${x[2].toFixed(3)}]`
                else
                    return "#" + VanillaColorUtils.formatHexColor(x) + Math.floor(x.a * 255).toString(16).padStart(2, '0').toUpperCase()
            default:
                return "???"
        }
    }
    return <Portal>
        <div className="k45_we_formulaeEditor">
            <div className="k45_we_formulaeEditor_title">{T_title}{WorldPickerService.instance.getCurrentEditingFormulaeFieldTitle()}</div>
            <EditorScrollable className="k45_we_formulaeEditor_content">
                {!formulaeSteps.length ? <><div className="k45_we_formulaeEditor_initial_dot" >{valueToString(WorldPickerService.instance.getCurrentEditingFormulaeValueField()?.value)}</div></> : <>
                    <div className="k45_we_formulaeEditor_initial_dot" >Entity</div>
                    <div className="k45_we_formulaeEditor_downArrow" />
                    {formulaeSteps.map((x, i) => {
                        switch (x.WEDescType) {
                            case WEDescType.COMPONENT:
                                return <WEComponentGetterBlock {...x} i={i} />
                            case WEDescType.MEMBER:
                                return <WEComponentMemberBlock {...x} i={i} />
                            case WEDescType.MATH_OPERATION:
                                return <WEMathOperatorBlock {...x} i={i} />
                            case WEDescType.STATIC_METHOD:
                                return <WEMethodCallBlock {...x} i={i} />
                        }
                    })}
                    {requiresConvert(formulaeSteps[formulaeSteps.length - 1]) && <>
                        <div className="k45_we_formulaeEditor_implicitConversion">{getImplicitConversionWarning()}</div>
                        <div className="k45_we_formulaeEditor_downArrow" />
                    </>}
                    <div className="k45_we_formulaeEditor_pipelineResult" >{getAlwaysConversionText()}</div>
                </>
                }
            </EditorScrollable>
            <div style={{ flexGrow: 1 }} />
            <div className="k45_we_formulaeEditor_actions">
                <button className="positiveBtn" onClick={() => setAddingItem(true)}>{T_addStageEnd}</button>
                <button className="negativeBtn" onClick={() => removeLastStage()} disabled={formulaeSteps.length <= 0}>{T_removeLastStage}</button>
                <div className="k45_we_formulaeEditor_footnote">{T_editorFootnote}</div>
            </div>
        </div>
        {addingItem && <WEAddFormulaeStageDialog formulaeStr={formulaeStr.value} callback={onAppend} referenceElement={formulaeSteps[formulaeSteps.length - 1] as WEFormulaeElement} />}
    </Portal>;
};

const WEMethodCallBlock = (data: WEStaticMethodDesc & { i: number }) => {
    const T_descType_staticMethod = translate("formulaeEditor.descType.staticMethod"); //Static method call
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
    const T_descType_componentGetter = translate("formulaeEditor.descType.componentGetter"); //Get component
    return <>
        <div className={"k45_we_formulaeEditor_componentGet" + (data.isBuffer ? "Buffer" : "")}>
            <div className="k45_we_formulaeEditor_dotTitle">{T_descType_componentGetter + (data.isBuffer ? " (Buffer)" : "")}</div>
            <WEReturnType>{data.returnClassName}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}
const WEComponentMemberBlock = (data: WETypeMemberDesc & { i: number }) => {
    let title: string;
    let className: string;

    const T_descType_fieldGetter = translate("formulaeEditor.descType.fieldGetter"); //Load field
    const T_descType_propertyGetter = translate("formulaeEditor.descType.propertyGetter"); //Get property
    const T_descType_parameterlessInstanceMethodCall = translate("formulaeEditor.descType.parameterlessInstanceMethodCall"); //Call instance method
    const T_descType_arrayIndexing = translate("formulaeEditor.descType.arrayIndexing"); //Call instance method


    switch (data.type.value__) {
        case WEMemberType.Field: title = T_descType_fieldGetter; className = "k45_we_formulaeEditor_componentField"; break;
        case WEMemberType.ParameterlessMethod: title = T_descType_parameterlessInstanceMethodCall; className = "k45_we_formulaeEditor_componentMethod"; break;
        case WEMemberType.Property: title = T_descType_propertyGetter; className = "k45_we_formulaeEditor_componentProperty"; break;
        case WEMemberType.ArraylikeIndexing: title = T_descType_arrayIndexing; className = "k45_we_formulaeEditor_componentArrayIndex"; break;
    }
    return <>
        <div className={className}>
            <div className="k45_we_formulaeEditor_dotTitle">{title}</div>
            {data.type.value__ == WEMemberType.Field && <div className="k45_we_formulaeEditor_fieldName">{data.memberName}</div>}
            {data.type.value__ == WEMemberType.Property && <div className="k45_we_formulaeEditor_propertyName">{data.memberName}</div>}
            {data.type.value__ == WEMemberType.ParameterlessMethod && <div className="k45_we_formulaeEditor_methodName">{data.memberName}</div>}
            {data.type.value__ == WEMemberType.ArraylikeIndexing && <div className="k45_we_formulaeEditor_arrayIndex">{data.memberName}</div>}
            <WEReturnType>{data.memberTypeClassName}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}


const WEMathOperatorBlock = (data: WEMathOperationDesc & { i: number }) => {
    return <>
        <div className="k45_we_formulaeEditor_mathOpContainer">
            <div className={["mathOp", "op" + WEFormulaeMathOperation[data.operation.value__]].join(" ")}>
                {data.operation.value__ == WEFormulaeMathOperation.NOT ? <>&nbsp;</> : <>
                    {data.value}
                    {data.enforceType.value__ != EnforceType.None && <Tooltip tooltip={translate("formulaeEditor.mathOp.force." + EnforceType[data.enforceType.value__])}><div className={"enforceType_" + EnforceType[data.enforceType.value__]} /></Tooltip>}
                </>}
            </div>
            <WEReturnType>{data.isDecimalResult ? "Single" : "Int32"}</WEReturnType>
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
