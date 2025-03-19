import { Entity, MultiUIValueBinding, UIColorRGBA } from "@klyte45/vuio-commons";
import { WEComponentTypeDesc, WEStaticMethodDesc, WETextItemResume } from "./WEFormulaeElement";
import { ObjectTyped } from "object-typed";
import { translate } from "utils/translate";
import engine from "cohtml/cohtml";

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
    ShowProjectionCube: MultiUIValueBinding<boolean>,
}
const WETextDataMainController = {
    _prefix: "k45::we.dataMain",
    CurrentItemName: MultiUIValueBinding<string>
}
const WETextDataTransformController = {
    _prefix: "k45::we.dataTransform",
    CurrentScale: MultiUIValueBinding<number3>,
    CurrentRotation: MultiUIValueBinding<number3>,
    CurrentPosition: MultiUIValueBinding<number3>,
    UseAbsoluteSizeEditing: MultiUIValueBinding<boolean>,
    Pivot: MultiUIValueBinding<WEPlacementPivot>,

}
const WETextDataMeshController = {
    _prefix: "k45::we.dataMesh",
    ValueText: MultiUIValueBinding<string>,
    MaxWidth: MultiUIValueBinding<number>,
    SelectedFont: MultiUIValueBinding<string>,
    TextSourceType: MultiUIValueBinding<number>,
    ImageAtlasName: MultiUIValueBinding<string>,
    ValueTextFormulaeStr: MultiUIValueBinding<string>,
    ValueTextFormulaeCompileResult: MultiUIValueBinding<number>,
    ValueTextFormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
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
    AffectSmoothness: MultiUIValueBinding<boolean>,
    AffectAO: MultiUIValueBinding<boolean>,
    AffectEmission: MultiUIValueBinding<boolean>,
    DrawOrder: MultiUIValueBinding<number>,
}

type FormulableMaterialKeys =
    'MainColor' |
    'EmissiveColor' |
    'ShaderType' |
    'GlassColor' |
    'ColorMask1' |
    'ColorMask2' |
    'ColorMask3' |
    'Metallic' |
    'Smoothness' |
    'EmissiveIntensity' |
    'CoatStrength' |
    'EmissiveExposureWeight' |
    'DecalFlags' |
    'GlassRefraction' |
    'NormalStrength' |
    'GlassThickness';
type FormulableMaterialValues<key extends string> = { str: `${key}FormulaeStr`, result: `${key}FormulaeCompileResult`, args: `${key}FormulaeCompileResultErrorArgs` };
export type FormulableMaterialEntries = { [key in FormulableMaterialKeys]: FormulableMaterialValues<key> }
export const FormulableMaterialEntries = ObjectTyped.fromEntries(([
    'MainColor', 'EmissiveColor', 'ShaderType', 'GlassColor', 'ColorMask1', 'ColorMask2', 'ColorMask3', 'Metallic', 'Smoothness', 'EmissiveIntensity', 'CoatStrength', 'EmissiveExposureWeight', 'DecalFlags', 'GlassRefraction', 'NormalStrength', 'GlassThickness'
] as FormulableMaterialKeys[]).map(x => [x, { str: `${x}FormulaeStr`, result: `${x}FormulaeCompileResult`, args: `${x}FormulaeCompileResultErrorArgs` }])) as FormulableMaterialEntries

export enum WEPlacementPivot {
    TopLeft = 0,
    TopCenter = 1,
    TopRight = 2,
    MiddleLeft = 4,
    MiddleCenter = 5,
    MiddleRight = 6,
    BottomLeft = 8,
    BottomCenter = 9,
    BottomRight = 10,
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

    public readonly formulaeTitleNames: Partial<{
        picker: Partial<{ [key in keyof typeof WEWorldPickerController]: () => string }>,
        main: Partial<{ [key in keyof typeof WETextDataMainController]: () => string }>,
        mesh: Partial<{ [key in keyof typeof WETextDataMeshController]: () => string }>,
        material: Partial<{ [key in keyof typeof WETextDataMaterialController]: () => string }>,
        transform: Partial<{ [key in keyof typeof WETextDataTransformController]: () => string }>,
    }>;

    public currentFormulaeModule?: keyof typeof WorldPickerService.instance.bindingList
    public currentFormulaeField?: keyof typeof WorldPickerService.instance.bindingList[Exclude<typeof this.currentFormulaeModule, undefined>]
    private refreshFnRegistered: (() => any)[] = []

    constructor() {

        this.bindingList = {
            picker: InitializeBindings(WEWorldPickerController),
            main: InitializeBindings(WETextDataMainController),
            material: InitializeBindings(WETextDataMaterialController),
            mesh: InitializeBindings(WETextDataMeshController),
            transform: InitializeBindings(WETextDataTransformController)
        }
        this.formulaeTitleNames = {
            mesh: {
                ValueText: () => translate("formulaeTitleName.mesh." + this.bindingList.mesh.TextSourceType.value + ".ValueText")
            },
            material: {
                MainColor: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".MainColor"),
                EmissiveColor: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".EmissiveColor"),
                GlassColor: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".GlassColor"),
                ColorMask1: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".ColorMask1"),
                ColorMask2: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".ColorMask2"),
                ColorMask3: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".ColorMask3"),
                Metallic: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".Metallic"),
                Smoothness: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".Smoothness"),
                EmissiveIntensity: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".EmissiveIntensity"),
                CoatStrength: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".CoatStrength"),
                EmissiveExposureWeight: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".EmissiveExposureWeight"),
                GlassRefraction: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".GlassRefraction"),
                NormalStrength: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".NormalStrength"),
                GlassThickness: () => translate("formulaeTitleName.material." + this.bindingList.material.ShaderType.value + ".GlassThickness"),
            }
        }

    }

    registerBindings(refreshFn: () => any) {
        Object.values(this.bindingList).map(y => {
            Object.values(y).map(z => z.subscribe(async () => refreshFn()));
        })
        this.refreshFnRegistered.push(refreshFn);

        this.bindingList.picker.CurrentSubEntity.subscribe(async () => {
            this.clearCurrentEditingFormulaeParam();
        })
        this.bindingList.picker.CurrentEntity.subscribe(async () => {
            this.clearCurrentEditingFormulaeParam()
        });
    }

    disposeBindings() {
        Object.values(this.bindingList).map(y => {
            Object.values(y).map(z => z.dispose())
        })
        this.refreshFnRegistered = []
    }



    setCurrentEditingFormulaeParam(module: keyof typeof this.bindingList, field: string) {
        if (this.bindingList[module][(field + "FormulaeStr" as never)]) {
            this.currentFormulaeModule = module;
            this.currentFormulaeField = field as any;
            this.refreshFnRegistered.map(x => x())
        }
    }
    clearCurrentEditingFormulaeParam() {
        this.currentFormulaeModule = undefined;
        this.currentFormulaeField = undefined;
        this.refreshFnRegistered.map(x => x())
    }

    getCurrentEditingFormulaeValueField(): MultiUIValueBinding<number | UIColorRGBA | string> | null {
        return this.currentFormulaeField && this.currentFormulaeModule ? this.bindingList[this.currentFormulaeModule][this.currentFormulaeField] : null
    }
    getCurrentEditingFormulaeFieldTitle(): string {
        return this.currentFormulaeField && this.currentFormulaeModule ? (this.formulaeTitleNames[this.currentFormulaeModule]?.[this.currentFormulaeField] as any)?.() : null!
    }
    getCurrentEditingFormulaeFn(): MultiUIValueBinding<string> | null {
        return this.currentFormulaeField && this.currentFormulaeModule ? this.bindingList[this.currentFormulaeModule][this.currentFormulaeField + "FormulaeStr" as never] : null
    }
    getCurrentEditingFormulaeFnResult(): MultiUIValueBinding<number> | null {
        return this.currentFormulaeField && this.currentFormulaeModule ? this.bindingList[this.currentFormulaeModule][this.currentFormulaeField + "FormulaeCompileResult" as never] as MultiUIValueBinding<number> : null
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
    static async debugAvailable(): Promise<boolean> {
        return await engine.call("k45::we.wpicker.debugAvailable");
    }
    static async currentIsDecal(): Promise<boolean> {
        return await engine.call("k45::we.dataMaterial.isDecalMesh");
    }
}