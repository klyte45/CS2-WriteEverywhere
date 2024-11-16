using Belzont.Utils;
using Game.SceneFlow;
using System;
using Unity.Entities;

namespace BelzontWE
{
    public abstract class WETextDataBaseController : DataBaseController
    {    
        protected WEWorldPickerController PickerController { get; private set; }            

        protected override void OnCreate()
        {
            base.OnCreate();
            PickerController = World.GetOrCreateSystemManaged<WEWorldPickerController>();
        }
        public abstract void OnCurrentItemChanged(Entity newSelection);
    }
}