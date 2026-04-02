using NUnit.Framework;
using BelzontWE.Font;

namespace BelzontWE.Tests.Font
{
    /// <summary>
    /// Tests for FakePtr&lt;T&gt; — a managed pointer abstraction over T[] with an offset.
    /// All tests use int or byte as the type parameter because both have new() constructors.
    /// </summary>
    [TestFixture]
    public class FakePtrTests
    {
        // ── Null sentinel ────────────────────────────────────────────────────

        [Test]
        public void Null_IsNull_IsTrue()
            => Assert.That(FakePtr<int>.Null.IsNull, Is.True);

        [Test]
        public void Null_ArrayData_IsNull()
            => Assert.That(FakePtr<int>.Null.ArrayData, Is.Null);

        [Test]
        public void Null_Offset_IsZero()
            => Assert.That(FakePtr<int>.Null.Offset, Is.EqualTo(0));

        // ── Constructor: T[] ─────────────────────────────────────────────────

        [Test]
        public void CtorArray_SetsArrayData()
        {
            var arr = new int[] { 1, 2, 3 };
            var ptr = new FakePtr<int>(arr);
            Assert.That(ptr.ArrayData, Is.SameAs(arr));
        }

        [Test]
        public void CtorArray_OffsetIsZero()
        {
            var ptr = new FakePtr<int>(new int[] { 1, 2 });
            Assert.That(ptr.Offset, Is.EqualTo(0));
        }

        [Test]
        public void CtorArray_IsNull_IsFalse()
        {
            var ptr = new FakePtr<int>(new int[1]);
            Assert.That(ptr.IsNull, Is.False);
        }

        // ── Constructor: T[] with offset ─────────────────────────────────────

        [Test]
        public void CtorArrayOffset_SetsOffset()
        {
            var arr = new int[] { 10, 20, 30 };
            var ptr = new FakePtr<int>(arr, 1);
            Assert.That(ptr.Offset, Is.EqualTo(1));
        }

        [Test]
        public void CtorArrayOffset_AccessesCorrectElement()
        {
            var arr = new int[] { 10, 20, 30 };
            var ptr = new FakePtr<int>(arr, 1);
            Assert.That(ptr[0], Is.EqualTo(20));
        }

        // ── Constructor: FakePtr<T> + int offset ──────────────────────────────

        [Test]
        public void CtorPtrOffset_CombinesOffsets()
        {
            var arr = new int[] { 1, 2, 3, 4, 5 };
            var base_ptr = new FakePtr<int>(arr, 1);
            var derived = new FakePtr<int>(base_ptr, 2);
            Assert.That(derived.Offset, Is.EqualTo(3));
        }

        [Test]
        public void CtorPtrOffset_SharesArrayData()
        {
            var arr = new int[] { 1, 2, 3 };
            var base_ptr = new FakePtr<int>(arr);
            var derived = new FakePtr<int>(base_ptr, 1);
            Assert.That(derived.ArrayData, Is.SameAs(arr));
        }

        // ── Constructor: single T value ───────────────────────────────────────

        [Test]
        public void CtorValue_CreatesOneElementArray()
        {
            var ptr = new FakePtr<int>(42);
            Assert.That(ptr.ArrayData.Length, Is.EqualTo(1));
        }

        [Test]
        public void CtorValue_ValueIsStored()
        {
            var ptr = new FakePtr<int>(42);
            Assert.That(ptr.Value, Is.EqualTo(42));
        }

        // ── Value property ────────────────────────────────────────────────────

        [Test]
        public void Value_Get_ReturnsElementAtOffset()
        {
            var ptr = new FakePtr<int>(new int[] { 5, 10, 15 }, 1);
            Assert.That(ptr.Value, Is.EqualTo(10));
        }

        [Test]
        public void Value_Set_WritesAtOffset()
        {
            var ptr = new FakePtr<int>(new int[] { 0, 0, 0 }, 2);
            ptr.Value = 99;
            Assert.That(ptr.Value, Is.EqualTo(99));
        }

        // ── Indexers ──────────────────────────────────────────────────────────

        [Test]
        public void Indexer_Int_GetReturnsCorrectValue()
        {
            var ptr = new FakePtr<int>(new int[] { 10, 20, 30 });
            Assert.That(ptr[1], Is.EqualTo(20));
        }

        [Test]
        public void Indexer_Int_SetWritesCorrectValue()
        {
            var arr = new int[] { 0, 0, 0 };
            var ptr = new FakePtr<int>(arr);
            ptr[2] = 99;
            Assert.That(arr[2], Is.EqualTo(99));
        }

        [Test]
        public void Indexer_Long_GetReturnsCorrectValue()
        {
            var ptr = new FakePtr<int>(new int[] { 10, 20, 30 });
            Assert.That(ptr[2L], Is.EqualTo(30));
        }

        // ── GetAndIncrease / SetAndIncrease ───────────────────────────────────

        [Test]
        public void GetAndIncrease_ReturnsCurrentValue()
        {
            var ptr = new FakePtr<int>(new int[] { 7, 8, 9 });
            Assert.That(ptr.GetAndIncrease(), Is.EqualTo(7));
        }

        [Test]
        public void GetAndIncrease_AdvancesOffset()
        {
            var ptr = new FakePtr<int>(new int[] { 7, 8, 9 });
            ptr.GetAndIncrease();
            Assert.That(ptr.Offset, Is.EqualTo(1));
        }

        [Test]
        public void SetAndIncrease_WritesValue()
        {
            var arr = new int[] { 0, 0, 0 };
            var ptr = new FakePtr<int>(arr);
            ptr.SetAndIncrease(42);
            Assert.That(arr[0], Is.EqualTo(42));
        }

        [Test]
        public void SetAndIncrease_AdvancesOffset()
        {
            var ptr = new FakePtr<int>(new int[] { 0, 0 });
            ptr.SetAndIncrease(1);
            Assert.That(ptr.Offset, Is.EqualTo(1));
        }

        [Test]
        public void Set_WritesValueWithoutMovingOffset()
        {
            var arr = new int[] { 0, 0 };
            var ptr = new FakePtr<int>(arr, 1);
            ptr.Set(55);
            Assert.That(arr[1], Is.EqualTo(55));
            Assert.That(ptr.Offset, Is.EqualTo(1));
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [Test]
        public void Clear_ZeroesElements()
        {
            var arr = new int[] { 1, 2, 3, 4 };
            var ptr = new FakePtr<int>(arr, 1);
            ptr.Clear(2);
            Assert.That(arr[1], Is.EqualTo(0));
            Assert.That(arr[2], Is.EqualTo(0));
            Assert.That(arr[0], Is.EqualTo(1)); // unchanged
        }

        // ── Arithmetic operators ──────────────────────────────────────────────

        [Test]
        public void OperatorPlus_Int_AddsToOffset()
        {
            var ptr = new FakePtr<int>(new int[] { 1, 2, 3, 4 });
            var shifted = ptr + 2;
            Assert.That(shifted.Offset, Is.EqualTo(2));
        }

        [Test]
        public void OperatorMinus_Int_SubtractsFromOffset()
        {
            var ptr = new FakePtr<int>(new int[] { 1, 2, 3, 4 }, 3);
            var shifted = ptr - 1;
            Assert.That(shifted.Offset, Is.EqualTo(2));
        }

        [Test]
        public void OperatorPlusPlus_IncrementsOffsetByOne()
        {
            var ptr = new FakePtr<int>(new int[] { 1, 2, 3 });
            ptr++;
            Assert.That(ptr.Offset, Is.EqualTo(1));
        }

        [Test]
        public void OperatorPlus_UInt_Works()
        {
            var ptr = new FakePtr<int>(new int[] { 1, 2, 3 });
            var shifted = ptr + 2u;
            Assert.That(shifted.Offset, Is.EqualTo(2));
        }

        [Test]
        public void OperatorPlus_Long_Works()
        {
            var ptr = new FakePtr<int>(new int[] { 1, 2, 3, 4 });
            var shifted = ptr + 3L;
            Assert.That(shifted.Offset, Is.EqualTo(3));
        }

        // ── CreateWithSize / Create ───────────────────────────────────────────

        [Test]
        public void CreateWithSize_Int_CreatesCorrectLength()
        {
            var ptr = FakePtr<int>.CreateWithSize(5);
            Assert.That(ptr.ArrayData.Length, Is.EqualTo(5));
        }

        [Test]
        public void CreateWithSize_Long_CreatesCorrectLength()
        {
            var ptr = FakePtr<int>.CreateWithSize(3L);
            Assert.That(ptr.ArrayData.Length, Is.EqualTo(3));
        }

        [Test]
        public void Create_CreatesSingleElementPointer()
        {
            var ptr = FakePtr<int>.Create();
            Assert.That(ptr.ArrayData.Length, Is.EqualTo(1));
        }

        // ── memset ────────────────────────────────────────────────────────────

        [Test]
        public void Memset_FillsRange()
        {
            var ptr = FakePtr<int>.CreateWithSize(4);
            ptr.memset(7, 3);
            Assert.That(ptr[0], Is.EqualTo(7));
            Assert.That(ptr[1], Is.EqualTo(7));
            Assert.That(ptr[2], Is.EqualTo(7));
            Assert.That(ptr[3], Is.EqualTo(0)); // untouched
        }

        // ── memcpy ────────────────────────────────────────────────────────────

        [Test]
        public void Memcpy_FakePtrToFakePtr_CopiesElements()
        {
            var src = new FakePtr<int>(new int[] { 10, 20, 30 });
            var dst = FakePtr<int>.CreateWithSize(3);
            FakePtr<int>.memcpy(dst, src, 3);
            Assert.That(dst[0], Is.EqualTo(10));
            Assert.That(dst[1], Is.EqualTo(20));
            Assert.That(dst[2], Is.EqualTo(30));
        }

        [Test]
        public void Memcpy_ArrayToFakePtr_CopiesElements()
        {
            var src = new int[] { 5, 6, 7 };
            var dst = FakePtr<int>.CreateWithSize(3);
            FakePtr<int>.memcpy(dst, src, 3);
            Assert.That(dst[0], Is.EqualTo(5));
            Assert.That(dst[1], Is.EqualTo(6));
        }

        [Test]
        public void Memcpy_FakePtrToArray_CopiesElements()
        {
            var src = new FakePtr<int>(new int[] { 1, 2, 3 });
            var dst = new int[3];
            FakePtr<int>.memcpy(dst, src, 3);
            Assert.That(dst[0], Is.EqualTo(1));
            Assert.That(dst[2], Is.EqualTo(3));
        }
    }
}
