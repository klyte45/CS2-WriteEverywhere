using Colossal.Logging;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.Editor;
using Game.UI.Tooltip;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public partial class WEWorldPickerUISystem : UISystemBase
    {
        private const string ModId = "k45-we-vuio";

        private ToolSystem m_ToolSystem;
        private WEWorldPickerController m_WorldPickerController;
        private WEWorldPickerTool m_WorldPickerTool;

      

        public enum CameraMode
        {
            Default,
            PlaneXY,
            PlaneYZ,
            PlaneXZ
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_WorldPickerController = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WEWorldPickerController>();
            m_WorldPickerTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WEWorldPickerTool>();
          
        }
    }

}