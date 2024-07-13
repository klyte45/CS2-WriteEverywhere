using Game.Input;
using Game.UI;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WEUISystem : UISystemBase
    {
        private WEWorldPickerTool m_pickerTool;
        private ProxyAction m_enableToolAction;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_pickerTool = World.GetOrCreateSystemManaged<WEWorldPickerTool>();
            m_enableToolAction = WEModData.Instance.GetAction(WEModData.kActionEnablePicker);
        }

        protected override void OnStartRunning()
        {
            m_enableToolAction.shouldBeEnabled = true;
        }
        protected override void OnStopRunning()
        {
            m_enableToolAction.shouldBeEnabled = false;
        }

        protected override void OnUpdate()
        {
            if (!m_pickerTool.IsSelected && m_enableToolAction.WasPerformedThisFrame())
            {
                m_pickerTool.Select(Entity.Null);
            }
        }
    }
}