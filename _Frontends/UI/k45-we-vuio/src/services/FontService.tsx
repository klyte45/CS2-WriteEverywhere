import { ModFolder } from "utils/ModFolder";

export class FontService {
    static async requireFontInstallation(fontName: string): Promise<string> { return await engine.call("k45::we.fonts.requireFontInstallation", fontName); }
    static async listCityFonts(): Promise<Record<string, boolean>> { return await engine.call("k45::we.fonts.listCityFonts"); }
    static async checkFontExists(fontName?: string): Promise<boolean> { return await engine.call("k45::we.fonts.checkFontExists", fontName); }
    static async getFontDetail(fontName: string): Promise<FontDetailResponse> { return await engine.call("k45::we.fonts.getFontDetail", fontName); }
    static async renameCityFont(oldName: string, newName: string): Promise<void> { return await engine.call("k45::we.fonts.renameCityFont", oldName, newName); }
    static async deleteCityFont(fontName: string): Promise<void> { return await engine.call("k45::we.fonts.deleteCityFont", fontName); }
    static async duplicateCityFont(srcName: string, newName: string): Promise<void> { return await engine.call("k45::we.fonts.duplicateCityFont", srcName, newName); }
    static async listModsFonts(): Promise<ModFolder[]> { return await engine.call("k45::we.fonts.listModsFonts"); }
}
export type FontDetailResponse = {
    name: string;
    guid: string;
}

