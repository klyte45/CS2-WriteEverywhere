using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using Colossal.Entities;
using Game;
using Game.Common;
using Game.Rendering;
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
                        ComponentType.ReadOnly<WEWaitingRenderingComponent>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            }); ;

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

                    var text = weCustomData.GetEffectiveText(EntityManager);
                    if (weCustomData.TextType == WESimulationTextType.Text)
                    {
                        UpdateTextMesh(ref weCustomData, text);
                    }
                    else if (weCustomData.TextType == WESimulationTextType.Image)
                    {
                        UpdateImageMesh(ref weCustomData, text);
                    }
                    EntityManager.SetComponentData(entity, weCustomData);
                    EntityManager.RemoveComponent<WEWaitingRenderingComponent>(entity);
                }
                entities.Dispose();
            }
        }

        private void UpdateImageMesh(ref WETextData weCustomData, string text)
        {
            var bri = m_atlasesLibrary.GetFromLocalAtlases(weCustomData.Atlas, text, true);
            if (bri == null)
            {
                LogUtils.DoWarnLog("IMAGE BRI STILL NULL!!!");
                return;
            }
            weCustomData.UpdateBRI(bri, text);
        }

        private void UpdateTextMesh(ref WETextData weCustomData, string text)
        {
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
    }

}