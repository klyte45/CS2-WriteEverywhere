//#define BURST
////#define VERBOSE 

//using Game;
//using Game.Areas;
//using Game.Prefabs;
//using Game.Rendering;
//using Game.Tools;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.Scripting;

//namespace BelzontWE.Rendering
//{
//    public unsafe partial class WERenderSystem : GameSystemBase
//    {
//        // Token: 0x060036C5 RID: 14021 RVA: 0x001E7700 File Offset: 0x001E5900
//        [Preserve]
//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
//            this.m_BufferSystem = base.World.GetOrCreateSystemManaged<NotificationIconBufferSystem>();
//            this.m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
//            this.m_ConfigurationQuery = base.GetEntityQuery(new ComponentType[]
//            {
//                ComponentType.ReadOnly<IconConfigurationData>()
//            });
//            this.m_PrefabQuery = base.GetEntityQuery(new ComponentType[]
//            {
//                ComponentType.ReadOnly<NotificationIconData>(),
//                ComponentType.ReadOnly<PrefabData>()
//            });
//            this.m_InstanceBufferID = Shader.PropertyToID("instanceBuffer");
//            base.RequireForUpdate(this.m_ConfigurationQuery);
//            RenderPipelineManager.beginContextRendering += this.Render;
//        }

//        // Token: 0x060036C6 RID: 14022 RVA: 0x001E77BC File Offset: 0x001E59BC
//        [Preserve]
//        protected override void OnDestroy()
//        {
//            RenderPipelineManager.beginContextRendering -= this.Render;
//            if (this.m_Mesh != null)
//            {
//                UnityEngine.Object.Destroy(this.m_Mesh);
//            }
//            if (this.m_Material != null)
//            {
//                UnityEngine.Object.Destroy(this.m_Material);
//            }
//            if (this.m_ArgsBuffer != null)
//            {
//                this.m_ArgsBuffer.Release();
//            }
//            if (this.m_InstanceBuffer != null)
//            {
//                this.m_InstanceBuffer.Release();
//            }
//            if (this.m_TextureArray != null)
//            {
//                UnityEngine.Object.Destroy(this.m_TextureArray);
//            }
//            base.OnDestroy();
//        }

//        // Token: 0x060036C7 RID: 14023 RVA: 0x001E7851 File Offset: 0x001E5A51
//        [Preserve]
//        protected override void OnUpdate()
//        {
//            this.m_UpdateBuffer = true;
//        }

//        // Token: 0x060036C8 RID: 14024 RVA: 0x001E785C File Offset: 0x001E5A5C
//        private void Render(ScriptableRenderContext context, List<Camera> cameras)
//        {
//            try
//            {
//                if (!this.m_RenderingSystem.hideOverlay)
//                {
//                    NotificationIconBufferSystem.IconData iconData = this.m_BufferSystem.GetIconData();
//                    if (iconData.m_InstanceData.IsCreated)
//                    {
//                        int length = iconData.m_InstanceData.Length;
//                        if (length != 0)
//                        {
//                            Bounds bounds = RenderingUtils.ToBounds(iconData.m_IconBounds.value);
//                            Mesh mesh = this.GetMesh();
//                            Material material = this.GetMaterial();
//                            ComputeBuffer argsBuffer = this.GetArgsBuffer();
//                            this.m_ArgsArray[0] = mesh.GetIndexCount(0);
//                            this.m_ArgsArray[1] = (uint)length;
//                            this.m_ArgsArray[2] = mesh.GetIndexStart(0);
//                            this.m_ArgsArray[3] = mesh.GetBaseVertex(0);
//                            this.m_ArgsArray[4] = 0u;
//                            argsBuffer.SetData(this.m_ArgsArray);
//                            if (this.m_UpdateBuffer)
//                            {
//                                this.m_UpdateBuffer = false;
//                                ComputeBuffer instanceBuffer = this.GetInstanceBuffer(length);
//                                instanceBuffer.SetData<InstanceData>(iconData.m_InstanceData, 0, 0, length);
//                                material.SetBuffer(this.m_InstanceBufferID, instanceBuffer);
//                            }
//                            foreach (Camera camera in cameras)
//                            {
//                                if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
//                                {
//                                    Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer, 0, null, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.BlendProbes);
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            finally
//            {
//            }
//        }

//        // Token: 0x060036C9 RID: 14025 RVA: 0x001E79E4 File Offset: 0x001E5BE4
//        private Mesh GetMesh()
//        {
//            if (this.m_Mesh == null)
//            {
//                this.m_Mesh = new Mesh();
//                this.m_Mesh.name = "Notification icon";
//                this.m_Mesh.vertices = new Vector3[]
//                {
//                    new Vector3(-1f, -1f, 0f),
//                    new Vector3(-1f, 1f, 0f),
//                    new Vector3(1f, 1f, 0f),
//                    new Vector3(1f, -1f, 0f)
//                };
//                this.m_Mesh.uv = new Vector2[]
//                {
//                    new Vector2(0f, 0f),
//                    new Vector2(0f, 1f),
//                    new Vector2(1f, 1f),
//                    new Vector2(1f, 0f)
//                };
//                this.m_Mesh.triangles = new int[]
//                {
//                    0,
//                    1,
//                    2,
//                    2,
//                    3,
//                    0
//                };
//            }
//            return this.m_Mesh;
//        }

//        // Token: 0x060036CA RID: 14026 RVA: 0x001E7B28 File Offset: 0x001E5D28
//        private Material GetMaterial()
//        {
//            if (this.m_Material == null)
//            {
//                Entity singletonEntity = this.m_ConfigurationQuery.GetSingletonEntity();
//                IconConfigurationPrefab prefab = this.m_PrefabSystem.GetPrefab<IconConfigurationPrefab>(singletonEntity);
//                this.m_Material = new Material(prefab.m_Material);
//                this.m_Material.name = "Notification icons";
//                NativeArray<ArchetypeChunk> nativeArray = this.m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
//                this.__TypeHandle.__Game_Prefabs_PrefabData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<PrefabData> _Game_Prefabs_PrefabData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_PrefabData_RW_ComponentTypeHandle;
//                this.__TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
//                ComponentTypeHandle<NotificationIconDisplayData> _Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle;
//                try
//                {
//                    int num = 1;
//                    int2 @int = new int2(prefab.m_MissingIcon.width, prefab.m_MissingIcon.height);
//                    TextureFormat format = prefab.m_MissingIcon.format;
//                    for (int i = 0; i < nativeArray.Length; i++)
//                    {
//                        ArchetypeChunk archetypeChunk = nativeArray[i];
//                        NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray<PrefabData>(ref _Game_Prefabs_PrefabData_RW_ComponentTypeHandle);
//                        NativeArray<NotificationIconDisplayData> nativeArray3 = archetypeChunk.GetNativeArray<NotificationIconDisplayData>(ref _Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle);
//                        for (int j = 0; j < nativeArray2.Length; j++)
//                        {
//                            PrefabData prefabData = nativeArray2[j];
//                            NotificationIconPrefab prefab2 = this.m_PrefabSystem.GetPrefab<NotificationIconPrefab>(prefabData);
//                            NotificationIconDisplayData notificationIconDisplayData = nativeArray3[j];
//                            num = math.max(num, notificationIconDisplayData.m_IconIndex + 1);
//                            @int = math.max(@int, new int2(prefab2.m_Icon.width, prefab2.m_Icon.height));
//                            format = prefab2.m_Icon.format;
//                        }
//                    }
//                    this.m_TextureArray = new Texture2DArray(@int.x, @int.y, num, format, true)
//                    {
//                        name = "NotificationIcons"
//                    };
//                    Graphics.CopyTexture(prefab.m_MissingIcon, 0, this.m_TextureArray, 0);
//                    for (int k = 0; k < nativeArray.Length; k++)
//                    {
//                        ArchetypeChunk archetypeChunk2 = nativeArray[k];
//                        NativeArray<PrefabData> nativeArray4 = archetypeChunk2.GetNativeArray<PrefabData>(ref _Game_Prefabs_PrefabData_RW_ComponentTypeHandle);
//                        NativeArray<NotificationIconDisplayData> nativeArray5 = archetypeChunk2.GetNativeArray<NotificationIconDisplayData>(ref _Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle);
//                        for (int l = 0; l < nativeArray4.Length; l++)
//                        {
//                            PrefabData prefabData2 = nativeArray4[l];
//                            NotificationIconPrefab prefab3 = this.m_PrefabSystem.GetPrefab<NotificationIconPrefab>(prefabData2);
//                            NotificationIconDisplayData notificationIconDisplayData2 = nativeArray5[l];
//                            Graphics.CopyTexture(prefab3.m_Icon, 0, this.m_TextureArray, notificationIconDisplayData2.m_IconIndex);
//                        }
//                    }
//                    this.m_Material.mainTexture = this.m_TextureArray;
//                }
//                finally
//                {
//                    nativeArray.Dispose();
//                }
//            }
//            return this.m_Material;
//        }

//        // Token: 0x060036CB RID: 14027 RVA: 0x001E7DC8 File Offset: 0x001E5FC8
//        private ComputeBuffer GetArgsBuffer()
//        {
//            if (this.m_ArgsBuffer == null)
//            {
//                this.m_ArgsArray = new uint[5];
//                this.m_ArgsBuffer = new ComputeBuffer(1, this.m_ArgsArray.Length * 4, ComputeBufferType.DrawIndirect);
//                this.m_ArgsBuffer.name = "Notification args buffer";
//            }
//            return this.m_ArgsBuffer;
//        }

//        // Token: 0x060036CC RID: 14028 RVA: 0x001E7E1C File Offset: 0x001E601C
//        private ComputeBuffer GetInstanceBuffer(int count)
//        {
//            if (this.m_InstanceBuffer != null && this.m_InstanceBuffer.count < count)
//            {
//                count = math.max(this.m_InstanceBuffer.count * 2, count);
//                this.m_InstanceBuffer.Release();
//                this.m_InstanceBuffer = null;
//            }
//            if (this.m_InstanceBuffer == null)
//            {
//                this.m_InstanceBuffer = new ComputeBuffer(math.max(64, count), sizeof(NotificationIconBufferSystem.InstanceData));
//                this.m_InstanceBuffer.name = "Notification instance buffer";
//            }
//            return this.m_InstanceBuffer;
//        }

//        // Token: 0x060036CD RID: 14029 RVA: 0x00002E1D File Offset: 0x0000101D
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void __AssignQueries(ref SystemState state)
//        {
//        }

//        // Token: 0x060036CE RID: 14030 RVA: 0x001E7E9D File Offset: 0x001E609D
//        protected override void OnCreateForCompiler()
//        {
//            base.OnCreateForCompiler();
//            this.__AssignQueries(ref base.CheckedStateRef);
//            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
//        }

//        // Token: 0x060036CF RID: 14031 RVA: 0x000068B3 File Offset: 0x00004AB3
//        [Preserve]
//        public WERenderSystem()
//        {
//        }

//        // Token: 0x04004F5D RID: 20317
//        private PrefabSystem m_PrefabSystem;

//        // Token: 0x04004F5E RID: 20318
//        private NotificationIconBufferSystem m_BufferSystem;

//        // Token: 0x04004F5F RID: 20319
//        private RenderingSystem m_RenderingSystem;

//        // Token: 0x04004F60 RID: 20320
//        private Mesh m_Mesh;

//        // Token: 0x04004F61 RID: 20321
//        private Material m_Material;

//        // Token: 0x04004F62 RID: 20322
//        private ComputeBuffer m_ArgsBuffer;

//        // Token: 0x04004F63 RID: 20323
//        private ComputeBuffer m_InstanceBuffer;

//        // Token: 0x04004F64 RID: 20324
//        private Texture2DArray m_TextureArray;

//        // Token: 0x04004F65 RID: 20325
//        private uint[] m_ArgsArray;

//        // Token: 0x04004F66 RID: 20326
//        private EntityQuery m_ConfigurationQuery;

//        // Token: 0x04004F67 RID: 20327
//        private EntityQuery m_PrefabQuery;

//        // Token: 0x04004F68 RID: 20328
//        private int m_InstanceBufferID;

//        // Token: 0x04004F69 RID: 20329
//        private bool m_UpdateBuffer;

//        // Token: 0x04004F6A RID: 20330
//        private WERenderSystem.TypeHandle __TypeHandle;

//        // Token: 0x02000C29 RID: 3113
//        private struct TypeHandle
//        {
//            // Token: 0x060036D0 RID: 14032 RVA: 0x001E7EC2 File Offset: 0x001E60C2
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public void __AssignHandles(ref SystemState state)
//            {
//                this.__Game_Prefabs_PrefabData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(false);
//                this.__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NotificationIconDisplayData>(false);
//            }

//            // Token: 0x04004F6B RID: 20331
//            public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RW_ComponentTypeHandle;

//            // Token: 0x04004F6C RID: 20332
//            public ComponentTypeHandle<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle;
//        }
//    }
//}