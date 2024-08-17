import { Entity } from "@klyte45/vuio-commons";

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
}

export type CityDetailResponse = {
    name: string
    usages: number;
}
