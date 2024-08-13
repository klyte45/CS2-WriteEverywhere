﻿using Belzont.Interfaces;
using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEFontManagementController : SystemBase, IBelzontBindable
    {
        private const string PREFIX = "fonts.";
        private FontServer m_fontServer;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}requireFontInstallation", RequireFontInstallation);
            callBinder($"{PREFIX}listCityFonts", ListCityFonts);
            callBinder($"{PREFIX}checkFontExists", CheckFontExists);
            callBinder($"{PREFIX}getFontDetail", GetFontDetail);
            callBinder($"{PREFIX}renameCityFont", RenameCityFont);
            callBinder($"{PREFIX}deleteCityFont", DeleteCityFont);
            callBinder($"{PREFIX}duplicateCityFont", DuplicateCityFont);
        }

        public void SetupCaller(Action<string, object[]> eventCaller) { }

        public void SetupEventBinder(Action<string, Delegate> eventBinder) { }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_fontServer = World.GetOrCreateSystemManaged<FontServer>();
        }

        protected override void OnUpdate() { }

        private bool CheckFontExists(string name) => m_fontServer.FontExists(name);

        private string RequireFontInstallation(string path)
        {
            if (!File.Exists(path)) return "";
            var name = Regex.Replace(Path.GetFileNameWithoutExtension(path), "[^A-Za-z0-9_]", "_").Truncate(30);
            if (FontServer.Instance.FontExists(name))
            {
                var i = 1;
                var baseName = name;
                do
                {
                    baseName = baseName.Truncate(29 - i.ToString().Length);
                    name = $"{baseName}_{i}";
                } while (FontServer.Instance.FontExists(name));
            }
            return FontServer.Instance.RegisterFont(name, File.ReadAllBytes(path)) ? name : null;
        }

        private Dictionary<string, bool> ListCityFonts() => m_fontServer.GetLoadedFontsNames().ToDictionary(x => x, x => x == FontServer.DEFAULT_FONT_KEY);

        private FontDetailResponse GetFontDetail(string name)
            => name == null || !m_fontServer.TryGetFontEntity(name, out var entity)
                ? null
                : new()
                {
                    name = name,
                    index = entity.Index
                };

        private void RenameCityFont(string oldName, string newName) => m_fontServer.RenameFont(oldName, newName);
        private void DeleteCityFont(string name) => m_fontServer.DestroyFont(name);
        private void DuplicateCityFont(string srcName, string newName) => m_fontServer.DuplicateFont(srcName, newName);

        private class FontDetailResponse
        {
            public string name;
            public int index;
        }
    }
}