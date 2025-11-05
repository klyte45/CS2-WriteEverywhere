using BelzontWE.Utils;
using Colossal.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public static class WELayoutUtility
    {
        public enum ParentEntityMode
        {
            TARGET_IS_TARGET,
            TARGET_IS_SELF,
            TARGET_IS_SELF_FOR_PARENT,
            TARGET_IS_SELF_PARENT_HAS_TARGET,
            TARGET_IS_PARENT
        }
        public static Entity DoCreateLayoutItem(WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, EntityManager em, ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET)
        {
            var newEntity = em.CreateEntity();
            var childTarget = CommonDataSetup(toCopy, parentEntity, targetEntity, childTargetMode, newEntity, out WETextDataMain weData, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform);
            var variablesBuffer = em.AddBuffer<WETextDataVariable>(newEntity);
            if (toCopy.variables?.Length > 0)
            {
                foreach (var entry in toCopy.variables)
                {
                    variablesBuffer.Add(new WETextDataVariable
                    {
                        Key = entry.key ?? "",
                        Value = entry.value ?? ""
                    });
                }
            }
            em.AddComponentData(newEntity, weData);
            em.AddComponentData(newEntity, mesh);
            em.AddComponentData(newEntity, material);
            em.AddComponentData(newEntity, transform);
            em.AddComponent<WETextComponentValid>(newEntity);
            if (childTargetMode == ParentEntityMode.TARGET_IS_TARGET)
            {
                if (!em.TryGetBuffer<WESubTextRef>(parentEntity, true, out var subRefBuff)) subRefBuff = em.AddBuffer<WESubTextRef>(parentEntity);
                subRefBuff.Add(new WESubTextRef
                {
                    m_weTextData = newEntity
                });
            }
            if (mesh.TextType != WESimulationTextType.Placeholder && toCopy.children?.Length > 0)
            {
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    DoCreateLayoutItem(toCopy.children[i], newEntity, childTarget, em);
                }
            }
            return newEntity;
        }
        public static Entity DoCreateLayoutItemCmdBuffer(bool fromTemplate, string modSource, WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, ref ComponentLookup<WETextDataMain> tdLookup,
                   ref BufferLookup<WESubTextRef> subTextLookup, EntityCommandBuffer cmd,
                   ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET)
        {
            if (!subTextLookup.TryGetBuffer(parentEntity, out var buff)) buff = childTargetMode == ParentEntityMode.TARGET_IS_TARGET ? cmd.AddBuffer<WESubTextRef>(parentEntity) : default;
            return DoCreateLayoutItemCommandBuffer(fromTemplate, modSource, toCopy, parentEntity, targetEntity, ref tdLookup, ref subTextLookup, cmd, ref buff, childTargetMode);
        }
        public static void DoCreateLayoutItemArray(bool fromTemplate, string modSource, WETextDataXmlTree[] toCopyArray, Entity parentEntity, Entity targetEntity, ref ComponentLookup<WETextDataMain> tdLookup,
                   ref BufferLookup<WESubTextRef> subTextLookup, EntityCommandBuffer cmd,
                   ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET)
        {
            if (!subTextLookup.TryGetBuffer(parentEntity, out var buff)) buff = childTargetMode == ParentEntityMode.TARGET_IS_TARGET ? cmd.AddBuffer<WESubTextRef>(parentEntity) : default;
            foreach (var toCopy in toCopyArray)
            {
                DoCreateLayoutItemCommandBuffer(fromTemplate, modSource, toCopy, parentEntity, targetEntity, ref tdLookup, ref subTextLookup, cmd, ref buff, childTargetMode);
            }
        }

        private static Entity DoCreateLayoutItemCommandBuffer(bool fromTemplate, string modSource, WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, ref ComponentLookup<WETextDataMain> tdLookup,
        ref BufferLookup<WESubTextRef> subTextLookup, EntityCommandBuffer cmd,
        ref DynamicBuffer<WESubTextRef> parentSubRefArray, ParentEntityMode childTargetMode)
        {
            var newEntity = cmd.CreateEntity();
            var childTarget = CommonDataSetup(toCopy, parentEntity, targetEntity, childTargetMode, newEntity, out WETextDataMain weData, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform);
            cmd.AddComponent(newEntity, weData);
            cmd.AddComponent(newEntity, mesh);
            cmd.AddComponent(newEntity, material);
            cmd.AddComponent(newEntity, transform);
            cmd.AddComponent<WETextComponentValid>(newEntity);
            var buffVars = cmd.AddBuffer<WETextDataVariable>(newEntity);
            if (toCopy.variables?.Length > 0)
            {
                foreach (var item in toCopy.variables)
                {
                    buffVars.Add(new()
                    {
                        Key = item.key,
                        Value = item.value
                    });
                }
            }

            if (childTargetMode == ParentEntityMode.TARGET_IS_TARGET)
            {
                parentSubRefArray.Add(new WESubTextRef
                {
                    m_weTextData = newEntity
                });
            }

            if (mesh.TextType != WESimulationTextType.Placeholder && toCopy.children?.Length > 0)
            {
                var buff = cmd.AddBuffer<WESubTextRef>(newEntity);
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    DoCreateLayoutItemCommandBuffer(fromTemplate, modSource, toCopy.children[i], newEntity, childTarget, ref tdLookup, ref subTextLookup, cmd, ref buff, ParentEntityMode.TARGET_IS_TARGET);
                }
            }
            return newEntity;
        }

        private static Entity CommonDataSetup(WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, ParentEntityMode childTargetMode, Entity newEntity, out WETextDataMain main, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform)
        {
            var parentTarget = ParentEntityMode.TARGET_IS_SELF_FOR_PARENT == childTargetMode || ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET == childTargetMode ? newEntity : targetEntity;
            FromXml(toCopy.self, parentEntity, parentTarget, out main, out mesh, out material, out transform);
            var childTarget = childTargetMode switch
            {
                ParentEntityMode.TARGET_IS_SELF => newEntity,
                ParentEntityMode.TARGET_IS_SELF_FOR_PARENT => parentEntity,
                ParentEntityMode.TARGET_IS_PARENT => parentEntity,
                ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET => newEntity,
                _ => targetEntity
            };
            return childTarget;
        }

        private static void FromXml(WETextDataXml xml, Entity parent, Entity target, out WETextDataMain main, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform)
        {
            xml.ToComponents(out main, out mesh, out material, out transform);
            main.ParentEntity = parent;
            main.TargetEntity = target;

        }
    }
}