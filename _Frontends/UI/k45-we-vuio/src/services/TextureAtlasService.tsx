
export class TextureAtlasService {
    static async listAvailableLibraries() { return await engine.call("k45::we.textureAtlas.listAvailableLibraries"); }
    static async listAtlasImages(atlas: string) { return await engine.call("k45::we.textureAtlas.listAtlasImages", atlas); }
    static async exportCityAtlas(atlas: string) { return await engine.call("k45::we.textureAtlas.exportCityAtlas", atlas); }
    static async copyToCity(atlas: string, newName: string) { return await engine.call("k45::we.textureAtlas.copyToCity", atlas, newName); }
    static async removeFromCity(atlas: string) { return await engine.call("k45::we.textureAtlas.removeFromCity", atlas); }
}
