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
        private readonly static CultureInfo convertCulture = new("en");
        public class WEMeshDescriptor
        {

            public readonly Vector3[] Vertices;
            public readonly Vector3[] Normals;
            public readonly Vector2[] UVs;
            public readonly int[] Triangles;

            public WEMeshDescriptor(WEMeshDescriptor mesh)
            {
                Vertices = [.. mesh.Vertices];
                Normals = [.. mesh.Normals];
                UVs = [.. mesh.UVs];
                Triangles = [.. mesh.Triangles];
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

                if (triangles.Count > 4000)
                {
                    throw new Exception("OBJ files can only have up to 4000 triangles!");
                }
                mesh = new WEMeshDescriptor(
                    [.. outputVertices],
                    [.. outputNormals],
                    [.. outputUv],
                    [.. triangles]
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
            var colorGroups = mesh.colors.Select((c, i) => (i, c)).GroupBy(x => x.c).ToDictionary(x => x.Key, x => x.Select(x => x.i).ToArray());



            StringBuilder sb = new();
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 v = mesh.vertices[i];
                Color32 c = mesh.colors[i];
                sb.Append(string.Format("v {0} {1} {2} {3} {4} {5}\n", v.x.ToString(convertCulture), v.y.ToString(convertCulture), v.z.ToString(convertCulture), c.r.ToString(convertCulture), c.g.ToString(convertCulture), c.b.ToString(convertCulture)));
            }
            foreach (Vector3 v in mesh.normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", v.x.ToString(convertCulture), v.y.ToString(convertCulture), v.z.ToString(convertCulture)));
            }
            foreach (Vector2 v in mesh.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x.ToString(convertCulture), v.y.ToString(convertCulture)));
            }
            foreach (Color32 v in mesh.colors)
            {
                sb.Append(string.Format("vc {0} {1} {2}\n", v.r.ToString(convertCulture), v.g.ToString(convertCulture), v.b.ToString(convertCulture)));
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

        public static void ExportToFbx(Mesh mesh, string path)
        {
            StringBuilder sb = new();

            // FBX Header
            sb.AppendLine("; FBX 7.4.0 project file");
            sb.AppendLine("; Copyright (C) 1997-2024 Autodesk Inc.");
            sb.AppendLine("; Created by BelzontWE");
            sb.AppendLine();
            sb.AppendLine("FBXHeaderExtension:  {");
            sb.AppendLine("\tFBXHeaderVersion: 1003");
            sb.AppendLine("\tFBXVersion: 7400");
            sb.AppendLine("\tCreator: \"BelzontWE FBX Exporter\"");
            sb.AppendLine("}");
            sb.AppendLine();

            // Object definitions
            sb.AppendLine("Definitions:  {");
            sb.AppendLine("\tVersion: 100");
            sb.AppendLine("\tCount: 2");
            sb.AppendLine("\tObjectType: \"Model\" {");
            sb.AppendLine("\t\tCount: 1");
            sb.AppendLine("\t}");
            sb.AppendLine("\tObjectType: \"Geometry\" {");
            sb.AppendLine("\t\tCount: 1");
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            sb.AppendLine();

            // Objects
            sb.AppendLine("Objects:  {");

            // Geometry object
            sb.AppendLine("\tGeometry: 1000000, \"Geometry::\", \"Mesh\" {");

            // Vertices
            sb.Append("\t\tVertices: *" + (mesh.vertices.Length * 3) + " {\n\t\t\ta: ");
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 v = mesh.vertices[i] * 100;
                sb.Append(v.x.ToString(convertCulture) + "," + v.y.ToString(convertCulture) + "," + v.z.ToString(convertCulture));
                if (i < mesh.vertices.Length - 1) sb.Append(",");
            }
            sb.AppendLine("\n\t\t}");

            // Polygon vertex indices (triangles)
            var allTriangles = new List<int>();
            for (int material = 0; material < mesh.subMeshCount; material++)
            {
                allTriangles.AddRange(mesh.GetTriangles(material));
            }

            sb.Append("\t\tPolygonVertexIndex: *" + allTriangles.Count + " {\n\t\t\ta: ");
            for (int i = 0; i < allTriangles.Count; i += 3)
            {
                // FBX uses negative indices to mark the end of polygons
                sb.Append(allTriangles[i] + "," + allTriangles[i + 1] + "," + (-allTriangles[i + 2] - 1));
                if (i < allTriangles.Count - 3) sb.Append(",");
            }
            sb.AppendLine("\n\t\t}");

            // Normals
            if (mesh.normals != null && mesh.normals.Length > 0)
            {
                sb.AppendLine("\t\tLayerElementNormal: 0 {");
                sb.AppendLine("\t\t\tVersion: 101");
                sb.AppendLine("\t\t\tName: \"\"");
                sb.AppendLine("\t\t\tMappingInformationType: \"ByVertice\"");
                sb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");
                sb.Append("\t\t\tNormals: *" + (mesh.normals.Length * 3) + " {\n\t\t\t\ta: ");
                for (int i = 0; i < mesh.normals.Length; i++)
                {
                    Vector3 n = mesh.normals[i];
                    sb.Append(n.x.ToString(convertCulture) + "," + n.y.ToString(convertCulture) + "," + n.z.ToString(convertCulture));
                    if (i < mesh.normals.Length - 1) sb.Append(",");
                }
                sb.AppendLine("\n\t\t\t}");
                sb.AppendLine("\t\t}");
            }

            // UV coordinates
            if (mesh.uv != null && mesh.uv.Length > 0)
            {
                sb.AppendLine("\t\tLayerElementUV: 0 {");
                sb.AppendLine("\t\t\tVersion: 101");
                sb.AppendLine("\t\t\tName: \"UVChannel_1\"");
                sb.AppendLine("\t\t\tMappingInformationType: \"ByVertice\"");
                sb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");
                sb.Append("\t\t\tUV: *" + (mesh.uv.Length * 2) + " {\n\t\t\t\ta: ");
                for (int i = 0; i < mesh.uv.Length; i++)
                {
                    Vector2 uv = mesh.uv[i];
                    sb.Append(uv.x.ToString(convertCulture) + "," + uv.y.ToString(convertCulture));
                    if (i < mesh.uv.Length - 1) sb.Append(",");
                }
                sb.AppendLine("\n\t\t\t}");
                sb.AppendLine("\t\t}");
            }

            // Vertex colors
            if (mesh.colors != null && mesh.colors.Length > 0)
            {
                sb.AppendLine("\t\tLayerElementColor: 0 {");
                sb.AppendLine("\t\t\tVersion: 101");
                sb.AppendLine("\t\t\tName: \"\"");
                sb.AppendLine("\t\t\tMappingInformationType: \"ByVertice\"");
                sb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");
                sb.Append("\t\t\tColors: *" + (mesh.colors.Length * 4) + " {\n\t\t\t\ta: ");
                for (int i = 0; i < mesh.colors.Length; i++)
                {
                    Color32 c = mesh.colors[i];
                    sb.Append((c.r / 255f).ToString(convertCulture) + "," + (c.g / 255f).ToString(convertCulture) + "," + (c.b / 255f).ToString(convertCulture) + "," + (c.a / 255f).ToString(convertCulture));
                    if (i < mesh.colors.Length - 1) sb.Append(",");
                }
                sb.AppendLine("\n\t\t\t}");
                sb.AppendLine("\t\t}");
            }

            sb.AppendLine("\t\tLayer: 0 {");
            sb.AppendLine("\t\t\tVersion: 100");
            sb.AppendLine("\t\t\tLayerElement:  {");
            sb.AppendLine("\t\t\t\tType: \"LayerElementNormal\"");
            sb.AppendLine("\t\t\t\tTypedIndex: 0");
            sb.AppendLine("\t\t\t}");
            if (mesh.uv != null && mesh.uv.Length > 0)
            {
                sb.AppendLine("\t\t\tLayerElement:  {");
                sb.AppendLine("\t\t\t\tType: \"LayerElementUV\"");
                sb.AppendLine("\t\t\t\tTypedIndex: 0");
                sb.AppendLine("\t\t\t}");
            }
            if (mesh.colors != null && mesh.colors.Length > 0)
            {
                sb.AppendLine("\t\t\tLayerElement:  {");
                sb.AppendLine("\t\t\t\tType: \"LayerElementColor\"");
                sb.AppendLine("\t\t\t\tTypedIndex: 0");
                sb.AppendLine("\t\t\t}");
            }
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");

            // Model object
            sb.AppendLine("\tModel: 2000000, \"Model::Mesh\", \"Mesh\" {");
            sb.AppendLine("\t\tVersion: 232");
            sb.AppendLine("\t\tProperties70:  {");
            sb.AppendLine("\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
            sb.AppendLine("\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\tShading: T");
            sb.AppendLine("\t\tCulling: \"CullingOff\"");
            sb.AppendLine("\t}");

            sb.AppendLine("}");
            sb.AppendLine();

            // Connections
            sb.AppendLine("Connections:  {");
            sb.AppendLine("\tC: \"OO\",1000000,2000000");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString());
        }
    }
}
