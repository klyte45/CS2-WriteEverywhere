import engine from "cohtml/cohtml";

export class WEModuleService {
    static async listAllOptions(): Promise<Record<string, Record<string, WEModuleOptionFieldTypes & number>>> { return engine.call("k45::we.modules.listAllOptions"); }
    static async getFieldValue<T>(moduleId: string, field: string): Promise<T> { return engine.call("k45::we.modules.getFieldValue", moduleId, field); }
    static async setFieldValue<T>(moduleId: string, field: string, value: T): Promise<void> { return engine.call("k45::we.modules.setFieldValue", moduleId, field, "" + value); }
    static async getFieldOptions(moduleId: string, field: string): Promise<Record<string, string>> { return engine.call("k45::we.modules.getFieldOptions", moduleId, field); }
    static async getMinMax<T extends number[] = [number]>(moduleId: string, field: string): Promise<[T, T]> { return engine.call("k45::we.modules.getMinMax", moduleId, field); }
    static async getFilePickerOptions(moduleId: string, field: string): Promise<{
        fileExtensionFilter: string,
        initialFolder: string,
        promptText: string
    }> { return engine.call("k45::we.modules.getFilePickerOptions", moduleId, field); }
}

export enum WEModuleOptionFieldTypes {
    BOOLEAN = 0,
    DROPDOWN = 1,
    SECTION_TITLE = 2,
    BUTTON_ROW = 3,
    SLIDER = 4,
    FILE_PICKER = 5,
    COLOR_PICKER = 6,
    SPACER = 7,
    TEXT_INPUT = 8,
    MULTILINE_TEXT_INPUT = 9,
    RADIO_BUTTON = 10,
    MULTISELECT = 11,
    VECTOR2 = 12,
    VECTOR3 = 13,
    VECTOR4 = 14,
    INT_INPUT = 15,
    FLOAT_INPUT = 16,
    RANGE_INPUT = 17
}