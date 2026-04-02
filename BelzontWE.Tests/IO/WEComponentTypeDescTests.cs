using NUnit.Framework;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WEComponentTypeDescTests
    {
        [Test]
        public void WEDescType_ReturnsComponent()
        {
            var desc = new WEComponentTypeDesc();
            Assert.AreEqual("COMPONENT", desc.WEDescType);
        }

        [Test]
        public void DllName_DefaultIsNull()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsNull(desc.dllName);
        }

        [Test]
        public void ClassName_DefaultIsNull()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsNull(desc.className);
        }

        [Test]
        public void IsBuffer_DefaultIsFalse()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsFalse(desc.isBuffer);
        }

        [Test]
        public void ModUrl_DefaultIsNull()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsNull(desc.modUrl);
        }

        [Test]
        public void ModName_DefaultIsNull()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsNull(desc.modName);
        }

        [Test]
        public void ReturnDllName_DefaultIsNull()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsNull(desc.returnDllName);
        }

        [Test]
        public void ReturnClassName_DefaultIsNull()
        {
            var desc = new WEComponentTypeDesc();
            Assert.IsNull(desc.returnClassName);
        }

        [Test]
        public void DllName_SetAndGet()
        {
            var desc = new WEComponentTypeDesc { dllName = "BelzontWE" };
            Assert.AreEqual("BelzontWE", desc.dllName);
        }

        [Test]
        public void ClassName_SetAndGet()
        {
            var desc = new WEComponentTypeDesc { className = "BelzontWE.WETextDataMain" };
            Assert.AreEqual("BelzontWE.WETextDataMain", desc.className);
        }

        [Test]
        public void IsBuffer_SetToTrue()
        {
            var desc = new WEComponentTypeDesc { isBuffer = true };
            Assert.IsTrue(desc.isBuffer);
        }

        [Test]
        public void Source_SetAndGet()
        {
            var desc = new WEComponentTypeDesc { source = WEMemberSource.Game };
            Assert.AreEqual(WEMemberSource.Game, desc.source);
        }

        [Test]
        public void AllFields_SetAndGet()
        {
            var desc = new WEComponentTypeDesc
            {
                dllName = "test.dll",
                className = "TestClass",
                modName = "TestMod",
                modUrl = "https://example.com",
                returnDllName = "return.dll",
                returnClassName = "ReturnClass",
                isBuffer = true,
                source = WEMemberSource.Mod
            };
            Assert.AreEqual("test.dll", desc.dllName);
            Assert.AreEqual("TestClass", desc.className);
            Assert.AreEqual("TestMod", desc.modName);
            Assert.AreEqual("https://example.com", desc.modUrl);
            Assert.AreEqual("return.dll", desc.returnDllName);
            Assert.AreEqual("ReturnClass", desc.returnClassName);
            Assert.IsTrue(desc.isBuffer);
            Assert.AreEqual(WEMemberSource.Mod, desc.source);
        }
    }
}
