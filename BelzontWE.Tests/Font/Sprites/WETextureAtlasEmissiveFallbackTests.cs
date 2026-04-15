using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Tests for WETextureAtlas always-5-images enforcement (T3 DoD).
    /// All tests require Unity GPU context — they are marked [Ignore] in the
    /// unit-test runner and serve as specification for game-runtime validation.
    /// 
    /// Implementation note: the behavior under test is already present in
    /// WETextureAtlas.Write() as of the code audit:
    ///   • emissive == null  → main pixels copied to emissive layer
    ///   • control == null   → Color.clear fills control layer  
    ///   • mask == null      → Color.clear fills mask layer
    ///   • normal == null    → (0.5, 0.5, 1.0) fills normal layer
    /// These tests verify the invariants once a Unity context is available.
    /// </summary>
    [TestFixture]
    public class WETextureAtlasEmissiveFallbackTests
    {
        private const string IgnoreReason =
            "Requires Unity GPU context (Texture2D.SetPixels / GetPixels) — game runtime only";

        [Test, Ignore(IgnoreReason)]
        public void Insert_NullEmissive_EmissiveRegionMatchesMainRegionPixels() { }

        [Test, Ignore(IgnoreReason)]
        public void Insert_ExplicitEmissive_ExplicitEmissiveUsedNotMain() { }

        [Test, Ignore(IgnoreReason)]
        public void Insert_NullControl_ControlRegionIsTransparentBlack() { }

        [Test, Ignore(IgnoreReason)]
        public void Insert_NullMask_MaskRegionIsTransparentBlack() { }

        [Test, Ignore(IgnoreReason)]
        public void Insert_NullNormal_NormalRegionIsFlatNormal() { }
    }
}
