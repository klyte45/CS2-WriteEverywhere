using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE.Tests.Templates
{
    [TestFixture]
    public class WEGamePropIndexTests
    {
        // ── WEGamePropIndex property exists on WETemplateManager ──────────

        [Test]
        public void WETemplateManager_HasWEGamePropIndexProperty()
        {
            var prop = typeof(WETemplateManager).GetProperty("WEGamePropIndex");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop.PropertyType, Is.EqualTo(typeof(Dictionary<string, Entity>)));
        }

        // ── Eligibility filter logic ──────────────────────────────────────

        [Test]
        public void EligibilityFilter_StaticObjectOnly_IsEligible()
            => Assert.That(WETemplateManager.IsEligibleForGamePropIndex(
                hasStaticObjectData: true,
                hasBuildingData: false,
                hasBuildingExtensionData: false), Is.True);

        [Test]
        public void EligibilityFilter_StaticObjectWithBuilding_IsNotEligible()
            => Assert.That(WETemplateManager.IsEligibleForGamePropIndex(
                hasStaticObjectData: true,
                hasBuildingData: true,
                hasBuildingExtensionData: false), Is.False);

        [Test]
        public void EligibilityFilter_StaticObjectWithBuildingExtension_IsNotEligible()
            => Assert.That(WETemplateManager.IsEligibleForGamePropIndex(
                hasStaticObjectData: true,
                hasBuildingData: false,
                hasBuildingExtensionData: true), Is.False);

        [Test]
        public void EligibilityFilter_NoStaticObject_IsNotEligible()
            => Assert.That(WETemplateManager.IsEligibleForGamePropIndex(
                hasStaticObjectData: false,
                hasBuildingData: false,
                hasBuildingExtensionData: false), Is.False);

        [Test]
        public void EligibilityFilter_Vehicle_NoStaticObject_IsNotEligible()
            => Assert.That(WETemplateManager.IsEligibleForGamePropIndex(
                hasStaticObjectData: false,
                hasBuildingData: false,
                hasBuildingExtensionData: false), Is.False);
    }
}
