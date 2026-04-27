using Unity.Mathematics;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Vectors")]
    public class WEVectorsFn
    {
        [WEFormula(typeof(float3))] public static float3 ToX00(float f) => new(f, 0, 0);
        [WEFormula(typeof(float3))] public static float3 ToX11(float f) => new(f, 1, 1);
        [WEFormula(typeof(int3))] public static int3 ToInt3(float3 f) => (int3)f;
        [WEFormula(typeof(float3))] public static float3 ToFloat3(int3 f) => f;
    }

}
