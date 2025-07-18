using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BelzontWE.IO
{
    public static class ObjFileHandler
    {
        private static CultureInfo convertCulture = new System.Globalization.CultureInfo("en");
        public class WEMeshDescriptor
        {

            public readonly Vector3[] Vertices;
            public readonly Vector3[] Normals;
            public readonly Vector2[] UVs;
            public readonly int[] Triangles;

            public WEMeshDescriptor(WEMeshDescriptor mesh)
            {
                Vertices = mesh.Vertices.ToArray();
                Normals = mesh.Normals.ToArray();
                UVs = mesh.UVs.ToArray();
                Triangles = mesh.Triangles.ToArray();
            }

            public WEMeshDescriptor(Vector3[] vertices, Vector3[] normals, Vector2[] uVs, int[] triangles)
            {
                Vertices = vertices;
                Normals = normals;
                UVs = uVs;
                Triangles = triangles;
            }
        }
        public static WEMeshDescriptor ImportFromObj(string pathToFile)
        {
            WEMeshDescriptor mesh = null;
            try
            {
                var objData = System.IO.File.ReadAllText(pathToFile);

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
                            vertices.Add(new Vector3(float.Parse(parts[1], convertCulture), float.Parse(parts[2], convertCulture), float.Parse(parts[3], convertCulture)));
                            break;
                        case "vn":
                            normals.Add(new Vector3(float.Parse(parts[1], convertCulture), float.Parse(parts[2], convertCulture), float.Parse(parts[3], convertCulture)));
                            break;
                        case "vt":
                            uvs.Add(new Vector2(float.Parse(parts[1], convertCulture), float.Parse(parts[2], convertCulture)));
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
                mesh = new WEMeshDescriptor(
                    outputVertices.ToArray(),
                    outputNormals.ToArray(),
                    outputUv.ToArray(),
                    triangles.ToArray()
                );
            }
            catch (Exception ex)
            {
                LogUtils.DoWarnLog($"Failed to import OBJ file: {ex.Message}\n{ex}");
            }
            return mesh;
        }

        public static void ExportToObj(Mesh mesh, string path)
        {
            StringBuilder sb = new();
            foreach (Vector3 v in mesh.vertices)
            {
                sb.Append(string.Format("v {0} {1} {2}\n", v.x.ToString(convertCulture), v.y.ToString(convertCulture), v.z.ToString(convertCulture)));
            }
            foreach (Vector3 v in mesh.normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", v.x.ToString(convertCulture), v.y.ToString(convertCulture), v.z.ToString(convertCulture)));
            }
            foreach (Vector2 v in mesh.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x.ToString(convertCulture), v.y.ToString(convertCulture)));
            }
            for (int material = 0; material < mesh.subMeshCount; material++)
            {
                sb.Append(string.Format("\ns mat{0}\n", material));
                int[] triangles = mesh.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1,
                    triangles[i + 1] + 1,
                    triangles[i + 2] + 1));
                }
            }
            File.WriteAllText(path, sb.ToString());
        }
    }
}
