import { Entity } from "@klyte45/vuio-commons";

export type WEFormulaeElement = WETypeMemberDesc | WEComponentTypeDesc | WEStaticMethodDesc;

export type WETextItemResume = {
    name: string;
    type: WESimulationTextType;
    id: Entity;
    children: WETextItemResume[];
};

export enum WESimulationTextType {
    Text = 0,
    Image = 1
}

export type EnumWrapper<T> = { value__: T; };

export enum WEMemberType {
    Field,
    Property,
    ParameterlessMethod
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
    STATIC_METHOD = "STATIC_METHOD"
}

export type WETypeMemberDesc = {
    WEDescType: WEDescType.MEMBER;
    memberName: string;
    memberTypeDllName: string;
    memberTypeClassName: string;
    type: EnumWrapper<WEMemberType>;
};

export type WEComponentTypeDesc = {
    WEDescType: WEDescType.COMPONENT;
    dllName: string;
    className: string;
    source: EnumWrapper<WEMethodSource>;
    modUrl: string;
    modName: string;
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
};

export function getDllNameFrom(el: WEFormulaeElement) {
    switch (el.WEDescType) {
        case WEDescType.COMPONENT: return el.dllName;
        case WEDescType.STATIC_METHOD: return el.returnTypeDll;
        case WEDescType.MEMBER: return el.memberTypeDllName;
    }
}
export function getClassNameFrom(el: WEFormulaeElement) {
    switch (el.WEDescType) {
        case WEDescType.COMPONENT: return el.className;
        case WEDescType.STATIC_METHOD: return el.returnType;
        case WEDescType.MEMBER: return el.memberTypeClassName;
    }
}

