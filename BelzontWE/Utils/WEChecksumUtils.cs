using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BelzontWE
{
    public static class WEChecksumUtils
    {
        private const uint FNV_OFFSET_BASIS = 2166136261u;
        private const uint FNV_PRIME = 16777619u;

        /// <summary>
        /// Computes a deterministic FNV-1a checksum of a folder's PNG contents
        /// based on sorted "{fullpath}:{filesize}" strings. Only .png files are included.
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
                if (!string.Equals(Path.GetExtension(file), ".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                HashFilePath(ref hash, file);
            }
            return hash;
        }

        /// <summary>
        /// Computes a deterministic FNV-1a checksum over an explicit list of file paths.
        /// Only existing .png files are included. Paths are hashed in their given order.
        /// </summary>
        public static uint ComputeFileListChecksum(IEnumerable<string> paths)
        {
            uint hash = FNV_OFFSET_BASIS;
            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;
                if (!string.Equals(Path.GetExtension(path), ".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                HashFilePath(ref hash, path);
            }
            return hash;
        }

        /// <summary>
        /// Computes a deterministic FNV-1a checksum over bridge producer entries.
        /// Hashes the entry name and the byte-length of each non-null channel array.
        /// </summary>
        public static uint ComputeBridgeMemoryChecksum(IEnumerable<(string Name, byte[] Main, byte[] ControlMask, byte[] MaskMap, byte[] Normal, byte[] Emissive, string XmlInfo)> entries)
        {
            uint hash = FNV_OFFSET_BASIS;
            foreach (var (name, main, controlMask, maskMap, normal, emissive, xmlInfo) in entries)
            {
                HashString(ref hash, name ?? string.Empty);
                HashLength(ref hash, main);
                HashLength(ref hash, controlMask);
                HashLength(ref hash, maskMap);
                HashLength(ref hash, normal);
                HashLength(ref hash, emissive);
                HashString(ref hash, xmlInfo ?? string.Empty);
                // null-byte entry separator
                hash ^= 0;
                hash *= FNV_PRIME;
            }
            return hash;
        }

        private static void HashFilePath(ref uint hash, string file)
        {
            var entry = $"{file}:{new FileInfo(file).Length}";
            foreach (byte b in Encoding.UTF8.GetBytes(entry))
            {
                hash ^= b;
                hash *= FNV_PRIME;
            }
            // Separate entries with a null byte to avoid cross-entry collisions
            hash ^= 0;
            hash *= FNV_PRIME;
        }

        private static void HashString(ref uint hash, string s)
        {
            foreach (byte b in Encoding.UTF8.GetBytes(s))
            {
                hash ^= b;
                hash *= FNV_PRIME;
            }
            hash ^= 0;
            hash *= FNV_PRIME;
        }

        private static void HashLength(ref uint hash, byte[] arr)
        {
            var len = arr?.Length ?? -1;
            hash ^= (uint)(len & 0xFF);
            hash *= FNV_PRIME;
            hash ^= (uint)((len >> 8) & 0xFF);
            hash *= FNV_PRIME;
            hash ^= (uint)((len >> 16) & 0xFF);
            hash *= FNV_PRIME;
            hash ^= (uint)((len >> 24) & 0xFF);
            hash *= FNV_PRIME;
        }
    }
}
