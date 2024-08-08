using BelzontWE.UI;
using Colossal.UI.Binding;
using Game.UI;
using Game.UI.InGame;
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
    }
}