using System;
using System.Linq;
using Unity.Entities;

namespace BelzontWE
{
    public struct WEComponentTypeDesc
    {
        public readonly string WEDescType => "COMPONENT";
        public string dllName;
        public string className;
        public WEMemberSource source;
        public string modUrl;
        public string modName;
        public string returnDllName;
        public string returnClassName;
        public bool isBuffer;

        internal static WEComponentTypeDesc From(Type x)
        {
            var source = WEMemberSourceExtensions.GetSource(x.Assembly, out var modUrl, out var modName, out var dllName);
            var isBuffer = x.GetInterfaces().Any(x => x == typeof(IBufferElementData));
            var returnType = isBuffer ? typeof(DynamicBuffer<>).MakeGenericType(x) : x;
            return new WEComponentTypeDesc
            {
                dllName = dllName,
                className = x.FullName,
                modName = modName,
                modUrl = modUrl,
                source = source,
                returnDllName = returnType.Assembly.GetName().Name,
                returnClassName = returnType.FullName,
                isBuffer = isBuffer
            };
        }
    }

}