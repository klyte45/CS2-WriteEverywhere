using System;

namespace BelzontWE
{
    /// <summary>
    /// Pure CLR extension methods for <see cref="Type"/> that carry no Unity dependencies.
    /// </summary>
    public static class WETypeExtensions
    {
        public static bool IsIntegerType(this Type t) => Type.GetTypeCode(t) switch
        {
            TypeCode.Byte or TypeCode.SByte
            or TypeCode.UInt16 or TypeCode.UInt32
            or TypeCode.UInt64 or TypeCode.Int16
            or TypeCode.Int32 or TypeCode.Int64
            => true,
            _ => false,
        };

        public static bool IsDecimalType(this Type t) => Type.GetTypeCode(t) switch
        {
            TypeCode.Decimal or TypeCode.Double
            or TypeCode.Single => true,
            _ => false,
        };
    }
}
