using System.Runtime.InteropServices;

namespace BelzontWE.Font
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct FontGlyphBounds
	{
		public float X0;
		public float Y0;
		public float X1;
		public float Y1;

        public override string ToString() => $"[x[{X0}-{X1}];y[{Y0}-{Y1}]]";
    }
}
