using NUnit.Framework;
using BelzontWE.Font;

namespace BelzontWE.Tests.Font
{
    [TestFixture]
    public class BufTests
    {
        private static Buf MakeBuf(params byte[] data)
        {
            var ptr = new FakePtr<byte>(data);
            return new Buf(ptr, (ulong)data.Length);
        }

        // ── Constructor ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_SetsFieldsCorrectly()
        {
            var buf = MakeBuf(0x01, 0x02, 0x03);
            Assert.AreEqual(3, buf.size);
            Assert.AreEqual(0, buf.cursor);
        }

        // ── stbtt__buf_get8 ───────────────────────────────────────────────────

        [Test]
        public void Get8_ReturnsFirstByte()
        {
            var buf = MakeBuf(0xAB, 0xCD);
            Assert.AreEqual(0xAB, buf.stbtt__buf_get8());
        }

        [Test]
        public void Get8_AdvancesCursor()
        {
            var buf = MakeBuf(0x11, 0x22);
            buf.stbtt__buf_get8();
            Assert.AreEqual(1, buf.cursor);
        }

        [Test]
        public void Get8_ReturnsZeroWhenAtEnd()
        {
            var buf = MakeBuf(0x01);
            buf.stbtt__buf_get8(); // consume only byte
            Assert.AreEqual(0, buf.stbtt__buf_get8());
        }

        [Test]
        public void Get8_ReadsSequentially()
        {
            var buf = MakeBuf(0x10, 0x20, 0x30);
            Assert.AreEqual(0x10, buf.stbtt__buf_get8());
            Assert.AreEqual(0x20, buf.stbtt__buf_get8());
            Assert.AreEqual(0x30, buf.stbtt__buf_get8());
        }

        // ── stbtt__buf_peek8 ──────────────────────────────────────────────────

        [Test]
        public void Peek8_ReturnsByteWithoutMovingCursor()
        {
            var buf = MakeBuf(0x42);
            var v = buf.stbtt__buf_peek8();
            Assert.AreEqual(0x42, v);
            Assert.AreEqual(0, buf.cursor);
        }

        [Test]
        public void Peek8_ReturnsZeroWhenAtEnd()
        {
            var buf = MakeBuf(0x01);
            buf.cursor = 1;
            Assert.AreEqual(0, buf.stbtt__buf_peek8());
        }

        // ── stbtt__buf_seek ───────────────────────────────────────────────────

        [Test]
        public void Seek_MovesCursorToPosition()
        {
            var buf = MakeBuf(0, 0, 0, 0, 0);
            buf.stbtt__buf_seek(3);
            Assert.AreEqual(3, buf.cursor);
        }

        [Test]
        public void Seek_ClampsNegativeToSize()
        {
            var buf = MakeBuf(0, 0, 0);
            buf.stbtt__buf_seek(-1);
            Assert.AreEqual(3, buf.cursor);
        }

        [Test]
        public void Seek_ClampsOverflowToSize()
        {
            var buf = MakeBuf(0, 0, 0);
            buf.stbtt__buf_seek(100);
            Assert.AreEqual(3, buf.cursor);
        }

        [Test]
        public void Seek_ToSizeIsValid()
        {
            var buf = MakeBuf(0, 0, 0);
            buf.stbtt__buf_seek(3);
            Assert.AreEqual(3, buf.cursor);
        }

        // ── stbtt__buf_skip ───────────────────────────────────────────────────

        [Test]
        public void Skip_AdvancesCursorByOffset()
        {
            var buf = MakeBuf(0, 0, 0, 0, 0);
            buf.stbtt__buf_skip(3);
            Assert.AreEqual(3, buf.cursor);
        }

        [Test]
        public void Skip_ClampsToSize()
        {
            var buf = MakeBuf(0, 0, 0);
            buf.stbtt__buf_skip(100);
            Assert.AreEqual(3, buf.cursor);
        }

        // ── stbtt__buf_get (big-endian multi-byte read) ───────────────────────

        [Test]
        public void Get_OneByte_ReturnsSingleByte()
        {
            var buf = MakeBuf(0x7F);
            Assert.AreEqual(0x7Fu, buf.stbtt__buf_get(1));
        }

        [Test]
        public void Get_TwoBytes_ReturnsBigEndianUInt16()
        {
            var buf = MakeBuf(0x12, 0x34);
            Assert.AreEqual(0x1234u, buf.stbtt__buf_get(2));
        }

        [Test]
        public void Get_FourBytes_ReturnsBigEndianUInt32()
        {
            var buf = MakeBuf(0x01, 0x02, 0x03, 0x04);
            Assert.AreEqual(0x01020304u, buf.stbtt__buf_get(4));
        }

        [Test]
        public void Get_ZeroBytes_ReturnsZero()
        {
            var buf = MakeBuf(0xFF);
            Assert.AreEqual(0u, buf.stbtt__buf_get(0));
        }

        [Test]
        public void Get_AdvancesCursorByN()
        {
            var buf = MakeBuf(0xAA, 0xBB, 0xCC);
            buf.stbtt__buf_get(2);
            Assert.AreEqual(2, buf.cursor);
        }

        // ── stbtt__buf_range ──────────────────────────────────────────────────

        [Test]
        public void Range_ReturnsSubBuffer_WithCorrectSize()
        {
            var buf = MakeBuf(0, 1, 2, 3, 4);
            var range = buf.stbtt__buf_range(1, 3);
            Assert.AreEqual(3, range.size);
        }

        [Test]
        public void Range_SubBuffer_ReadsCorrectly()
        {
            var buf = MakeBuf(0xAA, 0xBB, 0xCC, 0xDD);
            var range = buf.stbtt__buf_range(1, 2);
            Assert.AreEqual(0xBB, range.stbtt__buf_get8());
            Assert.AreEqual(0xCC, range.stbtt__buf_get8());
        }

        [Test]
        public void Range_NegativeOffset_ReturnsEmptyBuf()
        {
            var buf = MakeBuf(0, 1, 2);
            var range = buf.stbtt__buf_range(-1, 2);
            Assert.AreEqual(0, range.size);
        }

        [Test]
        public void Range_NegativeSize_ReturnsEmptyBuf()
        {
            var buf = MakeBuf(0, 1, 2);
            var range = buf.stbtt__buf_range(0, -1);
            Assert.AreEqual(0, range.size);
        }

        [Test]
        public void Range_OffsetPlusSizeExceedsBuffer_ReturnsEmptyBuf()
        {
            var buf = MakeBuf(0, 1, 2);
            var range = buf.stbtt__buf_range(2, 2); // 2+2 > 3
            Assert.AreEqual(0, range.size);
        }

        [Test]
        public void Range_ZeroSize_ReturnsZeroSizeBuf()
        {
            var buf = MakeBuf(0, 1, 2);
            var range = buf.stbtt__buf_range(1, 0);
            Assert.AreEqual(0, range.size);
        }

        // ── stbtt__cff_int ────────────────────────────────────────────────────

        [Test]
        public void CffInt_SingleByte_Range32To246()
        {
            // b0 = 32 → value = 32 - 139 = -107 (as uint: big number), but let's test a positive
            // b0 = 139 → value = 139 - 139 = 0
            var buf = MakeBuf(139);
            Assert.AreEqual(0u, buf.stbtt__cff_int());
        }

        [Test]
        public void CffInt_SingleByte_MaxSingleByteEncoding()
        {
            // b0 = 246 → value = 246 - 139 = 107
            var buf = MakeBuf(246);
            Assert.AreEqual(107u, buf.stbtt__cff_int());
        }

        [Test]
        public void CffInt_TwoByteHigh_Range247To250()
        {
            // b0=247, b1=0 → value = (247-247)*256 + 0 + 108 = 108
            var buf = MakeBuf(247, 0);
            Assert.AreEqual(108u, buf.stbtt__cff_int());
        }

        [Test]
        public void CffInt_TwoByteNegative_Range251To254()
        {
            // b0=251, b1=0 → value = -(251-251)*256 - 0 - 108 = -108
            var buf = MakeBuf(251, 0);
            Assert.AreEqual(unchecked((uint)-108), buf.stbtt__cff_int());
        }

        [Test]
        public void CffInt_TwoByteLiteral_28()
        {
            // b0=28, then 0x01 0x00 = 256
            var buf = MakeBuf(28, 0x01, 0x00);
            Assert.AreEqual(256u, buf.stbtt__cff_int());
        }

        [Test]
        public void CffInt_FourByteLiteral_29()
        {
            // b0=29, then 0x00 0x01 0x00 0x00 = 65536
            var buf = MakeBuf(29, 0x00, 0x01, 0x00, 0x00);
            Assert.AreEqual(65536u, buf.stbtt__cff_int());
        }

        [Test]
        public void CffInt_UnknownByte_ReturnsZero()
        {
            // b0=0, 1, 31 are not valid int encodings → returns 0
            var buf = MakeBuf(0);
            Assert.AreEqual(0u, buf.stbtt__cff_int());
        }

        // ── stbtt__cff_index_count ────────────────────────────────────────────

        [Test]
        public void CffIndexCount_ReadsCountFromFirst2Bytes()
        {
            // CFF index: count=3 (0x00, 0x03), then offsize and rest
            var buf = MakeBuf(0x00, 0x03, 0x01, 0x01, 0x01, 0x01, 0x01);
            Assert.AreEqual(3, buf.stbtt__cff_index_count());
        }

        [Test]
        public void CffIndexCount_ReturnsZeroForEmptyIndex()
        {
            var buf = MakeBuf(0x00, 0x00);
            Assert.AreEqual(0, buf.stbtt__cff_index_count());
        }

        // ── stbtt__cff_get_index ──────────────────────────────────────────────

        [Test]
        public void CffGetIndex_EmptyCount_ReturnsSizeZeroBuf()
        {
            // count=0: well-formed empty CFF index  
            var buf = MakeBuf(0x00, 0x00);
            var result = buf.stbtt__cff_get_index();
            // An empty CFF index occupies exactly 2 bytes (the count field)
            Assert.AreEqual(2, result.size);
        }

        // ── stbtt__get_subr bias ──────────────────────────────────────────────

        [Test]
        public void GetSubr_ReturnsNullBufForNegativeIndexAfterBias()
        {
            // Empty index (count=0): any n will result in n+bias out of range
            var buf = MakeBuf(0x00, 0x00);
            var result = buf.stbtt__get_subr(0);
            Assert.IsTrue(result.data.IsNull);
        }

        // ── stbtt__get_subrs (static) ─────────────────────────────────────────

        [Test]
        public void GetSubrs_ReturnNullBufWhenPrivateLoc1IsZero()
        {
            // Empty CFF buffer, so fontdict.stbtt__dict_get_ints(18,...) finds nothing → private_loc=[0,0]
            var cff = MakeBuf(new byte[16]);
            var fontdict = MakeBuf(new byte[4]);
            var result = Buf.stbtt__get_subrs(cff, fontdict);
            Assert.IsTrue(result.data.IsNull);
        }
    }
}
