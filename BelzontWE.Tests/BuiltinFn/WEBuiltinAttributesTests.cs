using NUnit.Framework;
using System;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WEBuiltinFunctionAttributeTests
    {
        [Test]
        public void Constructor_SetsCategory()
        {
            var attr = new WEBuiltinFunctionAttribute("vehicles");
            Assert.That(attr.Category, Is.EqualTo("vehicles"));
        }

        [Test]
        public void Constructor_EmptyCategory_IsAllowed()
        {
            var attr = new WEBuiltinFunctionAttribute(string.Empty);
            Assert.That(attr.Category, Is.EqualTo(string.Empty));
        }

        [Test]
        public void AttributeUsage_TargetsClass()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(WEBuiltinFunctionAttribute), typeof(AttributeUsageAttribute));
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Class));
        }

        [Test]
        public void AttributeUsage_IsNotInherited()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(WEBuiltinFunctionAttribute), typeof(AttributeUsageAttribute));
            Assert.That(usage.Inherited, Is.False);
        }

        [Test]
        public void CanApply_ToConcreteClass()
        {
            var attrs = typeof(DummyBuiltinClass).GetCustomAttributes(typeof(WEBuiltinFunctionAttribute), false);
            Assert.That(attrs.Length, Is.EqualTo(1));
            Assert.That(((WEBuiltinFunctionAttribute)attrs[0]).Category, Is.EqualTo("test"));
        }

        [WEBuiltinFunction("test")]
        private class DummyBuiltinClass { }
    }

    [TestFixture]
    public class WEFormulaAttributeTests
    {
        [Test]
        public void Constructor_SetsReturnType()
        {
            var attr = new WEFormulaAttribute(typeof(string));
            Assert.That(attr.ReturnType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Constructor_SetsDescription()
        {
            var attr = new WEFormulaAttribute(typeof(int), "Returns an integer");
            Assert.That(attr.Description, Is.EqualTo("Returns an integer"));
        }

        [Test]
        public void Constructor_Description_DefaultsToNull()
        {
            var attr = new WEFormulaAttribute(typeof(float));
            Assert.That(attr.Description, Is.Null);
        }

        [Test]
        public void AttributeUsage_TargetsMethod()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(WEFormulaAttribute), typeof(AttributeUsageAttribute));
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Method));
        }

        [Test]
        public void AttributeUsage_IsNotInherited()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(WEFormulaAttribute), typeof(AttributeUsageAttribute));
            Assert.That(usage.Inherited, Is.False);
        }

        [Test]
        public void CanApply_ToMethod_AndBeRetrieved()
        {
            var method = typeof(DummyFormulaClass).GetMethod("SomeFormula");
            var attrs = method.GetCustomAttributes(typeof(WEFormulaAttribute), false);
            Assert.That(attrs.Length, Is.EqualTo(1));
            var attr = (WEFormulaAttribute)attrs[0];
            Assert.That(attr.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(attr.Description, Is.EqualTo("desc"));
        }

        private class DummyFormulaClass
        {
            [WEFormula(typeof(string), "desc")]
            public string? SomeFormula() => null;
        }
    }
}
