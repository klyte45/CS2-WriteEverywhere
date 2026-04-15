using System.IO;
using System.Text;

namespace BelzontWE
{
    public static class WEChecksumUtils
    {
        private const uint FNV_OFFSET_BASIS = 2166136261u;
        private const uint FNV_PRIME = 16777619u;

        /// <summary>
        /// Computes a deterministic FNV-1a checksum of a folder's contents
        /// based on sorted "{filename}:{filesize}" strings.
        /// Returns <see cref="FNV_OFFSET_BASIS"/> (the FNV basis itself) for empty folders
        /// as a distinguishable sentinel — it equals the hash of an empty byte sequence.
        /// </summary>
        public static uint ComputeFolderChecksum(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return FNV_OFFSET_BASIS;

            var files = Directory.GetFiles(folderPath);
            System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);

            uint hash = FNV_OFFSET_BASIS;
            foreach (var file in files)
            {
                var entry = $"{Path.GetFileName(file)}:{new FileInfo(file).Length}";
                foreach (byte b in Encoding.UTF8.GetBytes(entry))
                {
                    hash ^= b;
                    hash *= FNV_PRIME;
                }
                // Separate entries with a null byte to avoid cross-entry collisions
                hash ^= 0;
                hash *= FNV_PRIME;
            }
            return hash;
        }
    }
}
