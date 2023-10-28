//#define BURST
////#define VERBOSE 

//using Game;
//using Game.Areas;
//using Game.Rendering;
//using Game.Tools;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.Scripting;
//using System;
//using System.Runtime.CompilerServices;
//using Colossal.Collections;
//using Colossal.Mathematics;
//using Colossal.Serialization.Entities;
//using Game.Common;
//using Game.Prefabs;
//using Game.Serialization;
//using Unity.Burst;
//using Unity.Burst.Intrinsics;
//using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using AreaColorData = Game.Rendering.AreaColorData;


//namespace BelzontWE.Rendering
//{
//    public unsafe partial class WEBatchSystem : GameSystemBase
//    {
//        // Token: 0x060032CD RID: 13005 RVA: 0x001AD6E8 File Offset: 0x001AB8E8
//        [Preserve]
//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
//            this.m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
//            this.m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
//            this.m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
//            this.m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
//            this.m_UpdatedQuery = base.GetEntityQuery(new EntityQueryDesc[]
//            {
//                new EntityQueryDesc
//                {
//                    All = new ComponentType[]
//                    {
//                        ComponentType.ReadOnly<Batch>()
//                    },
//                    Any = new ComponentType[]
//                    {
//                        ComponentType.ReadOnly<Updated>(),
//                        ComponentType.ReadOnly<Deleted>()
//                    },
//                    None = new ComponentType[]
//                    {
//                        ComponentType.ReadOnly<Temp>()
//                    }
//                }
//            });
//            this.m_PrefabQuery = base.GetEntityQuery(new ComponentType[]
//            {
//                ComponentType.ReadOnly<RenderedAreaData>(),
//                ComponentType.ReadOnly<Created>()
//            });
//            this.m_ManagedBatchData = new List<WEBatchSystem.ManagedBatchData>();
//            this.m_AreaBufferAllocator = new NativeHeapAllocator(4194304u / WEBatchSystem.GetTriangleSize(), 1u, Allocator.Persistent);
//            this.m_AllocationCount = new NativeReference<int>(0, Allocator.Persistent);
//            this.m_NativeBatchData = new NativeList<WEBatchSystem.NativeBatchData>(Allocator.Persistent);
//            this.m_AreaTriangleData = new NativeList<AreaTriangleData>(Allocator.Persistent);
//            this.m_TriangleMetaData = new NativeList<WEBatchSystem.TriangleMetaData>(Allocator.Persistent);
//            this.m_AreaColorData = new NativeList<AreaColorData>(Allocator.Persistent);
//            this.m_UpdatedTriangles = new NativeList<NativeHeapBlock>(100, Allocator.Persistent);
//            this.m_AreaTriangleData.ResizeUninitialized((int)this.m_AreaBufferAllocator.Size);
//            this.m_TriangleMetaData.ResizeUninitialized((int)this.m_AreaBufferAllocator.Size);
//            this.m_AreaColorData.ResizeUninitialized((int)this.m_AreaBufferAllocator.Size);
//            this.m_AreaTriangleBuffer = new ComputeBuffer(this.m_AreaTriangleData.Capacity, sizeof(AreaTriangleData));
//            this.m_AreaColorBuffer = new ComputeBuffer(this.m_AreaColorData.Capacity, sizeof(AreaColorData));
//            this.m_AreaParameters = Shader.PropertyToID("colossal_AreaParameters");
//            this.m_DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
//        }

//        // Token: 0x060032CE RID: 13006 RVA: 0x001AD910 File Offset: 0x001ABB10
//        [Preserve]
//        protected override void OnDestroy()
//        {
//            this.m_AreaTriangleBuffer.Release();
//            this.m_AreaColorBuffer.Release();
//            for (int i = 0; i < this.m_ManagedBatchData.Count; i++)
//            {
//                WEBatchSystem.ManagedBatchData managedBatchData = this.m_ManagedBatchData[i];
//                if (managedBatchData.m_Material != null)
//                {
//                    UnityEngine.Object.Destroy(managedBatchData.m_Material);
//                }
//                if (managedBatchData.m_VisibleIndices != null)
//                {
//                    managedBatchData.m_VisibleIndices.Release();
//                }
//            }
//            this.m_DataDependencies.Complete();
//            for (int j = 0; j < this.m_NativeBatchData.Length; j++)
//            {
//                ref WEBatchSystem.NativeBatchData ptr = ref this.m_NativeBatchData.ElementAt(j);
//                if (ptr.m_AreaMetaData.IsCreated)
//                {
//                    ptr.m_AreaMetaData.Dispose();
//                }
//                if (ptr.m_VisibleIndices.IsCreated)
//                {
//                    ptr.m_VisibleIndices.Dispose();
//                }
//            }
//            this.m_AreaBufferAllocator.Dispose();
//            this.m_AllocationCount.Dispose();
//            this.m_NativeBatchData.Dispose();
//            this.m_AreaTriangleData.Dispose();
//            this.m_TriangleMetaData.Dispose();
//            this.m_AreaColorData.Dispose();
//            this.m_UpdatedTriangles.Dispose();
//            base.OnDestroy();
//        }

//        // Token: 0x060032D0 RID: 13008 RVA: 0x001ADAC8 File Offset: 0x001ABCC8
//        public int GetBatchCount()
//        {
//            return this.m_ManagedBatchData.Count;
//        }

//        // Token: 0x060032D1 RID: 13009 RVA: 0x001ADAD5 File Offset: 0x001ABCD5
//        private bool GetLoaded()
//        {
//            if (this.m_Loaded)
//            {
//                this.m_Loaded = false;
//                return true;
//            }
//            return false;
//        }

//        // Token: 0x060032D2 RID: 13010 RVA: 0x001ADAEC File Offset: 0x001ABCEC
//        public unsafe bool GetAreaBatch(int index, out ComputeBuffer buffer, out ComputeBuffer colors, out GraphicsBuffer indices, out Material material, out Bounds bounds, out int count, out int rendererPriority)
//        {
//            this.m_DataDependencies.Complete();
//            WEBatchSystem.ManagedBatchData managedBatchData = this.m_ManagedBatchData[index];
//            ref WEBatchSystem.NativeBatchData ptr = ref this.m_NativeBatchData.ElementAt(index);
//            if (this.m_AreaTriangleBuffer.count != this.m_AreaTriangleData.Capacity)
//            {
//                this.m_AreaTriangleBuffer.Release();
//                this.m_AreaTriangleBuffer = new ComputeBuffer(this.m_AreaTriangleData.Capacity, sizeof(AreaTriangleData));
//                this.m_UpdatedTriangles.Clear();
//                uint onePastHighestUsedAddress = this.m_AreaBufferAllocator.OnePastHighestUsedAddress;
//                if (onePastHighestUsedAddress != 0u)
//                {
//                    NativeHeapBlock nativeHeapBlock = new NativeHeapBlock(new UnsafeHeapBlock(0u, onePastHighestUsedAddress));
//                    this.m_UpdatedTriangles.Add(nativeHeapBlock);
//                }
//            }
//            if (this.m_UpdatedTriangles.Length != 0)
//            {
//                for (int i = 0; i < this.m_UpdatedTriangles.Length; i++)
//                {
//                    NativeHeapBlock nativeHeapBlock2 = this.m_UpdatedTriangles[i];
//                    this.m_AreaTriangleBuffer.SetData<AreaTriangleData>(this.m_AreaTriangleData.AsArray(), (int)nativeHeapBlock2.Begin, (int)nativeHeapBlock2.Begin, (int)nativeHeapBlock2.Length);
//                }
//                this.m_UpdatedTriangles.Clear();
//            }
//            if (this.m_AreaColorBuffer.count != this.m_AreaColorData.Capacity)
//            {
//                this.m_AreaColorBuffer.Release();
//                this.m_AreaColorBuffer = new ComputeBuffer(this.m_AreaColorData.Capacity, sizeof(AreaColorData));
//            }
//            if (this.m_ColorsUpdated)
//            {
//                this.m_ColorsUpdated = false;
//                uint onePastHighestUsedAddress2 = this.m_AreaBufferAllocator.OnePastHighestUsedAddress;
//                if (onePastHighestUsedAddress2 != 0u)
//                {
//                    this.m_AreaColorBuffer.SetData<AreaColorData>(this.m_AreaColorData.AsArray(), 0, 0, (int)onePastHighestUsedAddress2);
//                }
//            }
//            if (managedBatchData.m_VisibleIndices.count != ptr.m_VisibleIndices.Capacity)
//            {
//                managedBatchData.m_VisibleIndices.Release();
//                managedBatchData.m_VisibleIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ptr.m_VisibleIndices.Capacity, 4);
//            }
//            if (ptr.m_VisibleIndicesUpdated)
//            {
//                ptr.m_VisibleIndicesUpdated = false;
//                if (ptr.m_VisibleIndices.Length != 0)
//                {
//                    NativeArray<int> data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)ptr.m_VisibleIndices.Ptr, ptr.m_VisibleIndices.Length, Allocator.None);
//                    managedBatchData.m_VisibleIndices.SetData<int>(data, 0, 0, data.Length);
//                }
//            }
//            buffer = this.m_AreaTriangleBuffer;
//            colors = this.m_AreaColorBuffer;
//            indices = managedBatchData.m_VisibleIndices;
//            material = managedBatchData.m_Material;
//            bounds = RenderingUtils.ToBounds(ptr.m_Bounds);
//            count = ptr.m_VisibleIndices.Length;
//            rendererPriority = managedBatchData.m_RendererPriority;
//            return count != 0;
//        }

//        // Token: 0x060032D3 RID: 13011 RVA: 0x001ADD53 File Offset: 0x001ABF53
//        public NativeList<AreaColorData> GetColorData(out JobHandle dependencies)
//        {
//            this.m_ColorsUpdated = true;
//            dependencies = this.m_DataDependencies;
//            return this.m_AreaColorData;
//        }

//        // Token: 0x060032D4 RID: 13012 RVA: 0x001ADD6E File Offset: 0x001ABF6E
//        public void AddColorWriter(JobHandle jobHandle)
//        {
//            this.m_DataDependencies = jobHandle;
//        }

//        // Token: 0x060032D5 RID: 13013 RVA: 0x001ADD77 File Offset: 0x001ABF77
//        public void GetAreaStats(out uint allocatedSize, out uint bufferSize, out uint count)
//        {
//            this.m_DataDependencies.Complete();
//            allocatedSize = this.m_AreaBufferAllocator.UsedSpace * WEBatchSystem.GetTriangleSize();
//            bufferSize = this.m_AreaBufferAllocator.Size * WEBatchSystem.GetTriangleSize();
//            count = (uint)this.m_AllocationCount.Value;
//        }

//        // Token: 0x060032D6 RID: 13014 RVA: 0x001ADDB7 File Offset: 0x001ABFB7
//        private static uint GetTriangleSize()
//        {
//            return (uint)(sizeof(AreaTriangleData) + sizeof(AreaColorData));
//        }

//        // Token: 0x060032D7 RID: 13015 RVA: 0x001ADDC8 File Offset: 0x001ABFC8
//        [Preserve]
//        protected override void OnUpdate()
//        {
//            bool loaded = this.GetLoaded();
//            this.m_DataDependencies.Complete();
//            this.m_UpdatedTriangles.Clear();
//            if (!this.m_PrefabQuery.IsEmptyIgnoreFilter)
//            {
//                this.UpdatePrefabs();
//            }
//            float3 @float = this.m_PrevCameraPosition;
//            float3 float2 = this.m_PrevCameraDirection;
//            float4 float3 = this.m_PrevLodParameters;
//            if (this.m_CameraUpdateSystem.TryGetLODParameters(out LODParameters lodParameters))
//            {
//                @float = lodParameters.cameraPosition;
//                IGameCameraController activeCameraController = this.m_CameraUpdateSystem.activeCameraController;
//                float3 = RenderingUtils.CalculateLodParameters(this.m_BatchDataSystem.GetLevelOfDetail(this.m_RenderingSystem.frameLod, activeCameraController), lodParameters);
//                float2 = this.m_CameraUpdateSystem.activeViewer.forward;
//            }
//            BoundsMask visibleMask = BoundsMask.NormalLayers;
//            BoundsMask prevVisibleMask = BoundsMask.NormalLayers;
//            if (loaded)
//            {
//                this.m_PrevCameraPosition = @float;
//                this.m_PrevCameraDirection = float2;
//                this.m_PrevLodParameters = float3;
//                prevVisibleMask = (BoundsMask)0;
//            }
//            NativeParallelQueue<WEBatchSystem.CullingAction> nativeParallelQueue = new NativeParallelQueue<WEBatchSystem.CullingAction>(Allocator.TempJob);
//            NativeQueue<WEBatchSystem.AllocationAction> allocationActions = new NativeQueue<WEBatchSystem.AllocationAction>(Allocator.TempJob);
//            NativeArray<int> nodeBuffer = new NativeArray<int>(512, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
//            NativeArray<int> subDataBuffer = new NativeArray<int>(512, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
//            WEBatchSystem.TreeCullingJob1 treeCullingJob = default(WEBatchSystem.TreeCullingJob1);
//            treeCullingJob.m_AreaSearchTree = this.m_AreaSearchSystem.GetSearchTree(true, out JobHandle dependsOn);
//            treeCullingJob.m_LodParameters = float3;
//            treeCullingJob.m_PrevLodParameters = this.m_PrevLodParameters;
//            treeCullingJob.m_CameraPosition = @float;
//            treeCullingJob.m_PrevCameraPosition = this.m_PrevCameraPosition;
//            treeCullingJob.m_CameraDirection = float2;
//            treeCullingJob.m_PrevCameraDirection = this.m_PrevCameraDirection;
//            treeCullingJob.m_VisibleMask = visibleMask;
//            treeCullingJob.m_PrevVisibleMask = prevVisibleMask;
//            treeCullingJob.m_NodeBuffer = nodeBuffer;
//            treeCullingJob.m_SubDataBuffer = subDataBuffer;
//            treeCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
//            WEBatchSystem.TreeCullingJob1 treeCullingJob2 = treeCullingJob;
//            WEBatchSystem.TreeCullingJob2 treeCullingJob3 = default(WEBatchSystem.TreeCullingJob2);
//            treeCullingJob3.m_AreaSearchTree = treeCullingJob2.m_AreaSearchTree;
//            treeCullingJob3.m_LodParameters = float3;
//            treeCullingJob3.m_PrevLodParameters = this.m_PrevLodParameters;
//            treeCullingJob3.m_CameraPosition = @float;
//            treeCullingJob3.m_PrevCameraPosition = this.m_PrevCameraPosition;
//            treeCullingJob3.m_CameraDirection = float2;
//            treeCullingJob3.m_PrevCameraDirection = this.m_PrevCameraDirection;
//            treeCullingJob3.m_VisibleMask = visibleMask;
//            treeCullingJob3.m_PrevVisibleMask = prevVisibleMask;
//            treeCullingJob3.m_NodeBuffer = nodeBuffer;
//            treeCullingJob3.m_SubDataBuffer = subDataBuffer;
//            treeCullingJob3.m_ActionQueue = nativeParallelQueue.AsWriter();
//            WEBatchSystem.TreeCullingJob2 jobData = treeCullingJob3;
//            WEBatchSystem.QueryCullingJob queryCullingJob = default(WEBatchSystem.QueryCullingJob);
//            queryCullingJob.m_EntityType = base.GetEntityTypeHandle();
//            queryCullingJob.m_BatchType = base.GetComponentTypeHandle<Batch>(true);
//            queryCullingJob.m_DeletedType = base.GetComponentTypeHandle<Deleted>(true);
//            queryCullingJob.m_PrefabRefType = base.GetComponentTypeHandle<PrefabRef>(true);
//            queryCullingJob.m_NodeType = base.GetBufferTypeHandle<Node>(true);
//            queryCullingJob.m_TriangleType = base.GetBufferTypeHandle<Triangle>(true);
//            queryCullingJob.m_PrefabAreaGeometryData = base.GetComponentLookup<AreaGeometryData>(true);
//            queryCullingJob.m_LodParameters = float3;
//            queryCullingJob.m_CameraPosition = @float;
//            queryCullingJob.m_CameraDirection = float2;
//            queryCullingJob.m_VisibleMask = visibleMask;
//            queryCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
//            WEBatchSystem.QueryCullingJob jobData2 = queryCullingJob;
//            WEBatchSystem.CullingActionJob cullingActionJob = default(WEBatchSystem.CullingActionJob);
//            cullingActionJob.m_PrefabRefData = base.GetComponentLookup<PrefabRef>(true);
//            cullingActionJob.m_PrefabRenderedAreaData = base.GetComponentLookup<RenderedAreaData>(true);
//            cullingActionJob.m_Triangles = base.GetBufferLookup<Triangle>(true);
//            cullingActionJob.m_CullingActions = nativeParallelQueue.AsReader();
//            cullingActionJob.m_AllocationActions = allocationActions.AsParallelWriter();
//            cullingActionJob.m_BatchData = base.GetComponentLookup<Batch>(false);
//            cullingActionJob.m_TriangleMetaData = this.m_TriangleMetaData;
//            WEBatchSystem.CullingActionJob jobData3 = cullingActionJob;
//            WEBatchSystem.BatchAllocationJob batchAllocationJob = default(WEBatchSystem.BatchAllocationJob);
//            batchAllocationJob.m_BatchData = base.GetComponentLookup<Batch>(false);
//            batchAllocationJob.m_NativeBatchData = this.m_NativeBatchData;
//            batchAllocationJob.m_TriangleMetaData = this.m_TriangleMetaData;
//            batchAllocationJob.m_AreaTriangleData = this.m_AreaTriangleData;
//            batchAllocationJob.m_AreaColorData = this.m_AreaColorData;
//            batchAllocationJob.m_UpdatedTriangles = this.m_UpdatedTriangles;
//            batchAllocationJob.m_AllocationActions = allocationActions;
//            batchAllocationJob.m_AreaBufferAllocator = this.m_AreaBufferAllocator;
//            batchAllocationJob.m_AllocationCount = this.m_AllocationCount;
//            WEBatchSystem.BatchAllocationJob jobData4 = batchAllocationJob;
//            WEBatchSystem.TriangleUpdateJob triangleUpdateJob = default(WEBatchSystem.TriangleUpdateJob);
//            triangleUpdateJob.m_AreaData = base.GetComponentLookup<Area>(true);
//            triangleUpdateJob.m_OwnerData = base.GetComponentLookup<Owner>(true);
//            triangleUpdateJob.m_TransformData = base.GetComponentLookup<Game.Objects.Transform>(true);
//            triangleUpdateJob.m_PrefabRefData = base.GetComponentLookup<PrefabRef>(true);
//            triangleUpdateJob.m_PrefabRenderedAreaData = base.GetComponentLookup<RenderedAreaData>(true);
//            triangleUpdateJob.m_Nodes = base.GetBufferLookup<Node>(true);
//            triangleUpdateJob.m_Triangles = base.GetBufferLookup<Triangle>(true);
//            triangleUpdateJob.m_Expands = base.GetBufferLookup<Expand>(true);
//            triangleUpdateJob.m_CullingActions = nativeParallelQueue.AsReader();
//            triangleUpdateJob.m_BatchData = base.GetComponentLookup<Batch>(false);
//            triangleUpdateJob.m_TriangleMetaData = this.m_TriangleMetaData;
//            triangleUpdateJob.m_AreaTriangleData = this.m_AreaTriangleData;
//            triangleUpdateJob.m_NativeBatchData = this.m_NativeBatchData;
//            WEBatchSystem.TriangleUpdateJob jobData5 = triangleUpdateJob;
//            WEBatchSystem.VisibleUpdateJob visibleUpdateJob = default(WEBatchSystem.VisibleUpdateJob);
//            visibleUpdateJob.m_NativeBatchData = this.m_NativeBatchData;
//            WEBatchSystem.VisibleUpdateJob jobData6 = visibleUpdateJob;
//            JobHandle dependsOn2 = treeCullingJob2.Schedule(dependsOn);
//            JobHandle jobHandle = jobData.Schedule(nodeBuffer.Length, 1, dependsOn2);
//            JobHandle job = jobData2.ScheduleParallel(this.m_UpdatedQuery, base.Dependency);
//            JobHandle dependsOn3 = jobData3.Schedule(nativeParallelQueue.HashRange, 1, JobHandle.CombineDependencies(jobHandle, job));
//            JobHandle jobHandle2 = jobData4.Schedule(dependsOn3);
//            JobHandle jobHandle3 = jobData5.Schedule(nativeParallelQueue.HashRange, 1, jobHandle2);
//            JobHandle dataDependencies = jobData6.Schedule(this.m_ManagedBatchData.Count, 1, jobHandle3);
//            this.m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
//            nativeParallelQueue.Dispose(jobHandle3);
//            allocationActions.Dispose(jobHandle2);
//            nodeBuffer.Dispose(jobHandle);
//            subDataBuffer.Dispose(jobHandle);
//            this.m_PrevCameraPosition = @float;
//            this.m_PrevCameraDirection = float2;
//            this.m_PrevLodParameters = float3;
//            base.Dependency = jobHandle3;
//            this.m_DataDependencies = dataDependencies;
//        }

//        // Token: 0x060032D8 RID: 13016 RVA: 0x001AE31C File Offset: 0x001AC51C
//        private void UpdatePrefabs()
//        {
//            using (NativeArray<ArchetypeChunk> nativeArray = this.m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob))
//            {
//                this.__TypeHandle.__Game_Prefabs_RenderedAreaData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<RenderedAreaData> _Game_Prefabs_RenderedAreaData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_RenderedAreaData_RW_ComponentTypeHandle;
//                this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<PrefabData> _Game_Prefabs_PrefabData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
//                this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<AreaGeometryData> _Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle;
//                base.CompleteDependency();
//                for (int i = 0; i < nativeArray.Length; i++)
//                {
//                    ArchetypeChunk archetypeChunk = nativeArray[i];
//                    NativeArray<RenderedAreaData> nativeArray2 = archetypeChunk.GetNativeArray<RenderedAreaData>(ref _Game_Prefabs_RenderedAreaData_RW_ComponentTypeHandle);
//                    NativeArray<PrefabData> nativeArray3 = archetypeChunk.GetNativeArray<PrefabData>(ref _Game_Prefabs_PrefabData_RO_ComponentTypeHandle);
//                    NativeArray<AreaGeometryData> nativeArray4 = archetypeChunk.GetNativeArray<AreaGeometryData>(ref _Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle);
//                    for (int j = 0; j < nativeArray2.Length; j++)
//                    {
//                        RenderedArea component = this.m_PrefabSystem.GetPrefab<AreaPrefab>(nativeArray3[j]).GetComponent<RenderedArea>();
//                        float minNodeDistance = AreaUtils.GetMinNodeDistance(nativeArray4[j].m_Type);
//                        float num = minNodeDistance * 2f;
//                        float num2 = math.clamp(component.m_Roundness, 0.01f, 0.99f) * minNodeDistance;
//                        RenderedAreaData value = nativeArray2[j];
//                        value.m_HeightOffset = num;
//                        value.m_ExpandAmount = num2 * 0.5f;
//                        value.m_BatchIndex = this.m_ManagedBatchData.Count;
//                        nativeArray2[j] = value;
//                        WEBatchSystem.ManagedBatchData managedBatchData = new WEBatchSystem.ManagedBatchData();
//                        managedBatchData.m_Material = new Material(component.m_Material);
//                        managedBatchData.m_Material.name = "Area batch (" + component.m_Material.name + ")";
//                        managedBatchData.m_Material.renderQueue = component.m_Material.shader.renderQueue;
//                        managedBatchData.m_Material.SetVector(this.m_AreaParameters, new Vector4(num2, num, 0f, 0f));
//                        managedBatchData.m_Material.SetFloat(this.m_DecalLayerMask, math.asfloat((int)component.m_DecalLayerMask));
//                        managedBatchData.m_RendererPriority = component.m_RendererPriority;
//                        WEBatchSystem.NativeBatchData nativeBatchData = default(WEBatchSystem.NativeBatchData);
//                        nativeBatchData.m_AreaMetaData = new UnsafeList<WEBatchSystem.AreaMetaData>(10, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
//                        nativeBatchData.m_VisibleIndices = new UnsafeList<int>(100, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
//                        managedBatchData.m_VisibleIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nativeBatchData.m_VisibleIndices.Capacity, 4);
//                        this.m_ManagedBatchData.Add(managedBatchData);
//                        this.m_NativeBatchData.Add(nativeBatchData);
//                    }
//                }
//            }
//        }

//        // Token: 0x060032D9 RID: 13017 RVA: 0x00002E1D File Offset: 0x0000101D
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void __AssignQueries(ref SystemState state)
//        {
//        }

//        // Token: 0x060032DA RID: 13018 RVA: 0x001AE5DC File Offset: 0x001AC7DC
//        protected override void OnCreateForCompiler()
//        {
//            base.OnCreateForCompiler();
//            this.__AssignQueries(ref base.CheckedStateRef);
//            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
//        }

//        // Token: 0x060032DB RID: 13019 RVA: 0x000068B3 File Offset: 0x00004AB3
//        [Preserve]
//        public WEBatchSystem()
//        {
//        }

//        // Token: 0x04004670 RID: 18032
//        public const uint AREABUFFER_MEMORY_DEFAULT = 4194304u;

//        // Token: 0x04004671 RID: 18033
//        public const uint AREABUFFER_MEMORY_INCREMENT = 1048576u;

//        // Token: 0x04004672 RID: 18034
//        private PrefabSystem m_PrefabSystem;

//        // Token: 0x04004673 RID: 18035
//        private CameraUpdateSystem m_CameraUpdateSystem;

//        // Token: 0x04004674 RID: 18036
//        private RenderingSystem m_RenderingSystem;

//        // Token: 0x04004675 RID: 18037
//        private BatchDataSystem m_BatchDataSystem;

//        // Token: 0x04004676 RID: 18038
//        private Game.Areas.SearchSystem m_AreaSearchSystem;

//        // Token: 0x04004677 RID: 18039
//        private ComputeBuffer m_AreaTriangleBuffer;

//        // Token: 0x04004678 RID: 18040
//        private ComputeBuffer m_AreaColorBuffer;

//        // Token: 0x04004679 RID: 18041
//        private List<WEBatchSystem.ManagedBatchData> m_ManagedBatchData;

//        // Token: 0x0400467A RID: 18042
//        private NativeHeapAllocator m_AreaBufferAllocator;

//        // Token: 0x0400467B RID: 18043
//        private NativeReference<int> m_AllocationCount;

//        // Token: 0x0400467C RID: 18044
//        private NativeList<WEBatchSystem.NativeBatchData> m_NativeBatchData;

//        // Token: 0x0400467D RID: 18045
//        private NativeList<AreaTriangleData> m_AreaTriangleData;

//        // Token: 0x0400467E RID: 18046
//        private NativeList<WEBatchSystem.TriangleMetaData> m_TriangleMetaData;

//        // Token: 0x0400467F RID: 18047
//        private NativeList<AreaColorData> m_AreaColorData;

//        // Token: 0x04004680 RID: 18048
//        private NativeList<NativeHeapBlock> m_UpdatedTriangles;

//        // Token: 0x04004681 RID: 18049
//        private EntityQuery m_UpdatedQuery;

//        // Token: 0x04004682 RID: 18050
//        private EntityQuery m_PrefabQuery;

//        // Token: 0x04004683 RID: 18051
//        private JobHandle m_DataDependencies;

//        // Token: 0x04004684 RID: 18052
//        private float3 m_PrevCameraPosition;

//        // Token: 0x04004685 RID: 18053
//        private float3 m_PrevCameraDirection;

//        // Token: 0x04004686 RID: 18054
//        private float4 m_PrevLodParameters;

//        // Token: 0x04004687 RID: 18055
//        private int m_AreaParameters;

//        // Token: 0x04004688 RID: 18056
//        private int m_DecalLayerMask;

//        // Token: 0x04004689 RID: 18057
//        private bool m_Loaded;

//        // Token: 0x0400468A RID: 18058
//        private bool m_ColorsUpdated;

//        // Token: 0x0400468B RID: 18059
//        private WEBatchSystem.TypeHandle __TypeHandle;

//        // Token: 0x02000B35 RID: 2869
//        private class ManagedBatchData
//        {
//            // Token: 0x0400468C RID: 18060
//            public GraphicsBuffer m_VisibleIndices;

//            // Token: 0x0400468D RID: 18061
//            public Material m_Material;

//            // Token: 0x0400468E RID: 18062
//            public int m_RendererPriority;
//        }

//        // Token: 0x02000B36 RID: 2870
//        private struct NativeBatchData
//        {
//            // Token: 0x0400468F RID: 18063
//            public UnsafeList<WEBatchSystem.AreaMetaData> m_AreaMetaData;

//            // Token: 0x04004690 RID: 18064
//            public UnsafeList<int> m_VisibleIndices;

//            // Token: 0x04004691 RID: 18065
//            public Bounds3 m_Bounds;

//            // Token: 0x04004692 RID: 18066
//            public bool m_VisibleUpdated;

//            // Token: 0x04004693 RID: 18067
//            public bool m_BoundsUpdated;

//            // Token: 0x04004694 RID: 18068
//            public bool m_VisibleIndicesUpdated;
//        }

//        // Token: 0x02000B37 RID: 2871
//        [BurstCompile]
//        private struct TreeCullingJob1 : IJob
//        {
//            // Token: 0x060032DD RID: 13021 RVA: 0x001AE604 File Offset: 0x001AC804
//            public void Execute()
//            {
//                WEBatchSystem.TreeCullingIterator treeCullingIterator = new WEBatchSystem.TreeCullingIterator
//                {
//                    m_LodParameters = this.m_LodParameters,
//                    m_PrevLodParameters = this.m_PrevLodParameters,
//                    m_CameraPosition = this.m_CameraPosition,
//                    m_PrevCameraPosition = this.m_PrevCameraPosition,
//                    m_CameraDirection = this.m_CameraDirection,
//                    m_PrevCameraDirection = this.m_PrevCameraDirection,
//                    m_VisibleMask = this.m_VisibleMask,
//                    m_PrevVisibleMask = this.m_PrevVisibleMask,
//                    m_ActionQueue = this.m_ActionQueue
//                };
//                this.m_AreaSearchTree.Iterate<WEBatchSystem.TreeCullingIterator, int>(ref treeCullingIterator, 3, this.m_NodeBuffer, this.m_SubDataBuffer);
//            }

//            // Token: 0x04004695 RID: 18069
//            [ReadOnly]
//            public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

//            // Token: 0x04004696 RID: 18070
//            [ReadOnly]
//            public float4 m_LodParameters;

//            // Token: 0x04004697 RID: 18071
//            [ReadOnly]
//            public float4 m_PrevLodParameters;

//            // Token: 0x04004698 RID: 18072
//            [ReadOnly]
//            public float3 m_CameraPosition;

//            // Token: 0x04004699 RID: 18073
//            [ReadOnly]
//            public float3 m_PrevCameraPosition;

//            // Token: 0x0400469A RID: 18074
//            [ReadOnly]
//            public float3 m_CameraDirection;

//            // Token: 0x0400469B RID: 18075
//            [ReadOnly]
//            public float3 m_PrevCameraDirection;

//            // Token: 0x0400469C RID: 18076
//            [ReadOnly]
//            public BoundsMask m_VisibleMask;

//            // Token: 0x0400469D RID: 18077
//            [ReadOnly]
//            public BoundsMask m_PrevVisibleMask;

//            // Token: 0x0400469E RID: 18078
//            public NativeArray<int> m_NodeBuffer;

//            // Token: 0x0400469F RID: 18079
//            public NativeArray<int> m_SubDataBuffer;

//            // Token: 0x040046A0 RID: 18080
//            [NativeDisableContainerSafetyRestriction]
//            public NativeParallelQueue<WEBatchSystem.CullingAction>.Writer m_ActionQueue;
//        }

//        // Token: 0x02000B38 RID: 2872
//        [BurstCompile]
//        private struct TreeCullingJob2 : IJobParallelFor
//        {
//            // Token: 0x060032DE RID: 13022 RVA: 0x001AE6AC File Offset: 0x001AC8AC
//            public void Execute(int index)
//            {
//                WEBatchSystem.TreeCullingIterator treeCullingIterator = new WEBatchSystem.TreeCullingIterator
//                {
//                    m_LodParameters = this.m_LodParameters,
//                    m_PrevLodParameters = this.m_PrevLodParameters,
//                    m_CameraPosition = this.m_CameraPosition,
//                    m_PrevCameraPosition = this.m_PrevCameraPosition,
//                    m_CameraDirection = this.m_CameraDirection,
//                    m_PrevCameraDirection = this.m_PrevCameraDirection,
//                    m_VisibleMask = this.m_VisibleMask,
//                    m_PrevVisibleMask = this.m_PrevVisibleMask,
//                    m_ActionQueue = this.m_ActionQueue
//                };
//                this.m_AreaSearchTree.Iterate<WEBatchSystem.TreeCullingIterator, int>(ref treeCullingIterator, this.m_SubDataBuffer[index], this.m_NodeBuffer[index]);
//            }

//            // Token: 0x040046A1 RID: 18081
//            [ReadOnly]
//            public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

//            // Token: 0x040046A2 RID: 18082
//            [ReadOnly]
//            public float4 m_LodParameters;

//            // Token: 0x040046A3 RID: 18083
//            [ReadOnly]
//            public float4 m_PrevLodParameters;

//            // Token: 0x040046A4 RID: 18084
//            [ReadOnly]
//            public float3 m_CameraPosition;

//            // Token: 0x040046A5 RID: 18085
//            [ReadOnly]
//            public float3 m_PrevCameraPosition;

//            // Token: 0x040046A6 RID: 18086
//            [ReadOnly]
//            public float3 m_CameraDirection;

//            // Token: 0x040046A7 RID: 18087
//            [ReadOnly]
//            public float3 m_PrevCameraDirection;

//            // Token: 0x040046A8 RID: 18088
//            [ReadOnly]
//            public BoundsMask m_VisibleMask;

//            // Token: 0x040046A9 RID: 18089
//            [ReadOnly]
//            public BoundsMask m_PrevVisibleMask;

//            // Token: 0x040046AA RID: 18090
//            [ReadOnly]
//            public NativeArray<int> m_NodeBuffer;

//            // Token: 0x040046AB RID: 18091
//            [ReadOnly]
//            public NativeArray<int> m_SubDataBuffer;

//            // Token: 0x040046AC RID: 18092
//            [NativeDisableContainerSafetyRestriction]
//            public NativeParallelQueue<WEBatchSystem.CullingAction>.Writer m_ActionQueue;
//        }

//        // Token: 0x02000B39 RID: 2873
//        private struct TreeCullingIterator : INativeQuadTreeIteratorWithSubData<AreaSearchItem, QuadTreeBoundsXZ, int>, IUnsafeQuadTreeIteratorWithSubData<AreaSearchItem, QuadTreeBoundsXZ, int>
//        {
//            // Token: 0x060032DF RID: 13023 RVA: 0x001AE760 File Offset: 0x001AC960
//            public bool Intersect(QuadTreeBoundsXZ bounds, ref int subData)
//            {
//                int num = subData;
//                if (num != 1)
//                {
//                    if (num != 2)
//                    {
//                        BoundsMask boundsMask = this.m_VisibleMask & bounds.m_Mask;
//                        BoundsMask boundsMask2 = this.m_PrevVisibleMask & bounds.m_Mask;
//                        float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                        float num3 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                        int num4 = RenderingUtils.CalculateLod(num2 * num2, this.m_LodParameters);
//                        int num5 = RenderingUtils.CalculateLod(num3 * num3, this.m_PrevLodParameters);
//                        subData = 0;
//                        if (boundsMask != (BoundsMask)0 && num4 >= (int)bounds.m_MinLod)
//                        {
//                            if ((boundsMask & ~this.m_PrevVisibleMask) != (BoundsMask)0)
//                            {
//                                subData |= 1;
//                            }
//                            else
//                            {
//                                float num6 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                                int num7 = RenderingUtils.CalculateLod(num6 * num6, this.m_PrevLodParameters);
//                                if (num7 < (int)bounds.m_MaxLod && num4 > num7)
//                                {
//                                    subData |= 1;
//                                }
//                            }
//                        }
//                        if (boundsMask2 != (BoundsMask)0 && num5 >= (int)bounds.m_MinLod)
//                        {
//                            if ((boundsMask2 & ~this.m_VisibleMask) != (BoundsMask)0)
//                            {
//                                subData |= 2;
//                            }
//                            else
//                            {
//                                float num8 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                                int num9 = RenderingUtils.CalculateLod(num8 * num8, this.m_LodParameters);
//                                if (num9 < (int)bounds.m_MaxLod && num5 > num9)
//                                {
//                                    subData |= 2;
//                                }
//                            }
//                        }
//                        return subData != 0;
//                    }
//                    BoundsMask boundsMask3 = this.m_PrevVisibleMask & bounds.m_Mask;
//                    float num10 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                    int num11 = RenderingUtils.CalculateLod(num10 * num10, this.m_PrevLodParameters);
//                    if (boundsMask3 == (BoundsMask)0 || num11 < (int)bounds.m_MinLod)
//                    {
//                        return false;
//                    }
//                    if ((boundsMask3 & ~this.m_VisibleMask) != (BoundsMask)0)
//                    {
//                        return true;
//                    }
//                    float num12 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                    int num13 = RenderingUtils.CalculateLod(num12 * num12, this.m_LodParameters);
//                    return num13 < (int)bounds.m_MaxLod && num11 > num13;
//                }
//                else
//                {
//                    BoundsMask boundsMask4 = this.m_VisibleMask & bounds.m_Mask;
//                    float num14 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                    int num15 = RenderingUtils.CalculateLod(num14 * num14, this.m_LodParameters);
//                    if (boundsMask4 == (BoundsMask)0 || num15 < (int)bounds.m_MinLod)
//                    {
//                        return false;
//                    }
//                    if ((boundsMask4 & ~this.m_PrevVisibleMask) != (BoundsMask)0)
//                    {
//                        return true;
//                    }
//                    float num16 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                    int num17 = RenderingUtils.CalculateLod(num16 * num16, this.m_PrevLodParameters);
//                    return num17 < (int)bounds.m_MaxLod && num15 > num17;
//                }
//            }

//            // Token: 0x060032E0 RID: 13024 RVA: 0x001AE9F8 File Offset: 0x001ACBF8
//            public void Iterate(QuadTreeBoundsXZ bounds, int subData, AreaSearchItem item)
//            {
//                if (subData != 1)
//                {
//                    if (subData != 2)
//                    {
//                        float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                        float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                        int num3 = RenderingUtils.CalculateLod(num * num, this.m_LodParameters);
//                        int num4 = RenderingUtils.CalculateLod(num2 * num2, this.m_PrevLodParameters);
//                        bool flag = (this.m_VisibleMask & bounds.m_Mask) != (BoundsMask)0 && num3 >= (int)bounds.m_MinLod;
//                        bool flag2 = (this.m_PrevVisibleMask & bounds.m_Mask) != (BoundsMask)0 && num4 >= (int)bounds.m_MaxLod;
//                        if (flag != flag2)
//                        {
//                            this.m_ActionQueue.Enqueue(new WEBatchSystem.CullingAction
//                            {
//                                m_Item = item,
//                                m_PassedCulling = flag
//                            });
//                        }
//                        return;
//                    }
//                    float num5 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                    int num6 = RenderingUtils.CalculateLod(num5 * num5, this.m_PrevLodParameters);
//                    if ((this.m_PrevVisibleMask & bounds.m_Mask) == (BoundsMask)0 || num6 < (int)bounds.m_MinLod)
//                    {
//                        return;
//                    }
//                    float num7 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                    int num8 = RenderingUtils.CalculateLod(num7 * num7, this.m_LodParameters);
//                    if ((this.m_VisibleMask & bounds.m_Mask) != (BoundsMask)0 && num8 >= (int)bounds.m_MaxLod)
//                    {
//                        return;
//                    }
//                    this.m_ActionQueue.Enqueue(new WEBatchSystem.CullingAction
//                    {
//                        m_Item = item
//                    });
//                    return;
//                }
//                else
//                {
//                    float num9 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                    int num10 = RenderingUtils.CalculateLod(num9 * num9, this.m_LodParameters);
//                    if ((this.m_VisibleMask & bounds.m_Mask) == (BoundsMask)0 || num10 < (int)bounds.m_MinLod)
//                    {
//                        return;
//                    }
//                    float num11 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, this.m_PrevCameraPosition, this.m_PrevCameraDirection, this.m_PrevLodParameters);
//                    int num12 = RenderingUtils.CalculateLod(num11 * num11, this.m_PrevLodParameters);
//                    if ((this.m_PrevVisibleMask & bounds.m_Mask) != (BoundsMask)0 && num12 >= (int)bounds.m_MaxLod)
//                    {
//                        return;
//                    }
//                    this.m_ActionQueue.Enqueue(new WEBatchSystem.CullingAction
//                    {
//                        m_Item = item,
//                        m_PassedCulling = true
//                    });
//                    return;
//                }
//            }

//            // Token: 0x040046AD RID: 18093
//            public float4 m_LodParameters;

//            // Token: 0x040046AE RID: 18094
//            public float3 m_CameraPosition;

//            // Token: 0x040046AF RID: 18095
//            public float3 m_CameraDirection;

//            // Token: 0x040046B0 RID: 18096
//            public float3 m_PrevCameraPosition;

//            // Token: 0x040046B1 RID: 18097
//            public float4 m_PrevLodParameters;

//            // Token: 0x040046B2 RID: 18098
//            public float3 m_PrevCameraDirection;

//            // Token: 0x040046B3 RID: 18099
//            public BoundsMask m_VisibleMask;

//            // Token: 0x040046B4 RID: 18100
//            public BoundsMask m_PrevVisibleMask;

//            // Token: 0x040046B5 RID: 18101
//            public NativeParallelQueue<WEBatchSystem.CullingAction>.Writer m_ActionQueue;
//        }

//        // Token: 0x02000B3A RID: 2874
//        [BurstCompile]
//        private struct QueryCullingJob : IJobChunk
//        {
//            // Token: 0x060032E1 RID: 13025 RVA: 0x001AEC34 File Offset: 0x001ACE34
//            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//            {
//                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
//                NativeArray<Batch> nativeArray2 = chunk.GetNativeArray<Batch>(ref this.m_BatchType);
//                if (chunk.Has<Deleted>(ref this.m_DeletedType))
//                {
//                    for (int i = 0; i < chunk.Count; i++)
//                    {
//                        Entity area = nativeArray[i];
//                        if (nativeArray2[i].m_AllocatedSize != 0)
//                        {
//                            this.m_ActionQueue.Enqueue(new WEBatchSystem.CullingAction
//                            {
//                                m_Item = new AreaSearchItem(area, -1)
//                            });
//                        }
//                    }
//                    return;
//                }
//                NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabRefType);
//                BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor<Node>(ref this.m_NodeType);
//                BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor<Triangle>(ref this.m_TriangleType);
//                BoundsMask boundsMask = BoundsMask.Debug | BoundsMask.NormalLayers | BoundsMask.NotOverridden | BoundsMask.NotWalkThrough;
//                for (int j = 0; j < chunk.Count; j++)
//                {
//                    Entity area2 = nativeArray[j];
//                    if (nativeArray2[j].m_AllocatedSize != 0)
//                    {
//                        this.m_ActionQueue.Enqueue(new WEBatchSystem.CullingAction
//                        {
//                            m_Item = new AreaSearchItem(area2, -1)
//                        });
//                    }
//                    if ((this.m_VisibleMask & boundsMask) != (BoundsMask)0)
//                    {
//                        PrefabRef prefabRef = nativeArray3[j];
//                        DynamicBuffer<Node> nodes = bufferAccessor[j];
//                        DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor2[j];
//                        AreaGeometryData areaData = this.m_PrefabAreaGeometryData[prefabRef.m_Prefab];
//                        for (int k = 0; k < dynamicBuffer.Length; k++)
//                        {
//                            Triangle triangle = dynamicBuffer[k];
//                            Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
//                            float num = RenderingUtils.CalculateMinDistance(AreaUtils.GetBounds(triangle, triangle2, areaData), this.m_CameraPosition, this.m_CameraDirection, this.m_LodParameters);
//                            if (RenderingUtils.CalculateLod(num * num, this.m_LodParameters) >= triangle.m_MinLod)
//                            {
//                                this.m_ActionQueue.Enqueue(new WEBatchSystem.CullingAction
//                                {
//                                    m_Item = new AreaSearchItem(area2, k),
//                                    m_PassedCulling = true
//                                });
//                            }
//                        }
//                    }
//                }
//            }

//            // Token: 0x060032E2 RID: 13026 RVA: 0x001AEE1B File Offset: 0x001AD01B
//            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//            {
//                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
//            }

//            // Token: 0x040046B6 RID: 18102
//            [ReadOnly]
//            public EntityTypeHandle m_EntityType;

//            // Token: 0x040046B7 RID: 18103
//            [ReadOnly]
//            public ComponentTypeHandle<Batch> m_BatchType;

//            // Token: 0x040046B8 RID: 18104
//            [ReadOnly]
//            public ComponentTypeHandle<Deleted> m_DeletedType;

//            // Token: 0x040046B9 RID: 18105
//            [ReadOnly]
//            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

//            // Token: 0x040046BA RID: 18106
//            [ReadOnly]
//            public BufferTypeHandle<Node> m_NodeType;

//            // Token: 0x040046BB RID: 18107
//            [ReadOnly]
//            public BufferTypeHandle<Triangle> m_TriangleType;

//            // Token: 0x040046BC RID: 18108
//            [ReadOnly]
//            public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

//            // Token: 0x040046BD RID: 18109
//            [ReadOnly]
//            public float4 m_LodParameters;

//            // Token: 0x040046BE RID: 18110
//            [ReadOnly]
//            public float3 m_CameraPosition;

//            // Token: 0x040046BF RID: 18111
//            [ReadOnly]
//            public float3 m_CameraDirection;

//            // Token: 0x040046C0 RID: 18112
//            [ReadOnly]
//            public BoundsMask m_VisibleMask;

//            // Token: 0x040046C1 RID: 18113
//            [NativeDisableContainerSafetyRestriction]
//            public NativeParallelQueue<WEBatchSystem.CullingAction>.Writer m_ActionQueue;
//        }

//        // Token: 0x02000B3B RID: 2875
//        private struct AreaMetaData
//        {
//            // Token: 0x040046C2 RID: 18114
//            public Entity m_Entity;

//            // Token: 0x040046C3 RID: 18115
//            public Bounds3 m_Bounds;

//            // Token: 0x040046C4 RID: 18116
//            public int m_StartIndex;

//            // Token: 0x040046C5 RID: 18117
//            public int m_VisibleCount;
//        }

//        // Token: 0x02000B3C RID: 2876
//        private struct TriangleMetaData
//        {
//            // Token: 0x040046C6 RID: 18118
//            public int m_Index;

//            // Token: 0x040046C7 RID: 18119
//            public bool m_IsVisible;
//        }

//        // Token: 0x02000B3D RID: 2877
//        private struct TriangleSortData : IComparable<WEBatchSystem.TriangleSortData>
//        {
//            // Token: 0x060032E3 RID: 13027 RVA: 0x001AEE28 File Offset: 0x001AD028
//            public int CompareTo(WEBatchSystem.TriangleSortData other)
//            {
//                return this.m_MinLod - other.m_MinLod;
//            }

//            // Token: 0x040046C8 RID: 18120
//            public int m_Index;

//            // Token: 0x040046C9 RID: 18121
//            public int m_MinLod;
//        }

//        // Token: 0x02000B3E RID: 2878
//        private struct CullingAction
//        {
//            // Token: 0x060032E4 RID: 13028 RVA: 0x001AEE37 File Offset: 0x001AD037
//            public override int GetHashCode()
//            {
//                return this.m_Item.m_Area.GetHashCode();
//            }

//            // Token: 0x040046CA RID: 18122
//            public AreaSearchItem m_Item;

//            // Token: 0x040046CB RID: 18123
//            public bool m_PassedCulling;
//        }

//        // Token: 0x02000B3F RID: 2879
//        private struct AllocationAction
//        {
//            // Token: 0x040046CC RID: 18124
//            public Entity m_Entity;

//            // Token: 0x040046CD RID: 18125
//            public int m_TriangleCount;
//        }

//        // Token: 0x02000B40 RID: 2880
//        [BurstCompile]
//        private struct CullingActionJob : IJobParallelFor
//        {
//            // Token: 0x060032E5 RID: 13029 RVA: 0x001AEE50 File Offset: 0x001AD050
//            public void Execute(int index)
//            {
//                NativeParallelQueue<WEBatchSystem.CullingAction>.Enumerator enumerator = this.m_CullingActions.GetEnumerator(index);
//                while (enumerator.MoveNext())
//                {
//                    WEBatchSystem.CullingAction cullingAction = enumerator.Current;
//                    if (cullingAction.m_PassedCulling)
//                    {
//                        this.PassedCulling(cullingAction);
//                    }
//                    else
//                    {
//                        this.FailedCulling(cullingAction);
//                    }
//                }
//                enumerator.Dispose();
//            }

//            // Token: 0x060032E6 RID: 13030 RVA: 0x001AEE9C File Offset: 0x001AD09C
//            private void PassedCulling(WEBatchSystem.CullingAction cullingAction)
//            {
//                ref Batch valueRW = ref this.m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
//                if (valueRW.m_VisibleCount == 0)
//                {
//                    valueRW.m_VisibleCount = -1;
//                    PrefabRef prefabRef = this.m_PrefabRefData[cullingAction.m_Item.m_Area];
//                    if (this.m_PrefabRenderedAreaData.TryGetComponent(prefabRef.m_Prefab, out RenderedAreaData renderedAreaData))
//                    {
//                        valueRW.m_BatchIndex = renderedAreaData.m_BatchIndex;
//                        this.m_AllocationActions.Enqueue(new WEBatchSystem.AllocationAction
//                        {
//                            m_Entity = cullingAction.m_Item.m_Area,
//                            m_TriangleCount = this.m_Triangles[cullingAction.m_Item.m_Area].Length
//                        });
//                        return;
//                    }
//                    valueRW.m_BatchIndex = -1;
//                }
//            }

//            // Token: 0x060032E7 RID: 13031 RVA: 0x001AEF68 File Offset: 0x001AD168
//            private void FailedCulling(WEBatchSystem.CullingAction cullingAction)
//            {
//                ref Batch valueRW = ref this.m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
//                if (valueRW.m_AllocatedSize != 0)
//                {
//                    if (cullingAction.m_Item.m_Triangle == -1)
//                    {
//                        if (valueRW.m_VisibleCount > 0)
//                        {
//                            for (int i = 0; i < valueRW.m_AllocatedSize; i++)
//                            {
//                                this.m_TriangleMetaData.ElementAt((int)(valueRW.m_BatchAllocation.Begin + (uint)i)).m_IsVisible = false;
//                            }
//                            valueRW.m_VisibleCount = 0;
//                            this.m_AllocationActions.Enqueue(new WEBatchSystem.AllocationAction
//                            {
//                                m_Entity = cullingAction.m_Item.m_Area
//                            });
//                            return;
//                        }
//                    }
//                    else
//                    {
//                        ref WEBatchSystem.TriangleMetaData ptr = ref this.m_TriangleMetaData.ElementAt((int)(valueRW.m_BatchAllocation.Begin + (uint)cullingAction.m_Item.m_Triangle));
//                        if (ptr.m_IsVisible)
//                        {
//                            ptr.m_IsVisible = false;
//                            ref Batch ptr2 = ref valueRW;
//                            int num = ptr2.m_VisibleCount - 1;
//                            ptr2.m_VisibleCount = num;
//                            if (num == 0)
//                            {
//                                this.m_AllocationActions.Enqueue(new WEBatchSystem.AllocationAction
//                                {
//                                    m_Entity = cullingAction.m_Item.m_Area
//                                });
//                            }
//                        }
//                    }
//                }
//            }

//            // Token: 0x040046CE RID: 18126
//            [ReadOnly]
//            public ComponentLookup<PrefabRef> m_PrefabRefData;

//            // Token: 0x040046CF RID: 18127
//            [ReadOnly]
//            public ComponentLookup<RenderedAreaData> m_PrefabRenderedAreaData;

//            // Token: 0x040046D0 RID: 18128
//            [ReadOnly]
//            public BufferLookup<Triangle> m_Triangles;

//            // Token: 0x040046D1 RID: 18129
//            [ReadOnly]
//            public NativeParallelQueue<WEBatchSystem.CullingAction>.Reader m_CullingActions;

//            // Token: 0x040046D2 RID: 18130
//            public NativeQueue<WEBatchSystem.AllocationAction>.ParallelWriter m_AllocationActions;

//            // Token: 0x040046D3 RID: 18131
//            [NativeDisableParallelForRestriction]
//            public ComponentLookup<Batch> m_BatchData;

//            // Token: 0x040046D4 RID: 18132
//            [NativeDisableParallelForRestriction]
//            public NativeList<WEBatchSystem.TriangleMetaData> m_TriangleMetaData;
//        }

//        // Token: 0x02000B41 RID: 2881
//        [BurstCompile]
//        private struct BatchAllocationJob : IJob
//        {
//            // Token: 0x060032E8 RID: 13032 RVA: 0x001AF084 File Offset: 0x001AD284
//            public void Execute()
//            {
//                while (this.m_AllocationActions.TryDequeue(out AllocationAction allocationAction))
//                {
//                    ref Batch valueRW = ref this.m_BatchData.GetRefRW(allocationAction.m_Entity).ValueRW;
//                    ref WEBatchSystem.NativeBatchData ptr = ref this.m_NativeBatchData.ElementAt(valueRW.m_BatchIndex);
//                    ptr.m_BoundsUpdated = true;
//                    if (allocationAction.m_TriangleCount != 0)
//                    {
//                        if (valueRW.m_AllocatedSize == 0)
//                        {
//                            this.Allocate(ref valueRW, allocationAction.m_TriangleCount);
//                            valueRW.m_MetaIndex = ptr.m_AreaMetaData.Length;
//                            ref WEBatchSystem.NativeBatchData ptr2 = ref ptr;
//                            WEBatchSystem.AreaMetaData areaMetaData = default(WEBatchSystem.AreaMetaData);
//                            areaMetaData.m_Entity = allocationAction.m_Entity;
//                            areaMetaData.m_StartIndex = (int)valueRW.m_BatchAllocation.Begin;
//                            ptr2.m_AreaMetaData.Add(areaMetaData);
//                            int value = this.m_AllocationCount.Value;
//                            this.m_AllocationCount.Value = value + 1;
//                        }
//                        else
//                        {
//                            ref WEBatchSystem.AreaMetaData ptr3 = ref ptr.m_AreaMetaData.ElementAt(valueRW.m_MetaIndex);
//                            ptr3.m_VisibleCount = 0;
//                            if (allocationAction.m_TriangleCount != valueRW.m_AllocatedSize)
//                            {
//                                this.m_AreaBufferAllocator.Release(valueRW.m_BatchAllocation);
//                                this.Allocate(ref valueRW, allocationAction.m_TriangleCount);
//                                ptr3.m_StartIndex = (int)valueRW.m_BatchAllocation.Begin;
//                            }
//                        }
//                        this.m_UpdatedTriangles.Add(valueRW.m_BatchAllocation);
//                    }
//                    else if (valueRW.m_VisibleCount == 0)
//                    {
//                        int value = this.m_AllocationCount.Value;
//                        this.m_AllocationCount.Value = value - 1;
//                        this.m_AreaBufferAllocator.Release(valueRW.m_BatchAllocation);
//                        valueRW.m_BatchAllocation = default(NativeHeapBlock);
//                        valueRW.m_AllocatedSize = 0;
//                        ptr.m_AreaMetaData.RemoveAtSwapBack(valueRW.m_MetaIndex);
//                        ptr.m_VisibleUpdated = true;
//                        if (valueRW.m_MetaIndex < ptr.m_AreaMetaData.Length)
//                        {
//                            this.m_BatchData.GetRefRW(ptr.m_AreaMetaData[valueRW.m_MetaIndex].m_Entity).ValueRW.m_MetaIndex = valueRW.m_MetaIndex;
//                        }
//                    }
//                }
//            }

//            // Token: 0x060032E9 RID: 13033 RVA: 0x001AF270 File Offset: 0x001AD470
//            private void Allocate(ref Batch batch, int allocationSize)
//            {
//                batch.m_BatchAllocation = this.m_AreaBufferAllocator.Allocate((uint)allocationSize, 1u);
//                batch.m_AllocatedSize = allocationSize;
//                if (batch.m_BatchAllocation.Empty)
//                {
//                    this.m_AreaBufferAllocator.Resize(this.m_AreaBufferAllocator.Size + 1048576u / WEBatchSystem.GetTriangleSize());
//                    this.m_TriangleMetaData.ResizeUninitialized((int)this.m_AreaBufferAllocator.Size);
//                    this.m_AreaTriangleData.ResizeUninitialized((int)this.m_AreaBufferAllocator.Size);
//                    this.m_AreaColorData.ResizeUninitialized((int)this.m_AreaBufferAllocator.Size);
//                    batch.m_BatchAllocation = this.m_AreaBufferAllocator.Allocate((uint)allocationSize, 1u);
//                }
//            }

//            // Token: 0x040046D5 RID: 18133
//            public ComponentLookup<Batch> m_BatchData;

//            // Token: 0x040046D6 RID: 18134
//            public NativeList<WEBatchSystem.NativeBatchData> m_NativeBatchData;

//            // Token: 0x040046D7 RID: 18135
//            public NativeList<WEBatchSystem.TriangleMetaData> m_TriangleMetaData;

//            // Token: 0x040046D8 RID: 18136
//            public NativeList<AreaTriangleData> m_AreaTriangleData;

//            // Token: 0x040046D9 RID: 18137
//            public NativeList<AreaColorData> m_AreaColorData;

//            // Token: 0x040046DA RID: 18138
//            public NativeList<NativeHeapBlock> m_UpdatedTriangles;

//            // Token: 0x040046DB RID: 18139
//            public NativeQueue<WEBatchSystem.AllocationAction> m_AllocationActions;

//            // Token: 0x040046DC RID: 18140
//            public NativeHeapAllocator m_AreaBufferAllocator;

//            // Token: 0x040046DD RID: 18141
//            public NativeReference<int> m_AllocationCount;
//        }

//        // Token: 0x02000B42 RID: 2882
//        [BurstCompile]
//        private struct TriangleUpdateJob : IJobParallelFor
//        {
//            // Token: 0x060032EA RID: 13034 RVA: 0x001AF31C File Offset: 0x001AD51C
//            public void Execute(int index)
//            {
//                NativeParallelQueue<WEBatchSystem.CullingAction>.Enumerator enumerator = this.m_CullingActions.GetEnumerator(index);
//                while (enumerator.MoveNext())
//                {
//                    WEBatchSystem.CullingAction cullingAction = enumerator.Current;
//                    if (cullingAction.m_PassedCulling)
//                    {
//                        this.PassedCulling(cullingAction);
//                    }
//                    else
//                    {
//                        this.FailedCulling(cullingAction);
//                    }
//                }
//                enumerator.Dispose();
//            }

//            // Token: 0x060032EB RID: 13035 RVA: 0x001AF368 File Offset: 0x001AD568
//            private void PassedCulling(WEBatchSystem.CullingAction cullingAction)
//            {
//                ref Batch valueRW = ref this.m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
//                if (valueRW.m_AllocatedSize != 0)
//                {
//                    if (valueRW.m_VisibleCount == -1)
//                    {
//                        this.GenerateTriangleData(cullingAction.m_Item.m_Area, ref valueRW);
//                        valueRW.m_VisibleCount = 0;
//                    }
//                    ref WEBatchSystem.TriangleMetaData ptr = ref this.m_TriangleMetaData.ElementAt((int)(valueRW.m_BatchAllocation.Begin + (uint)cullingAction.m_Item.m_Triangle));
//                    if (!ptr.m_IsVisible)
//                    {
//                        ptr.m_IsVisible = true;
//                        valueRW.m_VisibleCount++;
//                        ref WEBatchSystem.NativeBatchData ptr2 = ref this.m_NativeBatchData.ElementAt(valueRW.m_BatchIndex);
//                        ref WEBatchSystem.AreaMetaData ptr3 = ref ptr2.m_AreaMetaData.ElementAt(valueRW.m_MetaIndex);
//                        if (ptr.m_Index >= ptr3.m_VisibleCount)
//                        {
//                            ptr3.m_VisibleCount = ptr.m_Index + 1;
//                            if (!ptr2.m_VisibleUpdated)
//                            {
//                                ptr2.m_VisibleUpdated = true;
//                            }
//                        }
//                    }
//                }
//            }

//            // Token: 0x060032EC RID: 13036 RVA: 0x001AF450 File Offset: 0x001AD650
//            private void FailedCulling(WEBatchSystem.CullingAction cullingAction)
//            {
//                ref Batch valueRW = ref this.m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
//                if (valueRW.m_AllocatedSize != 0 && cullingAction.m_Item.m_Triangle != -1)
//                {
//                    ref WEBatchSystem.NativeBatchData ptr = ref this.m_NativeBatchData.ElementAt(valueRW.m_BatchIndex);
//                    ref WEBatchSystem.AreaMetaData ptr2 = ref ptr.m_AreaMetaData.ElementAt(valueRW.m_MetaIndex);
//                    WEBatchSystem.TriangleMetaData triangleMetaData = this.m_TriangleMetaData[(int)(valueRW.m_BatchAllocation.Begin + (uint)cullingAction.m_Item.m_Triangle)];
//                    if (triangleMetaData.m_Index == ptr2.m_VisibleCount - 1)
//                    {
//                        ptr2.m_VisibleCount = 0;
//                        for (int i = 0; i < valueRW.m_AllocatedSize; i++)
//                        {
//                            triangleMetaData = this.m_TriangleMetaData[(int)(valueRW.m_BatchAllocation.Begin + (uint)i)];
//                            ptr2.m_VisibleCount = math.select(ptr2.m_VisibleCount, triangleMetaData.m_Index + 1, triangleMetaData.m_IsVisible && triangleMetaData.m_Index >= ptr2.m_VisibleCount);
//                        }
//                        if (!ptr.m_VisibleUpdated)
//                        {
//                            ptr.m_VisibleUpdated = true;
//                        }
//                    }
//                }
//            }

//            // Token: 0x060032ED RID: 13037 RVA: 0x001AF570 File Offset: 0x001AD770
//            private void GenerateTriangleData(Entity entity, ref Batch batch)
//            {
//                Area area = this.m_AreaData[entity];
//                DynamicBuffer<Node> nodes = this.m_Nodes[entity];
//                DynamicBuffer<Triangle> triangles = this.m_Triangles[entity];
//                PrefabRef prefabRef = this.m_PrefabRefData[entity];
//                RenderedAreaData renderedAreaData = this.m_PrefabRenderedAreaData[prefabRef.m_Prefab];
//                float4 offsetDir = new float4(0f, 0f, 0f, 1f);
//                if (this.m_OwnerData.TryGetComponent(entity, out Owner owner))
//                {
//                    Game.Objects.Transform transform;
//                    while (!this.m_TransformData.TryGetComponent(owner.m_Owner, out transform))
//                    {
//                        if (!this.m_OwnerData.TryGetComponent(owner.m_Owner, out Owner owner2))
//                        {
//                            goto IL_D5;
//                        }
//                        owner = owner2;
//                    }
//                    offsetDir.xy = transform.m_Position.xz;
//                    offsetDir.zw = math.forward(transform.m_Rotation).xz;
//                }
//            IL_D5:
//                ref WEBatchSystem.AreaMetaData ptr = ref this.m_NativeBatchData.ElementAt(batch.m_BatchIndex).m_AreaMetaData.ElementAt(batch.m_MetaIndex);
//                bool isCounterClockwise = (area.m_Flags & AreaFlags.CounterClockwise) > (AreaFlags)0;
//                if (!this.m_BorderMap.IsCreated)
//                {
//                    this.m_BorderMap = new NativeParallelHashMap<WEBatchSystem.TriangleUpdateJob.Border, int2>(nodes.Length, Allocator.Temp);
//                }
//                if (!this.m_AdjacentNodes.IsCreated)
//                {
//                    this.m_AdjacentNodes = new NativeList<int2>(nodes.Length, Allocator.Temp);
//                }
//                if (!this.m_TriangleSortData.IsCreated)
//                {
//                    this.m_TriangleSortData = new NativeList<WEBatchSystem.TriangleSortData>(triangles.Length, Allocator.Temp);
//                }
//                this.SortTriangles(triangles, ref batch);
//                if (this.m_Expands.TryGetBuffer(entity, out DynamicBuffer<Expand> expands))
//                {
//                    if (!this.m_NodeList.IsCreated)
//                    {
//                        this.m_NodeList = new NativeList<Node>(nodes.Length, Allocator.Temp);
//                    }
//                    this.FillExpandedNodes(nodes, expands);
//                    this.AddBorders<NativeList<Node>>(this.m_NodeList, isCounterClockwise);
//                    this.AddNodes<NativeList<Node>>(this.m_NodeList, triangles, isCounterClockwise);
//                    ptr.m_Bounds = this.AddTriangles<NativeList<Node>>(this.m_NodeList, triangles, renderedAreaData, (int)batch.m_BatchAllocation.Begin, offsetDir, isCounterClockwise);
//                    return;
//                }
//                this.AddBorders<DynamicBuffer<Node>>(nodes, isCounterClockwise);
//                this.AddNodes<DynamicBuffer<Node>>(nodes, triangles, isCounterClockwise);
//                ptr.m_Bounds = this.AddTriangles<DynamicBuffer<Node>>(nodes, triangles, renderedAreaData, (int)batch.m_BatchAllocation.Begin, offsetDir, isCounterClockwise);
//            }

//            // Token: 0x060032EE RID: 13038 RVA: 0x001AF7B0 File Offset: 0x001AD9B0
//            private void SortTriangles(DynamicBuffer<Triangle> triangles, ref Batch batch)
//            {
//                this.m_TriangleSortData.ResizeUninitialized(triangles.Length);
//                for (int i = 0; i < this.m_TriangleSortData.Length; i++)
//                {
//                    this.m_TriangleSortData[i] = new WEBatchSystem.TriangleSortData
//                    {
//                        m_Index = i,
//                        m_MinLod = triangles[i].m_MinLod
//                    };
//                }
//                this.m_TriangleSortData.Sort<WEBatchSystem.TriangleSortData>();
//                for (int j = 0; j < this.m_TriangleSortData.Length; j++)
//                {
//                    WEBatchSystem.TriangleSortData triangleSortData = this.m_TriangleSortData[j];
//                    this.m_TriangleMetaData[(int)(batch.m_BatchAllocation.Begin + (uint)triangleSortData.m_Index)] = new WEBatchSystem.TriangleMetaData
//                    {
//                        m_Index = j
//                    };
//                }
//            }

//            // Token: 0x060032EF RID: 13039 RVA: 0x001AF874 File Offset: 0x001ADA74
//            private void FillExpandedNodes(DynamicBuffer<Node> nodes, DynamicBuffer<Expand> expands)
//            {
//                this.m_NodeList.ResizeUninitialized(nodes.Length);
//                for (int i = 0; i < nodes.Length; i++)
//                {
//                    Node value = nodes[i];
//                    Expand expand = expands[i];
//                    value.m_Position.xz = value.m_Position.xz + expand.m_Offset;
//                    this.m_NodeList[i] = value;
//                }
//            }

//            // Token: 0x060032F0 RID: 13040 RVA: 0x001AF8E4 File Offset: 0x001ADAE4
//            private void AddBorders<TNodeList>(TNodeList nodes, bool isCounterClockwise) where TNodeList : INativeList<Node>
//            {
//                this.m_BorderMap.Clear();
//                float3 @float = nodes[0].m_Position;
//                WEBatchSystem.TriangleUpdateJob.Border key;
//                for (int i = 1; i < nodes.Length; i++)
//                {
//                    float3 position = nodes[i].m_Position;
//                    if (isCounterClockwise)
//                    {
//                        key = default(WEBatchSystem.TriangleUpdateJob.Border);
//                        key.m_StartPos = position;
//                        key.m_EndPos = @float;
//                        this.m_BorderMap.Add(key, new int2(i, i - 1));
//                    }
//                    else
//                    {
//                        key = default(WEBatchSystem.TriangleUpdateJob.Border);
//                        key.m_StartPos = @float;
//                        key.m_EndPos = position;
//                        this.m_BorderMap.Add(key, new int2(i - 1, i));
//                    }
//                    @float = position;
//                }
//                float3 position2 = nodes[0].m_Position;
//                if (isCounterClockwise)
//                {
//                    key = default(WEBatchSystem.TriangleUpdateJob.Border);
//                    key.m_StartPos = position2;
//                    key.m_EndPos = @float;
//                    this.m_BorderMap.Add(key, new int2(0, nodes.Length - 1));
//                    return;
//                }
//                key = default(WEBatchSystem.TriangleUpdateJob.Border);
//                key.m_StartPos = @float;
//                key.m_EndPos = position2;
//                this.m_BorderMap.Add(key, new int2(nodes.Length - 1, 0));
//            }

//            // Token: 0x060032F1 RID: 13041 RVA: 0x001AFA30 File Offset: 0x001ADC30
//            private void AddNodes<TNodeList>(TNodeList nodes, DynamicBuffer<Triangle> triangles, bool isCounterClockwise) where TNodeList : INativeList<Node>
//            {
//                this.m_AdjacentNodes.ResizeUninitialized(nodes.Length);
//                for (int i = 0; i < this.m_AdjacentNodes.Length; i++)
//                {
//                    this.m_AdjacentNodes[i] = i;
//                }
//                for (int j = 0; j < triangles.Length; j++)
//                {
//                    Triangle triangle = triangles[j];
//                    int2 value = this.m_AdjacentNodes[triangle.m_Indices.x];
//                    int2 value2 = this.m_AdjacentNodes[triangle.m_Indices.y];
//                    int2 value3 = this.m_AdjacentNodes[triangle.m_Indices.z];
//                    this.CheckBorder<TNodeList>(ref value, ref value2, nodes, triangle.m_Indices.x, triangle.m_Indices.y, isCounterClockwise);
//                    this.CheckBorder<TNodeList>(ref value2, ref value3, nodes, triangle.m_Indices.y, triangle.m_Indices.z, isCounterClockwise);
//                    this.CheckBorder<TNodeList>(ref value3, ref value, nodes, triangle.m_Indices.z, triangle.m_Indices.x, isCounterClockwise);
//                    this.m_AdjacentNodes[triangle.m_Indices.x] = value;
//                    this.m_AdjacentNodes[triangle.m_Indices.y] = value2;
//                    this.m_AdjacentNodes[triangle.m_Indices.z] = value3;
//                }
//                for (int k = 0; k < this.m_AdjacentNodes.Length; k++)
//                {
//                    int2 @int = this.m_AdjacentNodes[k];
//                    bool2 @bool = @int != k;
//                    if (math.any(@bool))
//                    {
//                        if (@bool.x)
//                        {
//                            for (int l = 0; l < nodes.Length; l++)
//                            {
//                                int x = this.m_AdjacentNodes[@int.x].x;
//                                if (x == @int.x)
//                                {
//                                    break;
//                                }
//                                if (x == k || x == -1)
//                                {
//                                    @int.x = -1;
//                                    break;
//                                }
//                                @int.x = x;
//                            }
//                        }
//                        if (@bool.y)
//                        {
//                            for (int m = 0; m < nodes.Length; m++)
//                            {
//                                int y = this.m_AdjacentNodes[@int.y].y;
//                                if (y == @int.y)
//                                {
//                                    break;
//                                }
//                                if (y == k || y == -1)
//                                {
//                                    @int.y = -1;
//                                    break;
//                                }
//                                @int.y = y;
//                            }
//                        }
//                        this.m_AdjacentNodes[k] = @int;
//                    }
//                }
//                for (int n = 0; n < this.m_AdjacentNodes.Length; n++)
//                {
//                    int2 lhs = this.m_AdjacentNodes[n];
//                    this.m_AdjacentNodes[n] = math.select(math.select(lhs + new int2(-1, 1), new int2(nodes.Length - 1, 0), lhs == new int2(0, nodes.Length - 1)), n, lhs == -1);
//                }
//            }

//            // Token: 0x060032F2 RID: 13042 RVA: 0x001AFD48 File Offset: 0x001ADF48
//            private void CheckBorder<TNodeList>(ref int2 adjacentA, ref int2 adjacentB, TNodeList nodes, int nodeA, int nodeB, bool isCounterClockwise) where TNodeList : INativeList<Node>
//            {
//                WEBatchSystem.TriangleUpdateJob.Border border = default(WEBatchSystem.TriangleUpdateJob.Border);
//                border.m_StartPos = nodes[nodeA].m_Position;
//                border.m_EndPos = nodes[nodeB].m_Position;
//                WEBatchSystem.TriangleUpdateJob.Border key = border;
//                if (this.m_BorderMap.TryGetValue(key, out int2 @int))
//                {
//                    if (isCounterClockwise)
//                    {
//                        adjacentB.x = @int.y;
//                        adjacentA.y = @int.x;
//                        return;
//                    }
//                    adjacentA.x = @int.x;
//                    adjacentB.y = @int.y;
//                }
//            }

//            // Token: 0x060032F3 RID: 13043 RVA: 0x001AFDDC File Offset: 0x001ADFDC
//            private Bounds3 AddTriangles<TNodeList>(TNodeList nodes, DynamicBuffer<Triangle> triangles, RenderedAreaData renderedAreaData, int triangleOffset, float4 offsetDir, bool isCounterClockwise) where TNodeList : INativeList<Node>
//            {
//                Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
//                for (int i = 0; i < triangles.Length; i++)
//                {
//                    Triangle triangle = triangles[i];
//                    int2 @int = this.m_AdjacentNodes[triangle.m_Indices.x];
//                    int2 int2 = this.m_AdjacentNodes[triangle.m_Indices.y];
//                    int2 int3 = this.m_AdjacentNodes[triangle.m_Indices.z];
//                    int x = this.m_AdjacentNodes[@int.x].x;
//                    int x2 = this.m_AdjacentNodes[int2.x].x;
//                    int x3 = this.m_AdjacentNodes[int3.x].x;
//                    int y = this.m_AdjacentNodes[@int.y].y;
//                    int y2 = this.m_AdjacentNodes[int2.y].y;
//                    int y3 = this.m_AdjacentNodes[int3.y].y;
//                    AreaTriangleData areaTriangleData = default(AreaTriangleData);
//                    areaTriangleData.m_APos = AreaUtils.GetExpandedNode<TNodeList>(nodes, triangle.m_Indices.x, @int.x, @int.y, renderedAreaData.m_ExpandAmount, isCounterClockwise);
//                    areaTriangleData.m_BPos = AreaUtils.GetExpandedNode<TNodeList>(nodes, triangle.m_Indices.y, int2.x, int2.y, renderedAreaData.m_ExpandAmount, isCounterClockwise);
//                    areaTriangleData.m_CPos = AreaUtils.GetExpandedNode<TNodeList>(nodes, triangle.m_Indices.z, int3.x, int3.y, renderedAreaData.m_ExpandAmount, isCounterClockwise);
//                    areaTriangleData.m_APrevXZ = AreaUtils.GetExpandedNode<TNodeList>(nodes, @int.x, x, triangle.m_Indices.x, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz;
//                    areaTriangleData.m_BPrevXZ = AreaUtils.GetExpandedNode<TNodeList>(nodes, int2.x, x2, triangle.m_Indices.y, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz;
//                    areaTriangleData.m_CPrevXZ = AreaUtils.GetExpandedNode<TNodeList>(nodes, int3.x, x3, triangle.m_Indices.z, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz;
//                    areaTriangleData.m_ANextXZ = AreaUtils.GetExpandedNode<TNodeList>(nodes, @int.y, triangle.m_Indices.x, y, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz;
//                    areaTriangleData.m_BNextXZ = AreaUtils.GetExpandedNode<TNodeList>(nodes, int2.y, triangle.m_Indices.y, y2, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz;
//                    areaTriangleData.m_CNextXZ = AreaUtils.GetExpandedNode<TNodeList>(nodes, int3.y, triangle.m_Indices.z, y3, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz;
//                    float3 x4 = new float3(areaTriangleData.m_APos.y, areaTriangleData.m_BPos.y, areaTriangleData.m_CPos.y);
//                    areaTriangleData.m_YMinMax.x = triangle.m_HeightRange.min - renderedAreaData.m_HeightOffset + math.cmin(x4);
//                    areaTriangleData.m_YMinMax.y = triangle.m_HeightRange.max + renderedAreaData.m_HeightOffset + math.cmax(x4);
//                    areaTriangleData.m_OffsetDir = offsetDir;
//                    areaTriangleData.m_LodDistanceFactor = RenderingUtils.CalculateDistanceFactor(triangle.m_MinLod);
//                    Bounds3 rhs = MathUtils.Bounds(new Triangle3(areaTriangleData.m_APos, areaTriangleData.m_BPos, areaTriangleData.m_CPos));
//                    rhs.min.y = areaTriangleData.m_YMinMax.x;
//                    rhs.max.y = areaTriangleData.m_YMinMax.y;
//                    bounds |= rhs;
//                    ref WEBatchSystem.TriangleMetaData ptr = ref this.m_TriangleMetaData.ElementAt(triangleOffset + i);
//                    this.m_AreaTriangleData[triangleOffset + ptr.m_Index] = areaTriangleData;
//                }
//                return bounds;
//            }

//            // Token: 0x040046DE RID: 18142
//            [ReadOnly]
//            public ComponentLookup<Area> m_AreaData;

//            // Token: 0x040046DF RID: 18143
//            [ReadOnly]
//            public ComponentLookup<Owner> m_OwnerData;

//            // Token: 0x040046E0 RID: 18144
//            [ReadOnly]
//            public ComponentLookup<Game.Objects.Transform> m_TransformData;

//            // Token: 0x040046E1 RID: 18145
//            [ReadOnly]
//            public ComponentLookup<PrefabRef> m_PrefabRefData;

//            // Token: 0x040046E2 RID: 18146
//            [ReadOnly]
//            public ComponentLookup<RenderedAreaData> m_PrefabRenderedAreaData;

//            // Token: 0x040046E3 RID: 18147
//            [ReadOnly]
//            public BufferLookup<Node> m_Nodes;

//            // Token: 0x040046E4 RID: 18148
//            [ReadOnly]
//            public BufferLookup<Triangle> m_Triangles;

//            // Token: 0x040046E5 RID: 18149
//            [ReadOnly]
//            public BufferLookup<Expand> m_Expands;

//            // Token: 0x040046E6 RID: 18150
//            [ReadOnly]
//            public NativeParallelQueue<WEBatchSystem.CullingAction>.Reader m_CullingActions;

//            // Token: 0x040046E7 RID: 18151
//            [NativeDisableParallelForRestriction]
//            public ComponentLookup<Batch> m_BatchData;

//            // Token: 0x040046E8 RID: 18152
//            [NativeDisableParallelForRestriction]
//            public NativeList<WEBatchSystem.TriangleMetaData> m_TriangleMetaData;

//            // Token: 0x040046E9 RID: 18153
//            [NativeDisableParallelForRestriction]
//            public NativeList<AreaTriangleData> m_AreaTriangleData;

//            // Token: 0x040046EA RID: 18154
//            [NativeDisableParallelForRestriction]
//            public NativeList<WEBatchSystem.NativeBatchData> m_NativeBatchData;

//            // Token: 0x040046EB RID: 18155
//            [NativeDisableContainerSafetyRestriction]
//            private NativeParallelHashMap<WEBatchSystem.TriangleUpdateJob.Border, int2> m_BorderMap;

//            // Token: 0x040046EC RID: 18156
//            [NativeDisableContainerSafetyRestriction]
//            private NativeList<int2> m_AdjacentNodes;

//            // Token: 0x040046ED RID: 18157
//            [NativeDisableContainerSafetyRestriction]
//            private NativeList<Node> m_NodeList;

//            // Token: 0x040046EE RID: 18158
//            [NativeDisableContainerSafetyRestriction]
//            private NativeList<WEBatchSystem.TriangleSortData> m_TriangleSortData;

//            // Token: 0x02000B43 RID: 2883
//            private struct Border : IEquatable<WEBatchSystem.TriangleUpdateJob.Border>
//            {
//                // Token: 0x060032F4 RID: 13044 RVA: 0x001B01CF File Offset: 0x001AE3CF
//                public bool Equals(WEBatchSystem.TriangleUpdateJob.Border other)
//                {
//                    return this.m_StartPos.Equals(other.m_StartPos) & this.m_EndPos.Equals(other.m_EndPos);
//                }

//                // Token: 0x060032F5 RID: 13045 RVA: 0x001B01F4 File Offset: 0x001AE3F4
//                public override int GetHashCode()
//                {
//                    return this.m_StartPos.GetHashCode();
//                }

//                // Token: 0x040046EF RID: 18159
//                public float3 m_StartPos;

//                // Token: 0x040046F0 RID: 18160
//                public float3 m_EndPos;
//            }
//        }

//        // Token: 0x02000B44 RID: 2884
//        [BurstCompile]
//        private struct VisibleUpdateJob : IJobParallelFor
//        {
//            // Token: 0x060032F6 RID: 13046 RVA: 0x001B0208 File Offset: 0x001AE408
//            public void Execute(int index)
//            {
//                ref WEBatchSystem.NativeBatchData ptr = ref this.m_NativeBatchData.ElementAt(index);
//                if (ptr.m_BoundsUpdated)
//                {
//                    ptr.m_Bounds = new Bounds3(float.MaxValue, float.MinValue);
//                    ptr.m_BoundsUpdated = false;
//                    for (int i = 0; i < ptr.m_AreaMetaData.Length; i++)
//                    {
//                        ref WEBatchSystem.AreaMetaData ptr2 = ref ptr.m_AreaMetaData.ElementAt(i);
//                        ptr.m_Bounds |= ptr2.m_Bounds;
//                    }
//                }
//                if (ptr.m_VisibleUpdated)
//                {
//                    ptr.m_VisibleIndices.Clear();
//                    ptr.m_VisibleIndicesUpdated = true;
//                    ptr.m_VisibleUpdated = false;
//                    for (int j = 0; j < ptr.m_AreaMetaData.Length; j++)
//                    {
//                        ref WEBatchSystem.AreaMetaData ptr3 = ref ptr.m_AreaMetaData.ElementAt(j);
//                        ptr.m_Bounds |= ptr3.m_StartIndex;
//                        for (int k = 0; k < ptr3.m_VisibleCount; k++)
//                        {
//                            ref WEBatchSystem.NativeBatchData ptr4 = ref ptr;
//                            int num = ptr3.m_StartIndex + k;
//                            ptr4.m_VisibleIndices.Add(num);
//                        }
//                    }
//                }
//            }

//            // Token: 0x040046F1 RID: 18161
//            [NativeDisableParallelForRestriction]
//            public NativeList<WEBatchSystem.NativeBatchData> m_NativeBatchData;
//        }

//        // Token: 0x02000B45 RID: 2885
//        private struct TypeHandle
//        {
//            // Token: 0x060032F7 RID: 13047 RVA: 0x001B0328 File Offset: 0x001AE528
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public void __AssignHandles(ref SystemState state)
//            {
//                this.__Game_Prefabs_RenderedAreaData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RenderedAreaData>(false);
//                this.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(true);
//                this.__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AreaGeometryData>(true);
//            }

//            // Token: 0x040046F2 RID: 18162
//            public ComponentTypeHandle<RenderedAreaData> __Game_Prefabs_RenderedAreaData_RW_ComponentTypeHandle;

//            // Token: 0x040046F3 RID: 18163
//            [ReadOnly]
//            public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

//            // Token: 0x040046F4 RID: 18164
//            [ReadOnly]
//            public ComponentTypeHandle<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle;
//        }
//    }
//}