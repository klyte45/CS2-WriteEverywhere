import { MultiUIValueBinding, UIColorRGBA } from "@klyte45/vuio-commons"
import { Component } from "react"

type number3 = [number, number, number]

type Entity = {
    Index: number,
    Version: number
}
let _instance: WorldPickerService;

export class WorldPickerService {
    public static get instance(): WorldPickerService { return _instance ??= new WorldPickerService() }

    CurrentItemName: MultiUIValueBinding<string>
    CurrentItemIdx: MultiUIValueBinding<number>
    CurrentEntity: MultiUIValueBinding<Entity | null>
    CurrentScale: MultiUIValueBinding<number3>
    CurrentRotation: MultiUIValueBinding<number3>
    CurrentPosition: MultiUIValueBinding<number3>
    MouseSensibility: MultiUIValueBinding<number>
    CurrentPlaneMode: MultiUIValueBinding<number>
    CurrentItemCount: MultiUIValueBinding<number>
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


    private Bindings: MultiUIValueBinding<any>[] = []

    constructor() {
        this.CurrentItemName ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemName")
        this.CurrentItemIdx ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentItemIdx")
        this.CurrentEntity ??= new MultiUIValueBinding<Entity | null>("k45::we.wpicker.CurrentEntity")
        this.CurrentScale ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentScale")
        this.CurrentRotation ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentRotation")
        this.CurrentPosition ??= new MultiUIValueBinding<number3>("k45::we.wpicker.CurrentPosition")
        this.MouseSensibility ??= new MultiUIValueBinding<number>("k45::we.wpicker.MouseSensibility")
        this.CurrentPlaneMode ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentPlaneMode")
        this.CurrentItemText ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemText")
        this.CurrentItemIsValid ??= new MultiUIValueBinding<string>("k45::we.wpicker.CurrentItemIsValid")
        this.CurrentItemCount ??= new MultiUIValueBinding<number>("k45::we.wpicker.CurrentItemCount")
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

        this.Bindings.push(
            this.CurrentItemIdx,
            this.CurrentScale,
            this.CurrentRotation,
            this.CurrentPosition,
            this.MouseSensibility,
            this.CurrentPlaneMode,
            this.CurrentItemText,
            this.CurrentItemIsValid,
            this.CurrentEntity,
            this.CurrentItemName,
            this.CurrentItemCount,
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
            this.FormulaeCompileResult
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

}