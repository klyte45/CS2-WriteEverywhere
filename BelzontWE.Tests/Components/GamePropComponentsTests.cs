using Colossal.Serialization.Entities;
using NUnit.Framework;
using Unity.Entities;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class GamePropComponentsTests
    {
        [Test]
        public void WESimulationTextType_GameProp_HasValue7()
            => Assert.That((int)WESimulationTextType.GameProp, Is.EqualTo(7));

        [Test]
        public void WEChild_ImplementsIEmptySerializable()
            => Assert.That(typeof(WEChild).GetInterface(nameof(IEmptySerializable)), Is.Not.Null);

        [Test]
        public void WEChild_ImplementsIComponentData()
            => Assert.That(typeof(WEChild).GetInterface(nameof(IComponentData)), Is.Not.Null);

        [Test]
        public void WEOwner_DoesNotImplementISerializable()
        {
            Assert.That(typeof(WEOwner).GetInterface(nameof(ISerializable)), Is.Null);
            Assert.That(typeof(WEOwner).GetInterface(nameof(IEmptySerializable)), Is.Null);
        }

        [Test]
        public void WEOwner_ImplementsICleanupComponentData()
            => Assert.That(typeof(WEOwner).GetInterface(nameof(ICleanupComponentData)), Is.Not.Null);

        [Test]
        public void WESubObject_ImplementsICleanupBufferElementData()
            => Assert.That(typeof(WESubObject).GetInterface(nameof(ICleanupBufferElementData)), Is.Not.Null);

        [Test]
        public void WEInheritedVarsCache_ImplementsIEnableableComponent()
            => Assert.That(typeof(WEInheritedVarsCache).GetInterface(nameof(IEnableableComponent)), Is.Not.Null);

        [Test]
        public void WEInheritedVarsCache_VarsField_CanHold512Bytes()
        {
            var cache = new WEInheritedVarsCache { vars = new Unity.Collections.FixedString512Bytes("test") };
            Assert.That(cache.vars.ToString(), Is.EqualTo("test"));
        }
    }
}
