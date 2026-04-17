using BelzontWE.Commons.Utils.AssetPipeline;

// Compatibility shim: all logic moved to Belzont.Utils.KMaxRectsBinPack (Commons).
public class MaxRectsBinPack : KMaxRectsBinPack
{
    public MaxRectsBinPack() : base() { }
    public MaxRectsBinPack(int width, int height, bool rotations = true) : base(width, height, rotations) { }
}