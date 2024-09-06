import { Entity, MultiUIValueBinding, UIColorRGBA } from "@klyte45/vuio-commons";
import { WEComponentTypeDesc, WEStaticMethodDesc, WETextItemResume } from "./WEFormulaeElement";
import { ObjectTyped } from "object-typed";

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




type ConstructorReturnType<P> = P extends new (name: string) => MultiUIValueBinding<infer R> ? MultiUIValueBinding<R> : never
type ConstructorObjectToInstancesObject<P> = Omit<{ [key in keyof P]: ConstructorReturnType<P[key]> }, `_${string}`>
type alpha = 'A' | 'B' | 'C' | 'D' | 'E' | 'F' | 'G' | 'H' | 'I' | 'J' | 'K' | 'L' | 'M' | 'N' | 'O' | 'P' | 'Q' | 'R' | 'S' | 'T' | 'U' | 'V' | 'W' | 'X' | 'Y' | 'Z'
type BindingClassObj = {
    _prefix: string,
    [key: `${alpha}${string}`]: new (...x: any[]) => any
}
function InitializeBindings<T extends BindingClassObj>(obj: T): ConstructorObjectToInstancesObject<Omit<T, '_prefix'>> {
    const keys = ObjectTyped.keys(obj) as (Exclude<keyof typeof obj & string, `_${string}`>)[];
    const result = {} as ConstructorObjectToInstancesObject<typeof obj>
    for (let entry of keys) {
        if (entry == "_prefix") continue;
        result[entry] = new (obj as Omit<typeof obj, "_prefix">)[entry as any](obj._prefix + "." + entry)
    }
    return result;
}

const WEWorldPickerController = {
    _prefix: "k45::we.wpicker",
    CurrentTree: MultiUIValueBinding<WETextItemResume[]>,
    CurrentSubEntity: MultiUIValueBinding<Entity | null>,
    CurrentEntity: MultiUIValueBinding<Entity | null>,
    MouseSensibility: MultiUIValueBinding<number>,
    CurrentPlaneMode: MultiUIValueBinding<number>,
    CurrentMoveMode: MultiUIValueBinding<number>,
    CurrentItemIsValid: MultiUIValueBinding<boolean>,
    CameraLocked: MultiUIValueBinding<boolean>,
    CameraRotationLocked: MultiUIValueBinding<boolean>, 
    FontList: MultiUIValueBinding<string[]>,
}
const WETextDataMainController = {
    _prefix: "k45::we.dataMain",
    CurrentItemName: MultiUIValueBinding<string>
}
const WETextDataTransformController = {
    _prefix: "k45::we.dataTransform",
    CurrentScale: MultiUIValueBinding<number[]>,
    CurrentRotation: MultiUIValueBinding<number[]>,
    CurrentPosition: MultiUIValueBinding<number[]>,
    UseAbsoluteSizeEditing: MultiUIValueBinding<boolean>,

}
const WETextDataMeshController = {
    _prefix: "k45::we.dataMesh",
    CurrentItemText: MultiUIValueBinding<string>,
    MaxWidth: MultiUIValueBinding<number>,
    SelectedFont: MultiUIValueBinding<string>,
    TextSourceType: MultiUIValueBinding<number>,
    ImageAtlasName: MultiUIValueBinding<string>,
    FormulaeStr: MultiUIValueBinding<string>,
    FormulaeCompileResult: MultiUIValueBinding<number>,
    FormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
}
const WETextDataMaterialController = {
    _prefix: "k45::we.dataMaterial",
    MainColor: MultiUIValueBinding<UIColorRGBA>,
    EmissiveColor: MultiUIValueBinding<UIColorRGBA>,
    ShaderType: MultiUIValueBinding<number>,
    GlassColor: MultiUIValueBinding<UIColorRGBA>,
    ColorMask1: MultiUIValueBinding<UIColorRGBA>,
    ColorMask2: MultiUIValueBinding<UIColorRGBA>,
    ColorMask3: MultiUIValueBinding<UIColorRGBA>,
    Metallic: MultiUIValueBinding<number>,
    Smoothness: MultiUIValueBinding<number>,
    EmissiveIntensity: MultiUIValueBinding<number>,
    CoatStrength: MultiUIValueBinding<number>,
    EmissiveExposureWeight: MultiUIValueBinding<number>,
    DecalFlags: MultiUIValueBinding<number>,
    GlassRefraction: MultiUIValueBinding<number>,
    NormalStrength: MultiUIValueBinding<number>,
    GlassThickness: MultiUIValueBinding<number>,
    MainColorFormulaeStr: MultiUIValueBinding<string>,
    MainColorFormulaeCompileResult: MultiUIValueBinding<number>,
    MainColorFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    EmissiveColorFormulaeStr: MultiUIValueBinding<string>,
    EmissiveColorFormulaeCompileResult: MultiUIValueBinding<number>,
    EmissiveColorFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    MetallicFormulaeStr: MultiUIValueBinding<string>,
    MetallicFormulaeCompileResult: MultiUIValueBinding<number>,
    MetallicFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    SmoothnessFormulaeStr: MultiUIValueBinding<string>,
    SmoothnessFormulaeCompileResult: MultiUIValueBinding<number>,
    SmoothnessFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    EmissiveIntensityFormulaeStr: MultiUIValueBinding<string>,
    EmissiveIntensityFormulaeCompileResult: MultiUIValueBinding<number>,
    EmissiveIntensityFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    CoatStrengthFormulaeStr: MultiUIValueBinding<string>,
    CoatStrengthFormulaeCompileResult: MultiUIValueBinding<number>,
    CoatStrengthFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    EmissiveExposureWeightFormulaeStr: MultiUIValueBinding<string>,
    EmissiveExposureWeightFormulaeCompileResult: MultiUIValueBinding<number>,
    EmissiveExposureWeightFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    DecalFlagsFormulaeStr: MultiUIValueBinding<string>,
    DecalFlagsFormulaeCompileResult: MultiUIValueBinding<number>,
    DecalFlagsFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    ShaderTypeFormulaeStr: MultiUIValueBinding<string>,
    ShaderTypeFormulaeCompileResult: MultiUIValueBinding<number>,
    ShaderTypeFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    GlassColorFormulaeStr: MultiUIValueBinding<string>,
    GlassColorFormulaeCompileResult: MultiUIValueBinding<number>,
    GlassColorFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    GlassRefractionFormulaeStr: MultiUIValueBinding<string>,
    GlassRefractionFormulaeCompileResult: MultiUIValueBinding<number>,
    GlassRefractionFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    ColorMask1FormulaeStr: MultiUIValueBinding<string>,
    ColorMask1FormulaeCompileResult: MultiUIValueBinding<number>,
    ColorMask1FormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    ColorMask2FormulaeStr: MultiUIValueBinding<string>,
    ColorMask2FormulaeCompileResult: MultiUIValueBinding<number>,
    ColorMask2FormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    ColorMask3FormulaeStr: MultiUIValueBinding<string>,
    ColorMask3FormulaeCompileResult: MultiUIValueBinding<number>,
    ColorMask3FormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    NormalStrengthFormulaeStr: MultiUIValueBinding<string>,
    NormalStrengthFormulaeCompileResult: MultiUIValueBinding<number>,
    NormalStrengthFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
    GlassThicknessFormulaeStr: MultiUIValueBinding<string>,
    GlassThicknessFormulaeCompileResult: MultiUIValueBinding<number>,
    GlassThicknessFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
}

export class WorldPickerService {
    public static get instance(): WorldPickerService { return _instance ??= new WorldPickerService() }

    public readonly bindingList: {
        picker: ConstructorObjectToInstancesObject<typeof WEWorldPickerController>,
        main: ConstructorObjectToInstancesObject<typeof WETextDataMainController>,
        mesh: ConstructorObjectToInstancesObject<typeof WETextDataMeshController>,
        material: ConstructorObjectToInstancesObject<typeof WETextDataMaterialController>,
        transform: ConstructorObjectToInstancesObject<typeof WETextDataTransformController>,
    };

    constructor() {

        this.bindingList = {
            picker: InitializeBindings(WEWorldPickerController),
            main: InitializeBindings(WETextDataMainController),
            material: InitializeBindings(WETextDataMaterialController),
            mesh: InitializeBindings(WETextDataMeshController),
            transform: InitializeBindings(WETextDataTransformController)
        }
    }

    registerBindings(refreshFn: () => any) {
        Object.values(this.bindingList).map(y => {
            Object.values(y).map(z => z.subscribe(async () => refreshFn()));
        })
    }

    disposeBindings() {
        Object.values(this.bindingList).map(y => {
            Object.values(y).map(z => z.dispose())
        })
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