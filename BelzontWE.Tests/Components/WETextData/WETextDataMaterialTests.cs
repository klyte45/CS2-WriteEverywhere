using System.Reflection;
using NUnit.Framework;
using BelzontWE;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataMaterialTests
    {
        private static readonly FieldInfo DirtyField =
            typeof(WETextDataMaterial).GetField("dirty", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool GetDirty(WETextDataMaterial mat)
        {
            object boxed = mat;
            return (bool)DirtyField.GetValue(boxed);
        }

        // ── DEFAULT_DECAL_FLAGS ────────────────────────────────────────────

        [Test]
        public void DEFAULT_DECAL_FLAGS_Is8()
        {
            Assert.AreEqual(8, WETextDataMaterial.DEFAULT_DECAL_FLAGS);
        }

        // ── NormalStrength [0, 1] ──────────────────────────────────────────

        [TestCase(-1f, 0f)]
        [TestCase(-100f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(2f, 1f)]
        [TestCase(100f, 1f)]
        public void NormalStrength_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.NormalStrength = input;
            Assert.AreEqual(expected, mat.NormalStrength);
        }

        // ── GlassRefraction [1, 1000] ─────────────────────────────────────

        [TestCase(-5f, 1f)]
        [TestCase(0f, 1f)]
        [TestCase(1f, 1f)]
        [TestCase(500f, 500f)]
        [TestCase(1000f, 1000f)]
        [TestCase(1001f, 1000f)]
        [TestCase(9999f, 1000f)]
        public void GlassRefraction_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.GlassRefraction = input;
            Assert.AreEqual(expected, mat.GlassRefraction);
        }

        // ── Metallic [0, 1] ───────────────────────────────────────────────

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.3f, 0.3f)]
        [TestCase(1f, 1f)]
        [TestCase(5f, 1f)]
        public void Metallic_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.Metallic = input;
            Assert.AreEqual(expected, mat.Metallic);
        }

        // ── Smoothness [0, 1] ─────────────────────────────────────────────

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.7f, 0.7f)]
        [TestCase(1f, 1f)]
        [TestCase(10f, 1f)]
        public void Smoothness_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.Smoothness = input;
            Assert.AreEqual(expected, mat.Smoothness);
        }

        // ── EmissiveIntensity [0, 1000] ───────────────────────────────────

        [TestCase(-5f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(500f, 500f)]
        [TestCase(1000f, 1000f)]
        [TestCase(2000f, 1000f)]
        public void EmissiveIntensity_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.EmissiveIntensity = input;
            Assert.AreEqual(expected, mat.EmissiveIntensity);
        }

        // ── EmissiveExposureWeight [0, 1] ─────────────────────────────────

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.8f, 0.8f)]
        [TestCase(1f, 1f)]
        [TestCase(3f, 1f)]
        public void EmissiveExposureWeight_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.EmissiveExposureWeight = input;
            Assert.AreEqual(expected, mat.EmissiveExposureWeight);
        }

        // ── CoatStrength [0, 1] ───────────────────────────────────────────

        [TestCase(-2f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.4f, 0.4f)]
        [TestCase(1f, 1f)]
        [TestCase(7f, 1f)]
        public void CoatStrength_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.CoatStrength = input;
            Assert.AreEqual(expected, mat.CoatStrength);
        }

        // ── GlassThickness [0, 10] ───────────────────────────────────────

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(5f, 5f)]
        [TestCase(10f, 10f)]
        [TestCase(11f, 10f)]
        [TestCase(999f, 10f)]
        public void GlassThickness_Clamped(float input, float expected)
        {
            var mat = new WETextDataMaterial();
            mat.GlassThickness = input;
            Assert.AreEqual(expected, mat.GlassThickness);
        }

        // ── Shader setter stores value ────────────────────────────────────

        [Test]
        public void Shader_Setter_StoresValue_Default()
        {
            var mat = new WETextDataMaterial();
            mat.Shader = WEShader.Default;
            Assert.AreEqual(WEShader.Default, mat.Shader);
        }

        [Test]
        public void Shader_Setter_StoresValue_Glass()
        {
            var mat = new WETextDataMaterial();
            mat.Shader = WEShader.Glass;
            Assert.AreEqual(WEShader.Glass, mat.Shader);
        }

        [Test]
        public void Shader_Setter_StoresValue_Decal()
        {
            var mat = new WETextDataMaterial();
            mat.Shader = WEShader.Decal;
            Assert.AreEqual(WEShader.Decal, mat.Shader);
        }

        // ── RenderBackface sets dirty ─────────────────────────────────────

        [Test]
        public void RenderBackface_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.RenderBackface = true;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void RenderBackface_Setter_StoresValue_True()
        {
            var mat = new WETextDataMaterial();
            mat.RenderBackface = true;
            Assert.IsTrue(mat.RenderBackface);
        }

        [Test]
        public void RenderBackface_Setter_StoresValue_False()
        {
            var mat = new WETextDataMaterial();
            mat.RenderBackface = false;
            Assert.IsFalse(mat.RenderBackface);
        }

        [Test]
        public void RenderBackface_SetFalse_StillSetsDirty()
        {
            var mat = new WETextDataMaterial();
            mat.RenderBackface = false;
            Assert.IsTrue(GetDirty(mat));
        }

        // ── Color setter ──────────────────────────────────────────────────

        [Test]
        public void Color_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.Color = UnityEngine.Color.red;
            Assert.AreEqual(UnityEngine.Color.red, mat.Color);
        }

        [Test]
        public void Color_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.Color = UnityEngine.Color.blue;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void Color_Setter_OverwritesPreviousValue()
        {
            var mat = new WETextDataMaterial();
            mat.Color = UnityEngine.Color.red;
            mat.Color = UnityEngine.Color.green;
            Assert.AreEqual(UnityEngine.Color.green, mat.Color);
        }

        // ── EmissiveColor setter ──────────────────────────────────────────

        [Test]
        public void EmissiveColor_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.EmissiveColor = UnityEngine.Color.yellow;
            Assert.AreEqual(UnityEngine.Color.yellow, mat.EmissiveColor);
        }

        [Test]
        public void EmissiveColor_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.EmissiveColor = UnityEngine.Color.cyan;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void EmissiveColor_Setter_OverwritesPreviousValue()
        {
            var mat = new WETextDataMaterial();
            mat.EmissiveColor = UnityEngine.Color.red;
            mat.EmissiveColor = UnityEngine.Color.white;
            Assert.AreEqual(UnityEngine.Color.white, mat.EmissiveColor);
        }

        // ── GlassColor, ColorMask setters store value ─────────────────────

        [Test]
        public void GlassColor_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.GlassColor = UnityEngine.Color.magenta;
            Assert.AreEqual(UnityEngine.Color.magenta, mat.GlassColor);
        }

        [Test]
        public void ColorMask1_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.ColorMask1 = UnityEngine.Color.red;
            Assert.AreEqual(UnityEngine.Color.red, mat.ColorMask1);
        }

        [Test]
        public void ColorMask2_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.ColorMask2 = UnityEngine.Color.green;
            Assert.AreEqual(UnityEngine.Color.green, mat.ColorMask2);
        }

        [Test]
        public void ColorMask3_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.ColorMask3 = UnityEngine.Color.blue;
            Assert.AreEqual(UnityEngine.Color.blue, mat.ColorMask3);
        }

        // ── DecalFlags, AffectSmoothness, DrawOrder, UseGlobalLight dirty ─

        [Test]
        public void DecalFlags_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.DecalFlags = 4;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void DecalFlags_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.DecalFlags = 12;
            Assert.AreEqual(12, mat.DecalFlags);
        }

        [Test]
        public void AffectSmoothness_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.AffectSmoothness = true;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void AffectSmoothness_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.AffectSmoothness = true;
            Assert.IsTrue(mat.AffectSmoothness);
        }

        [Test]
        public void DrawOrder_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.DrawOrder = 5.5f;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void DrawOrder_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.DrawOrder = 3.14f;
            Assert.AreEqual(3.14f, mat.DrawOrder, 0.001f);
        }

        [Test]
        public void UseGlobalLight_Setter_SetsDirtyTrue()
        {
            var mat = new WETextDataMaterial();
            mat.UseGlobalLight = true;
            Assert.IsTrue(GetDirty(mat));
        }

        [Test]
        public void UseGlobalLight_Setter_StoresValue()
        {
            var mat = new WETextDataMaterial();
            mat.UseGlobalLight = true;
            Assert.IsTrue(mat.UseGlobalLight);
        }
    }
}
