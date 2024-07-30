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
}
