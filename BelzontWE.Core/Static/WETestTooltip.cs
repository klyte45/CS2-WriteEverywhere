using Belzont.Utils;
using Colossal.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.UI.Tooltip;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public partial class WETestTooltip : TooltipSystemBase
    {
        private ToolSystem m_ToolSystem;
        private TooltipGroup m_tooltipGroup;
        private WETestTool m_WETestTool;
        private NameSystem m_nameSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_WETestTool = World.GetOrCreateSystemManaged<WETestTool>();
            m_nameSystem = World.GetExistingSystemManaged<NameSystem>();
            m_tooltipGroup = new TooltipGroup
            {
                path = "WEPickerTooltipGroup",
                horizontalAlignment = TooltipGroup.Alignment.Start,
                verticalAlignment = TooltipGroup.Alignment.Center,
                children = new List<IWidget>(2)
            };
        }
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool != m_WETestTool || m_WETestTool.HoveredEntity == default)
            {
                return;
            }
            var hoveredEntity = m_WETestTool.HoveredEntity;
            m_tooltipGroup.children.Clear();
            m_tooltipGroup.children.Add(new StringTooltip
            {
                color = TooltipColor.Info,
                path = "main_name",
                value = m_nameSystem.GetName(hoveredEntity).Translate()
            });
            float2 position = WorldToTooltipPos(m_WETestTool.LastPos);
            m_tooltipGroup.position = new float2(position.x + 5f, position.y + 20f);
            if (EntityManager.HasComponent<Owner>(hoveredEntity))
            {
                CheckOwner(hoveredEntity);
            }
            else if (EntityManager.TryGetComponent(hoveredEntity, out Aggregated agg))
            {
                CheckAgg(agg);
            }
            AddGroup(m_tooltipGroup);
        }

        private void CheckAgg(Aggregated agg)
        {
            m_tooltipGroup.children.Add(new StringTooltip
            {
                color = TooltipColor.Success,
                path = "title1_2",
                value = m_nameSystem.GetName(agg.m_Aggregate).Translate()
            });
        }

        private void CheckOwner(Entity hoveredEntity)
        {
            var targetOwner = hoveredEntity;
            while (EntityManager.TryGetComponent(targetOwner, out Owner owner))
            {
                targetOwner = owner.m_Owner;
            }
            if (EntityManager.TryGetComponent(targetOwner, out Aggregated agg))
            {
                targetOwner = agg.m_Aggregate;
            }
            m_tooltipGroup.children.Add(new StringTooltip
            {
                color = TooltipColor.Success,
                path = "title1_1",
                value = m_nameSystem.GetName(targetOwner).Translate()
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

}