#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Game;
using Game.Modding;
using Game.UI.Menu;
using System.Collections.Generic;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod<WEModData>, IMod
    {
        public override string SimpleName => "Write Everywhere";

        public override string SafeName => "WriteEverywhere";

        public override string Acronym => "WE";

        public override string Description => "Write Everywhere for Cities Skylines 2";

        public override WEModData CreateNewModData() => new WEModData();

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<FontServer>(SystemUpdatePhase.Rendering);
        }

        public override void OnDispose()
        {
        }

        public override void DoOnLoad()
        {
        }

        protected override IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections()
        {
            yield break;
        }
    }
}