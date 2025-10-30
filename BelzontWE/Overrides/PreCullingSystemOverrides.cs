using Belzont.Utils;
using Colossal.Collections;
using Game.Rendering;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace BelzontWE
{
    public class PreCullingSystemOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(PreCullingSystem).GetMethod("OnUpdate", RedirectorUtils.allFlags), null, null, GetType().GetMethod(nameof(TranspileOnUpdate), RedirectorUtils.allFlags));
            LogUtils.DoInfoLog("PreCullingSystem OnUpdate transpiler applied.");
        }
        private static IEnumerable<CodeInstruction> TranspileOnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var instrList = new List<CodeInstruction>(instructions);

            // Validate that our CullingAction struct is still compatible with PreCullingSystem.CullingAction
            if (!ValidateCullingActionCompatibility())
            {
                LogUtils.DoErrorLog("CullingAction struct layout mismatch! The game's PreCullingSystem.CullingAction has changed and is no longer compatible with our implementation.");
                return instrList; // Return unmodified instructions to avoid crashes
            }

            // Find the local variable index for nativeParallelQueue
            int queueLocalIndex = FindNativeParallelQueueLocalIndex(original, out var originalType);
            if (queueLocalIndex == -1)
            {
                LogUtils.DoErrorLog("Failed to find NativeParallelQueue local variable in PreCullingSystem.OnUpdate");
                return instrList;
            }

            LogUtils.DoInfoLog($"Found NativeParallelQueue at local variable index {queueLocalIndex}");

            // Find the call to jobData8.Schedule (which is CullingActionJob.Schedule)
            // We need to inject our call before this, replacing the dependency parameter

            for (int i = 0; i < instrList.Count; i++)
            {
                // Look for the pattern where jobData8.Schedule is called
                // The pattern is: load nativeParallelQueue.HashRange, load 1, load this.m_WriteDependencies, call Schedule
                if (instrList[i].opcode == OpCodes.Call && instrList[i].operand is System.Reflection.MethodInfo method)
                {
                    if (method.Name == "Schedule" && method.GetParameters().Length == 4)
                    {
                        // We found a Schedule call with 4 parameters
                        // Now we need to inject our call before the last parameter (m_WriteDependencies)
                        // The stack at this point should have: jobData8, HashRange, 1, m_WriteDependencies

                        // Look backwards to find where m_WriteDependencies is loaded
                        int dependencyLoadIndex = -1;
                        for (int j = i - 1; j >= 0 && j >= i - 10; j--)
                        {
                            if (instrList[j].opcode == OpCodes.Ldfld &&
                                instrList[j].operand is System.Reflection.FieldInfo field &&
                                field.Name == "m_WriteDependencies")
                            {
                                dependencyLoadIndex = j;
                                break;
                            }
                        }

                        if (dependencyLoadIndex != -1)
                        {
                            // Found it! Now we need to modify the sequence
                            // Remove the old m_WriteDependencies load
                            var loadThisBeforeDep = instrList[dependencyLoadIndex - 1]; // Should be ldarg.0 or similar
                            instrList.RemoveAt(dependencyLoadIndex);
                            instrList.RemoveAt(dependencyLoadIndex - 1);
                            i -= 2; // Adjust index

                            var newInstructions = new List<CodeInstruction>
                            {
                                // Load nativeParallelQueue address (pointer)
                                new(OpCodes.Ldloca, queueLocalIndex),
                                // Load this.m_WriteDependencies
                                new(OpCodes.Ldarg_0), // this
                                new(OpCodes.Ldfld,
                                typeof(PreCullingSystem).GetField("m_WriteDependencies", RedirectorUtils.allFlags)),

                                // Load this (PreCullingSystem instance)
                                new(OpCodes.Ldarg_0), // this

                                // Call our method
                                new(OpCodes.Call,
                                typeof(PreCullingSystemOverrides).GetMethod(nameof(CallUpdateCallerInTheMiddle),RedirectorUtils.allFlags))
                            };

                            // Insert before the Schedule call
                            instrList.InsertRange(i, newInstructions);

                            break; // We're done
                        }
                    }
                }
            }
            LogUtils.PrintMethodIL(instrList);
            return instrList;
        }

        private static int FindNativeParallelQueueLocalIndex(MethodBase method, out Type localType)
        {
            var methodBody = method.GetMethodBody();
            localType = null;
            if (methodBody == null) return -1;

            var locals = methodBody.LocalVariables;
            for (int i = 0; i < locals.Count; i++)
            {
                localType = locals[i].LocalType;
                if (localType.IsGenericType &&
                    localType.GetGenericTypeDefinition().Name.Contains("NativeParallelQueue") &&
                    localType.GetGenericArguments().Length == 1)
                {
                    var genericArg = localType.GetGenericArguments()[0];
                    // Check if the generic argument is PreCullingSystem.CullingAction
                    if (genericArg.DeclaringType == typeof(PreCullingSystem) &&
                        genericArg.Name == "CullingAction")
                    {
                        return i;
                    }
                }
            }
            localType = null;
            return -1;
        }

        private static bool ValidateCullingActionCompatibility()
        {
            try
            {
                // Get the original CullingAction type from PreCullingSystem
                var originalCullingActionType = typeof(PreCullingSystem).GetNestedType("CullingAction", RedirectorUtils.allFlags);
                if (originalCullingActionType == null)
                {
                    LogUtils.DoErrorLog("Could not find PreCullingSystem.CullingAction type");
                    return false;
                }

                // Get fields from both types
                var originalFields = originalCullingActionType.GetFields(RedirectorUtils.allFlags);
                var ourFields = typeof(CullingAction).GetFields(RedirectorUtils.allFlags);

                // Check field count matches
                if (originalFields.Length != ourFields.Length)
                {
                    LogUtils.DoErrorLog($"Field count mismatch: Original has {originalFields.Length} fields, ours has {ourFields.Length}");
                    return false;
                }

                // Check struct size matches
                var originalSize = System.Runtime.InteropServices.Marshal.SizeOf(originalCullingActionType);
                var ourSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CullingAction));
                if (originalSize != ourSize)
                {
                    LogUtils.DoErrorLog($"Struct size mismatch: Original is {originalSize} bytes, ours is {ourSize} bytes");
                    return false;
                }

                // Check each field name and type matches (order matters for struct layout!)
                for (int i = 0; i < originalFields.Length; i++)
                {
                    if (originalFields[i].Name != ourFields[i].Name)
                    {
                        LogUtils.DoErrorLog($"Field name mismatch at index {i}: Original has '{originalFields[i].Name}', ours has '{ourFields[i].Name}'");
                        return false;
                    }

                    if (originalFields[i].FieldType != ourFields[i].FieldType)
                    {
                        LogUtils.DoErrorLog($"Field type mismatch for '{originalFields[i].Name}': Original is '{originalFields[i].FieldType}', ours is '{ourFields[i].FieldType}'");
                        return false;
                    }
                }

                LogUtils.DoLog("CullingAction struct compatibility validated successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogUtils.DoErrorLog($"Exception during CullingAction compatibility validation: {ex}");
                return false;
            }
        }

        private static unsafe JobHandle CallUpdateCallerInTheMiddle(void* nativeParallelQueuePtr, JobHandle dependsOn, SystemBase system)
        {

            // Reinterpret the pointer directly - no boxing, no copying!
            // This works because both NativeParallelQueue<PreCullingSystem.CullingAction> and
            // NativeParallelQueue<CullingAction> have identical memory layout
            ref var queue = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AsRef<NativeParallelQueue<CullingAction>>(nativeParallelQueuePtr);

            var reader = queue.AsReader();
            var hashRange = queue.HashRange;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            WECullingJob cullingActionJob = default;
            cullingActionJob.m_CullingActions = reader;
            cullingActionJob.m_WEDrawingLookup = system.GetComponentLookup<WEDrawing>(true);
            cullingActionJob.m_CommandBuffer = commandBuffer.AsParallelWriter();

            var jobHandle = cullingActionJob.Schedule(hashRange, 1, dependsOn);
            jobHandle.Complete();

            commandBuffer.Playback(system.EntityManager);
            commandBuffer.Dispose();

            return jobHandle;
        }

        private struct CullingAction
        {
            public override int GetHashCode()
            {
                return this.m_Entity.GetHashCode();
            }
            public Entity m_Entity;
            public PreCullingSystem.ActionFlags m_Flags;
            public sbyte m_UpdateFrame;
        }
        [BurstCompile]
        private struct WECullingJob : IJobParallelFor
        {
            public void Execute(int index)
            {
                NativeParallelQueue<CullingAction>.Enumerator enumerator = this.m_CullingActions.GetEnumerator(index);
                while (enumerator.MoveNext())
                {
                    CullingAction cullingAction = enumerator.Current;
                    if ((cullingAction.m_Flags & PreCullingSystem.ActionFlags.PassedCulling) != (PreCullingSystem.ActionFlags)0)
                    {
                        this.PassedCulling(cullingAction, index);
                    }
                    else
                    {
                        this.FailedCulling(cullingAction, index);
                    }
                }
                enumerator.Dispose();
            }
            private void PassedCulling(CullingAction cullingAction, int index)
            {
                if (m_WEDrawingLookup.HasComponent(cullingAction.m_Entity))
                {
                    m_CommandBuffer.SetComponentEnabled<WEDrawing>(index, cullingAction.m_Entity, true);
                }
                else
                {
                    m_CommandBuffer.AddComponent<WEDrawing>(index, cullingAction.m_Entity);
                }
            }
            private void FailedCulling(CullingAction cullingAction, int index)
            {
                if (m_WEDrawingLookup.HasComponent(cullingAction.m_Entity))
                {
                    m_CommandBuffer.SetComponentEnabled<WEDrawing>(index, cullingAction.m_Entity, false);
                }
            }
            [ReadOnly]
            public NativeParallelQueue<CullingAction>.Reader m_CullingActions;

            [ReadOnly]
            public ComponentLookup<WEDrawing> m_WEDrawingLookup;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }
    }

    public struct WEDrawing : IComponentData, IEnableableComponent { }
}
/** Original method body for reference
```cs
protected override void OnUpdate(){
            bool loaded = this.GetLoaded();
			this.m_WriteDependencies.Complete();
			this.m_ReadDependencies.Complete();
			float3 @float = this.m_PrevCameraPosition;
			float3 float2 = this.m_PrevCameraDirection;
			float4 float3 = this.m_PrevLodParameters;
			LODParameters lodParameters;
			if (this.m_CameraUpdateSystem.TryGetLODParameters(out lodParameters))
			{
				@float = lodParameters.cameraPosition;
				IGameCameraController activeCameraController = this.m_CameraUpdateSystem.activeCameraController;
				float3 = RenderingUtils.CalculateLodParameters(this.m_BatchDataSystem.GetLevelOfDetail(this.m_RenderingSystem.frameLod, activeCameraController), lodParameters);
				float2 = this.m_CameraUpdateSystem.activeViewer.forward;
			}
			BoundsMask boundsMask = BoundsMask.NormalLayers;
			if (this.m_UndergroundViewSystem.pipelinesOn)
			{
				boundsMask |= BoundsMask.PipelineLayer;
			}
			if (this.m_UndergroundViewSystem.subPipelinesOn)
			{
				boundsMask |= BoundsMask.SubPipelineLayer;
			}
			if (this.m_UndergroundViewSystem.waterwaysOn)
			{
				boundsMask |= BoundsMask.WaterwayLayer;
			}
			if (this.m_RenderingSystem.markersVisible)
			{
				boundsMask |= BoundsMask.Debug;
			}
			if (this.m_ResetPrevious)
			{
				this.m_PrevCameraPosition = @float;
				this.m_PrevCameraDirection = float2;
				this.m_PrevLodParameters = float3;
				this.m_PrevVisibleMask = (BoundsMask)0;
				this.visibleMask = boundsMask;
				this.becameVisible = boundsMask;
				this.becameHidden = (BoundsMask)0;
			}
			else
			{
				this.visibleMask = boundsMask;
				this.becameVisible = (boundsMask & ~this.m_PrevVisibleMask);
				this.becameHidden = (this.m_PrevVisibleMask & ~boundsMask);
			}
			int length = this.m_CullingData.Length;
			NativeParallelQueue<PreCullingSystem.CullingAction> nativeParallelQueue = new NativeParallelQueue<PreCullingSystem.CullingAction>(Allocator.TempJob);
			NativeReference<int> cullingDataIndex = new NativeReference<int>(length, Allocator.TempJob);
			NativeQueue<PreCullingSystem.OverflowAction> overflowActions = new NativeQueue<PreCullingSystem.OverflowAction>(Allocator.TempJob);
			NativeArray<int> nodeBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<int> subDataBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			PreCullingSystem.TreeCullingJob1 treeCullingJob = default(PreCullingSystem.TreeCullingJob1);
			JobHandle job;
			treeCullingJob.m_StaticObjectSearchTree = this.m_ObjectSearchSystem.GetStaticSearchTree(true, out job);
			JobHandle job2;
			treeCullingJob.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(true, out job2);
			JobHandle job3;
			treeCullingJob.m_LaneSearchTree = this.m_NetSearchSystem.GetLaneSearchTree(true, out job3);
			treeCullingJob.m_LodParameters = float3;
			treeCullingJob.m_PrevLodParameters = this.m_PrevLodParameters;
			treeCullingJob.m_CameraPosition = @float;
			treeCullingJob.m_PrevCameraPosition = this.m_PrevCameraPosition;
			treeCullingJob.m_CameraDirection = float2;
			treeCullingJob.m_PrevCameraDirection = this.m_PrevCameraDirection;
			treeCullingJob.m_VisibleMask = boundsMask;
			treeCullingJob.m_PrevVisibleMask = this.m_PrevVisibleMask;
			treeCullingJob.m_NodeBuffer = nodeBuffer;
			treeCullingJob.m_SubDataBuffer = subDataBuffer;
			treeCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
			PreCullingSystem.TreeCullingJob1 treeCullingJob2 = treeCullingJob;
			PreCullingSystem.TreeCullingJob2 treeCullingJob3 = default(PreCullingSystem.TreeCullingJob2);
			treeCullingJob3.m_StaticObjectSearchTree = treeCullingJob2.m_StaticObjectSearchTree;
			treeCullingJob3.m_NetSearchTree = treeCullingJob2.m_NetSearchTree;
			treeCullingJob3.m_LaneSearchTree = treeCullingJob2.m_LaneSearchTree;
			treeCullingJob3.m_LodParameters = float3;
			treeCullingJob3.m_PrevLodParameters = this.m_PrevLodParameters;
			treeCullingJob3.m_CameraPosition = @float;
			treeCullingJob3.m_PrevCameraPosition = this.m_PrevCameraPosition;
			treeCullingJob3.m_CameraDirection = float2;
			treeCullingJob3.m_PrevCameraDirection = this.m_PrevCameraDirection;
			treeCullingJob3.m_VisibleMask = boundsMask;
			treeCullingJob3.m_PrevVisibleMask = this.m_PrevVisibleMask;
			treeCullingJob3.m_NodeBuffer = nodeBuffer;
			treeCullingJob3.m_SubDataBuffer = subDataBuffer;
			treeCullingJob3.m_ActionQueue = nativeParallelQueue.AsWriter();
			PreCullingSystem.TreeCullingJob2 jobData = treeCullingJob3;
			JobHandle dependsOn = treeCullingJob2.Schedule(3, 1, JobHandle.CombineDependencies(job, job2, job3));
			JobHandle jobHandle = jobData.Schedule(nodeBuffer.Length, 1, dependsOn);
			JobHandle.ScheduleBatchedJobs();
			this.m_BatchMeshSystem.CompleteCaching();
			PreCullingSystem.QueryFlags queryFlags = this.GetQueryFlags();
			PreCullingSystem.InitializeCullingJob initializeCullingJob = default(PreCullingSystem.InitializeCullingJob);
			initializeCullingJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_UpdatedType = InternalCompilerInterface.GetComponentTypeHandle<Updated>(ref this.__TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_BatchesUpdatedType = InternalCompilerInterface.GetComponentTypeHandle<BatchesUpdated>(ref this.__TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_OverriddenType = InternalCompilerInterface.GetComponentTypeHandle<Overridden>(ref this.__TypeHandle.__Game_Common_Overridden_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle<Stack>(ref this.__TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_ObjectMarkerType = InternalCompilerInterface.GetComponentTypeHandle<Game.Objects.Marker>(ref this.__TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle<Unspawned>(ref this.__TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_NodeType = InternalCompilerInterface.GetComponentTypeHandle<Node>(ref this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle<Edge>(ref this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<NodeGeometry>(ref this.__TypeHandle.__Game_Net_NodeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<EdgeGeometry>(ref this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_StartNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<StartNodeGeometry>(ref this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_EndNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<EndNodeGeometry>(ref this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle<Composition>(ref this.__TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle<Orphan>(ref this.__TypeHandle.__Game_Net_Orphan_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_CurveType = InternalCompilerInterface.GetComponentTypeHandle<Curve>(ref this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle<Game.Net.UtilityLane>(ref this.__TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_NetMarkerType = InternalCompilerInterface.GetComponentTypeHandle<Game.Net.Marker>(ref this.__TypeHandle.__Game_Net_Marker_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_ZoneBlockType = InternalCompilerInterface.GetComponentTypeHandle<Block>(ref this.__TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle<TransformFrame>(ref this.__TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, base.CheckedStateRef);
			initializeCullingJob.m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup<StackData>(ref this.__TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup<NetLaneGeometryData>(ref this.__TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup<UtilityLaneData>(ref this.__TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup<NetCompositionData>(ref this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabCompositionMeshRef = InternalCompilerInterface.GetComponentLookup<NetCompositionMeshRef>(ref this.__TypeHandle.__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabCompositionMeshData = InternalCompilerInterface.GetComponentLookup<NetCompositionMeshData>(ref this.__TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabNetData = InternalCompilerInterface.GetComponentLookup<NetData>(ref this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup<NetGeometryData>(ref this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, base.CheckedStateRef);
			initializeCullingJob.m_EditorMode = this.m_ToolSystem.actionMode.IsEditor();
			initializeCullingJob.m_UpdateAll = loaded;
			initializeCullingJob.m_UnspawnedVisible = this.m_RenderingSystem.unspawnedVisible;
			initializeCullingJob.m_DilatedUtilityTypes = this.m_UndergroundViewSystem.utilityTypes;
			initializeCullingJob.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData(false);
			initializeCullingJob.m_CullingData = this.m_CullingData;
			PreCullingSystem.InitializeCullingJob initializeCullingJob2 = initializeCullingJob;
			PreCullingSystem.EventCullingJob eventCullingJob = default(PreCullingSystem.EventCullingJob);
			eventCullingJob.m_RentersUpdatedType = InternalCompilerInterface.GetComponentTypeHandle<RentersUpdated>(ref this.__TypeHandle.__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle, base.CheckedStateRef);
			eventCullingJob.m_ColorUpdatedType = InternalCompilerInterface.GetComponentTypeHandle<ColorUpdated>(ref this.__TypeHandle.__Game_Routes_ColorUpdated_RO_ComponentTypeHandle, base.CheckedStateRef);
			eventCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, base.CheckedStateRef);
			eventCullingJob.m_SubObjects = InternalCompilerInterface.GetBufferLookup<Game.Objects.SubObject>(ref this.__TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, base.CheckedStateRef);
			eventCullingJob.m_SubLanes = InternalCompilerInterface.GetBufferLookup<Game.Net.SubLane>(ref this.__TypeHandle.__Game_Net_SubLane_RO_BufferLookup, base.CheckedStateRef);
			eventCullingJob.m_RouteVehicles = InternalCompilerInterface.GetBufferLookup<RouteVehicle>(ref this.__TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, base.CheckedStateRef);
			eventCullingJob.m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, base.CheckedStateRef);
			eventCullingJob.m_CullingData = this.m_CullingData;
			PreCullingSystem.EventCullingJob jobData2 = eventCullingJob;
			PreCullingSystem.QueryCullingJob queryCullingJob = default(PreCullingSystem.QueryCullingJob);
			queryCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
			queryCullingJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, base.CheckedStateRef);
			queryCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, base.CheckedStateRef);
			queryCullingJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle<TransformFrame>(ref this.__TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, base.CheckedStateRef);
			queryCullingJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, base.CheckedStateRef);
			queryCullingJob.m_LodParameters = float3;
			queryCullingJob.m_CameraPosition = @float;
			queryCullingJob.m_CameraDirection = float2;
			queryCullingJob.m_FrameIndex = this.m_RenderingSystem.frameIndex;
			queryCullingJob.m_FrameTime = this.m_RenderingSystem.frameTime;
			queryCullingJob.m_VisibleMask = boundsMask;
			queryCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
			PreCullingSystem.QueryCullingJob jobData3 = queryCullingJob;
			PreCullingSystem.QueryRemoveJob queryRemoveJob = default(PreCullingSystem.QueryRemoveJob);
			queryRemoveJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
			queryRemoveJob.m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle<Deleted>(ref this.__TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, base.CheckedStateRef);
			queryRemoveJob.m_AppliedType = InternalCompilerInterface.GetComponentTypeHandle<Applied>(ref this.__TypeHandle.__Game_Common_Applied_RO_ComponentTypeHandle, base.CheckedStateRef);
			queryRemoveJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, base.CheckedStateRef);
			queryRemoveJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle<TransformFrame>(ref this.__TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, base.CheckedStateRef);
			queryRemoveJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, base.CheckedStateRef);
			queryRemoveJob.m_ActionQueue = nativeParallelQueue.AsWriter();
			PreCullingSystem.QueryRemoveJob jobData4 = queryRemoveJob;
			PreCullingSystem.RelativeCullingJob relativeCullingJob = default(PreCullingSystem.RelativeCullingJob);
			relativeCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
			relativeCullingJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, base.CheckedStateRef);
			relativeCullingJob.m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle<CurrentVehicle>(ref this.__TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, base.CheckedStateRef);
			relativeCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, base.CheckedStateRef);
			relativeCullingJob.m_LodParameters = float3;
			relativeCullingJob.m_CameraPosition = @float;
			relativeCullingJob.m_CameraDirection = float2;
			relativeCullingJob.m_VisibleMask = boundsMask;
			relativeCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
			PreCullingSystem.RelativeCullingJob jobData5 = relativeCullingJob;
			PreCullingSystem.TempCullingJob tempCullingJob = default(PreCullingSystem.TempCullingJob);
			tempCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
			tempCullingJob.m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle<InterpolatedTransform>(ref this.__TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle<Stack>(ref this.__TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_StaticType = InternalCompilerInterface.GetComponentTypeHandle<Static>(ref this.__TypeHandle.__Game_Objects_Static_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle<Stopped>(ref this.__TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_TempType = InternalCompilerInterface.GetComponentTypeHandle<Temp>(ref this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, base.CheckedStateRef);
			tempCullingJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, base.CheckedStateRef);
			tempCullingJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup<StackData>(ref this.__TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, base.CheckedStateRef);
			tempCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, base.CheckedStateRef);
			tempCullingJob.m_LodParameters = float3;
			tempCullingJob.m_CameraPosition = @float;
			tempCullingJob.m_CameraDirection = float2;
			tempCullingJob.m_VisibleMask = boundsMask;
			tempCullingJob.m_TerrainHeightData = initializeCullingJob2.m_TerrainHeightData;
			tempCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
			PreCullingSystem.TempCullingJob jobData6 = tempCullingJob;
			EntityQuery query = loaded ? this.m_CullingInfoQuery : this.m_InitializeQuery;
			EntityQuery cullingQuery = this.GetCullingQuery(queryFlags);
			EntityQuery relativeQuery = this.GetRelativeQuery(queryFlags);
			EntityQuery removeQuery = this.GetRemoveQuery(this.m_PrevQueryFlags & ~queryFlags);
			JobHandle dependsOn2 = initializeCullingJob2.ScheduleParallel(query, base.Dependency);
			JobHandle dependsOn3 = jobData2.Schedule(this.m_EventQuery, dependsOn2);
			JobHandle dependsOn4 = jobData3.ScheduleParallel(cullingQuery, dependsOn3);
			JobHandle dependsOn5 = jobData4.ScheduleParallel(removeQuery, dependsOn4);
			JobHandle dependsOn6 = jobData5.ScheduleParallel(relativeQuery, dependsOn5);
			JobHandle jobHandle2 = jobData6.ScheduleParallel(this.m_TempQuery, dependsOn6);
			if (this.m_ResetPrevious || this.becameHidden != (BoundsMask)0)
			{
				PreCullingSystem.VerifyVisibleJob jobData7 = default(PreCullingSystem.VerifyVisibleJob);
				jobData7.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, base.CheckedStateRef);
				jobData7.m_LodParameters = float3;
				jobData7.m_CameraPosition = @float;
				jobData7.m_CameraDirection = float2;
				jobData7.m_VisibleMask = boundsMask;
				jobData7.m_CullingData = this.m_CullingData;
				jobData7.m_ActionQueue = nativeParallelQueue.AsWriter();
				JobHandle job4 = jobData7.Schedule(length, 16, jobHandle2);
				this.m_WriteDependencies = JobHandle.CombineDependencies(jobHandle, job4);
			}
			else
			{
				this.m_WriteDependencies = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			PreCullingSystem.CullingActionJob cullingActionJob = default(PreCullingSystem.CullingActionJob);
			cullingActionJob.m_CullingActions = nativeParallelQueue.AsReader();
			cullingActionJob.m_OverflowActions = overflowActions.AsParallelWriter();
			cullingActionJob.m_CullingInfo = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, base.CheckedStateRef);
			cullingActionJob.m_CullingData = this.m_CullingData;
			cullingActionJob.m_CullingDataIndex = cullingDataIndex;
			PreCullingSystem.CullingActionJob jobData8 = cullingActionJob;
			PreCullingSystem.ResizeCullingDataJob resizeCullingDataJob = default(PreCullingSystem.ResizeCullingDataJob);
			resizeCullingDataJob.m_CullingDataIndex = cullingDataIndex;
			resizeCullingDataJob.m_CullingData = this.m_CullingData;
			resizeCullingDataJob.m_UpdatedData = this.m_UpdatedData;
			resizeCullingDataJob.m_OverflowActions = overflowActions;
			PreCullingSystem.ResizeCullingDataJob jobData9 = resizeCullingDataJob;
			PreCullingSystem.FilterUpdatesJob filterUpdatesJob = default(PreCullingSystem.FilterUpdatesJob);
			filterUpdatesJob.m_CreatedData = InternalCompilerInterface.GetComponentLookup<Created>(ref this.__TypeHandle.__Game_Common_Created_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_UpdatedData = InternalCompilerInterface.GetComponentLookup<Updated>(ref this.__TypeHandle.__Game_Common_Updated_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_AppliedData = InternalCompilerInterface.GetComponentLookup<Applied>(ref this.__TypeHandle.__Game_Common_Applied_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_BatchesUpdatedData = InternalCompilerInterface.GetComponentLookup<BatchesUpdated>(ref this.__TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup<InterpolatedTransform>(ref this.__TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_TempData = InternalCompilerInterface.GetComponentLookup<Temp>(ref this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_ObjectData = InternalCompilerInterface.GetComponentLookup<Game.Objects.Object>(ref this.__TypeHandle.__Game_Objects_Object_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometry>(ref this.__TypeHandle.__Game_Objects_ObjectGeometry_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_ObjectColorData = InternalCompilerInterface.GetComponentLookup<Game.Objects.Color>(ref this.__TypeHandle.__Game_Objects_Color_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_PlantData = InternalCompilerInterface.GetComponentLookup<Plant>(ref this.__TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_TreeData = InternalCompilerInterface.GetComponentLookup<Tree>(ref this.__TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_RelativeData = InternalCompilerInterface.GetComponentLookup<Relative>(ref this.__TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_DamagedData = InternalCompilerInterface.GetComponentLookup<Damaged>(ref this.__TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_BuildingData = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_ExtensionData = InternalCompilerInterface.GetComponentLookup<Extension>(ref this.__TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_EdgeData = InternalCompilerInterface.GetComponentLookup<Edge>(ref this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_NodeData = InternalCompilerInterface.GetComponentLookup<Node>(ref this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_LaneData = InternalCompilerInterface.GetComponentLookup<Lane>(ref this.__TypeHandle.__Game_Net_Lane_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_NodeColorData = InternalCompilerInterface.GetComponentLookup<NodeColor>(ref this.__TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_EdgeColorData = InternalCompilerInterface.GetComponentLookup<EdgeColor>(ref this.__TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_LaneColorData = InternalCompilerInterface.GetComponentLookup<LaneColor>(ref this.__TypeHandle.__Game_Net_LaneColor_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_LaneConditionData = InternalCompilerInterface.GetComponentLookup<LaneCondition>(ref this.__TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_ZoneData = InternalCompilerInterface.GetComponentLookup<Block>(ref this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_OnFireData = InternalCompilerInterface.GetComponentLookup<OnFire>(ref this.__TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, base.CheckedStateRef);
			filterUpdatesJob.m_AnimatedData = InternalCompilerInterface.GetBufferLookup<Animated>(ref this.__TypeHandle.__Game_Rendering_Animated_RO_BufferLookup, base.CheckedStateRef);
			filterUpdatesJob.m_SkeletonData = InternalCompilerInterface.GetBufferLookup<Skeleton>(ref this.__TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, base.CheckedStateRef);
			filterUpdatesJob.m_EmissiveData = InternalCompilerInterface.GetBufferLookup<Emissive>(ref this.__TypeHandle.__Game_Rendering_Emissive_RO_BufferLookup, base.CheckedStateRef);
			filterUpdatesJob.m_MeshColorData = InternalCompilerInterface.GetBufferLookup<MeshColor>(ref this.__TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, base.CheckedStateRef);
			filterUpdatesJob.m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, base.CheckedStateRef);
			filterUpdatesJob.m_EffectInstances = InternalCompilerInterface.GetBufferLookup<EnabledEffect>(ref this.__TypeHandle.__Game_Effects_EnabledEffect_RO_BufferLookup, base.CheckedStateRef);
			filterUpdatesJob.m_TimerDelta = this.m_RenderingSystem.lodTimerDelta;
			filterUpdatesJob.m_CullingData = this.m_CullingData;
			filterUpdatesJob.m_UpdatedCullingData = this.m_UpdatedData.AsParallelWriter();
			PreCullingSystem.FilterUpdatesJob jobData10 = filterUpdatesJob;
			JobHandle jobHandle3 = jobData8.Schedule(nativeParallelQueue.HashRange, 1, this.m_WriteDependencies);
			JobHandle jobHandle4 = jobData9.Schedule(jobHandle3);
			JobHandle jobHandle5 = jobData10.Schedule(this.m_CullingData, 16, jobHandle4);
			this.m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
			this.m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
			this.m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
			this.m_TerrainSystem.AddCPUHeightReader(jobHandle2);
			nativeParallelQueue.Dispose(jobHandle3);
			cullingDataIndex.Dispose(jobHandle4);
			overflowActions.Dispose(jobHandle4);
			nodeBuffer.Dispose(jobHandle);
			subDataBuffer.Dispose(jobHandle);
			this.m_PrevCameraPosition = @float;
			this.m_PrevCameraDirection = float2;
			this.m_PrevLodParameters = float3;
			this.m_PrevVisibleMask = boundsMask;
			this.m_PrevQueryFlags = queryFlags;
			this.m_ResetPrevious = false;
			this.m_WriteDependencies = jobHandle5;
			this.m_ReadDependencies = default(JobHandle);
			base.Dependency = jobHandle5;
}```
*/