using NUnit.Framework;
using System.Collections.Generic;

namespace BelzontWE.Tests.Controllers
{
    [TestFixture]
    public class WETextDataMeshControllerTests
    {
        // ── SortGamePropEntries ────────────────────────────────────────────────

        [Test]
        public void SortGamePropEntries_EmptyInput_ReturnsEmptyArray()
        {
            var result = WETextDataMeshController.SortGamePropEntries(new List<WETextDataMeshController.GamePropEntry>());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void SortGamePropEntries_SingleEntry_HasBothFields()
        {
            var entries = new[]
            {
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_Bench_01", localizedName = "Bench" }
            };
            var result = WETextDataMeshController.SortGamePropEntries(entries);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("SM_Bench_01", result[0].prefabName);
            Assert.AreEqual("Bench", result[0].localizedName);
        }

        [Test]
        public void SortGamePropEntries_MultipleEntries_SortedCaseInsensitiveByLocalizedName()
        {
            var entries = new[]
            {
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_Z_Prop", localizedName = "Zebra Plant" },
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_A_Prop", localizedName = "apple tree" },
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_B_Prop", localizedName = "Bench" },
            };
            var result = WETextDataMeshController.SortGamePropEntries(entries);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("SM_A_Prop", result[0].prefabName, "apple tree should be first (case-insensitive)");
            Assert.AreEqual("SM_B_Prop", result[1].prefabName, "Bench should be second");
            Assert.AreEqual("SM_Z_Prop", result[2].prefabName, "Zebra Plant should be last");
        }

        [Test]
        public void SortGamePropEntries_CaseInsensitive_LowercaseBeforeUppercameSameRoot()
        {
            var entries = new[]
            {
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_B", localizedName = "bench 02" },
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_A", localizedName = "Bench 01" },
            };
            var result = WETextDataMeshController.SortGamePropEntries(entries);
            // InvariantCultureIgnoreCase: "Bench 01" and "bench 02" — "01" < "02" so SM_A first
            Assert.AreEqual("SM_A", result[0].prefabName);
            Assert.AreEqual("SM_B", result[1].prefabName);
        }

        [Test]
        public void SortGamePropEntries_PrefabNamePreservedAfterSort()
        {
            var entries = new[]
            {
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_ParkBench_Large_01", localizedName = "Park Bench Large" },
                new WETextDataMeshController.GamePropEntry { prefabName = "SM_ParkBench_Small_01", localizedName = "Park Bench Small" },
            };
            var result = WETextDataMeshController.SortGamePropEntries(entries);
            Assert.AreEqual("SM_ParkBench_Large_01", result[0].prefabName);
            Assert.AreEqual("Park Bench Large", result[0].localizedName);
        }
    }
}
