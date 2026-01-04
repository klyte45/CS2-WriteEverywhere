using Belzont.Utils;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using MonoMod.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Unity.Collections;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        #region Modules integration

        private readonly Dictionary<AssetData, ModFolder> integrationLoadableTemplatesFromMod = new();
        private readonly Dictionary<string, HashSet<string>> m_atlasesMapped = new();
        private readonly Dictionary<string, HashSet<string>> m_fontsMapped = new();
        private readonly Dictionary<string, HashSet<string>> m_subtemplatesMapped = new();
        private readonly Dictionary<string, HashSet<string>> m_meshesMapped = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_atlasesReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_fontsReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_subtemplatesReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_meshesReplacements = new();
        public ushort SpritesAndLayoutsDataVersion { get; private set; } = 0;

        private Dictionary<string, ModTemplateRegistrationData> m_modsTemplatesFolder = new();

        public void RegisterModTemplatesForLoading(Assembly mainAssembly, string folderTemplatesSource)
        {
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly);
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            var modName = modData.GetMeta().displayName;

            if (m_modsTemplatesFolder.TryGetValue(modId, out var folder) && folder.rootFolder == folderTemplatesSource) return;
            m_modsTemplatesFolder[modId] = new(modName, modId, modId, folderTemplatesSource, false);
            GameManager.instance.StartCoroutine(LoadModSubtemplates_Item(0, 100, modId, modId));
            MarkPrefabsDirty();
        }

        public FixedString128Bytes[] GetTemplateAvailableKeys() => RegisteredTemplates.Keys.Union(ModsSubTemplates.SelectMany(x => x.Value.Keys.Select(y => new FixedString128Bytes($"{x.Key}:{y}")))).ToArray();

        internal void RegisterLoadableTemplatesFolder(AssetData assetData, ModFolder root)
        {
            integrationLoadableTemplatesFromMod[assetData] = root;
        }
        internal void RegisterAssetsLayoutsFolder(AssetData assetData, string templatesRoot)
        {
            m_modsTemplatesFolder[assetData.uniqueName] = new(assetData.GetMeta().displayName, assetData.uniqueName, WEModIntegrationUtility.GetModAtlasesPrefix(assetData), templatesRoot, true);
        }
        internal List<ModFolder> ListModsExtraFolders() => [.. integrationLoadableTemplatesFromMod.Values];

        internal FixedString64Bytes GetFontFor(string strOriginal, FixedString64Bytes currentFont, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            var result = m_fontsReplacements.TryGetValue(decomposedName[0], out var fontList) && fontList.TryGetValue(decomposedName[1], out var fontName)
                        ? (FixedString64Bytes)fontName
                        : default;
            if (result != currentFont)
            {
                haveChanges |= true;
            }
            return result;
        }

        internal FixedString64Bytes GetAtlasFor(string strOriginal, FixedString64Bytes currentAtlas, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            FixedString64Bytes result = (m_atlasesReplacements.TryGetValue(decomposedName[0], out var atlasList) && atlasList.TryGetValue(decomposedName[1], out var atlasName)
                        ? atlasName
                        : strOriginal) ?? "";
            if (result != currentAtlas)
            {
                haveChanges |= true;
            }
            return result;
        }

        internal FixedString64Bytes GetTemplateFor(string strOriginal, string currentTemplate, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            FixedString64Bytes result = (m_subtemplatesReplacements.TryGetValue(decomposedName[0], out var templateList) && templateList.TryGetValue(decomposedName[1], out var templateName)
                        ? templateName
                        : strOriginal) ?? "";
            if (result != currentTemplate)
            {
                haveChanges |= true;
            }
            return result;
        }

        internal FixedString64Bytes GetMeshFor(string strOriginal, string currentMesh, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            FixedString64Bytes result = (m_meshesReplacements.TryGetValue(decomposedName[0], out var templateList) && templateList.TryGetValue(decomposedName[1], out var templateName)
                        ? templateName
                        : strOriginal) ?? "";
            if (result != currentMesh)
            {
                haveChanges |= true;
            }
            return result;
        }

        [XmlRoot("WEModReplacementData")]
        public class ModReplacementDataXml
        {
            [XmlElement("Mod")]
            public ModReplacementData[] Mods;
        }

        public class ModReplacementData
        {
            [XmlAttribute] public string modId;
            [XmlIgnore] public string displayName;
            public StringableXmlDictionary atlases;
            public StringableXmlDictionary fonts;
            public StringableXmlDictionary subtemplates;
            public StringableXmlDictionary meshes;

            public ModReplacementData() { }
            internal ModReplacementData(string modId, string displayName, Dictionary<string, string> atlases, Dictionary<string, string> fonts, Dictionary<string, string> subtemplates, Dictionary<string, string> meshes)
            {
                this.modId = modId;
                this.displayName = displayName;
                this.atlases = new(); this.atlases.AddRange(atlases);
                this.fonts = new(); this.fonts.AddRange(fonts);
                this.subtemplates = new(); this.subtemplates.AddRange(subtemplates);
                this.meshes = new(); this.meshes.AddRange(meshes);
            }
        }

        internal ModReplacementData[] GetModsReplacementData()
            => m_modsTemplatesFolder.Where(x => !x.Value.isAsset).Select(x =>
            {
                var modId = x.Key;
                var modName = x.Value.name;
                var atlasesReplacements = MergeDictionaries(modId, m_atlasesMapped, m_atlasesReplacements);
                var fontsReplacements = MergeDictionaries(modId, m_fontsMapped, m_fontsReplacements);
                var subtemplateReplacements = MergeDictionaries(modId, m_subtemplatesMapped, m_subtemplatesReplacements);
                var meshesReplacements = MergeDictionaries(modId, m_meshesMapped, m_meshesReplacements);
                return new ModReplacementData(modId, modName, atlasesReplacements, fontsReplacements, subtemplateReplacements, meshesReplacements);
            }).ToArray();

        private static Dictionary<string, string> MergeDictionaries(string modId, Dictionary<string, HashSet<string>> mapped, Dictionary<string, Dictionary<string, string>> replacements)
        {
            if (mapped.TryGetValue(modId, out var mappedSet))
            {
                mappedSet.RemoveWhere(x => x.StartsWith("__"));
                return replacements.TryGetValue(modId, out var replacementDict)
                    ? mappedSet.ToDictionary(x => x, x => replacementDict.TryGetValue(x, out var data) ? data : null)
                    : mappedSet.ToDictionary(x => x, x => (string)null);
            }
            else
            {
                return new();
            }
        }

        internal string SetModAtlasReplacement(string modId, string original, string target)
        {
            if (m_atlasesMapped.ContainsKey(modId))
            {
                if (!m_atlasesReplacements.TryGetValue(modId, out var atlases))
                {
                    atlases = m_atlasesReplacements[modId] = new();
                }
                if (m_atlasesMapped[modId].Contains(original))
                {
                    IncreaseSpritesAndLayoutsDataVersion();
                    if (target.TrimToNull() is null)
                    {
                        atlases.Remove(original);
                        return null;
                    }
                    return atlases[original] = target.TrimToNull() ?? original;
                }
            }
            return null;
        }
        internal string SetModMeshReplacement(string modId, string original, string target)
        {
            if (m_meshesMapped.ContainsKey(modId))
            {
                if (!m_meshesReplacements.TryGetValue(modId, out var mesh))
                {
                    mesh = m_meshesReplacements[modId] = new();
                }
                if (m_meshesMapped[modId].Contains(original))
                {
                    IncreaseSpritesAndLayoutsDataVersion();
                    if (target.TrimToNull() is null)
                    {
                        mesh.Remove(original);
                        return null;
                    }
                    return mesh[original] = target.TrimToNull() ?? original;
                }
            }
            return null;
        }

        internal void IncreaseSpritesAndLayoutsDataVersion()
        {
            unchecked
            {
                SpritesAndLayoutsDataVersion++;
            }
        }

        internal string SetModFontReplacement(string modId, string original, string target)
        {
            if (m_fontsMapped.TryGetValue(modId, out var mapping) && mapping.Contains(original))
            {
                if (!m_fontsReplacements.TryGetValue(modId, out var fonts))
                {
                    fonts = m_fontsReplacements[modId] = new();
                }
                IncreaseSpritesAndLayoutsDataVersion();
                if (target.TrimToNull() is null)
                {
                    fonts.Remove(original);
                    return null;
                }
                return fonts[original] = target.TrimToNull() ?? original;
            }

            return null;
        }
        internal string SetModSubtemplateReplacement(string modId, string original, string target)
        {
            if (m_subtemplatesMapped.TryGetValue(modId, out var mapping) && mapping.Contains(original))
            {
                if (!m_subtemplatesReplacements.TryGetValue(modId, out var subtemplates))
                {
                    subtemplates = m_subtemplatesReplacements[modId] = new();
                }
                IncreaseSpritesAndLayoutsDataVersion();
                if (target.TrimToNull() is null)
                {
                    subtemplates.Remove(original);
                    return null;
                }
                return subtemplates[original] = target.TrimToNull() ?? original;
            }
            return null;
        }

        internal string SaveReplacementSettings(string fileName)
        {
            KFileUtils.EnsureFolderCreation(SAVED_MODREPLACEMENTS_FOLDER);
            var targetFileName = Path.Combine(SAVED_MODREPLACEMENTS_FOLDER, $"{KFileUtils.RemoveInvalidFilenameChars(fileName)}.{LAYOUT_REPLACEMENTS_EXTENSION}");
            var content = new ModReplacementDataXml
            {
                Mods = GetModsReplacementData()
            };
            File.WriteAllText(targetFileName, XmlUtils.DefaultXmlSerialize(content));
            return targetFileName;
        }

        internal bool CheckReplacementSettingFileExists(string fileName) => File.Exists(Path.Combine(SAVED_MODREPLACEMENTS_FOLDER, $"{KFileUtils.RemoveInvalidFilenameChars(fileName)}.{LAYOUT_REPLACEMENTS_EXTENSION}"));

        internal bool LoadReplacementSettings(string fullPath)
        {
            if (!File.Exists(fullPath)) return false;
            try
            {
                var content = XmlUtils.DefaultXmlDeserialize<ModReplacementDataXml>(File.ReadAllText(fullPath));
                m_atlasesReplacements.Clear();
                m_fontsReplacements.Clear();
                m_subtemplatesReplacements.Clear();
                m_atlasesReplacements.AddRange(m_atlasesMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.atlases ?? new()));
                m_fontsReplacements.AddRange(m_fontsMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.fonts ?? new()));
                m_subtemplatesReplacements.AddRange(m_subtemplatesMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.subtemplates ?? new()));
                IncreaseSpritesAndLayoutsDataVersion();
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal Dictionary<string, string> GetMetadatasFromReplacement(Assembly mainAssembly, string originalLayoutName)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            if (m_subtemplatesMapped.TryGetValue(modId, out var mappings) && mappings.Contains(originalLayoutName))
            {
                bool haveChanges = false;
                var targetTemplateName = GetTemplateFor(WEModIntegrationUtility.GetModAccessName(mainAssembly, originalLayoutName), "", ref haveChanges);
                if (TryGetTargetTemplate(targetTemplateName, out WETextDataXmlTree targetTemplate))
                {
                    return targetTemplate.metadatas.Where(x => x.dll == modId).GroupBy(x => x.refName).ToDictionary(x => x.Key, x => x.First().content);
                }
            }
            return null;
        }

        #endregion
    }

    internal record struct ModTemplateRegistrationData(string name, string id, string prefix, string rootFolder, bool isAsset)
    {
    }
}
