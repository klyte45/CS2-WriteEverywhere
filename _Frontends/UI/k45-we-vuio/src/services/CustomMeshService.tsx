import engine from "cohtml/cohtml";

export class CustomMeshService {
    static async listAvailableLibraries(): Promise<Record<string, string>> { return await engine.call("k45::we.customMesh.listAvailableLibraries"); }
    static async copyToCity(mesh: string, newName: string): Promise<boolean> { return await engine.call("k45::we.customMesh.copyToCity", mesh, newName); }
    static async removeFromCity(mesh: string): Promise<boolean> { return await engine.call("k45::we.customMesh.removeFromCity", mesh); }
}