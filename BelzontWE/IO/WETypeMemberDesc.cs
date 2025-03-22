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

        public static WETypeMemberDesc FromIndexing(int idx, Type resultType)
        {
            return new WETypeMemberDesc
            {
                memberName = idx.ToString(),
                memberTypeDllName = resultType.Assembly.GetName().Name,
                memberTypeClassName = resultType.FullName,
                type = WEMemberType.ArraylikeIndexing
            };
        }

        public static WETypeMemberDesc FromMemberInfo(MemberInfo m) => m switch
        {
            MethodInfo targetMethod => new WETypeMemberDesc
            {
                memberTypeDllName = targetMethod.ReturnType.Assembly.GetName().Name,
                memberTypeClassName = targetMethod.ReturnType.FullName,
                memberName = targetMethod.Name,
                type = WEMemberType.ParameterlessMethod,
            },
            PropertyInfo targetProperty => new WETypeMemberDesc
            {
                memberTypeDllName = targetProperty.PropertyType.Assembly.GetName().Name,
                memberTypeClassName = targetProperty.PropertyType.FullName,
                memberName = targetProperty.Name,
                type = WEMemberType.Property
            },
            FieldInfo targetField => new WETypeMemberDesc
            {
                memberTypeDllName = targetField.FieldType.Assembly.GetName().Name,
                memberTypeClassName = targetField.FieldType.FullName,
                memberName = targetField.Name,
                type = WEMemberType.Field
            },
            _ => default,
        };
    }

}