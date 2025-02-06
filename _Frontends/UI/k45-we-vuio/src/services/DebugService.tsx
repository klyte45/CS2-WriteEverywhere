import { Entity } from "@klyte45/vuio-commons";
import engine from "cohtml/cohtml";

export enum ShaderPropertyType {
    Color = "Color",
    Vector = "Vector",
    Float = "Float",
    Range = "Range",
    Texture = "Texture",
    Int = "Int"
}

export type WEDebugPropertyDescriptor = {
    Name: string;
    Idx: number;
    Id: number;
    Description: string;
    Type: ShaderPropertyType;
    Value: string;

}

export class DebugService {
    static async listShaderDatails(): Promise<Record<string, Record<string, any>>> { return await engine.call("k45::we.debug.listShaderDatails"); }
    static async listShader(): Promise<string[]> { return await engine.call("k45::we.debug.listShader"); }
    static async setShader(entity: Entity, shaderName: string): Promise<void> { return await engine.call("k45::we.debug.setShader", entity, shaderName); }
    static async getShader(entity: Entity): Promise<string> { return await engine.call("k45::we.debug.getShader", entity); }
    static async listCurrentMaterialSettings(entity: Entity): Promise<WEDebugPropertyDescriptor[]> { return await engine.call("k45::we.debug.listCurrentMaterialSettings", entity); }
    static async setCurrentMaterialSettings(entity: Entity, propertyIdxStr: string, value: string): Promise<string> { return await engine.call("k45::we.debug.setCurrentMaterialSettings", entity, propertyIdxStr, value); }
}
