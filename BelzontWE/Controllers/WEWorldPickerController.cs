using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Entities;
using Game.Common;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public partial class WEWorldPickerController : ComponentSystemBase, IBelzontBindable
    {
        private const string PREFIX = "wpicker.";

        #region UI Bindings
        private Action<string, object[]> m_eventCaller;
        private Action<string, Delegate> m_callBinder;
        private WETextDataBaseController[] m_baseControllers;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            m_callBinder = callBinder;
            callBinder($"{PREFIX}getItemsAvailable", GetItemsAvailable);
            callBinder($"{PREFIX}toolPrecisions", () => WEWorldPickerTool.precisionIdx);
            callBinder($"{PREFIX}enableTool", OnEnableTool);
            callBinder($"{PREFIX}addItem", AddItem);
            callBinder($"{PREFIX}removeItem", RemoveItem);
            callBinder($"{PREFIX}changeParent", ChangeParent);
            callBinder($"{PREFIX}cloneAsChild", CloneAsChild);
            callBinder($"{PREFIX}dumpBris", DumpBris);
            callBinder($"{PREFIX}debugAvailable", DebugAvailable);
            if (m_eventCaller != null) InitValueBindings();
        }

        private bool DebugAvailable()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        private void DumpBris()
        {
            WERendererSystem.dumpNextFrame = true;
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            m_eventCaller = eventCaller;
            if (m_callBinder != null) InitValueBindings();
        }

        public void SetupEventBinder(Action<string, Delegate> eventBinder)
        {
        }

        #endregion

        #region UI Calls Fn

        private string[] GetItemsAvailable()
        {
            if (CurrentEntity.Value == Entity.Null)
            {
                return null;
            }
            if (!EntityManager.TryGetBuffer<WESubTextRef>(CurrentEntity.Value, true, out var buffer))
            {
                return new string[0];
            }
            string[] result = new string[buffer.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = $"{EntityManager.GetComponentData<WETextDataMain>(buffer[i].m_weTextData).ItemName.ToString().TrimToNull() ?? "N/A"}";
            }
            return result;
        }


        private bool ChangeParent(Entity sourceLayoutEntity, Entity newParent)
        {
            if (!EntityManager.TryGetComponent<WETextDataMain>(sourceLayoutEntity, out var weData)) return false;
            if (weData.ParentEntity == newParent) return false;
            CloneAsChild(sourceLayoutEntity, newParent);
            DoRemoveItem(sourceLayoutEntity);

            //var parentCheck = newParent;
            //while (EntityManager.TryGetComponent<WETextDataMain>(parentCheck, out var data))
            //{
            //    if (data.ParentEntity == sourceLayoutEntity) return false;
            //    parentCheck = data.ParentEntity;
            //}
            //if (!EntityManager.TryGetComponent<WETextDataMain>(sourceLayoutEntity, out var weData)) return false;

            //if (weData.ParentEntity == newParent) return true;
            //if (weData.TargetEntity != newParent && (!EntityManager.TryGetComponent<WETextDataMain>(newParent, out var weDataParent) || weDataParent.TargetEntity != weData.TargetEntity)) return false;
            //if (!EntityManager.TryGetBuffer<WESubTextRef>(weData.ParentEntity, false, out var buff)) return false;
            //if (!EntityManager.HasBuffer<WESubTextRef>(newParent))
            //{
            //    EntityManager.AddBuffer<WESubTextRef>(newParent);
            //}
            //if (!RemoveSubItemRef(buff, sourceLayoutEntity, false)) return false;
            //var newBuff = EntityManager.GetBuffer<WESubTextRef>(newParent, false);
            //newBuff.Add(new WESubTextRef
            //{
            //    m_weTextData = sourceLayoutEntity
            //});
            //if (weData.SetNewParent(newParent, EntityManager))
            //{
            //    EntityManager.SetComponentData(sourceLayoutEntity, weData);
            //}
            ReloadTree();
            return true;
        }


        private bool CloneAsChild(Entity sourceLayoutEntity, Entity newParent)
        {
            if (!EntityManager.HasComponent<WETextDataMain>(sourceLayoutEntity)) return false;
            if (!EntityManager.HasBuffer<WESubTextRef>(newParent))
            {
                EntityManager.AddBuffer<WESubTextRef>(newParent);
            }
            var newTarget = EntityManager.TryGetComponent<WETextDataMain>(newParent, out var mainParent) ? mainParent.TargetEntity : newParent;
            WELayoutUtility.DoCreateLayoutItem(XmlUtils.CloneViaXml(WETextDataXmlTree.FromEntity(sourceLayoutEntity, EntityManager)), newParent, newTarget, EntityManager);
            ReloadTree();
            return true;
        }

        private void OnEnableTool(Entity e)
        {
            var wpt = World.GetExistingSystemManaged<WEWorldPickerTool>();
            wpt.Select(e);
        }

        #endregion

        #region Bindings settings

        private bool m_initialized = false;
        public MultiUIValueBinding<WETextItemResume[]> CurrentTree { get; private set; }
        public MultiUIValueBinding<Entity> CurrentSubEntity { get; private set; }
        public MultiUIValueBinding<Entity> CurrentEntity { get; private set; }
        public MultiUIValueBinding<int> MouseSensibility { get; private set; }
        public MultiUIValueBinding<int> CurrentPlaneMode { get; private set; }
        public MultiUIValueBinding<int> CurrentMoveMode { get; private set; }
        public MultiUIValueBinding<bool> CurrentItemIsValid { get; private set; }
        public MultiUIValueBinding<bool> CameraLocked { get; private set; }
        public MultiUIValueBinding<bool> CameraRotationLocked { get; private set; }
        public MultiUIValueBinding<bool> ShowProjectionCube { get; private set; }
        public MultiUIValueBinding<string[]> FontList { get; private set; }
        private void InitValueBindings()
        {
            if (m_initialized) return;
            CurrentSubEntity = new(default, $"{PREFIX}{nameof(CurrentSubEntity)}", m_eventCaller, m_callBinder);
            CurrentTree = new(default, $"{PREFIX}{nameof(CurrentTree)}", m_eventCaller, m_callBinder);
            CurrentEntity = new(default, $"{PREFIX}{nameof(CurrentEntity)}", m_eventCaller, m_callBinder);

            CurrentItemIsValid = new(default, $"{PREFIX}{nameof(CurrentItemIsValid)}", m_eventCaller, m_callBinder);
            CurrentPlaneMode = new(default, $"{PREFIX}{nameof(CurrentPlaneMode)}", m_eventCaller, m_callBinder, (x, _) => x % 3); // WEWorldPickerTool.ToolEditMode Count
            CurrentMoveMode = new(default, $"{PREFIX}{nameof(CurrentMoveMode)}", m_eventCaller, m_callBinder, (x, _) => x % 3); // All, Horizontal, Vertical

            MouseSensibility = new(6, $"{PREFIX}{nameof(MouseSensibility)}", m_eventCaller, m_callBinder, (x, _) => x % WEWorldPickerTool.precisionIdx.Length);
            CameraLocked = new(default, $"{PREFIX}{nameof(CameraLocked)}", m_eventCaller, m_callBinder);
            CameraRotationLocked = new(default, $"{PREFIX}{nameof(CameraRotationLocked)}", m_eventCaller, m_callBinder);
            ShowProjectionCube = new(true, $"{PREFIX}{nameof(ShowProjectionCube)}", m_eventCaller, m_callBinder);
            FontList = new(default, $"{PREFIX}{nameof(FontList)}", m_eventCaller, m_callBinder);
            CurrentSubEntity.OnScreenValueChanged += (x) => OnCurrentItemChanged();
            FontList.Value = FontServer.Instance.GetLoadedFontsNames();
            FontList.UpdateUIs();

            FontServer.Instance.OnFontsLoadedChanged += () =>
            {
                FontList.Value = FontServer.Instance.GetLoadedFontsNames();
                FontList.UpdateUIs();
            };

            m_initialized = true;
        }

        public void ForceReload()
        {
            if (IsValidEditingItem())
            {
                m_executionQueue.Enqueue(() => OnCurrentItemChanged(CurrentSubEntity.Value));
            }
        }

        private void OnCurrentItemChanged(Entity entity)
        {
            m_baseControllers ??= ReflectionUtils.GetInterfaceImplementations(typeof(WETextDataBaseController), new[] { GetType().Assembly }).Select(x => World.GetOrCreateSystemManaged(x) as WETextDataBaseController).ToArray();
            foreach (var controller in m_baseControllers)
            {
                controller.OnCurrentItemChanged(entity);
            }
        }

        #endregion

        #region Queue actions

        private readonly Queue<System.Action> m_executionQueue = new();

        internal void EnqueueModification<T, W>(T newVal, Func<T, W, W> x) where W : unmanaged, IComponentData
        {
            if (IsValidEditingItem())
            {
                var subEntity = CurrentSubEntity.Value;
                m_executionQueue.Enqueue(() =>
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"CurrentSubEntity => {subEntity}");
                    var currentItem = EntityManager.GetComponentData<W>(subEntity);
                    //  if (BasicIMod.DebugMode) LogUtils.DoLog($"x = {x}; CurrentSubEntity = {currentItem.ItemName}");
                    currentItem = x(newVal, currentItem);
                    EntityManager.SetComponentData(subEntity, currentItem);
                });
            }
        }

        private void DoWithBuffer<T>(Entity parent, Action<DynamicBuffer<T>> task) where T : unmanaged, IBufferElementData
        {
            if (BasicIMod.DebugMode) LogUtils.DoLog($"!!ad  + {m_EndBarrier} | {parent}");
            if (!EntityManager.TryGetBuffer<T>(parent, false, out var buff))
            {
                buff = EntityManager.AddBuffer<T>(parent);
            }
            task(buff);
            var cmd = m_EndBarrier.CreateCommandBuffer();
            cmd.AddBuffer<T>(parent).CopyFrom(buff);
            cmd.AddComponent<BatchesUpdated>(parent);

        }


        private void AddItem(Entity parent)
        {
            if (CurrentEntity.Value != Entity.Null)
            {
                var targetParent = parent.Index == 0 ? CurrentEntity.Value : parent;
                var currentEntity = CurrentEntity.Value;
                m_executionQueue.Enqueue(() => DoWithBuffer(targetParent,
                   (Action<DynamicBuffer<WESubTextRef>>)((buff) =>
                   {
                       var subref = new WESubTextRef
                       {
                           m_weTextData = EntityManager.CreateEntity(typeof(WEWaitingRendering))
                       };
                       EntityManager.AddComponentData(subref.m_weTextData, WETextDataMain.CreateDefault(currentEntity, targetParent));
                       EntityManager.AddComponentData(subref.m_weTextData, WETextDataMesh.CreateDefault(currentEntity, targetParent));
                       EntityManager.AddComponentData(subref.m_weTextData, WETextDataMaterial.CreateDefault(currentEntity, targetParent));
                       EntityManager.AddComponentData(subref.m_weTextData, WETextDataTransform.CreateDefault(currentEntity, targetParent));
                       EntityManager.AddComponent<WETextComponentValid>(subref.m_weTextData);
                       buff.Add(subref);
                       CurrentSubEntity.ChangeValueWithEffects(subref.m_weTextData);
                       UpdateTree();
                   })));
            }
        }

        public void UpdateTree()
        {
            CurrentTree.Value = GetTextTreeForEntity(CurrentEntity.Value);
        }

        private void RemoveItem()
        {
            if (IsValidEditingItem())
            {
                DoRemoveItem(CurrentSubEntity.Value);
            }
        }

        private void DoRemoveItem(Entity subEntity)
        {
            var main = EntityManager.GetComponentData<WETextDataMain>(subEntity);
            m_executionQueue.Enqueue(() => DoWithBuffer(main.ParentEntity
                , (Action<DynamicBuffer<WESubTextRef>>)((buff) =>
                {
                    if (RemoveSubItemRef(buff, subEntity, true))
                    {
                        CurrentSubEntity.ChangeValueWithEffects(Entity.Null);
                        UpdateTree();
                    }
                })));
        }
        #endregion

        #region Utility
        public bool IsValidEditingItem() => CurrentItemIsValid.Value = CurrentEntity.Value != default && EntityManager.HasComponent<WETextDataMain>(CurrentSubEntity.Value);

        private WETextItemResume[] GetTextTreeForEntity(Entity e)
        {
            if (!EntityManager.TryGetBuffer<WESubTextRef>(e, true, out var refSubs)) return new WETextItemResume[0];
            var result = new WETextItemResume[refSubs.Length];
            for (int i = 0; i < refSubs.Length; i++)
            {
                if (!EntityManager.TryGetComponent<WETextDataMain>(refSubs[i].m_weTextData, out var data) || !EntityManager.TryGetComponent<WETextDataMesh>(refSubs[i].m_weTextData, out var mesh)) continue;
                result[i] = new()
                {
                    name = data.ItemName.ToString(),
                    id = refSubs[i].m_weTextData,
                    type = (int)mesh.TextType,
                    children = GetTextTreeForEntity(refSubs[i].m_weTextData)
                };
            }
            return result;
        }
        private bool RemoveSubItemRef(DynamicBuffer<WESubTextRef> buff, Entity subEntity, bool destroy)
        {
            for (int i = 0; i < buff.Length; i++)
            {
                if (buff[i].m_weTextData == subEntity)
                {
                    if (destroy)
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {subEntity} - subEntity");
                        EntityManager.AddComponent<Game.Common.Deleted>(subEntity);
                    }

                    buff.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public Matrix4x4 CurrentItemMatrix { get; private set; }

        internal void SetCurrentTargetMatrix(Matrix4x4 transformMatrix)
        {
            if (transformMatrix.ValidTRS())
            {
                m_executionQueue.Enqueue(() => CurrentItemMatrix = transformMatrix);
            }
        }

        internal void OnCurrentItemChanged()
        {
            ReloadTree();
            OnCurrentItemChanged(CurrentSubEntity.Value);
        }

        private void ReloadTree()
        {
            CurrentTree.Value = GetTextTreeForEntity(CurrentEntity.Value);
        }
        #endregion

        #region System overrides

        private ModificationEndBarrier m_EndBarrier;

        protected override void OnCreate()
        {
            m_EndBarrier = World.GetExistingSystemManaged<ModificationEndBarrier>();
            GameManager.instance.userInterface.view.Listener.BindingsReleased += () => m_initialized = false;
        }

        public override void Update()
        {
            while (m_executionQueue.TryDequeue(out var action))
            {
                action();
            }
        }

        internal void ReloadTreeDelayed()
        {
            m_executionQueue.Enqueue(() => ReloadTree());
        }
        #endregion
    }
}