using Belzont.Utils;
using Colossal.Entities;
using System;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WETextDataMainController : WETextDataBaseController
    {
        private const string PREFIX = "dataMain.";
        public MultiUIValueBinding<string> CurrentItemName { get; private set; }
        protected override void DoInitValueBindings(Action<string, object[]> EventCaller, Action<string, Delegate> CallBinder)
        {
            CurrentItemName = new(default, $"{PREFIX}{nameof(CurrentItemName)}", EventCaller, CallBinder);
            CurrentItemName.OnScreenValueChanged += (x) => PickerController.EnqueueModification<string, WETextDataMain>(x, (x, currentItem) => { currentItem.ItemName = x.Truncate(24); PickerController.ReloadTreeDelayed(); return currentItem; });
        }

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataMain>(entity, out var main);
            CurrentItemName.Value = main.ItemName.ToString();
        }
    }
}