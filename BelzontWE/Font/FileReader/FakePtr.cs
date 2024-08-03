using System;

namespace BelzontWE.Font
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
# endif
	struct FakePtr<T> where T : new()
	{
		public static readonly FakePtr<T> Null = new FakePtr<T>(null);
        public int Offset;

		public bool IsNull => ArrayData == null;

        public T[] ArrayData { get; }

        public T this[int index]
		{
			get => ArrayData[Offset + index];

			set => ArrayData[Offset + index] = value;
		}

		public T this[long index]
		{
			get => ArrayData[Offset + index];

			set => ArrayData[Offset + index] = value;
		}

		public T Value
		{
			get => this[0];

			set => this[0] = value;
		}

		public FakePtr(FakePtr<T> ptr, int offset)
		{
			Offset = ptr.Offset + offset;
			ArrayData = ptr.ArrayData;
		}

		public FakePtr(T[] data, int offset)
		{
			Offset = offset;
			ArrayData = data;
		}

		public FakePtr(T[] data) : this(data, 0)
		{
		}

		public FakePtr(T value)
		{
			Offset = 0;
			ArrayData = new T[1];
			ArrayData[0] = value;
		}

		public void Clear(int count)
		{
			Array.Clear(ArrayData, Offset, count);
		}

		public T GetAndIncrease()
		{
			var result = ArrayData[Offset];
			++Offset;

			return result;
		}

		public void SetAndIncrease(T value)
		{
			ArrayData[Offset] = value;
			++Offset;
		}

		public void Set(T value)
		{
			ArrayData[Offset] = value;
		}

		public static FakePtr<T> operator +(FakePtr<T> p, int offset)
		{
			return new FakePtr<T>(p.ArrayData) {Offset = p.Offset + offset};
		}

		public static FakePtr<T> operator -(FakePtr<T> p, int offset)
		{
			return p + -offset;
		}

		public static FakePtr<T> operator +(FakePtr<T> p, uint offset)
		{
			return p + (int) offset;
		}

		public static FakePtr<T> operator -(FakePtr<T> p, uint offset)
		{
			return p - (int) offset;
		}

		public static FakePtr<T> operator +(FakePtr<T> p, long offset)
		{
			return p + (int) offset;
		}

		public static FakePtr<T> operator -(FakePtr<T> p, long offset)
		{
			return p - (int) offset;
		}

		public static FakePtr<T> operator ++(FakePtr<T> p)
		{
			return p + 1;
		}

		public static FakePtr<T> CreateWithSize(int size)
		{
			var result = new FakePtr<T>(new T[size]);

			for (var i = 0; i < size; ++i) result[i] = new T();

			return result;
		}

		public static FakePtr<T> CreateWithSize(long size)
		{
			return CreateWithSize((int) size);
		}

		public static FakePtr<T> Create()
		{
			return CreateWithSize(1);
		}

		public void memset(T value, int count)
		{
			for (long i = 0; i < count; ++i) this[i] = value;
		}

		public static void memcpy(FakePtr<T> a, FakePtr<T> b, int count)
		{
			for (long i = 0; i < count; ++i) a[i] = b[i];
		}

		public static void memcpy(T[] a, FakePtr<T> b, int count)
		{
			for (long i = 0; i < count; ++i) a[i] = b[i];
		}

		public static void memcpy(FakePtr<T> a, T[] b, int count)
		{
			for (long i = 0; i < count; ++i) a[i] = b[i];
		}
	}
}