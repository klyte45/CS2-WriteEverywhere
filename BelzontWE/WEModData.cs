using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Sprites;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;
using System;
using System.Globalization;
using System.Linq;

namespace BelzontWE
{
    [FileLocation("K45_WE_settings")]
    [SettingsUIShowGroupName(kLogSection, kFontsSection, kSpritesSection,
         kToolControlsSection, kItemEditing, kViewPerspectiveSection)]
    [SettingsUIMouseAction(kActionApplyMouse, ActionType.Button, false, false, new string[] { "K45_WE.Tool" })]
    [SettingsUIMouseAction(kActionCancelMouse, ActionType.Button, false, false, new string[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionIncreaseMovementStrenght, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionReduceMovementStrenght, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionAlternateFixedCamera, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionPerspectiveXY, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionPerspectiveZY, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionPerspectiveXZ, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionCycleEditAxisLock, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionToggleLockCameraRotation, ActionType.Button, "K45_WE.Tool")]
    [SettingsUIKeyboardAction(kActionNextText, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionPreviousText, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionMoveLeft, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionMoveRight, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionMoveUp, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionMoveDown, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionRotateClockwise, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    [SettingsUIKeyboardAction(kActionRotateCounterClockwise, ActionType.Button, false, false, usages: new[] { "K45_WE.Tool" })]
    public class WEModData : BasicModData
    {
        const string kFontsSection = "Font";
        const string kKeybindingSection = "Keybinding";
        const string kSpritesSection = "Sprites";
        const string kLayoutDefaultSection = "LayoutDefaults";
        const string kFormattingSection = "Formatting";
        const string kSourcesTab = "SourcesTab";

        const string kToolControlsSection = "ToolControls";
        const string kItemEditing = "ItemEditing";
        const string kViewPerspectiveSection = "ViewPerspective";

        public const string kActionApplyMouse = "K45_WE_MoveText";
        public const string kActionCancelMouse = "K45_WE_RotateText";
        public const string kActionIncreaseMovementStrenght = "K45_WE_PrecisionHigherNum";
        public const string kActionReduceMovementStrenght = "K45_WE_PrecisionLowerNum";
        public const string kActionEnablePicker = "K45_WE_EnablePicker";

        public const string kActionNextText = "K45_WE_NextText";
        public const string kActionPreviousText = "K45_WE_PreviousText";
        public const string kActionMoveLeft = "K45_WE_MoveLeft";
        public const string kActionMoveRight = "K45_WE_MoveRight";
        public const string kActionMoveUp = "K45_WE_MoveUp";
        public const string kActionMoveDown = "K45_WE_MoveDown";
        public const string kActionRotateClockwise = "K45_WE_RotateClockwise";
        public const string kActionRotateCounterClockwise = "K45_WE_RotateCounterClockwise";

        public const string kActionAlternateFixedCamera = "K45_WE_AlternateFixedCamera";
        public const string kActionPerspectiveXY = "K45_WE_PerspectiveXY";
        public const string kActionPerspectiveZY = "K45_WE_PerspectiveZY";
        public const string kActionPerspectiveXZ = "K45_WE_PerspectiveXZ";
        public const string kActionCycleEditAxisLock = "K45_WE_CycleEditAxisLock";
        public const string kActionToggleLockCameraRotation = "K45_WE_ToggleLockCameraRotation";

        private static readonly int[] m_qualityArray = new[] { 50, 75, 100, 125, 150, 200, 400, 800 };
        private static readonly int[] m_framesUpdate = new[] { 0x1f, 0x3f, 0x7f, 0xff, 0x1ff, 0x3ff, 0x7ff };

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
                FontServer.Instance?.OnChangeSizeParam();
            }
        }
        private DropdownItem<int>[] FontQualityValues() => m_qualityArray.Select((x, i) => new DropdownItem<int> { value = i, displayName = $"{x:0}%{(x >= 200 ? $" ({new string('!', x / 200)})" : "")}" }).ToArray();

        [SettingsUIDropdown(typeof(WEModData), nameof(FramesCheckUpdateValues))]
        [SettingsUISection(kSourcesTab, kFontsSection)]
        public int FramesCheckUpdate { get; set; } = 0;
        public int FramesCheckUpdateVal => m_framesUpdate[FramesCheckUpdate];
        private DropdownItem<int>[] FramesCheckUpdateValues() => m_framesUpdate.Select((x, i) => new DropdownItem<int> { value = i, displayName = $"{x + 1:0} Frames {(i == 0 ? $" (Default)" : "")}" }).ToArray();

        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kFontsSection)]
        public bool FontsFolder
        {
            set => RemoteProcess.OpenFolder(FontServer.FontFilesPath);
        }

        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kSpritesSection)]
        public bool SpritesFolder
        {
            set => RemoteProcess.OpenFolder(WEAtlasesLibrary.IMAGES_FOLDER);
        }
        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kSpritesSection, "A")]
        public bool SpritesFolderRefresh
        {
            set => WEAtlasesLibrary.Instance?.LoadImagesFromLocalFolders();
        }

        //[SettingsUIButton]
        //[SettingsUISection(kSourcesTab, kSpritesSection, "A")]
        //public bool ModulesSpritesFolderRefresh
        //{
        //    set => WEAtlasesLibrary.Instance?.LoadImagesFromMods();
        //}

        [SettingsUIButton]
        [SettingsUISection(kSourcesTab, kLayoutDefaultSection)]
        public bool PrefabLayoutsRefresh
        {
            set => WETemplateManager.Instance?.MarkPrefabsDirty();
        }

        //[SettingsUIButton]
        //[SettingsUISection(kSourcesTab, kLayoutDefaultSection)]
        //public bool ModulesSublayoutsRefresh
        //{
        //    set => WETemplateManager.Instance?.ReloadSubtemplates();
        //}

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

        #region Keybinding
        [SettingsUISection(kKeybindingSection, kToolControlsSection)][SettingsUIKeyboardBinding(BindingKeyboard.W, kActionEnablePicker, ctrl: true, shift: true)] public ProxyBinding EnableTool { get; set; }

        [SettingsUISection(kKeybindingSection, kToolControlsSection)][SettingsUIMouseBinding(BindingMouse.Left, kActionApplyMouse)] public ProxyBinding MouseToolMove { get; set; }
        [SettingsUISection(kKeybindingSection, kToolControlsSection)][SettingsUIMouseBinding(BindingMouse.Right, kActionCancelMouse)] public ProxyBinding MouseToolRotate { get; set; }
        [SettingsUISection(kKeybindingSection, kToolControlsSection)][SettingsUIKeyboardBinding(BindingKeyboard.NumpadPeriod, kActionIncreaseMovementStrenght)] public ProxyBinding ToolIncreaseMovementStrenght { get; set; }
        [SettingsUISection(kKeybindingSection, kToolControlsSection)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad0, kActionReduceMovementStrenght)] public ProxyBinding ToolReduceMovementStrenght { get; set; }

        [SettingsUISection(kKeybindingSection, kViewPerspectiveSection)][SettingsUIKeyboardBinding(BindingKeyboard.NumpadEnter, kActionAlternateFixedCamera)] public ProxyBinding AlternateFixedCamera { get; set; }
        [SettingsUISection(kKeybindingSection, kViewPerspectiveSection)][SettingsUIKeyboardBinding(BindingKeyboard.NumpadDivide, kActionToggleLockCameraRotation)] public ProxyBinding ToggleLockCameraRotation { get; set; }
        [SettingsUISection(kKeybindingSection, kViewPerspectiveSection)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad1, kActionPerspectiveXY)] public ProxyBinding ActionPerspectiveXY { get; set; }
        [SettingsUISection(kKeybindingSection, kViewPerspectiveSection)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad2, kActionPerspectiveZY)] public ProxyBinding ActionPerspectiveZY { get; set; }
        [SettingsUISection(kKeybindingSection, kViewPerspectiveSection)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad3, kActionPerspectiveXZ)] public ProxyBinding ActionPerspectiveXZ { get; set; }
        [SettingsUISection(kKeybindingSection, kViewPerspectiveSection)][SettingsUIKeyboardBinding(BindingKeyboard.NumpadMultiply, kActionCycleEditAxisLock)] public ProxyBinding ActionCycleAxisLock { get; set; }

        //[SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.NumpadPlus, kActionNextText)] public ProxyBinding ActionNextText { get; set; }
        //[SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.NumpadMinus, kActionPreviousText)] public ProxyBinding ActionPreviousText { get; set; }

        [SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad4, kActionMoveLeft)] public ProxyBinding ActionMoveLeft { get; set; }
        [SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad6, kActionMoveRight)] public ProxyBinding ActionMoveRight { get; set; }
        [SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad8, kActionMoveUp)] public ProxyBinding ActionMoveUp { get; set; }
        [SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad5, kActionMoveDown)] public ProxyBinding ActionMoveDown { get; set; }
        [SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad9, kActionRotateClockwise)] public ProxyBinding ActionRotateClockwise { get; set; }
        [SettingsUISection(kKeybindingSection, kItemEditing)][SettingsUIKeyboardBinding(BindingKeyboard.Numpad7, kActionRotateCounterClockwise)] public ProxyBinding ActionRotateCounterClockwise { get; set; }
        #endregion
    }

}
