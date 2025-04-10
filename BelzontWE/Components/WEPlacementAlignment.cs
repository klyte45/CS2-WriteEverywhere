namespace BelzontWE
{
    public enum WEPlacementAlignment
    {
        Left = 0, Center = 1, Right = 2, Justified = 3,
        X_Left = Left << 2, X_Center = Center << 2, X_Right = Right << 2, X_Justified = Justified << 2,
        Y_Left = Left << 4, Y_Center = Center << 4, Y_Right = Right << 4, Y_Justified = Justified << 4,
        Z_Left = Left << 6, Z_Center = Center << 6, Z_Right = Right << 6, Z_Justified = Justified << 6,
    }

    public static class WEPlacementAligmentUtility
    {
        public static WEPlacementAlignment ToX(this WEPlacementAlignment alignment) => (WEPlacementAlignment)(((int)alignment & 0x3) << 2);
        public static WEPlacementAlignment ToY(this WEPlacementAlignment alignment) => (WEPlacementAlignment)(((int)alignment & 0x3) << 4);
        public static WEPlacementAlignment ToZ(this WEPlacementAlignment alignment) => (WEPlacementAlignment)(((int)alignment & 0x3) << 6);

        public static WEPlacementAlignment GetX(this WEPlacementAlignment alignment) => (WEPlacementAlignment)(((int)alignment >> 2) & 0x3);
        public static WEPlacementAlignment GetY(this WEPlacementAlignment alignment) => (WEPlacementAlignment)(((int)alignment >> 4) & 0x3);
        public static WEPlacementAlignment GetZ(this WEPlacementAlignment alignment) => (WEPlacementAlignment)(((int)alignment >> 6) & 0x3);

        public static WEPlacementAlignment Encode(WEPlacementAlignment x, WEPlacementAlignment y, WEPlacementAlignment z) => x.ToX() | x.ToY() | y.ToZ();

        public static void Decode(this WEPlacementAlignment input, out WEPlacementAlignment x, out WEPlacementAlignment y, out WEPlacementAlignment z)
        {
            x = input.GetX(); y = input.GetY(); z = input.GetZ();
        }
    }
}