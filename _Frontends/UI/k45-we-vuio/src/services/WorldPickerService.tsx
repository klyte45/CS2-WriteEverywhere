import { Entity, MultiUIValueBinding, UIColorRGBA } from "@klyte45/vuio-commons";
import { WEComponentTypeDesc, WEStaticMethodDesc, WETextItemResume } from "./WEFormulaeElement";

type number3 = [number, number, number]

let _instance: WorldPickerService;

export type IndexedStaticMethodsListing = {
    [srcType: number]: {
        [dllName: string]: {
            [className: string]: WEStaticMethodDesc[]
        }
    }
}
export type IndexedComponentListing = {
    [srcType: number]: {
        [dllName: string]: WEComponentTypeDesc[]
    }
}//Record<number, Record<string, Record<string, WEStaticMethodDesc[]>>>

export class WorldPickerService {
    public static get instance(): WorldPickerService { return _instance ??= new WorldPickerService() }

    CurrentItemName: MultiUIValueBinding<string>
    CurrentTree: MultiUIValueBinding<WETextItemResume[]>
    CurrentSubEntity: MultiUIValueBinding<Entity | null>
    CurrentEntity: MultiUIValueBinding<Entity | null>
    CurrentScale: MultiUIValueBinding<number3>
    MaxWidth: MultiUIValueBinding<number>
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
    DecalFlags: MultiUIValueBinding<number>
    UseAbsoluteSizeEditing: MultiUIValueBinding<boolean>
    ShaderType: MultiUIValueBinding<number>
    GlassRefraction: MultiUIValueBinding<number>
    GlassColor: MultiUIValueBinding<UIColorRGBA>
    ColorMask1: MultiUIValueBinding<UIColorRGBA>
    ColorMask2: MultiUIValueBinding<UIColorRGBA>
    ColorMask3: MultiUIValueBinding<UIColorRGBA>
    NormalStrength: MultiUIValueBinding<number>


    private Bindings: MultiUIValueBinding<any>[] = []

    constructor() {
        this.CurrentItemName ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemName")
        this.CurrentTree ??= new MultiUIValueBinding<WETextItemResume[]>("k45::we.wpicker.CurrentTree")
        this.CurrentSubEntity ??= new MultiUIValueBinding<Entity | null>("k45::we.wpicker.CurrentSubEntity")
        this.CurrentEntity ??= new MultiUIValueBinding<Entity | null>("k45::we.wpicker.CurrentEntity")
        this.CurrentScale ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentScale")
        this.MaxWidth ??= new MultiUIValueBinding<number>("k45::we.wpicker.MaxWidth")
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
        this.DecalFlags = new MultiUIValueBinding<number>("k45::we.wpicker.DecalFlags")
        this.UseAbsoluteSizeEditing = new MultiUIValueBinding<boolean>("k45::we.wpicker.UseAbsoluteSizeEditing")
        this.ShaderType ??= new MultiUIValueBinding<number>("k45::we.wpicker.ShaderType")
        this.GlassRefraction ??= new MultiUIValueBinding<number>("k45::we.wpicker.GlassRefraction")
        this.GlassColor ??= new MultiUIValueBinding<UIColorRGBA>("k45::we.wpicker.GlassColor")
        this.ColorMask1 ??= new MultiUIValueBinding<UIColorRGBA>("k45::we.wpicker.ColorMask1")
        this.ColorMask2 ??= new MultiUIValueBinding<UIColorRGBA>("k45::we.wpicker.ColorMask2")
        this.ColorMask3 ??= new MultiUIValueBinding<UIColorRGBA>("k45::we.wpicker.ColorMask3")
        this.NormalStrength ??= new MultiUIValueBinding<number>("k45::we.wpicker.NormalStrength")

        this.Bindings.push(
            this.CurrentSubEntity,
            this.CurrentTree,
            this.CurrentScale,
            this.MaxWidth,
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
            this.DecalFlags,
            this.ImageAtlasName,
            this.ShaderType,
            this.GlassColor,
            this.GlassRefraction,
            this.UseAbsoluteSizeEditing,
            this.ColorMask1,
            this.ColorMask2,
            this.ColorMask3,
            this.NormalStrength,
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
    static async changeParent(target: Entity, newParent: Entity): Promise<boolean> {
        return await engine.call("k45::we.wpicker.changeParent", target, newParent);
    }
    static async cloneAsChild(target: Entity, newParent: Entity) {
        return await engine.call("k45::we.wpicker.cloneAsChild", target, newParent);
    }
    static async removeItem() {
        return await engine.call("k45::we.wpicker.removeItem");
    }
    static async dumpBris() {
        return await engine.call("k45::we.wpicker.dumpBris");
    }
    static async addEmpty(parent?: Entity) {
        return await engine.call("k45::we.wpicker.addItem", parent ?? { Index: 0, Version: 0, __Type: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' });
    }


}

