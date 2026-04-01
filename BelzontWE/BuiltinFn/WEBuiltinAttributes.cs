using System;

namespace BelzontWE
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class WEBuiltinFunctionAttribute : Attribute
    {
        public string Category { get; }
        public WEBuiltinFunctionAttribute(string category)
        {
            Category = category;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class WEFormulaAttribute : Attribute
    {
        public Type ReturnType { get; }
        public string Description { get; }
        public WEFormulaAttribute(Type returnType, string description = null)
        {
            ReturnType = returnType;
            Description = description;
        }
    }
}
