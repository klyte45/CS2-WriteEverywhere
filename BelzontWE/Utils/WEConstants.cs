namespace BelzontWE
{
    public static class WEConstants
    {
        // Variable serialization separators (used in WEPreCullingSystem / WEVarsCacheBank)
        public const char VARIABLE_ITEM_SEPARATOR = '↓';
        public const char VARIABLE_KV_SEPARATOR = '→';

        // Template replacement serialization separators (used in WETemplateManager)
        public const string REPLACEMENT_ITEM_SEPARATOR = "|";
        public const string REPLACEMENT_KV_SEPARATOR = "→";
        public const string REPLACEMENT_SUB_SEPARATOR = "∫";
        public const string REPLACEMENT_SUB_KV_SEPARATOR = "↓";

        // Font atlas limits
        public const int MAX_ATLAS_SIZE = 8192;

        // Font job configuration
        public const int FONT_JOB_BATCH_SIZE = 32;
        public const int STRING_RENDERING_BATCH = 4096;

        // Frame interval configuration
        public const int RENDERER_FRAME_CHECK_MASK = 0x1f;
        public const int DISPOSAL_FRAME_INTERVAL = 256;
    }
}
