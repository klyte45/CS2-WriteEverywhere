using Colossal.Reflection;
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
        public string weCategory;
        public string weTooltip;
        public string returnTypeDll;
        public string returnType;
        public bool supportsMathOp;
        public readonly string FormulaeString => $"&{className};{methodName}";

        public static WEStaticMethodDesc From(MethodInfo mi)
        {
            string modUrl = null;
            string modName = null;
            string dllName;
            string weCategory = null;
            string weTooltip = null;
            WEMemberSource source;
            if (mi.TryGetAttribute<WEFormulaAttribute>(out var attr) && mi.DeclaringType.TryGetAttribute<WEBuiltinFunctionAttribute>(out var attrBtn))
            {
                source = WEMemberSource.BuiltinFormulae;
                weTooltip = attr.Description;
                weCategory = attrBtn.Category;
                dllName = mi.DeclaringType.Assembly.GetName().Name;
            }
            else
            {
                source = WEMemberSourceExtensions.GetSource(mi.DeclaringType.Assembly, out modUrl, out modName, out dllName);
            }

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
                weTooltip = weTooltip,
                weCategory = weCategory,
                supportsMathOp = mi.ReturnType.IsIntegerType() || mi.ReturnType.IsDecimalType()
            };
        }
    }

}