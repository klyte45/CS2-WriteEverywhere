import { Entity } from "@klyte45/vuio-commons";

export type WEFormulaeElement = WETypeMemberDesc | WEComponentTypeDesc | WEStaticMethodDesc | WEMathOperationDesc;

export type WETextItemResume = {
    name: string;
    type: WESimulationTextType;
    id: Entity;
    children: WETextItemResume[];
};

export enum WESimulationTextType {
    Text = 0,
    Image = 1,
    Placeholder = 2,
    WhiteTexture = 4,
    MatrixTransform = 5,
    WhiteCube = 6
}

export type EnumWrapper<T> = { value__: T; };

export enum WEMemberType {
    Field,
    Property,
    ParameterlessMethod,
    ArraylikeIndexing
}
export enum WEMethodSource {
    Game,
    Unity,
    CoUI,
    System,
    Mod,
    Unknown
}
export enum WEDescType {
    MEMBER = "MEMBER",
    COMPONENT = "COMPONENT",
    STATIC_METHOD = "STATIC_METHOD",
    ARRAY_INDEXING = "ARRAY_INDEXING",
    MATH_OPERATION = "MATH_OPERATION",
}

export enum WEFormulaeMathOperation {
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,
    MODULUS,
    EQUALS,
    GREATER,
    LESSER,
    AND,
    OR,
    XOR,
    NOT
}

export enum EnforceType {
    None,
    Float,
    Double,
}

export function toFormulae(op: WEMathOperationDesc) {
    let result = "";

    switch (op.operation.value__) {
        case WEFormulaeMathOperation.ADD: result += "+"; break;
        case WEFormulaeMathOperation.SUBTRACT: result += "-"; break;
        case WEFormulaeMathOperation.MULTIPLY: result += "*"; break;
        case WEFormulaeMathOperation.DIVIDE: result += "÷"; break;
        case WEFormulaeMathOperation.MODULUS: result += "%"; break;
        case WEFormulaeMathOperation.EQUALS: result += "="; break;
        case WEFormulaeMathOperation.GREATER: result += ">"; break;
        case WEFormulaeMathOperation.LESSER: result += "<"; break;
        case WEFormulaeMathOperation.AND: result += "∧"; break;
        case WEFormulaeMathOperation.OR: result += "∨"; break;
        case WEFormulaeMathOperation.XOR: result += "⊕"; break;
        case WEFormulaeMathOperation.NOT: result += "¬"; break;
    }
    if (op.operation.value__ != WEFormulaeMathOperation.NOT) {
        result += op.value.toString().replace(".", ",")
        switch (op.enforceType.value__) {
            case EnforceType.Double:
                result += "d";
                break;
            case EnforceType.Float:
                result += "f";
                break;
        }
    }
    return result;
}

export type WETypeMemberDesc = {
    WEDescType: WEDescType.MEMBER;
    memberName: string;
    memberTypeDllName: string;
    memberTypeClassName: string;
    type: EnumWrapper<WEMemberType>;
    supportsMathOp: boolean;
};

export type WEComponentTypeDesc = {
    WEDescType: WEDescType.COMPONENT;
    dllName: string;
    className: string;
    source: EnumWrapper<WEMethodSource>;
    modUrl: string;
    modName: string;
    returnDllName: string;
    returnClassName: string;
    isBuffer: boolean;
    supportsMathOp?: undefined;
};

export type WEStaticMethodDesc = {
    WEDescType: WEDescType.STATIC_METHOD;
    dllName: string;
    className: string;
    methodName: string;
    source: EnumWrapper<WEMethodSource>;
    modUrl: string;
    modName: string;
    returnType: string;
    returnTypeDll: string;
    FormulaeString: string;
    supportsMathOp: boolean;
};

export type WEArrayIndexingDesc = {
    WEDescType: WEDescType.ARRAY_INDEXING,
    index: number
}
export type WEMathOperationDesc = {
    WEDescType: WEDescType.MATH_OPERATION,
    value: string,
    operation: EnumWrapper<WEFormulaeMathOperation>
    supportsMathOp?: true;
    isDecimalResult: boolean;
    enforceType: EnumWrapper<EnforceType>;
}

export function getDllNameFrom(el: WEFormulaeElement) {
    switch (el.WEDescType) {
        case WEDescType.COMPONENT: return el.returnDllName;
        case WEDescType.STATIC_METHOD: return el.returnTypeDll;
        case WEDescType.MEMBER: return el.memberTypeDllName;
        case WEDescType.MATH_OPERATION: return "mscorlib";
    }
}
export function getClassNameFrom(el: WEFormulaeElement) {
    switch (el.WEDescType) {
        case WEDescType.COMPONENT: return el.returnClassName;
        case WEDescType.STATIC_METHOD: return el.returnType;
        case WEDescType.MEMBER: return el.memberTypeClassName;
        case WEDescType.MATH_OPERATION: return el.isDecimalResult ? "System.Single" : "System.Int32";
    }
}

