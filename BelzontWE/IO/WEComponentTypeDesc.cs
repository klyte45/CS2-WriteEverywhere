using System;

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

        internal static WEComponentTypeDesc From(Type x)
        {
            var source = WEMemberSourceExtensions.GetSource(x.Assembly, out var modUrl, out var modName, out var dllName);
            return new WEComponentTypeDesc
            {
                dllName = dllName,
                className = x.FullName,
                modName = modName,
                modUrl = modUrl,
                source = source
            };
        }
    }

}