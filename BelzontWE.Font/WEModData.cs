using Belzont.Interfaces;
using Belzont.Utils;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;
using System;
using System.Globalization;
using System.Linq;

namespace BelzontWE
{
    [FileLocation("ModsData\\Klyte45Mods\\WriteEverywhere\\settings")]
    [SettingsUIShowGroupName(kLogSection, kFontsSection, kSpritesSection)]
    public class WEModData : BasicModData
    {
        const string kFontsSection = "Font";
        const string kSpritesSection = "Sprites";
        const string kFormattingSection = "Formatting";
        const string kSourcesTab = "SourcesTab";
        private static readonly int[] m_qualityArray = new[] { 50, 75, 100, 125, 150, 200, 400, 800 };

        public static WEModData InstanceWE => Instance as WEModData;
        public WEModData(IMod mod) : base(mod)
        {
        }
        public override void OnSetDefaults()
        {
        }

        [SettingsUIDropdown(typeof(WEModData), nameof(StartTextureSizeFontValues))]
        [SettingsUISection(kSourcesTab, kFontsSection)]
        public int StartTextureSizeFont
        {
            get => startTextureSizeFont; set
            {
                startTextureSizeFont = value;
                FontServer.Instance?.OnChangeSizeParam();
            }
        }
        private DropdownItem<int>[] StartTextureSizeFontValues() => new int[5].Select((_, i) => new DropdownItem<int> { value = i, displayName = $"{512 << i}x{512 << i}" }).ToArray();


        private int fontQualityIdx = 2;
        [SettingsUIDropdown(typeof(WEModData), nameof(FontQualityValues))]
        [SettingsUISection(kSourcesTab, kFontsSection)]
        public int FontQuality
        {
            get => fontQualityIdx; set
            {
                fontQualityIdx = value;
                FontServer.QualitySize = m_qualityArray[value];
            }
        }
        private DropdownItem<int>[] FontQualityValues() => m_qualityArray.Select((x, i) => new DropdownItem<int> { value = i, displayName = $"{x:0}%{(x >= 200 ? $" ({new string('!', x / 200)})" : "")}" }).ToArray();

        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kFontsSection)]
        public bool FontsFolder
        {
            set => RemoteProcess.OpenFolder(FontServer.FontFilesPath);
        }

        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kSpritesSection)]
        [SettingsUIDisableByCondition(typeof(WEModData), nameof(AlwaysDisabled))]
        public bool SpritesFolder
        {
            set { }
        }
        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kSpritesSection)]
        [SettingsUIDisableByCondition(typeof(WEModData), nameof(AlwaysDisabled))]
        public bool SpritesFolderRefresh
        {
            set { }
        }

        [SettingsUISection(kSourcesTab, kFormattingSection)]
        [SettingsUITextInput]
        public string LocaleFomatting
        {
            get => localeFomatting; set
            {
                localeFomatting = value;
                m_cachedCulture = null;
                FormatCulture.ClearCachedData();
            }
        }
        [SettingsUISection(kSourcesTab, kFormattingSection)] public string LocaleName => FormatCulture.DisplayName;
        [SettingsUISection(kSourcesTab, kFormattingSection)] public string LocaleDateFormat => new DateTime().ToString(FormatCulture);
        [SettingsUISection(kSourcesTab, kFormattingSection)] public string LocaleNumberFormat => 1234567.7869.ToString("#,##.000", FormatCulture);

        private CultureInfo m_cachedCulture;
        private string localeFomatting = GameManager.instance.localizationManager.activeLocaleId;
        private int startTextureSizeFont = 1;

        internal CultureInfo FormatCulture
        {
            get
            {
                if (m_cachedCulture is null)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Updating WE Locale to: {localeFomatting}");
                    try
                    {
                        m_cachedCulture = CultureInfo.GetCultureInfo(localeFomatting);
                        if (m_cachedCulture.IsNeutralCulture)
                        {
                            if (BasicIMod.DebugMode) LogUtils.DoLog($"Falling back because it's neutral");
                            m_cachedCulture = CultureInfo.GetCultureInfo("en-US");
                        }
                    }
                    catch (Exception e)
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"FALLING BACK BY ERROR! {e}");
                        m_cachedCulture = CultureInfo.GetCultureInfo("en-US");
                    }
                }
                return m_cachedCulture;
            }
            set
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Setting WE Locale to: {value?.TwoLetterISOLanguageName} ({value?.DisplayName})");
                if (!value.IsNeutralCulture)
                {
                    m_cachedCulture = value;
                    InstanceWE.localeFomatting = value.ToString();
                }
            }
        }


        //[SettingsUIButton]
        //[SettingsUISection(kSourcesTab, kFormattingSection)]
        //[SettingsUIDisableByCondition(typeof(WEModData), nameof(AlwaysDisabled))]
        //public bool SpritesFolderRefresh
        //{
        //    set { }
        //}
        private bool AlwaysDisabled() => true;
    }

}
