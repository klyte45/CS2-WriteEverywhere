import { Entity } from "@klyte45/vuio-commons";
import engine from "cohtml/cohtml";
import { ModFolder } from "utils/ModFolder";

export class LayoutsService {
    static async exportComponentAsXml(entity: Entity, name: string): Promise<string> {
        return engine.call("k45::we.layouts.exportComponentAsXml", entity, name);
    }
    static async loadAsChildFromXml(parent: Entity, name: string): Promise<string> {
        return engine.call("k45::we.layouts.loadAsChildFromXml", parent, name);
    }
    static async saveAsCityTemplate(parent: Entity, name: string): Promise<boolean> {
        return engine.call("k45::we.layouts.saveAsCityTemplate", parent, name);
    }
    static async exportComponentAsPrefabDefault(layout: Entity, force: boolean = false): Promise<string> {
        return engine.call("k45::we.layouts.exportComponentAsPrefabDefault", layout, force);
    }
    static async checkCityTemplateExists(name?: string): Promise<boolean> {
        return engine.call("k45::we.layouts.checkCityTemplateExists", name);
    }
    static async listCityTemplates(): Promise<Record<string, string>> {
        return await engine.call("k45::we.layouts.listCityTemplates");
    }
    static async getCityTemplateDetail(name: string): Promise<CityDetailResponse> {
        return engine.call("k45::we.layouts.getCityTemplateDetail", name ?? "");
    }
    static async renameCityTemplate(oldName: string, newName: string): Promise<void> {
        return engine.call("k45::we.layouts.renameCityTemplate", oldName, newName);
    }
    static async deleteTemplate(name: string): Promise<void> {
        return engine.call("k45::we.layouts.deleteCityTemplate", name ?? "");
    }
    static async duplicateCityTemplate(srcName: string, newName: string): Promise<void> {
        return engine.call("k45::we.layouts.duplicateCityTemplate", srcName, newName);
    }
    static async exportCityLayoutAsXml(srcName: string, saveName: string): Promise<string> {
        return engine.call("k45::we.layouts.exportCityLayoutAsXml", srcName, saveName);
    }
    static async openExportedFilesFolder(): Promise<string> {
        return engine.call("k45::we.layouts.openExportedFilesFolder");
    }
    static async loadAsChildFromCityTemplate(parent: Entity, templateName: string): Promise<boolean> {
        return engine.call("k45::we.layouts.loadAsChildFromCityTemplate", parent, templateName);
    }
    static async importAsCityTemplateFromXml(saveName: string): Promise<string> {
        return engine.call("k45::we.layouts.importAsCityTemplateFromXml", saveName);
    }
    static async listModsLoadableTemplates(): Promise<ModFolder[]> { return await engine.call("k45::we.layouts.listModsLoadableTemplates"); }
    static async listModsReplacementData(): Promise<ModReplacementData[]> { return await engine.call("k45::we.layouts.listModsReplacementData"); }
    static async setModFontReplacement(modId: string, original: string, target: string): Promise<string> { return await engine.call("k45::we.layouts.setModFontReplacement", modId, original, target); }
    static async setModAtlasReplacement(modId: string, original: string, target: string): Promise<string> { return await engine.call("k45::we.layouts.setModAtlasReplacement", modId, original, target); }
    static async saveReplacementSettings(fileName: string): Promise<string> { return await engine.call("k45::we.layouts.saveReplacementSettings", fileName); }
    static async loadReplacementSettings(filePath: string): Promise<boolean> { return await engine.call("k45::we.layouts.loadReplacementSettings", filePath); }
    static async checkReplacementSettingFileExists(fileName: string | undefined): Promise<boolean> { return await engine.call("k45::we.layouts.checkReplacementSettingFileExists", fileName); }
    static async getLocationSavedReplacements(): Promise<string> { return await engine.call("k45::we.layouts.getLocationSavedReplacements"); }
    static async getExtensionSavedReplacements(): Promise<string> { return await engine.call("k45::we.layouts.getExtensionSavedReplacements"); }
    static async openExportedReplacementSettingsFolder(): Promise<void> { return await engine.call("k45::we.layouts.openExportedReplacementSettingsFolder"); }
}

export type CityDetailResponse = {
    name: string
    usages: number;
}

export type ModReplacementData = {
    modId: string
    displayName: string
    atlases: Record<string, string>
    fonts: Record<string, string>
}

