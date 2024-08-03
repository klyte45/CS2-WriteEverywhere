﻿using Colossal.Entities;
using Unity.Entities;

namespace BelzontWE
{
    public static class WELayoutUtility
    {
        public static Entity DoCloneTextItemReferenceSelf(Entity toCopy, Entity newParent, EntityManager em, bool parentAsTargetAtTheEnd = false)
        {
            var cloneEntity = em.Instantiate(toCopy);
            var weData = em.GetComponentData<WETextData>(cloneEntity);
            weData.TargetEntity = cloneEntity;
            weData.OnPostInstantiate();
            weData.SetNewParentForced(newParent);
            em.SetComponentData(cloneEntity, weData);
            if (em.TryGetBuffer<WESubTextRef>(cloneEntity, false, out var subRefs))
            {
                for (int i = 0; i < subRefs.Length; i++)
                {
                    var subRef = subRefs[i];
                    subRef.m_weTextData = DoCloneTextItem(subRefs[i].m_weTextData, cloneEntity, em);
                    subRefs[i] = subRef;
                }
            }
            if (parentAsTargetAtTheEnd)
            {
                weData.TargetEntity = newParent;
                em.SetComponentData(cloneEntity, weData);
            }
            return cloneEntity;
        }
        public static Entity DoCloneTextItem(Entity target, Entity newParent, EntityManager em)
        {
            var finalTargetEntity = em.TryGetComponent<WETextData>(newParent, out var data) ? data.TargetEntity : newParent;
            var cloneEntity = em.Instantiate(target);
            var weData = em.GetComponentData<WETextData>(cloneEntity);
            weData.TargetEntity = finalTargetEntity;
            weData.OnPostInstantiate();
            if (weData.SetNewParent(newParent, em))
            {
                em.SetComponentData(cloneEntity, weData);
            }
            if (em.TryGetBuffer<WESubTextRef>(cloneEntity, false, out var subRefs))
            {
                for (int i = 0; i < subRefs.Length; i++)
                {
                    var subRef = subRefs[i];
                    subRef.m_weTextData = DoCloneTextItem(subRefs[i].m_weTextData, cloneEntity, em);
                    subRefs[i] = subRef;
                }
            }
            return cloneEntity;
        }

        public static Entity CreateEntityFromTree(Entity parent, WETextDataTree tree, EntityManager em)
        {
            var selfEntity = em.CreateEntity();
            var selfComponent = WETextData.FromDataXml(tree.self, parent, em);
            if (parent != Entity.Null)
            {
                if (!em.TryGetBuffer<WESubTextRef>(parent, true, out var subBuff)) subBuff = em.AddBuffer<WESubTextRef>(parent);
                subBuff.Add(new WESubTextRef
                {
                    m_weTextData = selfEntity
                });
            }
            else
            {
                em.AddComponent<WETemplateData>(selfEntity);
                selfComponent.TargetEntity = selfEntity;
            }
            em.AddComponentData(selfEntity, selfComponent);

            for (int i = 0; i < tree.children?.Length; i++)
            {
                var child = tree.children[i];
                CreateEntityFromTree(selfEntity, child, em);
            }
            if (parent == Entity.Null)
            {
                selfComponent.TargetEntity = Entity.Null;
                em.SetComponentData(selfEntity, selfComponent);
            }
            return selfEntity;
        }





        public static Entity DoCloneTextItemReferenceSelf(Entity toCopy, Entity newParent, EntityManager em, EntityCommandBuffer cmd, bool parentAsTargetAtTheEnd = false)
        {
            var cloneEntity = cmd.Instantiate(toCopy);
            var weData = em.GetComponentData<WETextData>(toCopy);
            weData.TargetEntity = cloneEntity;
            weData.OnPostInstantiate();
            weData.SetNewParentForced(newParent);
            cmd.SetComponent(cloneEntity, weData);
            if (em.TryGetBuffer<WESubTextRef>(toCopy, true, out var subRefs))
            {
                cmd.RemoveComponent<WESubTextRef>(cloneEntity);
                DynamicBuffer<WESubTextRef> newBuff = cmd.AddBuffer<WESubTextRef>(cloneEntity);
                newBuff.Length = subRefs.Length;
                for (int i = 0; i < subRefs.Length; i++)
                {
                    var subRef = subRefs[i];
                    subRef.m_weTextData = DoCloneTextItemForCommandBuffer(subRefs[i].m_weTextData, cloneEntity, weData, em, cmd);
                    newBuff[i] = subRef;
                }
               ;
            }
            if (parentAsTargetAtTheEnd)
            {
                weData.TargetEntity = newParent;
                cmd.SetComponent(cloneEntity, weData);
            }
            return cloneEntity;
        }


        private static Entity DoCloneTextItemForCommandBuffer(Entity toCopy, Entity newParent, WETextData refTextData, EntityManager em, EntityCommandBuffer cmd)
        {
            var finalTargetEntity = refTextData.TargetEntity;
            var cloneEntity = cmd.Instantiate(toCopy);
            var weData = em.GetComponentData<WETextData>(toCopy);
            weData.TargetEntity = finalTargetEntity;
            weData.OnPostInstantiate();
            weData.SetNewParentForced(newParent);
            cmd.SetComponent(cloneEntity, weData);
            if (em.TryGetBuffer<WESubTextRef>(toCopy, true, out var subRefs))
            {
                cmd.RemoveComponent<WESubTextRef>(cloneEntity);
                DynamicBuffer<WESubTextRef> newBuff = cmd.AddBuffer<WESubTextRef>(cloneEntity);
                newBuff.Length = subRefs.Length;
                for (int i = 0; i < subRefs.Length; i++)
                {
                    var subRef = subRefs[i];
                    subRef.m_weTextData = DoCloneTextItemForCommandBuffer(subRefs[i].m_weTextData, cloneEntity, weData, em, cmd);
                    newBuff[i] = subRef;
                }
            }
            return cloneEntity;
        }





        public static Entity DoCloneTextItemReferenceSelf(Entity toCopy, Entity newParent, ref ComponentLookup<WETextData> tdLookup, ref BufferLookup<WESubTextRef> subTextLookup, EntityCommandBuffer cmd, bool parentAsTargetAtTheEnd = false)
        {
            var cloneEntity = cmd.Instantiate(toCopy);
            tdLookup.TryGetComponent(toCopy, out var weData);
            weData.TargetEntity = cloneEntity;
            weData.SetNewParentForced(newParent);
            cmd.SetComponent(cloneEntity, weData);
            cmd.AddComponent<WEWaitingPostInstantiation>(cloneEntity);
            if (subTextLookup.TryGetBuffer(toCopy, out var subRefs))
            {
                cmd.RemoveComponent<WESubTextRef>(cloneEntity);
                DynamicBuffer<WESubTextRef> newBuff = cmd.AddBuffer<WESubTextRef>(cloneEntity);
                newBuff.Length = subRefs.Length;
                for (int i = 0; i < subRefs.Length; i++)
                {
                    var subRef = subRefs[i];
                    subRef.m_weTextData = DoCloneTextItemForCommandBuffer(subRefs[i].m_weTextData, cloneEntity, weData, ref tdLookup, ref subTextLookup, cmd);
                    newBuff[i] = subRef;
                }
               ;
            }
            if (parentAsTargetAtTheEnd)
            {
                weData.TargetEntity = newParent;
                cmd.SetComponent(cloneEntity, weData);
            }
            return cloneEntity;
        }


        private static Entity DoCloneTextItemForCommandBuffer(Entity toCopy, Entity newParent, WETextData refTextData, ref ComponentLookup<WETextData> tdLookup, ref BufferLookup<WESubTextRef> subTextLookup, EntityCommandBuffer cmd)
        {
            var finalTargetEntity = refTextData.TargetEntity;
            var cloneEntity = cmd.Instantiate(toCopy);
            tdLookup.TryGetComponent(toCopy, out var weData);
            weData.TargetEntity = finalTargetEntity;
            weData.SetNewParentForced(newParent);
            cmd.SetComponent(cloneEntity, weData);
            cmd.AddComponent<WEWaitingPostInstantiation>(cloneEntity);
            if (subTextLookup.TryGetBuffer(toCopy, out var subRefs))
            {
                cmd.RemoveComponent<WESubTextRef>(cloneEntity);
                DynamicBuffer<WESubTextRef> newBuff = cmd.AddBuffer<WESubTextRef>(cloneEntity);
                newBuff.Length = subRefs.Length;
                for (int i = 0; i < subRefs.Length; i++)
                {
                    var subRef = subRefs[i];
                    subRef.m_weTextData = DoCloneTextItemForCommandBuffer(subRefs[i].m_weTextData, cloneEntity, weData, ref tdLookup, ref subTextLookup, cmd);
                    newBuff[i] = subRef;
                }
            }
            return cloneEntity;
        }
    }
}