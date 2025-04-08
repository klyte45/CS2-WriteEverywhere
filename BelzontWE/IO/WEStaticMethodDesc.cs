using System.Reflection;

namespace BelzontWE
{
    public struct WEStaticMethodDesc
    {
        public readonly string WEDescType => "STATIC_METHOD";
        public string dllName;
        public string className;
        public string methodName;
        public WEMemberSource source;
        public string modUrl;
        public string modName;
        public string returnTypeDll;
        public string returnType;
        public bool supportsMathOp;
        public readonly string FormulaeString => $"&{className};{methodName}";

        public static WEStaticMethodDesc From(MethodInfo mi)
        {
            var source = WEMemberSourceExtensions.GetSource(mi.DeclaringType.Assembly, out var modUrl, out var modName, out var dllName);
            var className = mi.DeclaringType.FullName;
            var methodName = mi.Name;
            var returnType = mi.ReturnType.FullName;
            return new WEStaticMethodDesc
            {
                dllName = dllName,
                className = className,
                methodName = methodName,
                returnTypeDll = mi.ReturnType.Assembly?.GetName()?.Name,
                returnType = returnType,
                source = source,
                modUrl = modUrl,
                modName = modName,
                supportsMathOp = mi.ReturnType.IsIntegerType() || mi.ReturnType.IsDecimalType()
            };
        }
    }

}