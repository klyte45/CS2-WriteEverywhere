using Colossal.Entities;
using Unity.Entities;

namespace BelzontWE
{
    public static class WELayoutUtility
    {
        public static Entity DoCloneTextItem(Entity toCopy, Entity newParent, EntityManager em, Entity target)
        {
            var cloneEntity = em.Instantiate(toCopy);
            var finalTargetEntity = target == Entity.Null ? cloneEntity : target;
            var weData = em.GetComponentData<WETextData>(cloneEntity);
            weData.TargetEntity = finalTargetEntity;
            weData.OnPostInstantiate();
            weData.SetNewParent(newParent, em, target == Entity.Null);
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
    }
}