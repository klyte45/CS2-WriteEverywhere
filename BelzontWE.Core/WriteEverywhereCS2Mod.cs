#define BURST
//#define VERBOSE 
using Belzont.Interfaces;
using Belzont.Utils;
using Game;
using Game.Common;
using Game.Input;
using Game.Modding;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Tooltip;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public class WriteEverywhereCS2Mod : BasicIMod<WEModData>, IMod
    {
        public override string SimpleName => "Write Everywhere";

        public override string SafeName => "WriteEverywhere";

        public override string Acronym => "WE";

        public override string Description => "Write Everywhere for Cities Skylines 2";

        public override WEModData CreateNewModData() => new();

        public override void DoOnCreateWorld(UpdateSystem updateSystem)
        {
            updateSystem.UpdateBefore<FontServer>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<WETestTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WETestTooltip>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAfter<WERendererSystem>(SystemUpdatePhase.Rendering);
        }

        public override void OnDispose()
        {
        }

        public override void DoOnLoad()
        {
        }

        protected override IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections()
        {
            yield break;
        }
    }

    public struct WECustomComponent : IComponentData
    {
        public FixedString512Bytes fontName;
        public FixedString512Bytes text;
        public Vector3 offsetPosition;
        public Quaternion offsetRotation;
        public Vector3 scale;
    }

    public partial class WETestTool : ToolBaseSystem
    {
        public override string toolID => $"K45_WE_{GetType().Name}";
        private Func<Entity, bool> callback;

        private struct AddMyCustomComponentJob : IJob
        {
            public WECustomComponent myCustomComponent;
            public EntityCommandBuffer buffer;
            public Entity entity;
            public void Execute()
            {
                buffer.AddComponent<WECustomComponent>(entity);
                buffer.SetComponent(entity, myCustomComponent);
                buffer.AddComponent<Highlighted>(entity);
                LogUtils.DoInfoLog($"[WETestTool.AddMyCustomComponentJob] Scheduled MyCustomComponent to be added to Entity.Index = " + entity.Index.ToString() + " to Entity.Version = " + entity.Version.ToString());
            }
        }

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }


        private ProxyAction m_ApplyAction;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private WETestController m_Controller;

        protected override void OnCreate()
        {
            Enabled = false;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            LogUtils.DoLog("{MyTool.OnCreate} MyTool Created.");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_Controller = World.GetOrCreateSystemManaged<WETestController>();
            base.OnCreate();
        }
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
        }
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
        }
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.MovingObjects;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_ApplyAction.WasPressedThisFrame())
            {
                bool flag = GetRaycastResult(out Entity e, out RaycastHit hit);
                if (flag && !(callback?.Invoke(e) ?? true))
                {
                    RequestDisable();
                }
            }
            return inputDeps;
        }
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }
        public void Select()
        {
            m_ToolSystem.activeTool = this;
        }

        public void SetCallbackAndEnable(Func<Entity, bool> callback)
        {
            this.callback = callback;
            Select();
        }
    }

    public partial class WETestTooltip : TooltipSystemBase
    {
        private ToolSystem m_ToolSystem;
        private StringTooltip m_Tooltip;
        private WETestTool m_WETestTool;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            m_WETestTool = base.World.GetOrCreateSystemManaged<WETestTool>();
            m_Tooltip = new StringTooltip
            {
                path = "Tooltip.LABEL[XX.MyTool]"
            };
        }
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool != m_WETestTool)
            {
                return;
            }
            m_Tooltip.value = LocalizedString.IdWithFallback("Tooltip.LABEL[XX.MyTool]", "My Tool");
            AddMouseTooltip(m_Tooltip);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

    public class WETestController : ComponentSystemBase, IBelzontBindable
    {
        private Action<string, object[]> eventCaller;
        private WETestTool m_WETestTool;
        private FontServer m_FontServer;
        private EndFrameBarrier m_EndFrameBarrier;

        private Entity targetEntity;
        private string targetString;
        private string targetFont;

        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            eventCaller("test.enableTestTool", EnableTestTool);
            eventCaller("test.reloadFonts", ReloadFonts);
            eventCaller("test.listFonts", ListFonts);
            eventCaller("test.requestTextMesh", RequestTextMesh);
            eventCaller("test.listShaderDatails", ListShaders);
        }

        private Dictionary<string, Dictionary<string, string>> ListShaders()
        {
            var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var shader in allShaders)
            {
                result[shader.name] = new Dictionary<string, string>();
                var count = shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    result[shader.name][shader.GetPropertyName(i)] = shader.GetPropertyType(i).ToString();
                }
            }
            return result;
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            this.eventCaller = eventCaller;
        }

        public void SetupEventBinder(Action<string, Delegate> eventCaller)
        {
        }
        protected override void OnCreate()
        {
            m_WETestTool = World.GetExistingSystemManaged<WETestTool>();
            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            m_EndFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_FontServer.OnFontsLoadedChanged += () => SendToFrontend("test.fontsChanged->", new object[] { ListFonts() });
            base.OnCreate();
        }
        public override void Update()
        {


        }

        private void SendToFrontend(string eventName, params object[] args) => eventCaller?.Invoke(eventName, args);

        private void EnableTestTool()
        {
            m_WETestTool.SetCallbackAndEnable((e) =>
            {
                SendToFrontend("test.enableTestTool->", e);
                var prevEntity = targetEntity;
                targetEntity = e;
                UpdateDataAtEntity(prevEntity);
                return false;
            });
        }

        private void ReloadFonts()
        {
            m_FontServer.ReloadFontsFromPath();
        }

        private void UpdateDataAtEntity(Entity previousEntity)
        {
            if (targetEntity != Entity.Null && targetString != null && targetFont != null)
            {
                var buff = m_EndFrameBarrier.CreateCommandBuffer();
                if (previousEntity != Entity.Null && EntityManager.HasComponent<WECustomComponent>(previousEntity))
                {
                    buff.RemoveComponent<WECustomComponent>(previousEntity);
                }
                if (EntityManager.HasComponent<WECustomComponent>(targetEntity))
                {
                    var currentComponent = EntityManager.GetComponentData<WECustomComponent>(targetEntity);
                    currentComponent.fontName = targetFont;
                    currentComponent.text = targetString;
                    buff.SetComponent(targetEntity, currentComponent);
                }
                else
                {
                    var newComponent = new WECustomComponent
                    {
                        fontName = targetFont,
                        text = targetString,
                        offsetPosition = Vector3.up * 2,
                        scale = Vector3.one * 4
                    };
                    buff.AddComponent(targetEntity, newComponent);
                }
            }
        }

        private string[] ListFonts() => m_FontServer.GetAllFonts()?.ToArray();

        private string RequestTextMesh(string text, string fontName)
        {
            targetFont = fontName;
            targetString = text;
            UpdateDataAtEntity(Entity.Null);
            var result = m_FontServer[fontName]?.DrawString(text, Vector2.one);
            if (result is null) return null;

            return XmlUtils.DefaultXmlSerialize(result);
        }
    }

    public partial class WERendererSystem : SystemBase
    {
        private FontServer m_FontServer;
        private EntityQuery m_renderQueueEntities;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            m_renderQueueEntities = GetEntityQuery(new EntityQueryDesc[]
{
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<WECustomComponent>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
});
        }
        protected override void OnUpdate()
        {
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_renderQueueEntities.ToEntityArray(Allocator.TempJob);
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var cullInfo = EntityManager.GetComponentData<CullingInfo>(entity);
                    var transform = EntityManager.GetComponentData<InterpolatedTransform>(entity);
                    var weCustomData = EntityManager.GetComponentData<WECustomComponent>(entity);

                    var mesh = m_FontServer[weCustomData.fontName.ToString()].DrawString(weCustomData.text.ToString(), default);
                    if (mesh == null)
                    {
                        LogUtils.DoLog("MESH IS NULL !!!!");
                        continue;
                    }

                    float3 float2 = transform.m_Position + math.rotate(transform.m_Rotation, weCustomData.offsetPosition);
                    quaternion quaternion = math.mul(transform.m_Rotation, weCustomData.offsetRotation * Quaternion.Euler(0, 180, 0));
                    Graphics.DrawMesh(mesh.m_mesh, Matrix4x4.TRS(float2, quaternion, weCustomData.scale * mesh.m_offsetScaleX / mesh.m_pixelDensityMeters), mesh.m_generatedMaterial, 0);
                }
                entities.Dispose();
            }
        }

    }

}