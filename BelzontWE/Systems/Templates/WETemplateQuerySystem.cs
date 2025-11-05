using Belzont.Utils;
using Game;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
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

        /// <summary>
        /// Counts how many entities are using a specific city template
        /// </summary>
        public unsafe int GetCityTemplateUsageCount(string name)
        {
            if (m_templateBasedEntities.IsEmpty) return 0;
            
            if (!m_manager.TryGetCityTemplate(name, out var templateEntity)) 
                return 0;
            
            var counterResult = 0;
            var job = new WEPlaceholderTemplateUsageCount
            {
                m_templateToCheck = templateEntity.Guid,
                m_updaterHdl = GetBufferTypeHandle<WETemplateUpdater>(),
                m_counter = &counterResult
            };
            
            job.Schedule(m_templateBasedEntities, Dependency).Complete();
            return counterResult;
        }

        /// <summary>
        /// Validates if a template tree can be used as a prefab layout
        /// </summary>
        public int CanBePrefabLayout(WETextDataXmlTree data) => CanBePrefabLayout(data, true);

        /// <summary>
        /// Validates if a template tree can be used as a prefab layout
        /// </summary>
        public int CanBePrefabLayout(WETextDataXmlTree data, bool isRoot) 
            => CanBePrefabLayout(data.self, data.children, isRoot);

        /// <summary>
        /// Validates if a selfless template tree can be used as a prefab layout
        /// </summary>
        public int CanBePrefabLayout(WESelflessTextDataTree data) 
            => CanBePrefabLayout(null, data.children, true);

        private int CanBePrefabLayout(WETextDataXml self, WETextDataXmlTree[] children, bool isRoot)
        {
            if (isRoot)
            {
                if (children?.Length > 0)
                {
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (CanBePrefabLayout(children[i], false) != 0)
                        {
                            LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: A child node ({i}) failed validation");
                            return 2;
                        }
                    }
                }
                else
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: The root node had no children. When exporting for prefabs, the selected node must have children typed as Placeholder items. The root setting itself is ignored.");
                    return 1;
                }
            }
            else
            {
                if (self.layoutMesh is null && self.imageMesh is null && self.textMesh is null 
                    && self.whiteMesh is null && self.whiteCubeMesh is null && self.matrixTransform is null)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: All children must have type 'Placeholder', 'WhiteTexture', 'MatrixTransform', 'WhiteCube', 'Image' or 'Text'.");
                    return 4;
                }
                
                if (self.layoutMesh is not null && children?.Length > 0)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: The node must not have children, as any Placeholder item don't.");
                    return 5;
                }
            }
            return 0;
        }

        [BurstCompile]
        private unsafe struct WEPlaceholderTemplateUsageCount : IJobChunk
        {
            public Colossal.Hash128 m_templateToCheck;
            public BufferTypeHandle<WETemplateUpdater> m_updaterHdl;
            public int* m_counter;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, 
                bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var templates = chunk.GetBufferAccessor(ref m_updaterHdl);
                for (int i = 0; i < templates.Length; i++)
                {
                    for (int j = 0; j < templates[i].Length; j++)
                    {
                        if (templates[i][j].templateEntity == m_templateToCheck) 
                            *m_counter += 1;
                    }
                }
            }
        }
    }
}
