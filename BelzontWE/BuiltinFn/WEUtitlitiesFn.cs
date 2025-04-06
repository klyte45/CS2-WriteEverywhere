

using Belzont.Utils;
using Colossal.Entities;
using Game.Rendering;
using Game.UI;
using Unity.Entities;
using Color = UnityEngine.Color;

namespace BelzontWE.Builtin
{
    public class WEUtitlitiesFn
    {
        private static NameSystem nameSys;
        public static string GetEntityName(Entity reference) => (nameSys ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<NameSystem>()).GetName(reference).Translate();
        public static Color GetMainMeshColor1(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<MeshColor>(reference, true, out var meshes) && meshes.Length > 0 ? meshes[0].m_ColorSet.m_Channel0 : Color.magenta;
        public static Color GetMainMeshColor2(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<MeshColor>(reference, true, out var meshes) && meshes.Length > 0 ? meshes[0].m_ColorSet.m_Channel1 : Color.magenta;
        public static Color GetMainMeshColor3(Entity reference) => World.DefaultGameObjectInjectionWorld.EntityManager.TryGetBuffer<MeshColor>(reference, true, out var meshes) && meshes.Length > 0 ? meshes[0].m_ColorSet.m_Channel2 : Color.magenta;
    }
}
