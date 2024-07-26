import { Entity, MultiUIValueBinding, UIColorRGBA } from "@klyte45/vuio-commons"
import { Component, version } from "react"

type number3 = [number, number, number]

let _instance: WorldPickerService;

export class WorldPickerService {
    public static get instance(): WorldPickerService { return _instance ??= new WorldPickerService() }

    CurrentItemName: MultiUIValueBinding<string>
    CurrentTree: MultiUIValueBinding<WETextItemResume[]>
    CurrentSubEntity: MultiUIValueBinding<Entity | null>
    CurrentEntity: MultiUIValueBinding<Entity | null>
    CurrentScale: MultiUIValueBinding<number3>
    CurrentRotation: MultiUIValueBinding<number3>
    CurrentPosition: MultiUIValueBinding<number3>
    MouseSensibility: MultiUIValueBinding<number>
    CurrentPlaneMode: MultiUIValueBinding<number>
    CurrentItemText: MultiUIValueBinding<string>
    CurrentItemIsValid: MultiUIValueBinding<string>
    CameraLocked: MultiUIValueBinding<boolean>
    CameraRotationLocked: MultiUIValueBinding<boolean>
    CurrentMoveMode: MultiUIValueBinding<number>

    MainColor: MultiUIValueBinding<UIColorRGBA>
    EmissiveColor: MultiUIValueBinding<UIColorRGBA>
    Metallic: MultiUIValueBinding<number>
    Smoothness: MultiUIValueBinding<number>
    EmissiveIntensity: MultiUIValueBinding<number>
    CoatStrength: MultiUIValueBinding<number>
    EmissiveExposureWeight: MultiUIValueBinding<number>

    FontList: MultiUIValueBinding<string[]>
    SelectedFont: MultiUIValueBinding<string>

    FormulaeStr: MultiUIValueBinding<string>
    FormulaeCompileResult: MultiUIValueBinding<number>
    TextSourceType: MultiUIValueBinding<number>
    ImageAtlasName: MultiUIValueBinding<string>


    private Bindings: MultiUIValueBinding<any>[] = []

    constructor() {
        this.CurrentItemName ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemName")
        this.CurrentTree ??= new MultiUIValueBinding<WETextItemResume[]>("k45::we.wpicker.CurrentTree")
        this.CurrentSubEntity ??= new MultiUIValueBinding<Entity | null>("k45::we.wpicker.CurrentSubEntity")
        this.CurrentEntity ??= new MultiUIValueBinding<Entity | null>("k45::we.wpicker.CurrentEntity")
        this.CurrentScale ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentScale")
        this.CurrentRotation ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentRotation")
        this.CurrentPosition ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentPosition")
        this.MouseSensibility ??= new MultiUIValueBinding<number>("k45::we.wpicker.MouseSensibility")
        this.CurrentPlaneMode ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentPlaneMode")
        this.CurrentItemText ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemText")
        this.CurrentItemIsValid ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemIsValid")
        this.CameraLocked ??= new MultiUIValueBinding<boolean>("k45::we.wpicker.CameraLocked")
        this.CameraRotationLocked ??= new MultiUIValueBinding<boolean>("k45::we.wpicker.CameraRotationLocked")
        this.CurrentMoveMode ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentMoveMode")

        this.MainColor ??= new MultiUIValueBinding<UIColorRGBA>("k45::we.wpicker.MainColor")
        this.EmissiveColor ??= new MultiUIValueBinding<UIColorRGBA>("k45::we.wpicker.EmissiveColor")
        this.Metallic ??= new MultiUIValueBinding<number>("k45::we.wpicker.Metallic")
        this.Smoothness ??= new MultiUIValueBinding<number>("k45::we.wpicker.Smoothness")
        this.EmissiveIntensity ??= new MultiUIValueBinding<number>("k45::we.wpicker.EmissiveIntensity")
        this.CoatStrength ??= new MultiUIValueBinding<number>("k45::we.wpicker.CoatStrength")
        this.EmissiveExposureWeight ??= new MultiUIValueBinding<number>("k45::we.wpicker.EmissiveExposureWeight")
        this.FontList = new MultiUIValueBinding<string[]>("k45::we.wpicker.FontList")
        this.SelectedFont = new MultiUIValueBinding<string>("k45::we.wpicker.SelectedFont")
        this.FormulaeStr = new MultiUIValueBinding<string>("k45::we.wpicker.FormulaeStr")
        this.FormulaeCompileResult = new MultiUIValueBinding<number>("k45::we.wpicker.FormulaeCompileResult")
        this.TextSourceType = new MultiUIValueBinding<number>("k45::we.wpicker.TextSourceType")
        this.ImageAtlasName = new MultiUIValueBinding<string>("k45::we.wpicker.ImageAtlasName")

        this.Bindings.push(
            this.CurrentSubEntity,
            this.CurrentTree,
            this.CurrentScale,
            this.CurrentRotation,
            this.CurrentPosition,
            this.MouseSensibility,
            this.CurrentPlaneMode,
            this.CurrentItemText,
            this.CurrentItemIsValid,
            this.CurrentEntity,
            this.CurrentItemName,
            this.CameraLocked,
            this.CameraRotationLocked,
            this.CurrentMoveMode,
            this.MainColor,
            this.EmissiveColor,
            this.Metallic,
            this.Smoothness,
            this.EmissiveIntensity,
            this.CoatStrength,
            this.EmissiveExposureWeight,
            this.FontList,
            this.SelectedFont,
            this.FormulaeStr,
            this.FormulaeCompileResult,
            this.TextSourceType,
            this.ImageAtlasName
        );
    }

    registerBindings(refreshFn: () => any) {
        this.Bindings.map(y => {
            y.subscribe(async () => refreshFn());
        })
    }

    disposeBindings() {
        this.Bindings.map(y => {
            y.dispose();
        })
    }

    static async listAvailableLibraries() {
        return await engine.call("k45::we.wpicker.listAvailableLibraries");
    }
    static async listAtlasImages(atlas: string) {
        return await engine.call("k45::we.wpicker.listAtlasImages", atlas);
    }
    static async requireFontInstallation(fontName: string): Promise<string> {
        return await engine.call("k45::we.wpicker.requireFontInstallation", fontName);
    }
    static async changeParent(target: Entity, newParent: Entity): Promise<boolean> {
        return await engine.call("k45::we.wpicker.changeParent", target, newParent);
    }
    static async cloneAsChild(target: Entity, newParent: Entity) {
        return await engine.call("k45::we.wpicker.cloneAsChild", target, newParent);
    }
    static async removeItem() {
        return await engine.call("k45::we.wpicker.removeItem");
    }
    static async addEmpty(parent?: Entity) {
        return await engine.call("k45::we.wpicker.addItem", parent ?? { Index: 0, Version: 0, __Type: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' });
    }
    static async listAvailableMethodsForType(typeName: string): Promise<WEFormulaeMethodDesc[]> {
        return await engine.call("k45::we.wpicker.listAvailableMethodsForType", typeName);
    }
    static async formulaeToPathObjects(formulae: string): Promise<WEFormulaeElement[]> {
        return await engine.call("k45::we.wpicker.formulaeToPathObjects", formulae);
    }
}

export type WETextItemResume = {
    name: string;
    type: WESimulationTextType;
    id: Entity;
    children: WETextItemResume[];
}

export enum WESimulationTextType {
    Text = 0,
    Image = 1
}

export type EnumWrapper<T> = { value__: T }

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

export type WEComponentMemberDesc = {
    WEDescType: WEDescType.MEMBER,
    memberName: string;
    memberTypeDllName: string;
    memberTypeClassName: string;
    type: EnumWrapper<WEMemberType>
}

export type WEComponentTypeDesc = {
    WEDescType: WEDescType.COMPONENT,
    dllName: string;
    className: string;
}

export type WEFormulaeMethodDesc = {
    WEDescType: WEDescType.STATIC_METHOD,
    dllName: string;
    className: string;
    methodName: string;
    source: EnumWrapper<WEMethodSource>;
    modUrl: string;
    modName: string;
    returnType: string;
    FormulaeString: string;
}

export type WEFormulaeElement = WEComponentMemberDesc | WEComponentTypeDesc | WEFormulaeMethodDesc