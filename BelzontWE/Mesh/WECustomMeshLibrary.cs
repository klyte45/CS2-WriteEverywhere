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
using Unity.Mathematics;
using UnityEngine;

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
        private readonly Dictionary<string, Dictionary<string, Mesh>> MeshSources = new();


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
        }

        public CustomMeshRenderInformation GetMesh(string meshName, string atlasName, string imageName)
        {
            Mesh targetMesh;
            if (MeshInstances.TryGetValue(meshName, out var meshDict))
            {
                if (meshDict.TryGetValue(atlasName + "|" + imageName, out var meshInfo))
                {
                    return meshInfo;
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
                var newMeshInfo = new CustomMeshRenderInformation(targetMesh, (float2)spriteData.Region.min / imgSize, (float2)spriteData.Region.max / imgSize, atlas.Main,
                    imgInfo.Normal, imgInfo.Control, imgInfo.Emissive, imgInfo.Mask);
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
                    var mesh = ObjImporter.ImportFromObj(file);
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
            localMeshesCoroutine = null;
        }

        private void CleanupMeshSource(string key)
        {
            if (MeshSources.TryGetValue(key, out var localLib))
            {
                foreach (var meshPair in localLib)
                {
                    GameObject.Destroy(meshPair.Value);
                }
            }
            if (MeshInstances.TryGetValue(key, out var currentCityData))
            {
                foreach (var meshPair in currentCityData)
                {
                    meshPair.Value.Dispose();
                }
            }
            MeshSources[key] = new();
            MeshInstances[key] = new();
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
                    writer.Write(meshPair.Value.vertices.Length);
                    foreach (var vertex in meshPair.Value.vertices)
                    {
                        writer.Write(vertex);
                    }
                    writer.Write(meshPair.Value.normals.Length);
                    foreach (var normal in meshPair.Value.normals)
                    {
                        writer.Write(normal);
                    }
                    writer.Write(meshPair.Value.uv2.Length);
                    foreach (var uv in meshPair.Value.uv2)
                    {
                        writer.Write(uv);
                    }
                    writer.Write(meshPair.Value.triangles.Length);
                    foreach (var triangle in meshPair.Value.triangles)
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
                var mesh = new Mesh();
                reader.Read(out int vertexCount);
                mesh.vertices = new Vector3[vertexCount];
                for (int j = 0; j < vertexCount; j++)
                {
                    reader.Read(out float3 vertex);
                    mesh.vertices[j] = vertex;
                }
                reader.Read(out int normalCount);
                mesh.normals = new Vector3[normalCount];
                for (int j = 0; j < normalCount; j++)
                {
                    reader.Read(out float3 normal);
                    mesh.normals[j] = normal;
                }
                reader.Read(out int uv2Count);
                mesh.uv2 = new Vector2[uv2Count];
                for (int j = 0; j < uv2Count; j++)
                {
                    reader.Read(out float2 uv);
                    mesh.uv2[j] = uv;
                }
                reader.Read(out int triangleCount);
                mesh.triangles = new int[triangleCount];
                for (int j = 0; j < triangleCount; j++)
                {
                    reader.Read(out int triangle);
                    mesh.triangles[j] = triangle;
                }
                MeshSources[CURRENT_CITY_KEY][meshId] = mesh;
            }

        }
        #endregion
    }
}