import engine from "cohtml/cohtml";

export class WEModuleService {
    static async listAllOptions(): Promise<Record<string, Record<string, WEModuleOptionFieldTypes & number>>> { return engine.call("k45::we.modules.listAllOptions"); }
    static async getFieldValue<T>(moduleId: string, field: string): Promise<T> { return engine.call("k45::we.modules.getFieldValue", moduleId, field); }
    static async setFieldValue<T>(moduleId: string, field: string, value: T): Promise<void> { return engine.call("k45::we.modules.setFieldValue", moduleId, field, "" + value); }
    static async getFieldOptions(moduleId: string, field: string): Promise<Record<string, string>> { return engine.call("k45::we.modules.getFieldOptions", moduleId, field); }
}

export enum WEModuleOptionFieldTypes {
    BOOLEAN = 0,
    DROPDOWN = 1
}