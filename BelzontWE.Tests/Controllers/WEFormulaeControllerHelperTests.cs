using NUnit.Framework;
using System.Collections.Generic;

namespace BelzontWE.Tests.Controllers
{
    // Tests for WEFormulaeControllerHelper — pure static helper extracted from WEFormulaeController.
    // Covers IsTypeIndexable, ListAvailableMethodsForType, ListAvailableMembersForType.
    // ListAvailableComponents uses TypeManager.AllTypes (ECS runtime) and is F-tier — not tested here.

    [TestFixture]
    public class WEFormulaeControllerHelperTests
    {
        // ── IsTypeIndexable ────────────────────────────────────────────────────

        [Test]
        public void IsTypeIndexable_IntArray_ReturnsTrue()
        {
            bool result = WEFormulaeControllerHelper.IsTypeIndexable("mscorlib", "System.Int32[]");
            Assert.IsTrue(result, "int[] is an array and should be indexable");
        }

        [Test]
        public void IsTypeIndexable_Int_ReturnsFalse()
        {
            bool result = WEFormulaeControllerHelper.IsTypeIndexable("mscorlib", "System.Int32");
            Assert.IsFalse(result, "int is not indexable");
        }

        [Test]
        public void IsTypeIndexable_ListOfInt_ReturnsTrue()
        {
            // List<T> implements IList<T> and has get_Item(int) — should be indexable
            bool result = WEFormulaeControllerHelper.IsTypeIndexable(
                "mscorlib",
                "System.Collections.Generic.List`1[[System.Int32, mscorlib]]");
            Assert.IsTrue(result, "List<int> implements IList<> and should be indexable");
        }

        [Test]
        public void IsTypeIndexable_UnknownType_ReturnsFalse()
        {
            bool result = WEFormulaeControllerHelper.IsTypeIndexable("mscorlib", "System.DoesNotExist");
            Assert.IsFalse(result, "Null type should return false");
        }

        [Test]
        public void IsTypeIndexable_String_ReturnsFalse()
        {
            // String has a char indexer but does NOT implement IList<> or IIndexable<>
            bool result = WEFormulaeControllerHelper.IsTypeIndexable("mscorlib", "System.String");
            Assert.IsFalse(result, "string has get_Item but does not implement IList<>/IIndexable<>");
        }

        // ── ListAvailableMembersForType ────────────────────────────────────────

        [Test]
        public void ListAvailableMembersForType_NullType_ReturnsNull()
        {
            var result = WEFormulaeControllerHelper.ListAvailableMembersForType("mscorlib", "System.DoesNotExist");
            Assert.IsNull(result, "Unknown type should return null");
        }

        [Test]
        public void ListAvailableMembersForType_KnownType_ReturnsNonEmptyArray()
        {
            var result = WEFormulaeControllerHelper.ListAvailableMembersForType("mscorlib", "System.DateTime");
            Assert.IsNotNull(result, "DateTime should return non-null");
            Assert.Greater(result.Length, 0, "DateTime should have discoverable members");
        }

        // ── ListAvailableMethodsForType ────────────────────────────────────────

        [Test]
        public void ListAvailableMethodsForType_NullType_ReturnsNull()
        {
            var result = WEFormulaeControllerHelper.ListAvailableMethodsForType("mscorlib", "System.DoesNotExist");
            Assert.IsNull(result, "Unknown type should return null");
        }

        [Test, Ignore("WEStaticMethodDesc.From calls WEMemberSourceExtensions.GetSource which loads Colossal.IO.AssetDatabase — Game.dll dependency")]
        public void ListAvailableMethodsForType_EntityType_ReturnsGroupedDictionary()
        {
            // TestFormulaeClass has [WEBuiltinFunction] and [WEFormula] methods with Entity first param
            WEFormulaeHelper.ResetMethodCache();
            var result = WEFormulaeControllerHelper.ListAvailableMethodsForType(
                "Unity.Entities",
                "Unity.Entities.Entity");
            Assert.IsNotNull(result, "Entity param type should yield grouped results");
            Assert.Greater(result.Count, 0, "Should find at least one source group with Entity-param formulae");
        }
    }
}
