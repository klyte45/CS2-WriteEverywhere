import { Entity } from "@klyte45/vuio-commons";

export class LayoutsService {
    static async exportComponentAsXml(entity: Entity, name: string): Promise<string> {
        return engine.call("k45::we.layouts.exportComponentAsXml", entity, name);
    }
    static async loadAsChildFromXml(parent: Entity, name: string): Promise<string> {
        return engine.call("k45::we.layouts.loadAsChildFromXml", parent, name);
    }
    static async saveAsCityTemplate(parent: Entity, name: string): Promise<string> {
        return engine.call("k45::we.layouts.saveAsCityTemplate", parent, name);
    }
    static async exportComponentAsPrefabDefault(layout: Entity, force: boolean = false): Promise<string> {
        return engine.call("k45::we.layouts.exportComponentAsPrefabDefault", layout, force);
    }
    static async checkCityTemplateExists(name: string): Promise<boolean> {
        return engine.call("k45::we.layouts.checkCityTemplateExists", name);
    }
}
