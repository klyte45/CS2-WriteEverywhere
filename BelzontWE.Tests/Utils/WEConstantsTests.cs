using NUnit.Framework;

namespace BelzontWE.Tests.Utils
{
    [TestFixture]
    public class WEConstantsTests
    {
        // ── Variable serialization separators ─────────────────────────────────

        [Test]
        public void VARIABLE_ITEM_SEPARATOR_IsDownArrow()
            => Assert.That(WEConstants.VARIABLE_ITEM_SEPARATOR, Is.EqualTo('\u2193'));

        [Test]
        public void VARIABLE_KV_SEPARATOR_IsRightArrow()
            => Assert.That(WEConstants.VARIABLE_KV_SEPARATOR, Is.EqualTo('\u2192'));

        [Test]
        public void VARIABLE_ITEM_SEPARATOR_IsDifferentFrom_KV_SEPARATOR()
            => Assert.That(WEConstants.VARIABLE_ITEM_SEPARATOR, Is.Not.EqualTo(WEConstants.VARIABLE_KV_SEPARATOR));

        // ── Template replacement separators ───────────────────────────────────

        [Test]
        public void REPLACEMENT_ITEM_SEPARATOR_IsPipe()
            => Assert.That(WEConstants.REPLACEMENT_ITEM_SEPARATOR, Is.EqualTo("|"));

        [Test]
        public void REPLACEMENT_KV_SEPARATOR_IsRightArrow()
            => Assert.That(WEConstants.REPLACEMENT_KV_SEPARATOR, Is.EqualTo("\u2192"));

        [Test]
        public void REPLACEMENT_SUB_SEPARATOR_IsIntegralSign()
            => Assert.That(WEConstants.REPLACEMENT_SUB_SEPARATOR, Is.EqualTo("\u222b"));

        [Test]
        public void REPLACEMENT_SUB_KV_SEPARATOR_IsDownArrow()
            => Assert.That(WEConstants.REPLACEMENT_SUB_KV_SEPARATOR, Is.EqualTo("\u2193"));

        // ── Font atlas limits ─────────────────────────────────────────────────

        [Test]
        public void MAX_ATLAS_SIZE_Is8192()
            => Assert.That(WEConstants.MAX_ATLAS_SIZE, Is.EqualTo(8192));

        [Test]
        public void MAX_ATLAS_SIZE_IsPowerOfTwo()
        {
            var v = WEConstants.MAX_ATLAS_SIZE;
            Assert.That(v > 0 && (v & (v - 1)) == 0, Is.True, "MAX_ATLAS_SIZE should be a power of two");
        }

        // ── Font job configuration ──────────────────────────────────────────

        [Test]
        public void FONT_JOB_BATCH_SIZE_Is32()
            => Assert.That(WEConstants.FONT_JOB_BATCH_SIZE, Is.EqualTo(32));


        // ── Frame interval configuration ──────────────────────────────────────

        [Test]
        public void RENDERER_FRAME_CHECK_MASK_Is31()
            => Assert.That(WEConstants.RENDERER_FRAME_CHECK_MASK, Is.EqualTo(0x1f));

        [Test]
        public void RENDERER_FRAME_CHECK_MASK_IsPowerOfTwoMinusOne()
        {
            var m = WEConstants.RENDERER_FRAME_CHECK_MASK;
            // A valid bit mask of the form 2^n - 1: (m+1) should be a power of two
            Assert.That((m + 1) > 0 && ((m + 1) & m) == 0, Is.True);
        }

        [Test]
        public void DISPOSAL_FRAME_INTERVAL_Is256()
            => Assert.That(WEConstants.DISPOSAL_FRAME_INTERVAL, Is.EqualTo(256));

        // ── Separator cross-uniqueness ─────────────────────────────────────────

        [Test]
        public void VariableSeparators_AreDistinctFromEachOther()
            => Assert.That(WEConstants.VARIABLE_ITEM_SEPARATOR, Is.Not.EqualTo(WEConstants.VARIABLE_KV_SEPARATOR));

        [Test]
        public void ReplacementItemSeparator_DifferentFromKV()
            => Assert.That(WEConstants.REPLACEMENT_ITEM_SEPARATOR, Is.Not.EqualTo(WEConstants.REPLACEMENT_KV_SEPARATOR));

        [Test]
        public void ReplacementKV_MatchesVariableKV_ByDesign()
        {
            // Both systems share the → separator for key-value separation
            Assert.That(WEConstants.REPLACEMENT_KV_SEPARATOR, Is.EqualTo(WEConstants.VARIABLE_KV_SEPARATOR.ToString()));
        }
    }
}
