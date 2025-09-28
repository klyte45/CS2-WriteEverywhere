using BelzontWE.Sprites;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using System;
using System.IO;

namespace BelzontWE.AssetDatabases
{
    internal struct K45WE_VTLocalDatabase : IAssetDatabaseDescriptor<K45WE_VTLocalDatabase>, IEquatable<K45WE_VTLocalDatabase>
    {
        public readonly bool canWriteSettings =>
#if DEBUG
            true;
#else
            false;
#endif

        public readonly string name => "K45WE_LocalDatabase";

        public readonly IAssetFactory assetFactory => DefaultAssetFactory.instance;

        internal static string EffectivePath => Path.Combine(WEAtlasesLibrary.CACHED_VT_FOLDER, "Local");

        public readonly IDataSourceProvider dataSourceProvider => new FileSystemDataSource(name, EffectivePath, assetFactory, 0L);

        public readonly DlcId dlcId => DlcId.Virtual;

        public readonly bool Equals(K45WE_VTLocalDatabase other) => true;

        public override readonly bool Equals(object obj) => obj is K45WE_VTLocalDatabase;

        public override int GetHashCode() => GetType().GetHashCode();

    }
}
