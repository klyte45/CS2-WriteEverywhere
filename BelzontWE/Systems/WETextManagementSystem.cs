using Game;
using Unity.Entities;

//namespace BelzontWE.Systems
//{
//    public class WETextManagementSystem : GameSystemBase
//    {
//        protected override void OnUpdate()
//        {

//        }



//        private void ResetBri(Entity e)
//        {
//            materialData.ResetMaterial();
//            basicRenderInformation.Free();
//        }

//        public int SetFormulae(string value, out string[] cmpErr) => valueData.SetFormulae(value, out cmpErr);

//        public static WETextData_ CreateDefault(Entity target, Entity? parent = null)
//        {
//            return new WETextData_
//            {
//                targetEntity = target,
//                parentEntity = parent ?? target,
//                transformData = new()
//                {
//                    offsetPosition = new(0, 0, 0),
//                    offsetRotation = new(),
//                    scale = new(1, 1, 1),
//                },
//                shaderData = new()
//                {
//                    shader = WEShader.Default,
//                    decalFlags = DEFAULT_DECAL_FLAGS,
//                },
//                materialData = new()
//                {
//                    dirty = true,
//                    color = new() { defaultValue = new(0xff, 0xff, 0xff, 0xff) },
//                    emissiveColor = new() { defaultValue = new(0xff, 0xff, 0xff, 0xff) },
//                    metallic = new() { defaultValue = 0 },
//                    smoothness = new() { defaultValue = 0 },
//                    emissiveIntensity = new() { defaultValue = 0 },
//                    glassRefraction = new() { defaultValue = 1f },
//                    colorMask1 = new() { defaultValue = UnityEngine.Color.white },
//                    colorMask2 = new() { defaultValue = UnityEngine.Color.white },
//                    colorMask3 = new() { defaultValue = UnityEngine.Color.white },
//                    glassColor = new() { defaultValue = UnityEngine.Color.white },
//                    glassThickness = new() { defaultValue = .5f },
//                    coatStrength = new() { defaultValue = 0f },
//                },
//                valueData = new()
//                {
//                    defaultValue = "NEW TEXT"
//                },
//                itemName = "New item",
//            };
//        }

//        public bool SetNewParent(Entity e, EntityManager em)
//        {
//            if ((e != targetEntity && e != Entity.Null && (!em.TryGetComponent<WETextData_>(e, out var weData) || weData.TextType == WESimulationTextType.Placeholder || (weData.targetEntity != Entity.Null && weData.targetEntity != targetEntity))))
//            {
//                if (BasicIMod.DebugMode) LogUtils.DoLog($"NOPE: e = {e}; weData = {weData}; targetEntity = {targetEntity}; weData.targetEntity = {weData.targetEntity}");
//                return false;
//            }
//            if (BasicIMod.DebugMode) LogUtils.DoLog($"YEP: e = {e};  targetEntity = {targetEntity}");
//            parentEntity = e;
//            return true;
//        }
//        public void SetNewParentForced(Entity e)
//        {
//            parentEntity = e;
//        }

//        public readonly bool IsTemplateDirty() => templateDirty;
//        public void ClearTemplateDirty() => templateDirty = false;
//        public WETextData_ UpdateBRI(BasicRenderInformation bri, string text)
//        {
//            materialData.dirty = true;
//            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
//            basicRenderInformation = default;
//            basicRenderInformation = GCHandle.Alloc(bri, GCHandleType.Weak);
//            Bounds = bri.m_bounds;
//            BriWidthMetersUnscaled = bri.m_sizeMetersUnscaled.x;
//            if (bri.m_isError)
//            {
//                LastErrorStr = text;
//            }
//            dirtyBRI = false;
//            return this;
//        }

//        public void Dispose()
//        {
//            if (basicRenderInformation.IsAllocated) basicRenderInformation.Free();
//            if (materialData.ownMaterial.IsAllocated)
//            {
//                GameObject.Destroy(materialData.ownMaterial.Target as Material);
//                materialData.ownMaterial.Free();
//            }
//            basicRenderInformation = default;
//            materialData.ownMaterial = default;
//        }

//        public WETextData_ OnPostInstantiate(EntityManager em)
//        {
//            FontServer.Instance.EnsureFont(fontName);
//            UpdateEffectiveText(em, targetEntity);
//            return this;
//        }


//        public void UpdateEffectiveText(EntityManager em, Entity geometryEntity)
//        {
//            var result = valueData.UpdateEffectiveText(em, geometryEntity, (RenderInformation?.m_isError ?? false) ? LastErrorStr.ToString() : RenderInformation?.m_refText);
//            if (result) dirtyBRI = true;
//        }

//        public static Entity GetTargetEntityEffective(Entity target, EntityManager em, bool fullRecursive = false)
//        {
//            return em.TryGetComponent<WETextData_>(target, out var weData)
//                ? (fullRecursive || weData.TextType == WESimulationTextType.Archetype) && weData.TargetEntity != target
//                    ? GetTargetEntityEffective(weData.TargetEntity, em)
//                    : em.TryGetComponent<WETextData_>(weData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder
//                    ? GetTargetEntityEffective(weDataParent.targetEntity, em)
//                    : weData.TargetEntity
//                : target;
//        }
//    }
//}
