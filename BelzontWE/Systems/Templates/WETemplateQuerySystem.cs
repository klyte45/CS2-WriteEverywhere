using Game;
using Game.Common;
using Unity.Entities;

namespace BelzontWE
{
    /// <summary>
    /// UI support system responsible for counting template usage,
    /// validating templates, and providing data for UI displays.
    /// This system provides on-demand queries and does not require regular updates.
    /// </summary>
    public partial class WETemplateQuerySystem : SystemBase
    {
        private WETemplateManager m_manager;
        private EntityQuery m_templateBasedEntities;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_manager = World.GetExistingSystemManaged<WETemplateManager>();
            
            m_templateBasedEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEIsPlaceholder>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
        }

        protected override void OnUpdate()
        {
            // This system only provides on-demand queries
            // No regular update needed
        }
    }
}
