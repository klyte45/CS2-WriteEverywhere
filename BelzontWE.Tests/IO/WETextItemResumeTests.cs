using NUnit.Framework;
using Unity.Entities;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WETextItemResumeFieldTests
    {
        [Test]
        public void Name_DefaultIsNull()
        {
            var item = new WETextItemResume();
            Assert.IsNull(item.name);
        }

        [Test]
        public void Type_DefaultIsZero()
        {
            var item = new WETextItemResume();
            Assert.AreEqual(0, item.type);
        }

        [Test]
        public void Children_DefaultIsNull()
        {
            var item = new WETextItemResume();
            Assert.IsNull(item.children);
        }

        [Test]
        public void Id_DefaultIsEntityNull()
        {
            var item = new WETextItemResume();
            Assert.AreEqual(Entity.Null, item.id);
        }

        [Test]
        public void Name_SetAndGet()
        {
            var item = new WETextItemResume { name = "sign_01" };
            Assert.AreEqual("sign_01", item.name);
        }

        [Test]
        public void Type_SetAndGet()
        {
            var item = new WETextItemResume { type = 3 };
            Assert.AreEqual(3, item.type);
        }

        [Test]
        public void Children_SetAndGet()
        {
            var child = new WETextItemResume { name = "child_01" };
            var parent = new WETextItemResume { children = new[] { child } };
            Assert.AreEqual(1, parent.children.Length);
            Assert.AreEqual("child_01", parent.children[0].name);
        }

        [Test]
        public void AllFields_SetTogether()
        {
            var entity = new Entity { Index = 42, Version = 1 };
            var item = new WETextItemResume
            {
                name = "label",
                type = 2,
                id = entity,
                children = new WETextItemResume[0]
            };
            Assert.AreEqual("label", item.name);
            Assert.AreEqual(2, item.type);
            Assert.AreEqual(entity, item.id);
            Assert.AreEqual(0, item.children.Length);
        }
    }
}
