using System.Runtime.InteropServices;

namespace BelzontWE.Font
{
	[StructLayout(LayoutKind.Sequential)]
	public struct FontAtlasNode
	{
		public int X;
		public int Y;
		public int Width;
	}
}
