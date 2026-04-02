using NUnit.Framework;
using System;

namespace BelzontWE.Tests.Enum
{
    [TestFixture]
    public class WEShaderTests
    {
        [Test] public void UnderlyingType_IsByte() => Assert.That(System.Enum.GetUnderlyingType(typeof(WEShader)), Is.EqualTo(typeof(byte)));
        [Test] public void HasExactly_ThreeValues() => Assert.That(System.Enum.GetValues(typeof(WEShader)).Length, Is.EqualTo(3));
        [Test] public void Default_HasValue_Zero() => Assert.That((byte)WEShader.Default, Is.EqualTo(0));
        [Test] public void Glass_HasValue_One() => Assert.That((byte)WEShader.Glass, Is.EqualTo(1));
        [Test] public void Decal_HasValue_Two() => Assert.That((byte)WEShader.Decal, Is.EqualTo(2));
    }

    [TestFixture]
    public class WEMemberTypeTests
    {
        [Test] public void HasExactly_FourValues() => Assert.That(System.Enum.GetValues(typeof(WEMemberType)).Length, Is.EqualTo(4));
        [Test] public void Field_HasValue_Zero() => Assert.That((int)WEMemberType.Field, Is.EqualTo(0));
        [Test] public void Property_HasValue_One() => Assert.That((int)WEMemberType.Property, Is.EqualTo(1));
        [Test] public void ParameterlessMethod_HasValue_Two() => Assert.That((int)WEMemberType.ParameterlessMethod, Is.EqualTo(2));
        [Test] public void ArraylikeIndexing_HasValue_Three() => Assert.That((int)WEMemberType.ArraylikeIndexing, Is.EqualTo(3));
    }

    [TestFixture]
    public class WEMemberSourceTests
    {
        [Test] public void HasExactly_SixValues() => Assert.That(System.Enum.GetValues(typeof(WEMemberSource)).Length, Is.EqualTo(6));
        [Test] public void Game_HasValue_Zero() => Assert.That((int)WEMemberSource.Game, Is.EqualTo(0));
        [Test] public void Unity_HasValue_One() => Assert.That((int)WEMemberSource.Unity, Is.EqualTo(1));
        [Test] public void CoUI_HasValue_Two() => Assert.That((int)WEMemberSource.CoUI, Is.EqualTo(2));
        [Test] public void System_HasValue_Three() => Assert.That((int)WEMemberSource.System, Is.EqualTo(3));
        [Test] public void Mod_HasValue_Four() => Assert.That((int)WEMemberSource.Mod, Is.EqualTo(4));
        [Test] public void Unknown_HasValue_Five() => Assert.That((int)WEMemberSource.Unknown, Is.EqualTo(5));

        [Test]
        public void GetSource_UnityStartingAssembly_ReturnsUnity()
        {
            // mscorlib.dll does NOT start with "Unity" — use a synthetic test via
            // the WEMemberSourceExtensions logic: if dllName starts with "Unity" → Unity.
            // We can test indirectly by invoking GetSource with a reflection-crafted assembly
            // whose name starts with Unity only when game DLLs are present.
            // Since we cannot instantiate Assembly with a custom name, this is tested via
            // the enum contract only for now; game-integration test deferred to tst-seam-refac.
            Assert.Ignore("Requires game DLLs or a fakes assembly — deferred to seam-refactor epic.");
        }
    }

    [TestFixture]
    public class WESimulationTextTypeTests
    {
        [Test] public void HasExactly_SevenValues() => Assert.That(System.Enum.GetValues(typeof(WESimulationTextType)).Length, Is.EqualTo(7));
        [Test] public void Text_HasValue_Zero() => Assert.That((int)WESimulationTextType.Text, Is.EqualTo(0));
        [Test] public void Image_HasValue_One() => Assert.That((int)WESimulationTextType.Image, Is.EqualTo(1));
        [Test] public void Placeholder_HasValue_Two() => Assert.That((int)WESimulationTextType.Placeholder, Is.EqualTo(2));
        [Test] public void Archetype_HasValue_Three() => Assert.That((int)WESimulationTextType.Archetype, Is.EqualTo(3));
        [Test] public void WhiteTexture_HasValue_Four() => Assert.That((int)WESimulationTextType.WhiteTexture, Is.EqualTo(4));
        [Test] public void MatrixTransform_HasValue_Five() => Assert.That((int)WESimulationTextType.MatrixTransform, Is.EqualTo(5));
        [Test] public void WhiteCube_HasValue_Six() => Assert.That((int)WESimulationTextType.WhiteCube, Is.EqualTo(6));
    }

    [TestFixture]
    public class WEPlacementPivotTests
    {
        [Test] public void HasExactly_NineValues() => Assert.That(System.Enum.GetValues(typeof(WEPlacementPivot)).Length, Is.EqualTo(9));
        [Test] public void TopLeft_HasValue_Zero() => Assert.That((int)WEPlacementPivot.TopLeft, Is.EqualTo(0));
        [Test] public void TopCenter_HasValue_One() => Assert.That((int)WEPlacementPivot.TopCenter, Is.EqualTo(1));
        [Test] public void TopRight_HasValue_Two() => Assert.That((int)WEPlacementPivot.TopRight, Is.EqualTo(2));
        [Test] public void MiddleLeft_HasValue_Four() => Assert.That((int)WEPlacementPivot.MiddleLeft, Is.EqualTo(4));
        [Test] public void MiddleCenter_HasValue_Five() => Assert.That((int)WEPlacementPivot.MiddleCenter, Is.EqualTo(5));
        [Test] public void MiddleRight_HasValue_Six() => Assert.That((int)WEPlacementPivot.MiddleRight, Is.EqualTo(6));
        [Test] public void BottomLeft_HasValue_Eight() => Assert.That((int)WEPlacementPivot.BottomLeft, Is.EqualTo(8));
        [Test] public void BottomCenter_HasValue_Nine() => Assert.That((int)WEPlacementPivot.BottomCenter, Is.EqualTo(9));
        [Test] public void BottomRight_HasValue_Ten() => Assert.That((int)WEPlacementPivot.BottomRight, Is.EqualTo(10));
    }

    [TestFixture]
    public class WEZPlacementPivotTests
    {
        [Test] public void HasExactly_ThreeValues() => Assert.That(System.Enum.GetValues(typeof(WEZPlacementPivot)).Length, Is.EqualTo(3));
        [Test] public void Front_HasValue_Zero() => Assert.That((int)WEZPlacementPivot.Front, Is.EqualTo(0));
        [Test] public void Middle_HasValue_One() => Assert.That((int)WEZPlacementPivot.Middle, Is.EqualTo(1));
        [Test] public void Back_HasValue_Two() => Assert.That((int)WEZPlacementPivot.Back, Is.EqualTo(2));
    }
}
