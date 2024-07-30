using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using WriteEverywhere.Sprites;

namespace BelzontWE
{
    public partial class WEPreRendererSystem : SystemBase
    {
        private EntityQuery m_pendingQueueEntities;
        private EndFrameBarrier m_endFrameBarrier;
        private WEAtlasesLibrary m_atlasesLibrary;
        private WETemplateManager m_templateManager;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_atlasesLibrary = World.GetOrCreateSystemManaged<WEAtlasesLibrary>();
            m_pendingQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextData>(),
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            }); ;

            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            RequireAnyForUpdate(m_pendingQueueEntities);
        }
        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            CheckPendingQueue();

        }

        private void CheckPendingQueue()
        {
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_pendingQueueEntities.ToEntityArray(Allocator.TempJob);
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = EntityManager.GetComponentData<WETextData>(entity);
                    if (!EntityManager.Exists(weCustomData.TargetEntity) || (weCustomData.TargetEntity == Entity.Null && !EntityManager.HasComponent<WETemplateData>(entity)))
                    {
                        LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
                        EntityManager.DestroyEntity(entity);
                        continue;
                    }
                    if (weCustomData.TextType == WESimulationTextType.Text)
                    {
                        var text = weCustomData.GetEffectiveText(EntityManager);
                        UpdateTextMesh(entity, ref weCustomData, text);
                    }
                    else if (weCustomData.TextType == WESimulationTextType.Image)
                    {
                        var text = weCustomData.GetEffectiveText(EntityManager);
                        UpdateImageMesh(entity, ref weCustomData, text);
                    }
                    else if (weCustomData.TextType == WESimulationTextType.Placeholder)
                    {
                        UpdatePlaceholder(entity, ref weCustomData);
                    }
                    EntityManager.SetComponentData(entity, weCustomData);
                    EntityManager.RemoveComponent<WEWaitingRendering>(entity);
                }
                entities.Dispose();
            }
        }
        private void UpdatePlaceholder(Entity e, ref WETextData weCustomData)
        {
            if (!SetupTemplateComponent(e, ref weCustomData))
            {
                var targetTemplate = m_templateManager[weCustomData.ItemName.ToString()];
                if (EntityManager.TryGetComponent<WETemplateUpdater>(e, out var templateUpdated) && templateUpdated.childEntity != Entity.Null)
                {
                    LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
                    EntityManager.DestroyEntity(templateUpdated.childEntity);
                }

                var newData = new WETemplateUpdater()
                {
                    templateEntity = targetTemplate,
                    childEntity = targetTemplate == Entity.Null ? Entity.Null : WELayoutUtility.DoCloneTextItem(targetTemplate, e, EntityManager, Entity.Null)
                };
                LogUtils.DoLog($"Cloned info! {weCustomData.ItemName} => {targetTemplate}");

                if (EntityManager.HasComponent<WETemplateUpdater>(e))
                {
                    EntityManager.SetComponentData(e, newData);
                }
                else
                {
                    EntityManager.AddComponentData(e, newData);
                }
            }
        }
        private void UpdateImageMesh(Entity e, ref WETextData weCustomData, string text)
        {
            if (EntityManager.HasComponent<WETemplateUpdater>(e)) EntityManager.RemoveComponent<WETemplateUpdater>(e);
            SetupTemplateComponent(e, ref weCustomData);
            var bri = m_atlasesLibrary.GetFromLocalAtlases(weCustomData.Atlas, text, true);
            if (bri == null)
            {
                LogUtils.DoWarnLog("IMAGE BRI STILL NULL!!!");
                return;
            }
            weCustomData.UpdateBRI(bri, text);
        }


        private void UpdateTextMesh(Entity e, ref WETextData weCustomData, string text)
        {
            if (EntityManager.HasComponent<WETemplateUpdater>(e)) EntityManager.RemoveComponent<WETemplateUpdater>(e);
            SetupTemplateComponent(e, ref weCustomData);
            var font = EntityManager.TryGetComponent<FontSystemData>(weCustomData.Font, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
            if (font.Font == null)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog("Font not initialized!!!");
                return;
            }

            var bri = font.FontSystem.DrawText(text, FontServer.Instance.ScaleEffective);
            if (bri == null)
            {
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog("BRI STILL NULL!!!");
                return;
            }
            weCustomData.UpdateBRI(bri, text);

        }

        private bool SetupTemplateComponent(Entity e, ref WETextData weCustomData)
        {
            if (EntityManager.HasComponent<WETemplateData>(weCustomData.TargetEntity))
            {
                if (!EntityManager.HasComponent<WETemplateData>(e)) EntityManager.AddComponent<WETemplateData>(e);
                return true;
            }
            if (EntityManager.HasComponent<WETemplateData>(e)) EntityManager.RemoveComponent<WETemplateData>(e);
            return false;
        }
    }

}