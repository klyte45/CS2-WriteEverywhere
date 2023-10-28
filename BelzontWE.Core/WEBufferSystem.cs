//#define BURST
////#define VERBOSE 
//using System;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using Colossal.Collections;
//using Colossal.Mathematics;
//using Colossal.Serialization.Entities;
//using Game.Areas;
//using Game.Common;
//using Game.Prefabs;
//using Game.SceneFlow;
//using Game.Serialization;
//using Game.Tools;
//using Game.UI;
//using TMPro;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.Scripting;
//using Game.Rendering;
//using Game;

//namespace BelzontWE.Rendering
//{
//    [CompilerGenerated]
//    public unsafe partial class WEBufferSystem : GameSystemBase, IPreDeserialize
//    {
//        // Token: 0x06003305 RID: 13061 RVA: 0x001B0FA0 File Offset: 0x001AF1A0
//        [Preserve]
//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            this.m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
//            this.m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
//            this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
//            this.m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
//            this.m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
//            this.m_AreaTypeData = new WEBufferSystem.AreaTypeData[5];
//            this.m_AreaTypeData[0] = this.InitializeAreaData<Lot>();
//            this.m_AreaTypeData[1] = this.InitializeAreaData<District>();
//            this.m_AreaTypeData[2] = this.InitializeAreaData<MapTile>();
//            this.m_AreaTypeData[3] = this.InitializeAreaData<Game.Areas.Space>();
//            this.m_AreaTypeData[4] = this.InitializeAreaData<Surface>();
//            this.m_SettingsQuery = base.GetEntityQuery(new ComponentType[]
//            {
//                ComponentType.ReadOnly<Created>(),
//                ComponentType.ReadOnly<Game.Prefabs.AreaTypeData>()
//            });
//            this.m_SelectionQuery = base.GetEntityQuery(new ComponentType[]
//            {
//                ComponentType.ReadOnly<SelectionInfo>(),
//                ComponentType.ReadOnly<SelectionElement>()
//            });
//            this.m_AreaParameters = Shader.PropertyToID("colossal_AreaParameters");
//            GameManager.instance.localizationManager.onActiveDictionaryChanged += this.OnDictionaryChanged;
//        }

//        // Token: 0x06003306 RID: 13062 RVA: 0x001B10DC File Offset: 0x001AF2DC
//        [Preserve]
//        protected override void OnDestroy()
//        {
//            for (int i = 0; i < this.m_AreaTypeData.Length; i++)
//            {
//                WEBufferSystem.AreaTypeData areaTypeData = this.m_AreaTypeData[i];
//                if (areaTypeData.m_NameMaterials != null)
//                {
//                    for (int j = 0; j < areaTypeData.m_NameMaterials.Count; j++)
//                    {
//                        WEBufferSystem.MaterialData materialData = areaTypeData.m_NameMaterials[j];
//                        if (materialData.m_Material != null)
//                        {
//                            UnityEngine.Object.Destroy(materialData.m_Material);
//                        }
//                    }
//                }
//                if (areaTypeData.m_BufferData.IsCreated)
//                {
//                    areaTypeData.m_BufferData.Dispose();
//                }
//                if (areaTypeData.m_Bounds.IsCreated)
//                {
//                    areaTypeData.m_Bounds.Dispose();
//                }
//                if (areaTypeData.m_Material != null)
//                {
//                    UnityEngine.Object.Destroy(areaTypeData.m_Material);
//                }
//                if (areaTypeData.m_NameMesh != null)
//                {
//                    UnityEngine.Object.Destroy(areaTypeData.m_NameMesh);
//                }
//                if (areaTypeData.m_Buffer != null)
//                {
//                    areaTypeData.m_Buffer.Release();
//                }
//                if (areaTypeData.m_HasNameMeshData)
//                {
//                    areaTypeData.m_NameMeshData.Dispose();
//                }
//            }
//            GameManager.instance.localizationManager.onActiveDictionaryChanged -= this.OnDictionaryChanged;
//            base.OnDestroy();
//        }

//        // Token: 0x06003307 RID: 13063 RVA: 0x001B11F8 File Offset: 0x001AF3F8
//        private void OnDictionaryChanged()
//        {
//            base.EntityManager.AddComponent<Updated>(this.m_AreaTypeData[1].m_AreaQuery);
//        }

//        // Token: 0x06003308 RID: 13064 RVA: 0x001B1220 File Offset: 0x001AF420
//        private WEBufferSystem.AreaTypeData InitializeAreaData<T>() where T : struct, IComponentData
//        {
//            WEBufferSystem.AreaTypeData areaTypeData = new WEBufferSystem.AreaTypeData();
//            areaTypeData.m_UpdatedQuery = base.GetEntityQuery(new EntityQueryDesc[]
//            {
//                new EntityQueryDesc
//                {
//                    All = new ComponentType[]
//                    {
//                        ComponentType.ReadOnly<Area>(),
//                        ComponentType.ReadOnly<T>(),
//                        ComponentType.ReadOnly<Node>(),
//                        ComponentType.ReadOnly<Triangle>()
//                    },
//                    Any = new ComponentType[]
//                    {
//                        ComponentType.ReadOnly<Updated>(),
//                        ComponentType.ReadOnly<BatchesUpdated>(),
//                        ComponentType.ReadOnly<Deleted>()
//                    }
//                }
//            });
//            areaTypeData.m_AreaQuery = base.GetEntityQuery(new ComponentType[]
//            {
//                ComponentType.ReadOnly<Area>(),
//                ComponentType.ReadOnly<T>(),
//                ComponentType.ReadOnly<Node>(),
//                ComponentType.ReadOnly<Triangle>(),
//                ComponentType.Exclude<Deleted>()
//            });
//            return areaTypeData;
//        }

//        // Token: 0x06003309 RID: 13065 RVA: 0x001B130C File Offset: 0x001AF50C
//        public void PreDeserialize(Context context)
//        {
//            for (int i = 0; i < this.m_AreaTypeData.Length; i++)
//            {
//                WEBufferSystem.AreaTypeData areaTypeData = this.m_AreaTypeData[i];
//                if (areaTypeData.m_BufferData.IsCreated)
//                {
//                    areaTypeData.m_BufferData.Dispose();
//                    areaTypeData.m_BufferData = default(NativeList<WEBufferSystem.AreaTriangleData>);
//                }
//                if (areaTypeData.m_Buffer != null)
//                {
//                    areaTypeData.m_Buffer.Release();
//                    areaTypeData.m_Buffer = null;
//                }
//                if (areaTypeData.m_NameMesh != null)
//                {
//                    UnityEngine.Object.Destroy(areaTypeData.m_NameMesh);
//                    areaTypeData.m_NameMesh = null;
//                }
//                if (areaTypeData.m_HasNameMeshData)
//                {
//                    areaTypeData.m_NameMeshData.Dispose();
//                    areaTypeData.m_HasNameMeshData = false;
//                }
//            }
//            if (this.m_CachedLabels != null)
//            {
//                this.m_CachedLabels.Clear();
//            }
//            this.m_Loaded = true;
//        }

//        // Token: 0x0600330A RID: 13066 RVA: 0x001B13CD File Offset: 0x001AF5CD
//        private bool GetLoaded()
//        {
//            if (this.m_Loaded)
//            {
//                this.m_Loaded = false;
//                return true;
//            }
//            return false;
//        }

//        // Token: 0x0600330B RID: 13067 RVA: 0x001B13E4 File Offset: 0x001AF5E4
//        [Preserve]
//        protected override void OnUpdate()
//        {
//            bool loaded = this.GetLoaded();
//            if (!this.m_SettingsQuery.IsEmptyIgnoreFilter)
//            {
//                NativeArray<ArchetypeChunk> nativeArray = this.m_SettingsQuery.ToArchetypeChunkArray(Allocator.TempJob);
//                this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<PrefabData> _Game_Prefabs_PrefabData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
//                for (int i = 0; i < nativeArray.Length; i++)
//                {
//                    NativeArray<PrefabData> nativeArray2 = nativeArray[i].GetNativeArray<PrefabData>(ref _Game_Prefabs_PrefabData_RO_ComponentTypeHandle);
//                    for (int j = 0; j < nativeArray2.Length; j++)
//                    {
//                        AreaTypePrefab prefab = this.m_PrefabSystem.GetPrefab<AreaTypePrefab>(nativeArray2[j]);
//                        WEBufferSystem.AreaTypeData areaTypeData = this.m_AreaTypeData[(int)prefab.m_Type];
//                        float minNodeDistance = AreaUtils.GetMinNodeDistance(prefab.m_Type);
//                        if (areaTypeData.m_Material != null)
//                        {
//                            UnityEngine.Object.Destroy(areaTypeData.m_Material);
//                        }
//                        areaTypeData.m_Material = new Material(prefab.m_Material);
//                        areaTypeData.m_Material.name = "Area buffer (" + prefab.m_Material.name + ")";
//                        areaTypeData.m_Material.SetVector(this.m_AreaParameters, new Vector4(minNodeDistance * 0.03125f, minNodeDistance * 0.25f, minNodeDistance * 2f, 0f));
//                        if (areaTypeData.m_NameMaterials != null)
//                        {
//                            for (int k = 0; k < areaTypeData.m_NameMaterials.Count; k++)
//                            {
//                                WEBufferSystem.MaterialData materialData = areaTypeData.m_NameMaterials[k];
//                                if (materialData.m_Material != null)
//                                {
//                                    UnityEngine.Object.Destroy(materialData.m_Material);
//                                }
//                            }
//                            areaTypeData.m_NameMaterials = null;
//                        }
//                        areaTypeData.m_OriginalNameMaterial = prefab.m_NameMaterial;
//                        if (prefab.m_NameMaterial != null)
//                        {
//                            areaTypeData.m_NameMaterials = new List<WEBufferSystem.MaterialData>(1);
//                        }
//                    }
//                }
//                nativeArray.Dispose();
//            }
//            JobHandle jobHandle = default(JobHandle);
//            AreaType areaType = AreaType.None;
//            Entity entity = Entity.Null;
//            if (!this.m_SelectionQuery.IsEmptyIgnoreFilter)
//            {
//                entity = this.m_SelectionQuery.GetSingletonEntity();
//                areaType = base.EntityManager.GetComponentData<SelectionInfo>(entity).m_AreaType;
//            }
//            if (this.m_LastSelectionAreaType != AreaType.None)
//            {
//                this.m_AreaTypeData[(int)this.m_LastSelectionAreaType].m_BufferDataDirty = true;
//            }
//            if (areaType != AreaType.None)
//            {
//                this.m_AreaTypeData[(int)areaType].m_BufferDataDirty = true;
//            }
//            this.m_LastSelectionAreaType = areaType;
//            for (int l = 0; l < this.m_AreaTypeData.Length; l++)
//            {
//                WEBufferSystem.AreaTypeData areaTypeData2 = this.m_AreaTypeData[l];
//                EntityQuery entityQuery = loaded ? areaTypeData2.m_AreaQuery : areaTypeData2.m_UpdatedQuery;
//                if (areaTypeData2.m_BufferDataDirty || !entityQuery.IsEmptyIgnoreFilter)
//                {
//                    if (areaTypeData2.m_AreaQuery.IsEmptyIgnoreFilter)
//                    {
//                        areaTypeData2.m_BufferDataDirty = false;
//                        areaTypeData2.m_BufferDirty = false;
//                        if (areaTypeData2.m_Buffer != null)
//                        {
//                            areaTypeData2.m_Buffer.Release();
//                            areaTypeData2.m_Buffer = null;
//                        }
//                        if (areaTypeData2.m_NameMesh != null)
//                        {
//                            UnityEngine.Object.Destroy(areaTypeData2.m_NameMesh);
//                            areaTypeData2.m_NameMesh = null;
//                        }
//                    }
//                    else
//                    {
//                        areaTypeData2.m_BufferDataDirty = true;
//                    }
//                    if (areaTypeData2.m_NameMaterials != null && !entityQuery.IsEmptyIgnoreFilter)
//                    {
//                        this.UpdateLabelVertices(areaTypeData2, loaded);
//                    }
//                }
//            }
//            if (!this.m_RenderingSystem.hideOverlay)
//            {
//                for (int m = 0; m < this.m_AreaTypeData.Length; m++)
//                {
//                    WEBufferSystem.AreaTypeData areaTypeData3 = this.m_AreaTypeData[m];
//                    if (areaTypeData3.m_BufferDataDirty && (areaTypeData3.m_NameMaterials != null || (this.m_ToolSystem.activeTool != null && (this.m_ToolSystem.activeTool.requireAreas & (AreaTypeMask)(1 << m)) != AreaTypeMask.None)))
//                    {
//                        areaTypeData3.m_BufferDataDirty = false;
//                        areaTypeData3.m_BufferDirty = true;
//                        JobHandle job;
//                        NativeList<ArchetypeChunk> nativeList = areaTypeData3.m_AreaQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out job);
//                        NativeList<WEBufferSystem.ChunkData> chunkData = new NativeList<WEBufferSystem.ChunkData>(0, Allocator.TempJob);
//                        if (!areaTypeData3.m_BufferData.IsCreated)
//                        {
//                            areaTypeData3.m_BufferData = new NativeList<WEBufferSystem.AreaTriangleData>(Allocator.Persistent);
//                        }
//                        if (!areaTypeData3.m_Bounds.IsCreated)
//                        {
//                            areaTypeData3.m_Bounds = new NativeValue<Bounds3>(Allocator.Persistent);
//                        }
//                        this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        WEBufferSystem.ResetChunkDataJob resetChunkDataJob = default(WEBufferSystem.ResetChunkDataJob);
//                        resetChunkDataJob.m_Chunks = nativeList;
//                        resetChunkDataJob.m_AreaType = this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle;
//                        resetChunkDataJob.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
//                        resetChunkDataJob.m_HiddenType = this.__TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle;
//                        resetChunkDataJob.m_TriangleType = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle;
//                        resetChunkDataJob.m_ChunkData = chunkData;
//                        resetChunkDataJob.m_AreaTriangleData = areaTypeData3.m_BufferData;
//                        WEBufferSystem.ResetChunkDataJob jobData = resetChunkDataJob;
//                        this.__TypeHandle.__Game_Tools_SelectionElement_RO_BufferLookup.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Prefabs_AreaColorData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                        this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
//                        WEBufferSystem.FillMeshDataJob fillMeshDataJob = default(WEBufferSystem.FillMeshDataJob);
//                        fillMeshDataJob.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
//                        fillMeshDataJob.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
//                        fillMeshDataJob.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
//                        fillMeshDataJob.m_HiddenType = this.__TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle;
//                        fillMeshDataJob.m_AreaType = this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle;
//                        fillMeshDataJob.m_NativeType = this.__TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle;
//                        fillMeshDataJob.m_NodeType = this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle;
//                        fillMeshDataJob.m_TriangleType = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle;
//                        fillMeshDataJob.m_GeometryData = this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup;
//                        fillMeshDataJob.m_ColorData = this.__TypeHandle.__Game_Prefabs_AreaColorData_RO_ComponentLookup;
//                        fillMeshDataJob.m_SelectionElements = this.__TypeHandle.__Game_Tools_SelectionElement_RO_BufferLookup;
//                        fillMeshDataJob.m_SelectionEntity = entity;
//                        fillMeshDataJob.m_EditorMode = this.m_ToolSystem.actionMode.IsEditor();
//                        fillMeshDataJob.m_Chunks = nativeList.AsDeferredJobArray();
//                        fillMeshDataJob.m_ChunkData = chunkData;
//                        fillMeshDataJob.m_AreaTriangleData = areaTypeData3.m_BufferData;
//                        WEBufferSystem.FillMeshDataJob jobData2 = fillMeshDataJob;
//                        WEBufferSystem.CalculateBoundsJob calculateBoundsJob = default(WEBufferSystem.CalculateBoundsJob);
//                        calculateBoundsJob.m_ChunkData = chunkData;
//                        calculateBoundsJob.m_Bounds = areaTypeData3.m_Bounds;
//                        WEBufferSystem.CalculateBoundsJob jobData3 = calculateBoundsJob;
//                        JobHandle dependsOn = JobHandle.CombineDependencies(base.Dependency, job);
//                        JobHandle dependsOn2 = jobData.Schedule(dependsOn);
//                        JobHandle jobHandle2 = jobData2.Schedule(nativeList, 1, dependsOn2);
//                        JobHandle jobHandle3 = jobData3.Schedule(jobHandle2);
//                        chunkData.Dispose(jobHandle3);
//                        if (areaTypeData3.m_NameMaterials != null)
//                        {
//                            if (!areaTypeData3.m_HasNameMeshData)
//                            {
//                                areaTypeData3.m_HasNameMeshData = true;
//                                areaTypeData3.m_NameMeshData = Mesh.AllocateWritableMeshData(1);
//                            }
//                            this.__TypeHandle.__Game_Prefabs_AreaNameData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
//                            this.__TypeHandle.__Game_Areas_LabelVertex_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
//                            this.__TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                            this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                            this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                            WEBufferSystem.FillNameDataJob jobData4 = default(WEBufferSystem.FillNameDataJob);
//                            jobData4.m_GeometryType = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle;
//                            jobData4.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
//                            jobData4.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
//                            jobData4.m_HiddenType = this.__TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle;
//                            jobData4.m_LabelVertexType = this.__TypeHandle.__Game_Areas_LabelVertex_RO_BufferTypeHandle;
//                            jobData4.m_AreaNameData = this.__TypeHandle.__Game_Prefabs_AreaNameData_RO_ComponentLookup;
//                            jobData4.m_Chunks = nativeList;
//                            jobData4.m_SubMeshCount = areaTypeData3.m_NameMaterials.Count;
//                            jobData4.m_NameMeshData = areaTypeData3.m_NameMeshData;
//                            JobHandle job2 = jobData4.Schedule(dependsOn);
//                            nativeList.Dispose(JobHandle.CombineDependencies(jobHandle2, job2));
//                            areaTypeData3.m_DataDependencies = JobHandle.CombineDependencies(jobHandle3, job2);
//                        }
//                        else
//                        {
//                            nativeList.Dispose(jobHandle2);
//                            areaTypeData3.m_DataDependencies = jobHandle3;
//                        }
//                        jobHandle = JobHandle.CombineDependencies(jobHandle, areaTypeData3.m_DataDependencies);
//                    }
//                }
//            }
//            base.Dependency = jobHandle;
//        }

//        // Token: 0x0600330C RID: 13068 RVA: 0x001B1CD8 File Offset: 0x001AFED8
//        public bool GetAreaBuffer(AreaType type, out ComputeBuffer buffer, out Material material, out Bounds bounds)
//        {
//            WEBufferSystem.AreaTypeData areaTypeData = this.m_AreaTypeData[(int)type];
//            if (areaTypeData.m_BufferDirty)
//            {
//                areaTypeData.m_BufferDirty = false;
//                areaTypeData.m_DataDependencies.Complete();
//                areaTypeData.m_DataDependencies = default(JobHandle);
//                if (areaTypeData.m_BufferData.IsCreated)
//                {
//                    if (areaTypeData.m_Buffer != null && areaTypeData.m_Buffer.count != areaTypeData.m_BufferData.Length)
//                    {
//                        areaTypeData.m_Buffer.Release();
//                        areaTypeData.m_Buffer = null;
//                    }
//                    if (areaTypeData.m_BufferData.Length > 0)
//                    {
//                        if (areaTypeData.m_Buffer == null)
//                        {
//                            areaTypeData.m_Buffer = new ComputeBuffer(areaTypeData.m_BufferData.Length, sizeof(WEBufferSystem.AreaTriangleData));
//                        }
//                        areaTypeData.m_Buffer.SetData<WEBufferSystem.AreaTriangleData>(areaTypeData.m_BufferData.AsArray());
//                    }
//                    areaTypeData.m_BufferData.Dispose();
//                }
//            }
//            buffer = areaTypeData.m_Buffer;
//            material = areaTypeData.m_Material;
//            if (areaTypeData.m_Bounds.IsCreated)
//            {
//                bounds = RenderingUtils.ToBounds(areaTypeData.m_Bounds.value);
//            }
//            else
//            {
//                bounds = default(Bounds);
//            }
//            return areaTypeData.m_Buffer != null && areaTypeData.m_Buffer.count != 0;
//        }

//        // Token: 0x0600330D RID: 13069 RVA: 0x001B1E04 File Offset: 0x001B0004
//        private void UpdateLabelVertices(WEBufferSystem.AreaTypeData data, bool isLoaded)
//        {
//            using (NativeArray<ArchetypeChunk> nativeArray = (isLoaded ? data.m_AreaQuery : data.m_UpdatedQuery).ToArchetypeChunkArray(Allocator.TempJob))
//            {
//                TextMeshPro textMesh = this.m_OverlayRenderSystem.GetTextMesh();
//                textMesh.rectTransform.sizeDelta = new Vector2(250f, 100f);
//                textMesh.fontSize = 200f;
//                textMesh.alignment = TextAlignmentOptions.Center;
//                this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
//                EntityTypeHandle _Unity_Entities_Entity_TypeHandle = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
//                this.__TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<Updated> _Game_Common_Updated_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle;
//                this.__TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<BatchesUpdated> _Game_Common_BatchesUpdated_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle;
//                this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<Temp> _Game_Tools_Temp_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
//                this.__TypeHandle.__Game_Areas_LabelExtents_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
//                BufferTypeHandle<LabelExtents> _Game_Areas_LabelExtents_RW_BufferTypeHandle = this.__TypeHandle.__Game_Areas_LabelExtents_RW_BufferTypeHandle;
//                this.__TypeHandle.__Game_Areas_LabelVertex_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
//                BufferTypeHandle<LabelVertex> _Game_Areas_LabelVertex_RW_BufferTypeHandle = this.__TypeHandle.__Game_Areas_LabelVertex_RW_BufferTypeHandle;
//                for (int i = 0; i < nativeArray.Length; i++)
//                {
//                    ArchetypeChunk archetypeChunk = nativeArray[i];
//                    if (isLoaded || archetypeChunk.Has<Updated>(ref _Game_Common_Updated_RO_ComponentTypeHandle) || archetypeChunk.Has<BatchesUpdated>(ref _Game_Common_BatchesUpdated_RO_ComponentTypeHandle))
//                    {
//                        NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
//                        NativeArray<Temp> nativeArray3 = archetypeChunk.GetNativeArray<Temp>(ref _Game_Tools_Temp_RO_ComponentTypeHandle);
//                        BufferAccessor<LabelExtents> bufferAccessor = archetypeChunk.GetBufferAccessor<LabelExtents>(ref _Game_Areas_LabelExtents_RW_BufferTypeHandle);
//                        BufferAccessor<LabelVertex> bufferAccessor2 = archetypeChunk.GetBufferAccessor<LabelVertex>(ref _Game_Areas_LabelVertex_RW_BufferTypeHandle);
//                        int j = 0;
//                        while (j < nativeArray2.Length)
//                        {
//                            Entity entity = nativeArray2[j];
//                            DynamicBuffer<LabelExtents> dynamicBuffer = bufferAccessor[j];
//                            DynamicBuffer<LabelVertex> dynamicBuffer2 = bufferAccessor2[j];
//                            string renderedLabelName;
//                            if (nativeArray3.Length == 0)
//                            {
//                                renderedLabelName = this.m_NameSystem.GetRenderedLabelName(entity);
//                                goto IL_22D;
//                            }
//                            Temp temp = nativeArray3[j];
//                            if (temp.m_Original != Entity.Null)
//                            {
//                                renderedLabelName = this.m_NameSystem.GetRenderedLabelName(temp.m_Original);
//                                goto IL_22D;
//                            }
//                            if (this.m_CachedLabels != null && this.m_CachedLabels.ContainsKey(entity))
//                            {
//                                this.m_CachedLabels.Remove(entity);
//                            }
//                            dynamicBuffer2.Clear();
//                        IL_4ED:
//                            j++;
//                            continue;
//                        IL_22D:
//                            if (this.m_CachedLabels != null)
//                            {
//                                string a;
//                                if (this.m_CachedLabels.TryGetValue(entity, out a))
//                                {
//                                    if (a == renderedLabelName)
//                                    {
//                                        goto IL_4ED;
//                                    }
//                                    this.m_CachedLabels[entity] = renderedLabelName;
//                                }
//                                else
//                                {
//                                    this.m_CachedLabels.Add(entity, renderedLabelName);
//                                }
//                            }
//                            else
//                            {
//                                this.m_CachedLabels = new Dictionary<Entity, string>();
//                                this.m_CachedLabels.Add(entity, renderedLabelName);
//                            }
//                            TMP_TextInfo textInfo = textMesh.GetTextInfo(renderedLabelName);
//                            int num = 0;
//                            for (int k = 0; k < textInfo.meshInfo.Length; k++)
//                            {
//                                TMP_MeshInfo tmp_MeshInfo = textInfo.meshInfo[k];
//                                num += tmp_MeshInfo.vertexCount;
//                            }
//                            dynamicBuffer2.ResizeUninitialized(num);
//                            num = 0;
//                            for (int l = 0; l < textInfo.meshInfo.Length; l++)
//                            {
//                                TMP_MeshInfo tmp_MeshInfo2 = textInfo.meshInfo[l];
//                                if (tmp_MeshInfo2.vertexCount != 0)
//                                {
//                                    Texture mainTexture = tmp_MeshInfo2.material.mainTexture;
//                                    int num2 = -1;
//                                    for (int m = 0; m < data.m_NameMaterials.Count; m++)
//                                    {
//                                        if (data.m_NameMaterials[m].m_Material.mainTexture == mainTexture)
//                                        {
//                                            num2 = m;
//                                            break;
//                                        }
//                                    }
//                                    if (num2 == -1)
//                                    {
//                                        WEBufferSystem.MaterialData materialData = default(WEBufferSystem.MaterialData);
//                                        materialData.m_Material = new Material(data.m_OriginalNameMaterial);
//                                        this.m_OverlayRenderSystem.CopyFontAtlasParameters(tmp_MeshInfo2.material, materialData.m_Material);
//                                        num2 = data.m_NameMaterials.Count;
//                                        data.m_NameMaterials.Add(materialData);
//                                        materialData.m_Material.name = string.Format("Area names {0} ({1})", num2, data.m_OriginalNameMaterial.name);
//                                    }
//                                    Vector3[] vertices = tmp_MeshInfo2.vertices;
//                                    Vector2[] uvs = tmp_MeshInfo2.uvs0;
//                                    Vector2[] uvs2 = tmp_MeshInfo2.uvs2;
//                                    Color32[] colors = tmp_MeshInfo2.colors32;
//                                    for (int n = 0; n < tmp_MeshInfo2.vertexCount; n++)
//                                    {
//                                        LabelVertex value;
//                                        value.m_Position = vertices[n];
//                                        value.m_Color = colors[n];
//                                        value.m_UV0 = uvs[n];
//                                        value.m_UV1 = uvs2[n];
//                                        value.m_Material = num2;
//                                        dynamicBuffer2[num + n] = value;
//                                    }
//                                    num += tmp_MeshInfo2.vertexCount;
//                                }
//                            }
//                            dynamicBuffer.ResizeUninitialized(textInfo.lineCount);
//                            for (int num3 = 0; num3 < textInfo.lineCount; num3++)
//                            {
//                                Extents lineExtents = textInfo.lineInfo[num3].lineExtents;
//                                dynamicBuffer[num3] = new LabelExtents(lineExtents.min, lineExtents.max);
//                            }
//                            goto IL_4ED;
//                        }
//                    }
//                    else if (this.m_CachedLabels != null)
//                    {
//                        NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
//                        for (int num4 = 0; num4 < nativeArray4.Length; num4++)
//                        {
//                            Entity key = nativeArray4[num4];
//                            this.m_CachedLabels.Remove(key);
//                        }
//                    }
//                }
//            }
//        }

//        // Token: 0x0600330E RID: 13070 RVA: 0x001B2390 File Offset: 0x001B0590
//        public bool GetNameMesh(AreaType type, out Mesh mesh, out int subMeshCount)
//        {
//            WEBufferSystem.AreaTypeData areaTypeData = this.m_AreaTypeData[(int)type];
//            if (areaTypeData.m_NameMaterials != null)
//            {
//                subMeshCount = areaTypeData.m_NameMaterials.Count;
//            }
//            else
//            {
//                subMeshCount = 0;
//            }
//            if (areaTypeData.m_HasNameMeshData)
//            {
//                areaTypeData.m_HasNameMeshData = false;
//                areaTypeData.m_DataDependencies.Complete();
//                areaTypeData.m_DataDependencies = default(JobHandle);
//                if (areaTypeData.m_NameMesh == null)
//                {
//                    areaTypeData.m_NameMesh = new Mesh();
//                    areaTypeData.m_NameMesh.name = string.Format("Area names ({0})", type);
//                }
//                Mesh.ApplyAndDisposeWritableMeshData(areaTypeData.m_NameMeshData, areaTypeData.m_NameMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
//                if (areaTypeData.m_Bounds.IsCreated && math.all(areaTypeData.m_Bounds.value.max >= areaTypeData.m_Bounds.value.min))
//                {
//                    areaTypeData.m_NameMesh.bounds = RenderingUtils.ToBounds(areaTypeData.m_Bounds.value);
//                }
//                else
//                {
//                    areaTypeData.m_NameMesh.RecalculateBounds();
//                }
//                areaTypeData.m_HasNameMesh = false;
//                for (int i = 0; i < subMeshCount; i++)
//                {
//                    WEBufferSystem.MaterialData materialData = areaTypeData.m_NameMaterials[i];
//                    materialData.m_HasMesh = (areaTypeData.m_NameMesh.GetSubMesh(i).vertexCount > 0);
//                    areaTypeData.m_HasNameMesh |= materialData.m_HasMesh;
//                    areaTypeData.m_NameMaterials[i] = materialData;
//                }
//            }
//            mesh = areaTypeData.m_NameMesh;
//            return areaTypeData.m_HasNameMesh;
//        }

//        // Token: 0x0600330F RID: 13071 RVA: 0x001B2500 File Offset: 0x001B0700
//        public bool GetNameMaterial(AreaType type, int subMeshIndex, out Material material)
//        {
//            WEBufferSystem.MaterialData materialData = this.m_AreaTypeData[(int)type].m_NameMaterials[subMeshIndex];
//            material = materialData.m_Material;
//            return materialData.m_HasMesh;
//        }

//        // Token: 0x06003310 RID: 13072 RVA: 0x00002E1D File Offset: 0x0000101D
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void __AssignQueries(ref SystemState state)
//        {
//        }

//        // Token: 0x06003311 RID: 13073 RVA: 0x001B252F File Offset: 0x001B072F
//        protected override void OnCreateForCompiler()
//        {
//            base.OnCreateForCompiler();
//            this.__AssignQueries(ref base.CheckedStateRef);
//            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
//        }

//        // Token: 0x06003312 RID: 13074 RVA: 0x000068B3 File Offset: 0x00004AB3
//        [Preserve]
//        public WEBufferSystem()
//        {
//        }

//        // Token: 0x04004712 RID: 18194
//        private EntityQuery m_SettingsQuery;

//        // Token: 0x04004713 RID: 18195
//        private RenderingSystem m_RenderingSystem;

//        // Token: 0x04004714 RID: 18196
//        private OverlayRenderSystem m_OverlayRenderSystem;

//        // Token: 0x04004715 RID: 18197
//        private PrefabSystem m_PrefabSystem;

//        // Token: 0x04004716 RID: 18198
//        private ToolSystem m_ToolSystem;

//        // Token: 0x04004717 RID: 18199
//        private NameSystem m_NameSystem;

//        // Token: 0x04004718 RID: 18200
//        private WEBufferSystem.AreaTypeData[] m_AreaTypeData;

//        // Token: 0x04004719 RID: 18201
//        private AreaType m_LastSelectionAreaType;

//        // Token: 0x0400471A RID: 18202
//        private EntityQuery m_SelectionQuery;

//        // Token: 0x0400471B RID: 18203
//        private bool m_Loaded;

//        // Token: 0x0400471C RID: 18204
//        private int m_AreaParameters;

//        // Token: 0x0400471D RID: 18205
//        private Dictionary<Entity, string> m_CachedLabels;

//        // Token: 0x0400471E RID: 18206
//        private WEBufferSystem.TypeHandle __TypeHandle;

//        // Token: 0x02000B4B RID: 2891
//        private struct AreaTriangleData
//        {
//            // Token: 0x0400471F RID: 18207
//            public Vector3 m_APos;

//            // Token: 0x04004720 RID: 18208
//            public Vector3 m_BPos;

//            // Token: 0x04004721 RID: 18209
//            public Vector3 m_CPos;

//            // Token: 0x04004722 RID: 18210
//            public Vector2 m_APrevXZ;

//            // Token: 0x04004723 RID: 18211
//            public Vector2 m_BPrevXZ;

//            // Token: 0x04004724 RID: 18212
//            public Vector2 m_CPrevXZ;

//            // Token: 0x04004725 RID: 18213
//            public Vector2 m_ANextXZ;

//            // Token: 0x04004726 RID: 18214
//            public Vector2 m_BNextXZ;

//            // Token: 0x04004727 RID: 18215
//            public Vector2 m_CNextXZ;

//            // Token: 0x04004728 RID: 18216
//            public Vector2 m_YMinMax;

//            // Token: 0x04004729 RID: 18217
//            public Vector4 m_FillColor;

//            // Token: 0x0400472A RID: 18218
//            public Vector4 m_EdgeColor;
//        }

//        // Token: 0x02000B4C RID: 2892
//        private struct MaterialData
//        {
//            // Token: 0x0400472B RID: 18219
//            public Material m_Material;

//            // Token: 0x0400472C RID: 18220
//            public bool m_HasMesh;
//        }

//        // Token: 0x02000B4D RID: 2893
//        private class AreaTypeData
//        {
//            // Token: 0x0400472D RID: 18221
//            public EntityQuery m_UpdatedQuery;

//            // Token: 0x0400472E RID: 18222
//            public EntityQuery m_AreaQuery;

//            // Token: 0x0400472F RID: 18223
//            public NativeList<WEBufferSystem.AreaTriangleData> m_BufferData;

//            // Token: 0x04004730 RID: 18224
//            public NativeValue<Bounds3> m_Bounds;

//            // Token: 0x04004731 RID: 18225
//            public JobHandle m_DataDependencies;

//            // Token: 0x04004732 RID: 18226
//            public ComputeBuffer m_Buffer;

//            // Token: 0x04004733 RID: 18227
//            public Material m_Material;

//            // Token: 0x04004734 RID: 18228
//            public Material m_OriginalNameMaterial;

//            // Token: 0x04004735 RID: 18229
//            public List<WEBufferSystem.MaterialData> m_NameMaterials;

//            // Token: 0x04004736 RID: 18230
//            public Mesh m_NameMesh;

//            // Token: 0x04004737 RID: 18231
//            public Mesh.MeshDataArray m_NameMeshData;

//            // Token: 0x04004738 RID: 18232
//            public bool m_BufferDataDirty;

//            // Token: 0x04004739 RID: 18233
//            public bool m_BufferDirty;

//            // Token: 0x0400473A RID: 18234
//            public bool m_HasNameMeshData;

//            // Token: 0x0400473B RID: 18235
//            public bool m_HasNameMesh;
//        }

//        // Token: 0x02000B4E RID: 2894
//        private struct ChunkData
//        {
//            // Token: 0x0400473C RID: 18236
//            public int m_TriangleOffset;

//            // Token: 0x0400473D RID: 18237
//            public Bounds3 m_Bounds;
//        }

//        // Token: 0x02000B4F RID: 2895
//        [BurstCompile]
//        private struct ResetChunkDataJob : IJob
//        {
//            // Token: 0x06003314 RID: 13076 RVA: 0x001B2554 File Offset: 0x001B0754
//            public void Execute()
//            {
//                this.m_ChunkData.ResizeUninitialized(this.m_Chunks.Length);
//                WEBufferSystem.ChunkData chunkData = default(WEBufferSystem.ChunkData);
//                chunkData.m_Bounds = new Bounds3(float.MaxValue, float.MinValue);
//                for (int i = 0; i < this.m_Chunks.Length; i++)
//                {
//                    this.m_ChunkData[i] = chunkData;
//                    ArchetypeChunk archetypeChunk = this.m_Chunks[i];
//                    if (!archetypeChunk.Has<Hidden>(ref this.m_HiddenType))
//                    {
//                        NativeArray<Area> nativeArray = archetypeChunk.GetNativeArray<Area>(ref this.m_AreaType);
//                        NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray<Temp>(ref this.m_TempType);
//                        BufferAccessor<Triangle> bufferAccessor = archetypeChunk.GetBufferAccessor<Triangle>(ref this.m_TriangleType);
//                        for (int j = 0; j < bufferAccessor.Length; j++)
//                        {
//                            if ((nativeArray[j].m_Flags & AreaFlags.Slave) == (AreaFlags)0 && (nativeArray2.Length == 0 || (nativeArray2[j].m_Flags & TempFlags.Hidden) == (TempFlags)0u))
//                            {
//                                DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor[j];
//                                chunkData.m_TriangleOffset += dynamicBuffer.Length;
//                            }
//                        }
//                    }
//                }
//                this.m_AreaTriangleData.ResizeUninitialized(chunkData.m_TriangleOffset);
//            }

//            // Token: 0x0400473E RID: 18238
//            [ReadOnly]
//            public NativeList<ArchetypeChunk> m_Chunks;

//            // Token: 0x0400473F RID: 18239
//            [ReadOnly]
//            public ComponentTypeHandle<Area> m_AreaType;

//            // Token: 0x04004740 RID: 18240
//            [ReadOnly]
//            public ComponentTypeHandle<Temp> m_TempType;

//            // Token: 0x04004741 RID: 18241
//            [ReadOnly]
//            public ComponentTypeHandle<Hidden> m_HiddenType;

//            // Token: 0x04004742 RID: 18242
//            [ReadOnly]
//            public BufferTypeHandle<Triangle> m_TriangleType;

//            // Token: 0x04004743 RID: 18243
//            public NativeList<WEBufferSystem.ChunkData> m_ChunkData;

//            // Token: 0x04004744 RID: 18244
//            public NativeList<WEBufferSystem.AreaTriangleData> m_AreaTriangleData;
//        }

//        // Token: 0x02000B50 RID: 2896
//        [BurstCompile]
//        private struct FillMeshDataJob : IJobParallelForDefer
//        {
//            // Token: 0x06003315 RID: 13077 RVA: 0x001B2688 File Offset: 0x001B0888
//            public void Execute(int index)
//            {
//                ArchetypeChunk archetypeChunk = this.m_Chunks[index];
//                if (archetypeChunk.Has<Hidden>(ref this.m_HiddenType))
//                {
//                    return;
//                }
//                WEBufferSystem.ChunkData value = this.m_ChunkData[index];
//                NativeArray<Area> nativeArray = archetypeChunk.GetNativeArray<Area>(ref this.m_AreaType);
//                NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray<Temp>(ref this.m_TempType);
//                NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray<PrefabRef>(ref this.m_PrefabRefType);
//                BufferAccessor<Node> bufferAccessor = archetypeChunk.GetBufferAccessor<Node>(ref this.m_NodeType);
//                BufferAccessor<Triangle> bufferAccessor2 = archetypeChunk.GetBufferAccessor<Triangle>(ref this.m_TriangleType);
//                bool flag = this.m_EditorMode || archetypeChunk.Has<Native>(ref this.m_NativeType);
//                if (this.m_SelectionElements.HasBuffer(this.m_SelectionEntity))
//                {
//                    DynamicBuffer<SelectionElement> dynamicBuffer = this.m_SelectionElements[this.m_SelectionEntity];
//                    NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(dynamicBuffer.Length, Allocator.Temp);
//                    for (int i = 0; i < dynamicBuffer.Length; i++)
//                    {
//                        nativeParallelHashSet.Add(dynamicBuffer[i].m_Entity);
//                    }
//                    NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(this.m_EntityType);
//                    for (int j = 0; j < nativeArray4.Length; j++)
//                    {
//                        if ((nativeArray[j].m_Flags & AreaFlags.Slave) == (AreaFlags)0)
//                        {
//                            PrefabRef prefabRef = nativeArray3[j];
//                            DynamicBuffer<Node> nodes = bufferAccessor[j];
//                            DynamicBuffer<Triangle> triangles = bufferAccessor2[j];
//                            AreaGeometryData geometryData = this.m_GeometryData[prefabRef.m_Prefab];
//                            AreaColorData areaColorData = this.m_ColorData[prefabRef.m_Prefab];
//                            Entity item;
//                            if (nativeArray2.Length != 0)
//                            {
//                                Temp temp = nativeArray2[j];
//                                if ((temp.m_Flags & TempFlags.Hidden) != (TempFlags)0u)
//                                {
//                                    goto IL_242;
//                                }
//                                item = temp.m_Original;
//                            }
//                            else
//                            {
//                                item = nativeArray4[j];
//                            }
//                            Color color;
//                            Color color2;
//                            if (nativeParallelHashSet.Contains(item))
//                            {
//                                color = areaColorData.m_SelectionFillColor.linear;
//                                color2 = areaColorData.m_SelectionEdgeColor.linear;
//                            }
//                            else
//                            {
//                                color = areaColorData.m_FillColor.linear;
//                                color2 = areaColorData.m_EdgeColor.linear;
//                            }
//                            if (!flag)
//                            {
//                                color = WEBufferSystem.FillMeshDataJob.GetDisabledColor(color);
//                                color2 = WEBufferSystem.FillMeshDataJob.GetDisabledColor(color2);
//                            }
//                            this.AddTriangles(nodes, triangles, color, color2, geometryData, ref value);
//                        }
//                    IL_242:;
//                    }
//                    nativeParallelHashSet.Dispose();
//                }
//                else
//                {
//                    for (int k = 0; k < bufferAccessor.Length; k++)
//                    {
//                        if ((nativeArray[k].m_Flags & AreaFlags.Slave) == (AreaFlags)0 && (nativeArray2.Length == 0 || (nativeArray2[k].m_Flags & TempFlags.Hidden) == (TempFlags)0u))
//                        {
//                            PrefabRef prefabRef2 = nativeArray3[k];
//                            DynamicBuffer<Node> nodes2 = bufferAccessor[k];
//                            DynamicBuffer<Triangle> triangles2 = bufferAccessor2[k];
//                            AreaGeometryData geometryData2 = this.m_GeometryData[prefabRef2.m_Prefab];
//                            AreaColorData areaColorData2 = this.m_ColorData[prefabRef2.m_Prefab];
//                            Color color3 = areaColorData2.m_FillColor.linear;
//                            Color color4 = areaColorData2.m_EdgeColor.linear;
//                            if (!flag)
//                            {
//                                color3 = WEBufferSystem.FillMeshDataJob.GetDisabledColor(color3);
//                                color4 = WEBufferSystem.FillMeshDataJob.GetDisabledColor(color4);
//                            }
//                            this.AddTriangles(nodes2, triangles2, color3, color4, geometryData2, ref value);
//                        }
//                    }
//                }
//                this.m_ChunkData[index] = value;
//            }

//            // Token: 0x06003316 RID: 13078 RVA: 0x001B29FB File Offset: 0x001B0BFB
//            private static Color GetDisabledColor(Color color)
//            {
//                color.a *= 0.25f;
//                return color;
//            }

//            // Token: 0x06003317 RID: 13079 RVA: 0x001B2A10 File Offset: 0x001B0C10
//            private void AddTriangles(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, Vector4 fillColor, Vector4 edgeColor, AreaGeometryData geometryData, ref WEBufferSystem.ChunkData chunkData)
//            {
//                for (int i = 0; i < triangles.Length; i++)
//                {
//                    Triangle triangle = triangles[i];
//                    Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
//                    Bounds3 bounds = MathUtils.Bounds(triangle2);
//                    int3 @int = math.select(triangle.m_Indices - 1, nodes.Length - 1, triangle.m_Indices == 0);
//                    int3 int2 = math.select(triangle.m_Indices + 1, 0, triangle.m_Indices == nodes.Length - 1);
//                    bounds.min.y = bounds.min.y + (triangle.m_HeightRange.min - geometryData.m_SnapDistance * 2f);
//                    bounds.max.y = bounds.max.y + (triangle.m_HeightRange.max + geometryData.m_SnapDistance * 2f);
//                    WEBufferSystem.AreaTriangleData value = default(WEBufferSystem.AreaTriangleData);
//                    value.m_APos = triangle2.a;
//                    value.m_BPos = triangle2.b;
//                    value.m_CPos = triangle2.c;
//                    Node node = nodes[@int.x];
//                    value.m_APrevXZ = node.m_Position.xz;
//                    node = nodes[@int.y];
//                    value.m_BPrevXZ = node.m_Position.xz;
//                    node = nodes[@int.z];
//                    value.m_CPrevXZ = node.m_Position.xz;
//                    node = nodes[int2.x];
//                    value.m_ANextXZ = node.m_Position.xz;
//                    node = nodes[int2.y];
//                    value.m_BNextXZ = node.m_Position.xz;
//                    node = nodes[int2.z];
//                    value.m_CNextXZ = node.m_Position.xz;
//                    value.m_YMinMax.x = bounds.min.y;
//                    value.m_YMinMax.y = bounds.max.y;
//                    value.m_FillColor = fillColor;
//                    value.m_EdgeColor = edgeColor;
//                    int triangleOffset = chunkData.m_TriangleOffset;
//                    chunkData.m_TriangleOffset = triangleOffset + 1;
//                    this.m_AreaTriangleData[triangleOffset] = value;
//                    chunkData.m_Bounds |= bounds;
//                }
//            }

//            // Token: 0x04004745 RID: 18245
//            [ReadOnly]
//            public EntityTypeHandle m_EntityType;

//            // Token: 0x04004746 RID: 18246
//            [ReadOnly]
//            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

//            // Token: 0x04004747 RID: 18247
//            [ReadOnly]
//            public ComponentTypeHandle<Temp> m_TempType;

//            // Token: 0x04004748 RID: 18248
//            [ReadOnly]
//            public ComponentTypeHandle<Hidden> m_HiddenType;

//            // Token: 0x04004749 RID: 18249
//            [ReadOnly]
//            public ComponentTypeHandle<Area> m_AreaType;

//            // Token: 0x0400474A RID: 18250
//            [ReadOnly]
//            public ComponentTypeHandle<Native> m_NativeType;

//            // Token: 0x0400474B RID: 18251
//            [ReadOnly]
//            public BufferTypeHandle<Node> m_NodeType;

//            // Token: 0x0400474C RID: 18252
//            [ReadOnly]
//            public BufferTypeHandle<Triangle> m_TriangleType;

//            // Token: 0x0400474D RID: 18253
//            [ReadOnly]
//            public ComponentLookup<AreaGeometryData> m_GeometryData;

//            // Token: 0x0400474E RID: 18254
//            [ReadOnly]
//            public ComponentLookup<AreaColorData> m_ColorData;

//            // Token: 0x0400474F RID: 18255
//            [ReadOnly]
//            public BufferLookup<SelectionElement> m_SelectionElements;

//            // Token: 0x04004750 RID: 18256
//            [ReadOnly]
//            public Entity m_SelectionEntity;

//            // Token: 0x04004751 RID: 18257
//            [ReadOnly]
//            public bool m_EditorMode;

//            // Token: 0x04004752 RID: 18258
//            [ReadOnly]
//            public NativeArray<ArchetypeChunk> m_Chunks;

//            // Token: 0x04004753 RID: 18259
//            [NativeDisableParallelForRestriction]
//            public NativeList<WEBufferSystem.ChunkData> m_ChunkData;

//            // Token: 0x04004754 RID: 18260
//            [NativeDisableParallelForRestriction]
//            public NativeList<WEBufferSystem.AreaTriangleData> m_AreaTriangleData;
//        }

//        // Token: 0x02000B51 RID: 2897
//        [BurstCompile]
//        private struct CalculateBoundsJob : IJob
//        {
//            // Token: 0x06003318 RID: 13080 RVA: 0x001B2CA0 File Offset: 0x001B0EA0
//            public void Execute()
//            {
//                Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
//                for (int i = 0; i < this.m_ChunkData.Length; i++)
//                {
//                    bounds |= this.m_ChunkData[i].m_Bounds;
//                }
//                this.m_Bounds.value = bounds;
//            }

//            // Token: 0x04004755 RID: 18261
//            [ReadOnly]
//            public NativeList<WEBufferSystem.ChunkData> m_ChunkData;

//            // Token: 0x04004756 RID: 18262
//            public NativeValue<Bounds3> m_Bounds;
//        }

//        // Token: 0x02000B52 RID: 2898
//        private struct LabelVertexData
//        {
//            // Token: 0x04004757 RID: 18263
//            public Vector3 m_Position;

//            // Token: 0x04004758 RID: 18264
//            public Color32 m_Color;

//            // Token: 0x04004759 RID: 18265
//            public Vector2 m_UV0;

//            // Token: 0x0400475A RID: 18266
//            public Vector2 m_UV1;

//            // Token: 0x0400475B RID: 18267
//            public Vector3 m_UV2;
//        }

//        // Token: 0x02000B53 RID: 2899
//        [BurstCompile]
//        private struct FillNameDataJob : IJob
//        {
//            // Token: 0x06003319 RID: 13081 RVA: 0x001B2D04 File Offset: 0x001B0F04
//            public unsafe void Execute()
//            {
//                NativeArray<int> array = new NativeArray<int>(this.m_SubMeshCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
//                for (int i = 0; i < this.m_Chunks.Length; i++)
//                {
//                    ArchetypeChunk archetypeChunk = this.m_Chunks[i];
//                    if (!archetypeChunk.Has<Hidden>(ref this.m_HiddenType))
//                    {
//                        BufferAccessor<LabelVertex> bufferAccessor = archetypeChunk.GetBufferAccessor<LabelVertex>(ref this.m_LabelVertexType);
//                        for (int j = 0; j < bufferAccessor.Length; j++)
//                        {
//                            DynamicBuffer<LabelVertex> dynamicBuffer = bufferAccessor[j];
//                            for (int k = 0; k < dynamicBuffer.Length; k += 4)
//                            {
//                                int material = dynamicBuffer[k].m_Material;
//                                *array.ElementAt(material) += 4;
//                            }
//                        }
//                    }
//                }
//                int num = 0;
//                for (int l = 0; l < this.m_SubMeshCount; l++)
//                {
//                    num += array[l];
//                }
//                Mesh.MeshData meshData = this.m_NameMeshData[0];
//                NativeArray<VertexAttributeDescriptor> attributes = new NativeArray<VertexAttributeDescriptor>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
//                attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
//                attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0);
//                attributes[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0);
//                attributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 0);
//                attributes[4] = new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 3, 0);
//                meshData.SetVertexBufferParams(num, attributes);
//                meshData.SetIndexBufferParams((num >> 2) * 6, IndexFormat.UInt32);
//                attributes.Dispose();
//                num = 0;
//                meshData.subMeshCount = this.m_SubMeshCount;
//                for (int m = 0; m < this.m_SubMeshCount; m++)
//                {
//                    ref int ptr = ref array.ElementAt(m);
//                    meshData.SetSubMesh(m, new SubMeshDescriptor
//                    {
//                        firstVertex = num,
//                        indexStart = (num >> 2) * 6,
//                        vertexCount = ptr,
//                        indexCount = (ptr >> 2) * 6,
//                        topology = MeshTopology.Triangles
//                    }, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
//                    num += ptr;
//                    ptr = 0;
//                }
//                NativeArray<WEBufferSystem.LabelVertexData> vertexData = meshData.GetVertexData<WEBufferSystem.LabelVertexData>(0);
//                NativeArray<uint> indexData = meshData.GetIndexData<uint>();
//                for (int n = 0; n < this.m_Chunks.Length; n++)
//                {
//                    ArchetypeChunk archetypeChunk2 = this.m_Chunks[n];
//                    if (!archetypeChunk2.Has<Hidden>(ref this.m_HiddenType))
//                    {
//                        NativeArray<Geometry> nativeArray = archetypeChunk2.GetNativeArray<Geometry>(ref this.m_GeometryType);
//                        NativeArray<PrefabRef> nativeArray2 = archetypeChunk2.GetNativeArray<PrefabRef>(ref this.m_PrefabRefType);
//                        NativeArray<Temp> nativeArray3 = archetypeChunk2.GetNativeArray<Temp>(ref this.m_TempType);
//                        BufferAccessor<LabelVertex> bufferAccessor2 = archetypeChunk2.GetBufferAccessor<LabelVertex>(ref this.m_LabelVertexType);
//                        for (int num2 = 0; num2 < bufferAccessor2.Length; num2++)
//                        {
//                            Geometry geometry = nativeArray[num2];
//                            PrefabRef prefabRef = nativeArray2[num2];
//                            DynamicBuffer<LabelVertex> dynamicBuffer2 = bufferAccessor2[num2];
//                            AreaNameData areaNameData = this.m_AreaNameData[prefabRef.m_Prefab];
//                            float3 v = AreaUtils.CalculateLabelPosition(geometry);
//                            Color32 color = areaNameData.m_Color;
//                            if (nativeArray3.Length != 0 && (nativeArray3[num2].m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace)) != (TempFlags)0u)
//                            {
//                                color = areaNameData.m_SelectedColor;
//                            }
//                            SubMeshDescriptor subMeshDescriptor = default(SubMeshDescriptor);
//                            int num3 = -1;
//                            for (int num4 = 0; num4 < dynamicBuffer2.Length; num4 += 4)
//                            {
//                                int material2 = dynamicBuffer2[num4].m_Material;
//                                ref int ptr2 = ref array.ElementAt(material2);
//                                if (material2 != num3)
//                                {
//                                    subMeshDescriptor = meshData.GetSubMesh(material2);
//                                    num3 = material2;
//                                }
//                                int num5 = subMeshDescriptor.firstVertex + ptr2;
//                                int num6 = subMeshDescriptor.indexStart + (ptr2 >> 2) * 6;
//                                ptr2 += 4;
//                                indexData[num6] = (uint)num5;
//                                indexData[num6 + 1] = (uint)(num5 + 1);
//                                indexData[num6 + 2] = (uint)(num5 + 2);
//                                indexData[num6 + 3] = (uint)(num5 + 2);
//                                indexData[num6 + 4] = (uint)(num5 + 3);
//                                indexData[num6 + 5] = (uint)num5;
//                                for (int num7 = 0; num7 < 4; num7++)
//                                {
//                                    LabelVertex labelVertex = dynamicBuffer2[num4 + num7];
//                                    WEBufferSystem.LabelVertexData value;
//                                    value.m_Position = labelVertex.m_Position;
//                                    value.m_Color = new Color32((byte)(labelVertex.m_Color.r * color.r >> 8), (byte)(labelVertex.m_Color.g * color.g >> 8), (byte)(labelVertex.m_Color.b * color.b >> 8), (byte)(labelVertex.m_Color.a * color.a >> 8));
//                                    value.m_UV0 = labelVertex.m_UV0;
//                                    value.m_UV1 = labelVertex.m_UV1;
//                                    value.m_UV2 = v;
//                                    vertexData[num5 + num7] = value;
//                                }
//                            }
//                        }
//                    }
//                }
//                for (int num8 = 0; num8 < this.m_SubMeshCount; num8++)
//                {
//                    SubMeshDescriptor subMesh = meshData.GetSubMesh(num8);
//                    meshData.SetSubMesh(num8, subMesh, MeshUpdateFlags.Default);
//                }
//                array.Dispose();
//            }

//            // Token: 0x0400475C RID: 18268
//            [ReadOnly]
//            public ComponentTypeHandle<Geometry> m_GeometryType;

//            // Token: 0x0400475D RID: 18269
//            [ReadOnly]
//            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

//            // Token: 0x0400475E RID: 18270
//            [ReadOnly]
//            public ComponentTypeHandle<Temp> m_TempType;

//            // Token: 0x0400475F RID: 18271
//            [ReadOnly]
//            public ComponentTypeHandle<Hidden> m_HiddenType;

//            // Token: 0x04004760 RID: 18272
//            [ReadOnly]
//            public BufferTypeHandle<LabelVertex> m_LabelVertexType;

//            // Token: 0x04004761 RID: 18273
//            [ReadOnly]
//            public ComponentLookup<AreaNameData> m_AreaNameData;

//            // Token: 0x04004762 RID: 18274
//            [ReadOnly]
//            public NativeList<ArchetypeChunk> m_Chunks;

//            // Token: 0x04004763 RID: 18275
//            [ReadOnly]
//            public int m_SubMeshCount;

//            // Token: 0x04004764 RID: 18276
//            public Mesh.MeshDataArray m_NameMeshData;
//        }

//        // Token: 0x02000B54 RID: 2900
//        private struct TypeHandle
//        {
//            // Token: 0x0600331A RID: 13082 RVA: 0x001B31F0 File Offset: 0x001B13F0
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public void __AssignHandles(ref SystemState state)
//            {
//                this.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(true);
//                this.__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(true);
//                this.__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(true);
//                this.__Game_Tools_Hidden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Hidden>(true);
//                this.__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(true);
//                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
//                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
//                this.__Game_Common_Native_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Native>(true);
//                this.__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(true);
//                this.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(true);
//                this.__Game_Prefabs_AreaColorData_RO_ComponentLookup = state.GetComponentLookup<AreaColorData>(true);
//                this.__Game_Tools_SelectionElement_RO_BufferLookup = state.GetBufferLookup<SelectionElement>(true);
//                this.__Game_Areas_Geometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Geometry>(true);
//                this.__Game_Areas_LabelVertex_RO_BufferTypeHandle = state.GetBufferTypeHandle<LabelVertex>(true);
//                this.__Game_Prefabs_AreaNameData_RO_ComponentLookup = state.GetComponentLookup<AreaNameData>(true);
//                this.__Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(true);
//                this.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatchesUpdated>(true);
//                this.__Game_Areas_LabelExtents_RW_BufferTypeHandle = state.GetBufferTypeHandle<LabelExtents>(false);
//                this.__Game_Areas_LabelVertex_RW_BufferTypeHandle = state.GetBufferTypeHandle<LabelVertex>(false);
//            }

//            // Token: 0x04004765 RID: 18277
//            [ReadOnly]
//            public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

//            // Token: 0x04004766 RID: 18278
//            [ReadOnly]
//            public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

//            // Token: 0x04004767 RID: 18279
//            [ReadOnly]
//            public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

//            // Token: 0x04004768 RID: 18280
//            [ReadOnly]
//            public ComponentTypeHandle<Hidden> __Game_Tools_Hidden_RO_ComponentTypeHandle;

//            // Token: 0x04004769 RID: 18281
//            [ReadOnly]
//            public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

//            // Token: 0x0400476A RID: 18282
//            [ReadOnly]
//            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

//            // Token: 0x0400476B RID: 18283
//            [ReadOnly]
//            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

//            // Token: 0x0400476C RID: 18284
//            [ReadOnly]
//            public ComponentTypeHandle<Native> __Game_Common_Native_RO_ComponentTypeHandle;

//            // Token: 0x0400476D RID: 18285
//            [ReadOnly]
//            public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

//            // Token: 0x0400476E RID: 18286
//            [ReadOnly]
//            public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

//            // Token: 0x0400476F RID: 18287
//            [ReadOnly]
//            public ComponentLookup<AreaColorData> __Game_Prefabs_AreaColorData_RO_ComponentLookup;

//            // Token: 0x04004770 RID: 18288
//            [ReadOnly]
//            public BufferLookup<SelectionElement> __Game_Tools_SelectionElement_RO_BufferLookup;

//            // Token: 0x04004771 RID: 18289
//            [ReadOnly]
//            public ComponentTypeHandle<Geometry> __Game_Areas_Geometry_RO_ComponentTypeHandle;

//            // Token: 0x04004772 RID: 18290
//            [ReadOnly]
//            public BufferTypeHandle<LabelVertex> __Game_Areas_LabelVertex_RO_BufferTypeHandle;

//            // Token: 0x04004773 RID: 18291
//            [ReadOnly]
//            public ComponentLookup<AreaNameData> __Game_Prefabs_AreaNameData_RO_ComponentLookup;

//            // Token: 0x04004774 RID: 18292
//            [ReadOnly]
//            public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

//            // Token: 0x04004775 RID: 18293
//            [ReadOnly]
//            public ComponentTypeHandle<BatchesUpdated> __Game_Common_BatchesUpdated_RO_ComponentTypeHandle;

//            // Token: 0x04004776 RID: 18294
//            public BufferTypeHandle<LabelExtents> __Game_Areas_LabelExtents_RW_BufferTypeHandle;

//            // Token: 0x04004777 RID: 18295
//            public BufferTypeHandle<LabelVertex> __Game_Areas_LabelVertex_RW_BufferTypeHandle;
//        }
//    }
//}