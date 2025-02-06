import engine from "cohtml/cohtml";

export type DataProvider = { displayName: string, directory: boolean, fullPath: string }[]

export class FileService {
    static async generateDataProvider(folder: string, allowedExtension: string): Promise<DataProvider> {
        return await FileService.listFiles(folder, allowedExtension)
    }
    static async listFiles(folder: string, extensionAllowed: string): Promise<{ displayName: string, directory: boolean, fullPath: string }[]> { return await engine.call("k45::we.file.listFiles", folder, extensionAllowed); }
    static async getLayoutFolder(): Promise<string> { return await engine.call("k45::we.file.getLayoutFolder"); }
    static async getPrefabLayoutExtension(): Promise<string> { return await engine.call("k45::we.file.getPrefabLayoutExtension"); }
    static async getStoredLayoutExtension(): Promise<string> { return await engine.call("k45::we.file.getStoredLayoutExtension"); }
    static async getFontDefaultLocation(): Promise<string> { return await engine.call("k45::we.file.getFontDefaultLocation"); }
}
