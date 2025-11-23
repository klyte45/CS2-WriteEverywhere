using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.IO;
using BelzontWE.Sprites;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using Game.UI.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using static BelzontWE.IO.ObjFileHandler;

namespace BelzontWE
{
    public partial class WECustomMeshLibrary : GameSystemBase, IDefaultSerializable
    {
        public static WECustomMeshLibrary Instance { get; private set; }
        public static string MESHES_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "objMeshes");

        private static readonly string CURRENT_CITY_KEY = "\0CURRENT_CITY\0";
        private static readonly string LOCAL_LIB_KEY = "\0LOCAL_LIB\0";

        // Transitive instances, just for caching
        private readonly Dictionary<string, Dictionary<string, CustomMeshRenderInformation>> MeshInstances = new();
        // Meshes are stored in a dictionary with modId as key, and another dictionary with meshId as value
        // Special keys on readonly variables above
        // CURRENT_CITY_KEY - for the current city meshes (must be serialized)
        // LOCAL_LIB_KEY - for the local library meshes
        private readonly Dictionary<string, Dictionary<string, WEMeshDescriptor>> MeshSources = new();


        private readonly Queue<Action> actionQueue = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            KFileUtils.EnsureFolderCreation(MESHES_FOLDER);
            actionQueue.Enqueue(() => LoadMeshesFromLocalFolders());
        }
        protected override void OnUpdate()
        {
            while (actionQueue.TryDequeue(out var action))
            {
                action();
            }
        }
        #region Mods integration
        internal bool LoadMeshToMod(Assembly mainAssembly, string meshName, string objPath)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            if (meshName.TrimToNull() == null)
            {
                LogUtils.DoWarnLog($"Mesh name is null or empty for mod identified by '{modId}' ({mainAssembly.GetName().Name})");
                return false;
            }
            if (!File.Exists(objPath))
            {
                LogUtils.DoWarnLog($"Mesh file '{objPath}' does not exist for mod identified by '{modId}' mesh '{meshName}' ({mainAssembly.GetName().Name})");
                return false;
            }
            var mesh = ObjFileHandler.ImportFromObj(objPath);
            if (mesh == null)
            {
                LogUtils.DoWarnLog($"Failed to load mesh from {objPath} for mod identified by '{modId}' mesh '{meshName}' ({mainAssembly.GetName().Name})");
                return false;
            }
            if (!MeshSources.TryGetValue(modId, out var meshSources))
            {
                meshSources = MeshSources[modId] = new();
            }
            meshSources[meshName] = mesh;
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Registered mesh '{meshName}' for mod '{modId}' via file");
            CleanupMeshCache(modId);
            return true;
        }
        internal bool LoadMeshToMod(Assembly mainAssembly, string meshName, Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            if (meshName.TrimToNull() == null)
            {
                LogUtils.DoWarnLog($"Mesh name is null or empty for mod identified by '{modId}' ({mainAssembly.GetName().Name})");
                return false;
            }
            if (vertices.Length == 0)
            {
                LogUtils.DoWarnLog($"Mesh vertices are empty for mod identified by '{modId}' mesh '{meshName}' ({mainAssembly.GetName().Name})");
                return false;
            }
            if (vertices.Length != normals.Length)
            {
                LogUtils.DoWarnLog($"Mesh vertices and normals count mismatch for mod identified by '{modId}' mesh '{meshName}' ({mainAssembly.GetName().Name})");
                return false;
            }
            if (uv.Length != vertices.Length)
            {
                LogUtils.DoWarnLog($"Mesh vertices and UVs count mismatch for mod identified by '{modId}' mesh '{meshName}' ({mainAssembly.GetName().Name})");
                return false;
            }
            if (triangles.Any(x => x >= vertices.Length))
            {
                LogUtils.DoWarnLog($"Mesh triangles contain invalid indices for mod identified by '{modId}' mesh '{meshName}' ({mainAssembly.GetName().Name})");
                return false;
            }

            if (!MeshSources.TryGetValue(modId, out var meshSources))
            {
                meshSources = MeshSources[modId] = new();
            }
            meshSources[meshName] = new WEMeshDescriptor(vertices, normals, uv, triangles);
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Registered mesh '{meshName}' for mod '{modId}' via arrays");
            CleanupMeshCache(modId);
            return true;
        }


        #endregion

        public CustomMeshRenderInformation GetMesh(string meshName, string atlasName, string imageName)
        {
            WEMeshDescriptor targetMesh;
            if (MeshInstances.TryGetValue(meshName, out var meshDict))
            {
                if (meshDict.TryGetValue(atlasName + "|" + imageName, out var meshInfo))
                {
                    if (meshInfo.IsValid())
                    {
                        return meshInfo;
                    }
                    else
                    {
                        actionQueue.Enqueue(() => meshInfo.Dispose());
                    }
                }
                if (!GetEffectiveIds(meshName, out string modId, out string meshId))
                {
                    return null; // Invalid mesh name format or not found in any library
                }
                if (!MeshSources.TryGetValue(modId, out var targetLib) || !targetLib.TryGetValue(meshId, out targetMesh))
                {
                    return null;
                }
            }
            else
            {
                if (!GetEffectiveIds(meshName, out string modId, out string meshId))
                {
                    return null; // Invalid mesh name format or not found in any library
                }
                if (!MeshSources.TryGetValue(modId, out var targetLib) || !targetLib.TryGetValue(meshId, out targetMesh))
                {
                    return null;
                }
                MeshInstances[meshName] = meshDict = new Dictionary<string, CustomMeshRenderInformation>();
            }
            if (WEAtlasesLibrary.Instance.TryGetAtlas(atlasName, out var atlas) && atlas.TryGetValue(imageName, out var imgInfo))
            {
                var spriteData = atlas.Sprites[imageName];
                var imgSize = new float2(atlas.Width, atlas.Height);
                var newMeshInfo = new CustomMeshRenderInformation(atlas, targetMesh, (float2)spriteData.Region.min / imgSize, (float2)spriteData.Region.max / imgSize);
                meshDict[atlasName + "|" + imageName] = newMeshInfo;
                return newMeshInfo;
            }
            return null;
        }

        private bool GetEffectiveIds(string meshName, out string modId, out string meshId)
        {
            if (meshName.Contains(":"))
            {
                var split = meshName.Split(":", 2);
                modId = split[0];
                meshId = split[1];
            }
            else
            {
                if (MeshSources.TryGetValue(CURRENT_CITY_KEY, out var currCityLib) && currCityLib.ContainsKey(meshName))
                {
                    modId = CURRENT_CITY_KEY;
                    meshId = meshName;
                }
                else if (MeshSources.TryGetValue(LOCAL_LIB_KEY, out var localLib) && localLib.ContainsKey(meshName))
                {
                    modId = LOCAL_LIB_KEY;
                    meshId = meshName;
                }
                else
                {
                    modId = null;
                    meshId = null;
                    return false;
                }
            }
            return true;
        }
        #region Loading
        private Coroutine localMeshesCoroutine;
        public void LoadMeshesFromLocalFolders() => localMeshesCoroutine ??= GameManager.instance.StartCoroutine(LoadMeshesFromLocalFoldersCoroutine());

        private const string LOADING_LOCAL_MESHES = "loadingLocalMeshes";
        public IEnumerator LoadMeshesFromLocalFoldersCoroutine()
        {
            if (localMeshesCoroutine != null) yield return 0;
            NotificationHelper.NotifyProgress(LOADING_LOCAL_MESHES, 0);
            yield return 0;
            CleanupMeshSource(LOCAL_LIB_KEY);

            var errors = new List<string>();
            var files = Directory.GetFiles(MESHES_FOLDER, "*.obj");
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                var argsNotif = new Dictionary<string, ILocElement>()
                {
                    ["progress"] = LocalizedString.Value($"{i + 1}/{files.Length}"),
                    ["meshName"] = LocalizedString.Value(file[(MESHES_FOLDER.Length + 1)..])
                };
                NotificationHelper.NotifyProgress(LOADING_LOCAL_MESHES, Mathf.RoundToInt((75f * i / files.Length) + 25), textI18n: $"{LOADING_LOCAL_MESHES}.loadingFolders", argsText: argsNotif);
                yield return 0;
                try
                {
                    var mesh = ObjFileHandler.ImportFromObj(file);
                    if (mesh == null)
                    {
                        errors.Add($"Failed to load mesh from {file}");
                        continue;
                    }
                    var meshId = Path.GetFileNameWithoutExtension(file);
                    MeshSources[LOCAL_LIB_KEY][meshId] = mesh;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error loading mesh from {file}: {ex.Message}");
                }
            }
            if (errors.Count > 0)
            {
                for (var i = 0; i < errors.Count; i++)
                {
                    LogUtils.DoWarnLog($"Error loading WE meshes: {errors[i]}");
                }
            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded meshes: {string.Join(", ", MeshSources.Select(x => x.Key))}");

            NotificationHelper.NotifyProgress(LOADING_LOCAL_MESHES, 100, textI18n: $"{LOADING_LOCAL_MESHES}.complete");
            CleanupMeshCache(LOCAL_LIB_KEY);
            localMeshesCoroutine = null;
        }

        private void CleanupMeshSource(string key)
        {
            MeshSources[key] = new();
        }

        private void CleanupMeshCache(string key)
        {
            var keysToErase
                = (key == null ? MeshInstances.Keys
                : key.Contains("\0") ? MeshInstances.Keys.Where(x => !x.Contains(":"))
                : MeshInstances.Keys.Where(x => x.StartsWith(key + ":"))).ToArray();

            foreach (var keyInstance in keysToErase)
            {
                foreach (var entry in MeshInstances[keyInstance])
                {
                    entry.Value.Dispose();
                }
                MeshInstances.Remove(keyInstance);
            }
        }

        #endregion
        #region Serialization

        private const uint CURRENT_VERSION = 0;

        public void SetDefaults(Context context)
        {
            CleanupMeshSource(CURRENT_CITY_KEY);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            if (MeshSources[CURRENT_CITY_KEY].Count > 0)
            {
                writer.Write(MeshSources[CURRENT_CITY_KEY].Count);
                foreach (var meshPair in MeshSources[CURRENT_CITY_KEY])
                {
                    writer.Write(meshPair.Key); // Mesh ID
                    writer.Write(meshPair.Value.Vertices.Length);
                    foreach (var vertex in meshPair.Value.Vertices)
                    {
                        writer.Write(vertex);
                    }
                    writer.Write(meshPair.Value.Normals.Length);
                    foreach (var normal in meshPair.Value.Normals)
                    {
                        writer.Write(normal);
                    }
                    writer.Write(meshPair.Value.UVs.Length);
                    foreach (var uv in meshPair.Value.UVs)
                    {
                        writer.Write(uv);
                    }
                    writer.Write(meshPair.Value.Triangles.Length);
                    foreach (var triangle in meshPair.Value.Triangles)
                    {
                        writer.Write(triangle);
                    }
                }
            }
            else
            {
                writer.Write(0); // No meshes in the current city
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            CleanupMeshSource(CURRENT_CITY_KEY);
            reader.CheckVersionK45(CURRENT_VERSION, GetType());
            reader.Read(out int meshesCount);
            for (int i = 0; i < meshesCount; i++)
            {
                reader.Read(out string meshId);
                reader.Read(out int vertexCount);
                var vertices = new Vector3[vertexCount];
                for (int j = 0; j < vertexCount; j++)
                {
                    reader.Read(out float3 vertex);
                    vertices[j] = vertex;
                }
                reader.Read(out int normalCount);
                var normals = new Vector3[normalCount];
                for (int j = 0; j < normalCount; j++)
                {
                    reader.Read(out float3 normal);
                    normals[j] = normal;
                }
                reader.Read(out int uv2Count);
                var uv2 = new Vector2[uv2Count];
                for (int j = 0; j < uv2Count; j++)
                {
                    reader.Read(out float2 uv);
                    uv2[j] = uv;
                }
                reader.Read(out int triangleCount);
                var triangles = new int[triangleCount];
                for (int j = 0; j < triangleCount; j++)
                {
                    reader.Read(out int triangle);
                    triangles[j] = triangle;
                }
                MeshSources[CURRENT_CITY_KEY][meshId] = new WEMeshDescriptor(vertices, normals, uv2, triangles);
            }

        }

        internal Dictionary<string, string> ListAvailableMeshesUI()
        {
            return MeshSources.Where(x => x.Key != CURRENT_CITY_KEY && x.Key != LOCAL_LIB_KEY && x.Value.Count > 0)
                .SelectMany(parent => parent.Value.Where(x => !x.Key.StartsWith("__")).Select(child => ($"{parent.Key}:{child.Key}", $"{parent.Key}:{child.Key}")))
                .Concat(MeshSources[CURRENT_CITY_KEY].Select(x => (x.Key, x.Key)))
                .Concat(MeshSources[LOCAL_LIB_KEY].Where(x => !MeshSources[CURRENT_CITY_KEY].ContainsKey(x.Key)).Select(x => (x.Key, x.Key)))
                .ToDictionary(x => x.Item1, x => x.Item2);
        }

        internal bool CopyToCity(string v, string newName)
        {
            if (!MeshSources[LOCAL_LIB_KEY].TryGetValue(v, out var mesh) || MeshSources[CURRENT_CITY_KEY].ContainsKey(newName))
            {
                return false;
            }
            MeshSources[CURRENT_CITY_KEY][newName] = new WEMeshDescriptor(mesh);

            CleanCityInstanceForSource(newName);
            return true;
        }

        public void ClearAllCache()
        {
            foreach (var entry in MeshInstances)
            {
                foreach (var meshEntry in entry.Value)
                {
                    meshEntry.Value.Dispose();
                }
            }
            MeshInstances.Clear();
        }

        private void CleanCityInstanceForSource(string meshName)
        {
            List<string> entriesToRemove = new();
            foreach (var entry in MeshInstances[CURRENT_CITY_KEY])
            {
                if (entry.Key.StartsWith(meshName + "|"))
                {
                    entry.Value.Dispose();
                    entriesToRemove.Add(entry.Key);
                }
            }
            foreach (var key in entriesToRemove)
            {
                MeshInstances[CURRENT_CITY_KEY].Remove(key);
            }
        }

        internal bool RemoveFromCity(string meshNameToRemove)
        {
            if (!MeshSources[CURRENT_CITY_KEY].ContainsKey(meshNameToRemove)) return false;
            MeshSources[CURRENT_CITY_KEY].Remove(meshNameToRemove);
            CleanCityInstanceForSource(meshNameToRemove);
            return true;
        }
        #endregion
    }
}