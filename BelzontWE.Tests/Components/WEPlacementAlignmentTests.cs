using NUnit.Framework;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WEPlacementAlignmentTests
    {
        // ── ToX / ToY / ToZ shift tests ──────────────────────────────────────

        [Test]
        public void ToX_Left_ProducesX_Left()
            => Assert.That(WEPlacementAlignment.Left.ToX(), Is.EqualTo(WEPlacementAlignment.X_Left));

        [Test]
        public void ToX_Center_ProducesX_Center()
            => Assert.That(WEPlacementAlignment.Center.ToX(), Is.EqualTo(WEPlacementAlignment.X_Center));

        [Test]
        public void ToX_Right_ProducesX_Right()
            => Assert.That(WEPlacementAlignment.Right.ToX(), Is.EqualTo(WEPlacementAlignment.X_Right));

        [Test]
        public void ToX_Justified_ProducesX_Justified()
            => Assert.That(WEPlacementAlignment.Justified.ToX(), Is.EqualTo(WEPlacementAlignment.X_Justified));

        [Test]
        public void ToY_Left_ProducesY_Left()
            => Assert.That(WEPlacementAlignment.Left.ToY(), Is.EqualTo(WEPlacementAlignment.Y_Left));

        [Test]
        public void ToY_Center_ProducesY_Center()
            => Assert.That(WEPlacementAlignment.Center.ToY(), Is.EqualTo(WEPlacementAlignment.Y_Center));

        [Test]
        public void ToZ_Left_ProducesZ_Left()
            => Assert.That(WEPlacementAlignment.Left.ToZ(), Is.EqualTo(WEPlacementAlignment.Z_Left));

        [Test]
        public void ToZ_Right_ProducesZ_Right()
            => Assert.That(WEPlacementAlignment.Right.ToZ(), Is.EqualTo(WEPlacementAlignment.Z_Right));

        // ── GetX / GetY / GetZ extraction tests ──────────────────────────────

        [Test]
        public void GetX_FromX_Center_ReturnsCenter()
            => Assert.That(WEPlacementAlignment.X_Center.GetX(), Is.EqualTo(WEPlacementAlignment.Center));

        [Test]
        public void GetX_FromX_Right_ReturnsRight()
            => Assert.That(WEPlacementAlignment.X_Right.GetX(), Is.EqualTo(WEPlacementAlignment.Right));

        [Test]
        public void GetY_FromY_Center_ReturnsCenter()
            => Assert.That(WEPlacementAlignment.Y_Center.GetY(), Is.EqualTo(WEPlacementAlignment.Center));

        [Test]
        public void GetZ_FromZ_Justified_ReturnsJustified()
            => Assert.That(WEPlacementAlignment.Z_Justified.GetZ(), Is.EqualTo(WEPlacementAlignment.Justified));

        [Test]
        public void GetX_FromY_Bit_ReturnsLeft()
        {
            // Bits for Y do not overlap with X bits: GetX should return Left (0)
            Assert.That(WEPlacementAlignment.Y_Right.GetX(), Is.EqualTo(WEPlacementAlignment.Left));
        }

        // ── Encode / Decode round-trip ────────────────────────────────────────

        [Test]
        public void Encode_Center_Center_Center_RoundTrips()
        {
            var encoded = WEPlacementAligmentUtility.Encode(
                WEPlacementAlignment.Center,
                WEPlacementAlignment.Center,
                WEPlacementAlignment.Center);
            encoded.Decode(out var x, out var y, out var z);
            Assert.That(x, Is.EqualTo(WEPlacementAlignment.Center));
            Assert.That(y, Is.EqualTo(WEPlacementAlignment.Center));
            Assert.That(z, Is.EqualTo(WEPlacementAlignment.Center));
        }

        [Test]
        public void Encode_AllDifferent_RoundTrips()
        {
            var encoded = WEPlacementAligmentUtility.Encode(
                WEPlacementAlignment.Left,
                WEPlacementAlignment.Center,
                WEPlacementAlignment.Right);
            encoded.Decode(out var x, out var y, out var z);
            Assert.That(x, Is.EqualTo(WEPlacementAlignment.Left));
            Assert.That(y, Is.EqualTo(WEPlacementAlignment.Center));
            Assert.That(z, Is.EqualTo(WEPlacementAlignment.Right));
        }

        [Test]
        public void Encode_Justified_Left_Right_RoundTrips()
        {
            var encoded = WEPlacementAligmentUtility.Encode(
                WEPlacementAlignment.Justified,
                WEPlacementAlignment.Left,
                WEPlacementAlignment.Right);
            encoded.Decode(out var x, out var y, out var z);
            Assert.That(x, Is.EqualTo(WEPlacementAlignment.Justified));
            Assert.That(y, Is.EqualTo(WEPlacementAlignment.Left));
            Assert.That(z, Is.EqualTo(WEPlacementAlignment.Right));
        }

        // ── Constant bit-value contracts ─────────────────────────────────────

        [Test]
        public void X_Left_HasValue_Zero() => Assert.That((int)WEPlacementAlignment.X_Left, Is.EqualTo(0));

        [Test]
        public void X_Center_HasValue_Four() => Assert.That((int)WEPlacementAlignment.X_Center, Is.EqualTo(4));

        [Test]
        public void X_Right_HasValue_Eight() => Assert.That((int)WEPlacementAlignment.X_Right, Is.EqualTo(8));

        [Test]
        public void Y_Left_HasValue_Zero() => Assert.That((int)WEPlacementAlignment.Y_Left, Is.EqualTo(0));

        [Test]
        public void Y_Center_HasValue_Sixteen() => Assert.That((int)WEPlacementAlignment.Y_Center, Is.EqualTo(16));

        [Test]
        public void Z_Right_HasValue_128() => Assert.That((int)WEPlacementAlignment.Z_Right, Is.EqualTo(128));
    }
}
