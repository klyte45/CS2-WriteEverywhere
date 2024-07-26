import { Entity, HierarchyViewport, LocElementType, VanillaComponentResolver, VanillaWidgets } from "@klyte45/vuio-commons";
import { Portal, Panel } from "cs2/ui";
import { useEffect, useState } from "react";
import { WEComponentMemberDesc, WEComponentTypeDesc, WEDescType, WEFormulaeElement, WEFormulaeMethodDesc, WEMemberType, WEMethodSource, WESimulationTextType, WETextItemResume, WorldPickerService } from "services/WorldPickerService";
import { translate } from "utils/translate";



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
                    output += `${item.FormulaeString};`
                    break;
            }
        }
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
    return <Portal>
        <div className="k45_we_formulaeEditor">
            <div className="k45_we_formulaeEditor_title">Formulae stages</div>
            <EditorScrollable className="k45_we_formulaeEditor_content">
                <div className="k45_we_formulaeEditor_initial_dot" >Unity.Entities.Entity</div>
                <div className="k45_we_formulaeEditor_downArrow" />
                {formulaeSteps.map(x => {
                    switch (x.WEDescType) {
                        case WEDescType.COMPONENT:
                            return <WEComponentGetterBlock {...x} />
                        case WEDescType.MEMBER:
                            return <WEComponentMemberBlock {...x} />
                        case WEDescType.STATIC_METHOD:
                            return <WEMethodCallBlock {...x} />
                    }
                })}
                {requiresConvert(formulaeSteps[formulaeSteps.length - 1]) && <>
                    <div className="k45_we_formulaeEditor_implicitConversion" >Implicit Conversion to String</div>
                    <div className="k45_we_formulaeEditor_downArrow" />
                </>}
                <div className="k45_we_formulaeEditor_pipelineResult" >The final text will always have type System.String</div>
            </EditorScrollable>
        </div>
    </Portal>;
};

const WEMethodCallBlock = (data: WEFormulaeMethodDesc) => {
    return <>
        <div className="k45_we_formulaeEditor_methodCall">
            <div className="k45_we_formulaeEditor_dotTitle">Static Method Call</div>
            <div className="k45_we_formulaeEditor_assembly">{`${data.dllName} [${WEMethodSource[data.source.value__]}]`}</div>
            <div className="k45_we_formulaeEditor_class">{data.className}</div>
            <div className="k45_we_formulaeEditor_methodName">{data.methodName}</div>
            <WEReturnType>{data.returnType}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}

const WEComponentGetterBlock = (data: WEComponentTypeDesc) => {
    return <>
        <div className="k45_we_formulaeEditor_componentGet">
            <div className="k45_we_formulaeEditor_dotTitle">Get Component</div>
            <WEReturnType>{data.className}</WEReturnType>
        </div>
        <div className="k45_we_formulaeEditor_downArrow" />
    </>
}
const WEComponentMemberBlock = (data: WEComponentMemberDesc) => {
    let title: string;
    let className: string;
    switch (data.type.value__) {
        case WEMemberType.Field: title = "Get Field"; className = "k45_we_formulaeEditor_componentField"; break;
        case WEMemberType.ParameterlessMethod: title = "Run Class Method"; className = "k45_we_formulaeEditor_componentMethod"; break;
        case WEMemberType.Property: title = "Get Property"; className = "k45_we_formulaeEditor_componentProperty"; break;
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