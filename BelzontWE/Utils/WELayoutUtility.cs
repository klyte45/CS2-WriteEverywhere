﻿using BelzontWE.Utils;
using Colossal.Entities;
using Unity.Entities;

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
            em.AddComponentData(newEntity, weData);
            em.AddComponentData(newEntity, mesh);
            em.AddComponentData(newEntity, material);
            em.AddComponentData(newEntity, transform);
            if (childTargetMode == ParentEntityMode.TARGET_IS_TARGET)
            {
                if (!em.TryGetBuffer<WESubTextRef>(parentEntity, true, out var subRefBuff)) subRefBuff = em.AddBuffer<WESubTextRef>(parentEntity);
                subRefBuff.Add(new WESubTextRef
                {
                    m_weTextData = newEntity
                });
            }
            if (weData.TextType != WESimulationTextType.Placeholder && toCopy.children.Length > 0)
            {
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    DoCreateLayoutItem(toCopy.children[i], newEntity, childTarget, em);
                }
            }
            return newEntity;
        }
        public static Entity DoCreateLayoutItem(WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, ref ComponentLookup<WETextDataMain> tdLookup,
                   ref BufferLookup<WESubTextRef> subTextLookup, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd,
                   ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET)
        {
            if (!subTextLookup.TryGetBuffer(parentEntity, out var buff)) buff = childTargetMode == ParentEntityMode.TARGET_IS_TARGET ? cmd.AddBuffer<WESubTextRef>(unfilteredChunkIndex, parentEntity) : default;
            return DoCreateLayoutItem(toCopy, parentEntity, targetEntity, ref tdLookup, ref subTextLookup, unfilteredChunkIndex, cmd, ref buff, childTargetMode);
        }

        private static Entity DoCreateLayoutItem(WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, ref ComponentLookup<WETextDataMain> tdLookup,
        ref BufferLookup<WESubTextRef> subTextLookup, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd,
        ref DynamicBuffer<WESubTextRef> parentSubRefArray, ParentEntityMode childTargetMode)
        {
            var newEntity = cmd.CreateEntity(unfilteredChunkIndex);
            var childTarget = CommonDataSetup(toCopy, parentEntity, targetEntity, childTargetMode, newEntity, out WETextDataMain weData, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform);
            cmd.AddComponent(unfilteredChunkIndex, newEntity, weData);
            cmd.AddComponent(unfilteredChunkIndex, newEntity, mesh);
            cmd.AddComponent(unfilteredChunkIndex, newEntity, material);
            cmd.AddComponent(unfilteredChunkIndex, newEntity, transform);
            cmd.AddComponent<WEWaitingPostInstantiation>(unfilteredChunkIndex, newEntity);

            if (childTargetMode == ParentEntityMode.TARGET_IS_TARGET)
            {
                parentSubRefArray.Add(new WESubTextRef
                {
                    m_weTextData = newEntity
                });
            }

            if (weData.TextType != WESimulationTextType.Placeholder && toCopy.children.Length > 0)
            {
                var buff = cmd.AddBuffer<WESubTextRef>(unfilteredChunkIndex, newEntity);
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    DoCreateLayoutItem(toCopy.children[i], newEntity, childTarget, ref tdLookup, ref subTextLookup, unfilteredChunkIndex, cmd, ref buff, ParentEntityMode.TARGET_IS_TARGET);
                }
            }
            return newEntity;
        }

        private static Entity CommonDataSetup(WETextDataXmlTree toCopy, Entity parentEntity, Entity targetEntity, ParentEntityMode childTargetMode, Entity newEntity, out WETextDataMain main, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform)
        {
            var parentTarget = ParentEntityMode.TARGET_IS_SELF_FOR_PARENT == childTargetMode || ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET == childTargetMode ? newEntity : targetEntity;
            FromDataStruct(toCopy.self, newEntity, parentTarget, out main, out mesh, out material, out transform);
            var childTarget = childTargetMode switch
            {
                ParentEntityMode.TARGET_IS_SELF => newEntity,
                ParentEntityMode.TARGET_IS_SELF_FOR_PARENT => parentEntity,
                ParentEntityMode.TARGET_IS_PARENT => parentEntity,
                ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET => newEntity,
                _ => targetEntity
            };
            main.SetNewParentForced(parentEntity);
            return childTarget;
        }




        private static void FromDataStruct(WETextDataXml xml, Entity parent, Entity target, out WETextDataMain main, out WETextDataMesh mesh, out WETextDataMaterial material, out WETextDataTransform transform)
        {
            xml.ToComponents(out main, out mesh, out material, out transform);
            main.ParentEntity = parent;
            main.TargetEntity = target;

        }
    }
}