using System;
using System.Reflection;

namespace BelzontWE
{
    public struct WETypeMemberDesc
    {
        public readonly string WEDescType => "MEMBER";
        public string memberName;
        public string memberTypeDllName;
        public string memberTypeClassName;
        public WEMemberType type;
        public bool supportsMathOp;

        public static WETypeMemberDesc FromIndexing(int idx, Type resultType) => new()
        {
            memberName = idx.ToString(),
            memberTypeDllName = resultType.Assembly.GetName().Name,
            memberTypeClassName = resultType.FullName,
            type = WEMemberType.ArraylikeIndexing,
            supportsMathOp = resultType.IsDecimalType() || resultType.IsIntegerType()
        };

        public static WETypeMemberDesc FromMemberInfo(MemberInfo m) => m switch
        {
            MethodInfo targetMethod => new WETypeMemberDesc
            {
                memberTypeDllName = targetMethod.ReturnType.Assembly.GetName().Name,
                memberTypeClassName = targetMethod.ReturnType.FullName,
                memberName = targetMethod.Name,
                type = WEMemberType.ParameterlessMethod,
                supportsMathOp = targetMethod.ReturnType.IsDecimalType() || targetMethod.ReturnType.IsIntegerType()
            },
            PropertyInfo targetProperty => new WETypeMemberDesc
            {
                memberTypeDllName = targetProperty.PropertyType.Assembly.GetName().Name,
                memberTypeClassName = targetProperty.PropertyType.FullName,
                memberName = targetProperty.Name,
                type = WEMemberType.Property,
                supportsMathOp = targetProperty.PropertyType.IsDecimalType() || targetProperty.PropertyType.IsIntegerType()
            },
            FieldInfo targetField => new WETypeMemberDesc
            {
                memberTypeDllName = targetField.FieldType.Assembly.GetName().Name,
                memberTypeClassName = targetField.FieldType.FullName,
                memberName = targetField.Name,
                type = WEMemberType.Field,
                supportsMathOp = targetField.FieldType.IsDecimalType() || targetField.FieldType.IsIntegerType()
            },
            _ => default,
        };
    }

}