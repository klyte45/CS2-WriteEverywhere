using BelzontWE.UI;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.UI;
using Game.UI.Editor;
using Game.UI.InGame;
using System;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEMainUISystem : UISystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            var panelSystem = World.GetOrCreateSystemManaged<GamePanelUISystem>();
            AddBinding(new TriggerBinding<int>("k45::we.main", "setTabActive", (x) => panelSystem.ShowPanel<WEMainPanel>(x)));
        }

        private bool intialized;

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode == GameMode.Editor && !intialized)
            {
                intialized = true;
                EditorToolUISystem editorToolUISystem = World.GetExistingSystemManaged<EditorToolUISystem>();
                var newTool = new WEEditorTool(World);
                var tools = editorToolUISystem.tools;
                Array.Resize(ref tools, tools.Length + 1);
                tools[^1] = newTool;
                editorToolUISystem.tools = tools;
            }
        }

    }
}