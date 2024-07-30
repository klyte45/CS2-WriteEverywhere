import { Entity } from "@klyte45/vuio-commons";
import { WETypeMemberDesc, WEFormulaeElement } from "./WEFormulaeElement";
import { IndexedStaticMethodsListing, IndexedComponentListing } from "./WorldPickerService";

export class FormulaeService {
    static async listAvailableMethodsForType(dllName: string, typeName: string): Promise<IndexedStaticMethodsListing> {
        return await engine.call("k45::we.formulae.listAvailableMethodsForType", dllName, typeName);
    }
    static async listAvailableMembersForType(dllName: string, typeName: string): Promise<WETypeMemberDesc[]> {
        return await engine.call("k45::we.formulae.listAvailableMembersForType", dllName, typeName);
    }
    static async listAvailableComponents(): Promise<IndexedComponentListing> {
        return await engine.call("k45::we.formulae.listAvailableComponents");
    }
    static async formulaeToPathObjects(formulae: string): Promise<WEFormulaeElement[]> {
        return engine.call("k45::we.formulae.formulaeToPathObjects", formulae);
    }
    static async exportComponentAsJson(entity: Entity, name: string): Promise<string> {
        return engine.call("k45::we.formulae.exportComponentAsJson", entity, name);
    }
}
