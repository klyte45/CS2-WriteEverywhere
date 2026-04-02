using NUnit.Framework;
using Unity.Mathematics;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataTransformTests
    {
        // ── PivotAsFloat3 — all 9 XY pivots with Z=Front (z=0) ───────────

        [TestCase(WEPlacementPivot.TopLeft,     0f,   0f, 0f)]
        [TestCase(WEPlacementPivot.TopCenter,   0.5f, 0f, 0f)]
        [TestCase(WEPlacementPivot.TopRight,    1f,   0f, 0f)]
        [TestCase(WEPlacementPivot.MiddleLeft,  0f,   0.5f, 0f)]
        [TestCase(WEPlacementPivot.MiddleCenter, 0.5f, 0.5f, 0f)]
        [TestCase(WEPlacementPivot.MiddleRight, 1f,   0.5f, 0f)]
        [TestCase(WEPlacementPivot.BottomLeft,  0f,   1f, 0f)]
        [TestCase(WEPlacementPivot.BottomCenter, 0.5f, 1f, 0f)]
        [TestCase(WEPlacementPivot.BottomRight, 1f,   1f, 0f)]
        public void PivotAsFloat3_ZFront(WEPlacementPivot p, float ex, float ey, float ez)
        {
            var t = new WETextDataTransform { pivot = p, pivotZ = WEZPlacementPivot.Front };
            var v = t.PivotAsFloat3;
            Assert.AreEqual(ex, v.x, 0.0001f, $"pivot={p} x");
            Assert.AreEqual(ey, v.y, 0.0001f, $"pivot={p} y");
            Assert.AreEqual(ez, v.z, 0.0001f, $"pivot={p} z");
        }

        // ── PivotAsFloat3 — all 9 XY pivots with Z=Middle (z=0.5) ────────

        [TestCase(WEPlacementPivot.TopLeft,     0f,   0f,   0.5f)]
        [TestCase(WEPlacementPivot.TopCenter,   0.5f, 0f,   0.5f)]
        [TestCase(WEPlacementPivot.TopRight,    1f,   0f,   0.5f)]
        [TestCase(WEPlacementPivot.MiddleLeft,  0f,   0.5f, 0.5f)]
        [TestCase(WEPlacementPivot.MiddleCenter, 0.5f, 0.5f, 0.5f)]
        [TestCase(WEPlacementPivot.MiddleRight, 1f,   0.5f, 0.5f)]
        [TestCase(WEPlacementPivot.BottomLeft,  0f,   1f,   0.5f)]
        [TestCase(WEPlacementPivot.BottomCenter, 0.5f, 1f,   0.5f)]
        [TestCase(WEPlacementPivot.BottomRight, 1f,   1f,   0.5f)]
        public void PivotAsFloat3_ZMiddle(WEPlacementPivot p, float ex, float ey, float ez)
        {
            var t = new WETextDataTransform { pivot = p, pivotZ = WEZPlacementPivot.Middle };
            var v = t.PivotAsFloat3;
            Assert.AreEqual(ex, v.x, 0.0001f, $"pivot={p} x");
            Assert.AreEqual(ey, v.y, 0.0001f, $"pivot={p} y");
            Assert.AreEqual(ez, v.z, 0.0001f, $"pivot={p} z");
        }

        // ── PivotAsFloat3 — Z=Back (z=1) spot checks ─────────────────────

        [Test]
        public void PivotAsFloat3_ZBack_MiddleCenter()
        {
            var t = new WETextDataTransform { pivot = WEPlacementPivot.MiddleCenter, pivotZ = WEZPlacementPivot.Back };
            var v = t.PivotAsFloat3;
            Assert.AreEqual(0.5f, v.x, 0.0001f);
            Assert.AreEqual(0.5f, v.y, 0.0001f);
            Assert.AreEqual(1f, v.z, 0.0001f);
        }

        [Test]
        public void PivotAsFloat3_ZBack_TopLeft()
        {
            var t = new WETextDataTransform { pivot = WEPlacementPivot.TopLeft, pivotZ = WEZPlacementPivot.Back };
            var v = t.PivotAsFloat3;
            Assert.AreEqual(0f, v.x, 0.0001f);
            Assert.AreEqual(0f, v.y, 0.0001f);
            Assert.AreEqual(1f, v.z, 0.0001f);
        }

        // ── ArrayInstancing clamp ─────────────────────────────────────────

        [Test]
        public void ArrayInstancing_ZeroInput_ClampedToOne()
        {
            var t = new WETextDataTransform();
            t.ArrayInstancing = new uint3(0, 0, 0);
            Assert.AreEqual(new uint3(1, 1, 1), t.ArrayInstancing);
        }

        [Test]
        public void ArrayInstancing_OverflowInput_ClampedTo100()
        {
            var t = new WETextDataTransform();
            t.ArrayInstancing = new uint3(200, 300, 999);
            Assert.AreEqual(new uint3(100, 100, 100), t.ArrayInstancing);
        }

        [Test]
        public void ArrayInstancing_ValidInput_Unchanged()
        {
            var t = new WETextDataTransform();
            t.ArrayInstancing = new uint3(5, 10, 50);
            Assert.AreEqual(new uint3(5, 10, 50), t.ArrayInstancing);
        }

        [Test]
        public void ArrayInstancing_MixedBoundary_CorrectlyClamped()
        {
            var t = new WETextDataTransform();
            t.ArrayInstancing = new uint3(0, 5, 200);
            Assert.AreEqual(new uint3(1, 5, 100), t.ArrayInstancing);
        }

        [Test]
        public void ArrayInstancing_Boundary100_Allowed()
        {
            var t = new WETextDataTransform();
            t.ArrayInstancing = new uint3(100, 100, 100);
            Assert.AreEqual(new uint3(100, 100, 100), t.ArrayInstancing);
        }

        [Test]
        public void ArrayInstancing_Boundary1_Allowed()
        {
            var t = new WETextDataTransform();
            t.ArrayInstancing = new uint3(1, 1, 1);
            Assert.AreEqual(new uint3(1, 1, 1), t.ArrayInstancing);
        }

        // ── SpacingByAxisOrder ────────────────────────────────────────────

        private static WETextDataTransform MakeTransformWithGap(float x, float y, float z, WETextDataTransform.ArrayInstancingAxisOrder order)
        {
            var t = new WETextDataTransform
            {
                arrayInstancingGapMeters = new float3(x, y, z),
                arrayAxisGrowthOrder = order
            };
            t.ArrayInstancing = new uint3(2, 2, 2);
            return t;
        }

        [Test]
        public void SpacingByAxisOrder_XYZ_CorrectStrides()
        {
            var t = MakeTransformWithGap(1, 2, 3, WETextDataTransform.ArrayInstancingAxisOrder.XYZ);
            var s = t.SpacingByAxisOrder;
            Assert.AreEqual(new float3(1, 0, 0), s[0]);
            Assert.AreEqual(new float3(0, 2, 0), s[1]);
            Assert.AreEqual(new float3(0, 0, 3), s[2]);
        }

        [Test]
        public void SpacingByAxisOrder_XZY_CorrectStrides()
        {
            var t = MakeTransformWithGap(1, 2, 3, WETextDataTransform.ArrayInstancingAxisOrder.XZY);
            var s = t.SpacingByAxisOrder;
            Assert.AreEqual(new float3(1, 0, 0), s[0]);
            Assert.AreEqual(new float3(0, 0, 3), s[1]);
            Assert.AreEqual(new float3(0, 2, 0), s[2]);
        }

        [Test]
        public void SpacingByAxisOrder_YXZ_CorrectStrides()
        {
            var t = MakeTransformWithGap(1, 2, 3, WETextDataTransform.ArrayInstancingAxisOrder.YXZ);
            var s = t.SpacingByAxisOrder;
            Assert.AreEqual(new float3(0, 2, 0), s[0]);
            Assert.AreEqual(new float3(1, 0, 0), s[1]);
            Assert.AreEqual(new float3(0, 0, 3), s[2]);
        }

        [Test]
        public void SpacingByAxisOrder_YZX_CorrectStrides()
        {
            var t = MakeTransformWithGap(1, 2, 3, WETextDataTransform.ArrayInstancingAxisOrder.YZX);
            var s = t.SpacingByAxisOrder;
            Assert.AreEqual(new float3(0, 2, 0), s[0]);
            Assert.AreEqual(new float3(0, 0, 3), s[1]);
            Assert.AreEqual(new float3(1, 0, 0), s[2]);
        }

        [Test]
        public void SpacingByAxisOrder_ZXY_CorrectStrides()
        {
            var t = MakeTransformWithGap(1, 2, 3, WETextDataTransform.ArrayInstancingAxisOrder.ZXY);
            var s = t.SpacingByAxisOrder;
            Assert.AreEqual(new float3(0, 0, 3), s[0]);
            Assert.AreEqual(new float3(1, 0, 0), s[1]);
            Assert.AreEqual(new float3(0, 2, 0), s[2]);
        }

        [Test]
        public void SpacingByAxisOrder_ZYX_CorrectStrides()
        {
            var t = MakeTransformWithGap(1, 2, 3, WETextDataTransform.ArrayInstancingAxisOrder.ZYX);
            var s = t.SpacingByAxisOrder;
            Assert.AreEqual(new float3(0, 0, 3), s[0]);
            Assert.AreEqual(new float3(0, 2, 0), s[1]);
            Assert.AreEqual(new float3(1, 0, 0), s[2]);
        }

        // ── InstanceCountByAxisOrder ──────────────────────────────────────

        [Test]
        public void InstanceCountByAxisOrder_XYZ_PreservesOrder()
        {
            var t = new WETextDataTransform { arrayAxisGrowthOrder = WETextDataTransform.ArrayInstancingAxisOrder.XYZ };
            t.ArrayInstancing = new uint3(2, 3, 4);
            Assert.AreEqual(new uint3(2, 3, 4), t.InstanceCountByAxisOrder);
        }

        [Test]
        public void InstanceCountByAxisOrder_ZYX_Reverses()
        {
            var t = new WETextDataTransform { arrayAxisGrowthOrder = WETextDataTransform.ArrayInstancingAxisOrder.ZYX };
            t.ArrayInstancing = new uint3(2, 3, 4);
            Assert.AreEqual(new uint3(4, 3, 2), t.InstanceCountByAxisOrder);
        }

        [Test]
        public void InstanceCountByAxisOrder_XZY_SwapsYZ()
        {
            var t = new WETextDataTransform { arrayAxisGrowthOrder = WETextDataTransform.ArrayInstancingAxisOrder.XZY };
            t.ArrayInstancing = new uint3(2, 3, 4);
            Assert.AreEqual(new uint3(2, 4, 3), t.InstanceCountByAxisOrder);
        }

        // ── MustDraw logic ────────────────────────────────────────────────

        [Test]
        public void MustDraw_FormulaDisabled_AlwaysTrue()
        {
            var t = new WETextDataTransform { useFormulaeToCheckIfDraw = false };
            Assert.IsTrue(t.MustDraw);
        }

        [Test]
        public void MustDraw_FormulaEnabled_ZeroEffective_ReturnsFalse()
        {
            var t = new WETextDataTransform { useFormulaeToCheckIfDraw = true };
            t.MustDrawFn = new WETextDataValueFloat { defaultValue = 0 };
            Assert.IsFalse(t.MustDraw);
        }

        [Test]
        public void MustDraw_FormulaEnabled_InitialEffectiveValueIsZero_ReturnsFalse()
        {
            // EffectiveValue is 0 by default (requires ECS to update), so MustDraw is false when formulae enabled
            var t = new WETextDataTransform { useFormulaeToCheckIfDraw = true };
            t.MustDrawFn = new WETextDataValueFloat { defaultValue = 1 };
            Assert.IsFalse(t.MustDraw);
        }

        // ── DefaultInstanceCount ──────────────────────────────────────────

        [Test]
        public void DefaultInstanceCount_SetAndGet()
        {
            var t = new WETextDataTransform();
            t.DefaultInstanceCount = 5;
            Assert.AreEqual(5, t.DefaultInstanceCount);
        }

        [Test]
        public void DefaultInstanceCount_NegativeValueStored()
        {
            var t = new WETextDataTransform();
            t.DefaultInstanceCount = -1;
            Assert.AreEqual(-1, t.DefaultInstanceCount);
        }

        // ── CreateDefault ─────────────────────────────────────────────────

        [Test]
        public void CreateDefault_PivotIsMiddleCenter()
        {
            var t = WETextDataTransform.CreateDefault(default);
            Assert.AreEqual(WEPlacementPivot.MiddleCenter, t.pivot);
        }

        [Test]
        public void CreateDefault_PivotZIsMiddle()
        {
            var t = WETextDataTransform.CreateDefault(default);
            Assert.AreEqual(WEZPlacementPivot.Middle, t.pivotZ);
        }

        [Test]
        public void CreateDefault_ScaleIsOne()
        {
            var t = WETextDataTransform.CreateDefault(default);
            Assert.AreEqual(new float3(1, 1, 1), t.scale);
        }

        [Test]
        public void CreateDefault_ArrayInstancingIsOneOneOne()
        {
            var t = WETextDataTransform.CreateDefault(default);
            Assert.AreEqual(new uint3(1, 1, 1), t.ArrayInstancing);
        }

        [Test]
        public void CreateDefault_OffsetPositionIsZero()
        {
            var t = WETextDataTransform.CreateDefault(default);
            Assert.AreEqual(new float3(0, 0, 0), t.offsetPosition);
        }
    }
}
