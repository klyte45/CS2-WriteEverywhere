
export class TextureAtlasService {
    static async listAvailableLibraries(): Promise<Record<string, boolean>> { return await engine.call("k45::we.textureAtlas.listAvailableLibraries"); }
    static async listModAtlases(): Promise<ModAtlasRegistry[]> { return await engine.call("k45::we.textureAtlas.listModAtlases"); }
    static async listAtlasImages(atlas: string): Promise<string[]> { return await engine.call("k45::we.textureAtlas.listAtlasImages", atlas); }
    static async exportCityAtlas(atlas: string, folder: string): Promise<string> { return await engine.call("k45::we.textureAtlas.exportCityAtlas", atlas, folder); }
    static async copyToCity(atlas: string, newName: string): Promise<boolean> { return await engine.call("k45::we.textureAtlas.copyToCity", atlas, newName); }
    static async removeFromCity(atlas: string): Promise<boolean> { return await engine.call("k45::we.textureAtlas.removeFromCity", atlas); }
    static async getCityAtlasDetail(atlas: string): Promise<AtlasCityDetailResponse> { return await engine.call("k45::we.textureAtlas.getCityAtlasDetail", atlas); }
    static async openExportFolder(exportName: string): Promise<void> { return await engine.call("k45::we.textureAtlas.openExportFolder", exportName); }
}


export type AtlasCityDetailResponse = {
    name: string
    isFromSavegame: boolean
    usages: number;
    imageCount: number
    textureSize: number
}

export type ModAtlasRegistry = {
    ModId: string,
    ModName: string,
    Atlases: string[]
}