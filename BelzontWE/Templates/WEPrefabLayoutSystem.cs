using Game;
using Game.Prefabs;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    /// <summary>
    /// File I/O system responsible for loading templates from disk,
    /// managing prefab template associations, and loading mod-provided templates.
    /// </summary>
    [UpdateAfter(typeof(WETemplateUpdateSystem))]
    public partial class WEPrefabLayoutSystem : SystemBase
    {
        private WETemplateManager m_manager;
        private PrefabSystem m_prefabSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_manager = World.GetExistingSystemManaged<WETemplateManager>();
            m_prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
        }

        protected override void OnUpdate()
        {
            // Will implement loading logic here in Phase 4
        }
    }
}
