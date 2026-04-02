using NUnit.Framework;
using System;
using System.Reflection;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WEStaticMethodDescTests
    {
        // ── WEDescType constant ───────────────────────────────────────────────

        [Test]
        public void WEDescType_ReturnsStaticMethod()
        {
            var desc = new WEStaticMethodDesc();
            Assert.That(desc.WEDescType, Is.EqualTo("STATIC_METHOD"));
        }

        // ── FormulaeString computed property ─────────────────────────────────

        [Test]
        public void FormulaeString_UsesAmpersandClassSemicolonMethod()
        {
            var desc = new WEStaticMethodDesc
            {
                className = "MyNamespace.MyClass",
                methodName = "DoSomething"
            };
            Assert.That(desc.FormulaeString, Is.EqualTo("&MyNamespace.MyClass;DoSomething"));
        }

        [Test]
        public void FormulaeString_EmptyNames_ProducesAmpersandSemicolon()
        {
            var desc = new WEStaticMethodDesc { className = string.Empty, methodName = string.Empty };
            Assert.That(desc.FormulaeString, Is.EqualTo("&;"));
        }

        // ── Field assignment / struct integrity ───────────────────────────────

        [Test]
        public void FieldAssignment_AllFields_RoundTrip()
        {
            var desc = new WEStaticMethodDesc
            {
                dllName = "TestDll",
                className = "TestClass",
                methodName = "TestMethod",
                source = WEMemberSource.Mod,
                modUrl = "https://example.com",
                modName = "TestMod",
                returnTypeDll = "mscorlib",
                returnType = "System.String",
                supportsMathOp = false
            };
            Assert.That(desc.dllName, Is.EqualTo("TestDll"));
            Assert.That(desc.className, Is.EqualTo("TestClass"));
            Assert.That(desc.methodName, Is.EqualTo("TestMethod"));
            Assert.That(desc.source, Is.EqualTo(WEMemberSource.Mod));
            Assert.That(desc.modUrl, Is.EqualTo("https://example.com"));
            Assert.That(desc.modName, Is.EqualTo("TestMod"));
            Assert.That(desc.returnTypeDll, Is.EqualTo("mscorlib"));
            Assert.That(desc.returnType, Is.EqualTo("System.String"));
            Assert.That(desc.supportsMathOp, Is.False);
        }

        [Test]
        public void SupportsMathOp_TrueForIntegerReturn()
        {
            var desc = new WEStaticMethodDesc { supportsMathOp = true };
            Assert.That(desc.supportsMathOp, Is.True);
        }
    }

    [TestFixture]
    public class WETypeMemberDescTests
    {
        // ── WEDescType constant ───────────────────────────────────────────────

        [Test]
        public void WEDescType_ReturnsMember()
        {
            var desc = new WETypeMemberDesc();
            Assert.That(desc.WEDescType, Is.EqualTo("MEMBER"));
        }

        // ── FromIndexing ─────────────────────────────────────────────────────

        [Test]
        public void FromIndexing_SetsIndexAsStringName()
        {
            var desc = WETypeMemberDesc.FromIndexing(3, typeof(string));
            Assert.That(desc.memberName, Is.EqualTo("3"));
        }

        [Test]
        public void FromIndexing_SetsTypeInfo()
        {
            var desc = WETypeMemberDesc.FromIndexing(0, typeof(string));
            Assert.That(desc.memberTypeDllName, Is.EqualTo("mscorlib"));
            Assert.That(desc.memberTypeClassName, Is.EqualTo(typeof(string).FullName));
        }

        [Test]
        public void FromIndexing_SetsArraylikeType()
        {
            var desc = WETypeMemberDesc.FromIndexing(0, typeof(string));
            Assert.That(desc.type, Is.EqualTo(WEMemberType.ArraylikeIndexing));
        }

        [Test]
        public void FromIndexing_IntResult_SupportsMathOp()
        {
            var desc = WETypeMemberDesc.FromIndexing(0, typeof(int));
            Assert.That(desc.supportsMathOp, Is.True);
        }

        [Test]
        public void FromIndexing_FloatResult_SupportsMathOp()
        {
            var desc = WETypeMemberDesc.FromIndexing(0, typeof(float));
            Assert.That(desc.supportsMathOp, Is.True);
        }

        [Test]
        public void FromIndexing_StringResult_NoMathOp()
        {
            var desc = WETypeMemberDesc.FromIndexing(0, typeof(string));
            Assert.That(desc.supportsMathOp, Is.False);
        }

        // ── FromMemberInfo: PropertyInfo ──────────────────────────────────────

        [Test]
        public void FromMemberInfo_Property_SetsPropertyType()
        {
            var prop = typeof(string).GetProperty("Length")!;
            var desc = WETypeMemberDesc.FromMemberInfo(prop);
            Assert.That(desc.type, Is.EqualTo(WEMemberType.Property));
            Assert.That(desc.memberName, Is.EqualTo("Length"));
        }

        [Test]
        public void FromMemberInfo_Property_IntLength_SupportsMathOp()
        {
            var prop = typeof(string).GetProperty("Length")!;
            var desc = WETypeMemberDesc.FromMemberInfo(prop);
            Assert.That(desc.supportsMathOp, Is.True);
        }

        [Test]
        public void FromMemberInfo_Property_TypeInfo_Correct()
        {
            var prop = typeof(string).GetProperty("Length")!;
            var desc = WETypeMemberDesc.FromMemberInfo(prop);
            Assert.That(desc.memberTypeClassName, Is.EqualTo(typeof(int).FullName));
        }

        // ── FromMemberInfo: MethodInfo ────────────────────────────────────────

        [Test]
        public void FromMemberInfo_Method_SetsParameterlessMethodType()
        {
            var method = typeof(string).GetMethod("ToUpperInvariant")!;
            var desc = WETypeMemberDesc.FromMemberInfo(method);
            Assert.That(desc.type, Is.EqualTo(WEMemberType.ParameterlessMethod));
            Assert.That(desc.memberName, Is.EqualTo("ToUpperInvariant"));
        }

        [Test]
        public void FromMemberInfo_MethodReturningString_NoMathOp()
        {
            var method = typeof(string).GetMethod("ToUpperInvariant")!;
            var desc = WETypeMemberDesc.FromMemberInfo(method);
            Assert.That(desc.supportsMathOp, Is.False);
        }

        // ── FromMemberInfo: FieldInfo ─────────────────────────────────────────

        [Test]
        public void FromMemberInfo_Field_SetsFieldType()
        {
            var field = typeof(string).GetField("Empty")!;
            var desc = WETypeMemberDesc.FromMemberInfo(field);
            Assert.That(desc.type, Is.EqualTo(WEMemberType.Field));
            Assert.That(desc.memberName, Is.EqualTo("Empty"));
        }

        [Test]
        public void FromMemberInfo_FieldString_NoMathOp()
        {
            var field = typeof(string).GetField("Empty")!;
            var desc = WETypeMemberDesc.FromMemberInfo(field);
            Assert.That(desc.supportsMathOp, Is.False);
        }

        // ── FromMemberInfo: unknown MemberInfo type → default ────────────────

        [Test]
        public void FromMemberInfo_UnknownType_ReturnsDefault()
        {
            // EventInfo is not handled → should fall through to default
            var eventInfo = typeof(System.AppDomain).GetEvent("UnhandledException")!;
            var desc = WETypeMemberDesc.FromMemberInfo(eventInfo);
            Assert.That(desc.memberName, Is.Null);
        }
    }
}
