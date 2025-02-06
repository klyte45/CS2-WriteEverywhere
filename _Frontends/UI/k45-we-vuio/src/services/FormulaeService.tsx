import engine from "cohtml/cohtml";
import { WETypeMemberDesc, WEFormulaeElement, WEComponentTypeDesc } from "./WEFormulaeElement";
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
    static async listComponentsOnCurrentEntity(formulae: string): Promise<WEComponentTypeDesc[]> {
        return await engine.call("k45::we.formulae.listComponentsOnCurrentEntity", formulae);
    }
    static async formulaeToPathObjects(formulae: string): Promise<WEFormulaeElement[]> {
        return engine.call("k45::we.formulae.formulaeToPathObjects", formulae);
    }
}

