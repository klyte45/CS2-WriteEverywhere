using Belzont.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelzontWE.IO
{
    internal static class ObjImporter
    {
        public static Mesh ImportFromObj(string pathToFile)
        {
            Mesh mesh = null;
            try
            {
                var objData = System.IO.File.ReadAllText(pathToFile);
                mesh = new Mesh();
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var triangles = new List<int>();

                var outputVertices = new List<Vector3>();
                var outputNormals = new List<Vector3>();
                var outputUv = new List<Vector2>();                

                var globalToLocalVertexIndex = new Dictionary<string, int>();

                Mesh currentSubmesh = null;
                var shadingState = false;
                foreach (var line in objData.Split('\n'))
                {
                    var parts = line.Trim().Split(' ');
                    if (parts.Length == 0) continue;
                    switch (parts[0])
                    {
                        case "v":
                            vertices.Add(new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                            break;
                        case "vn":
                            normals.Add(new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                            break;
                        case "vt":
                            uvs.Add(new Vector2(float.Parse(parts[1]), float.Parse(parts[2])));
                            break;
                        case "s":
                            currentSubmesh = new Mesh();

                            if (parts.Length > 1 && (parts[1] == "off" || parts[1] == "0"))
                            {
                                shadingState = false;
                            }
                            else
                            {
                                shadingState = true; // Default to smooth shading if no argument is provided
                            }


                            globalToLocalVertexIndex.Clear();
                            break;
                        case "f":
                            for (int i = 1; i < parts.Length; i++)
                            {
                                if (shadingState && globalToLocalVertexIndex.TryGetValue(parts[i], out int localIndex))
                                {
                                    // Use existing vertex index
                                    triangles.Add(localIndex);
                                    continue;
                                }
                                var vertexData = parts[i].Split('/');
                                int vertexIndex = int.Parse(vertexData[0]) - 1; // OBJ indices are 1-based
                                outputVertices.Add(vertices[vertexIndex]);
                                if (vertexData.Length > 2 && !string.IsNullOrEmpty(vertexData[2]))
                                {
                                    // Handle normals if present
                                    int normalIndex = int.Parse(vertexData[2]) - 1;
                                    outputNormals.Add(normals[normalIndex]);
                                }
                                if (vertexData.Length > 1 && !string.IsNullOrEmpty(vertexData[1]))
                                {
                                    // Handle UVs if present
                                    int uvIndex = int.Parse(vertexData[1]) - 1;
                                    outputUv.Add(uvs[uvIndex]);
                                }
                                var localIdx = outputVertices.Count - 1;
                                triangles.Add(localIdx);
                                if (shadingState)
                                {
                                    // Store the local index for this vertex
                                    globalToLocalVertexIndex[parts[i]] = localIdx;
                                }
                            }
                            break;
                    }
                }
                mesh.vertices = outputVertices.ToArray();
                mesh.uv2 = outputUv.ToArray();
                mesh.normals = outputNormals.ToArray();
                mesh.triangles = triangles.ToArray();
            }
            catch (Exception ex)
            {
                LogUtils.DoWarnLog($"Failed to import OBJ file: {ex.Message}");
            }
            return mesh;
        }
    }
}
