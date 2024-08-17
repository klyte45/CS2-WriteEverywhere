using Unity.Entities;

namespace BelzontWE
{
    public static class WELayoutUtility
    {
        public enum ParentEntityMode
        {
            TARGET_IS_TARGET,
            TARGET_IS_SELF,
            TARGET_IS_PARENT
        }
        public static Entity DoCreateLayoutItem(WETextDataTreeStruct toCopy, Entity parentEntity, Entity targetEntity, EntityManager em, ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET, bool selfTarget = false)
        {
            var newEntity = em.CreateEntity();
            var childTarget = CommonDataSetup(toCopy, parentEntity, targetEntity, childTargetMode, selfTarget, newEntity, out WETextData weData);
            em.AddComponentData(newEntity, weData);
            if (weData.TextType != WESimulationTextType.Placeholder && toCopy.children.Length > 0)
            {
                var buff = em.AddBuffer<WESubTextRef>(newEntity);
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    buff.Add(new WESubTextRef { m_weTextData = DoCreateLayoutItem(toCopy.children[i], newEntity, childTarget, em) });
                }
            }
            return newEntity;
        }

        public static Entity DoCreateLayoutItem(WETextDataTreeStruct toCopy, Entity parentEntity, Entity targetEntity, EntityManager em, EntityCommandBuffer cmd, ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET, bool selfTarget = false)
        {
            var newEntity = cmd.CreateEntity();
            var childTarget = CommonDataSetup(toCopy, parentEntity, targetEntity, childTargetMode, selfTarget, newEntity, out WETextData weData);
            cmd.AddComponent(newEntity, weData);
            cmd.AddComponent<WEWaitingPostInstantiation>(newEntity);
            if (weData.TextType != WESimulationTextType.Placeholder && toCopy.children.Length > 0)
            {
                var buff = em.AddBuffer<WESubTextRef>(newEntity);
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    buff.Add(new WESubTextRef { m_weTextData = DoCreateLayoutItem(toCopy.children[i], newEntity, childTarget, em, cmd) });
                }
            }
            return newEntity;
        }

        public static Entity DoCreateLayoutItem(WETextDataTreeStruct toCopy, Entity parentEntity, Entity targetEntity, ref ComponentLookup<WETextData> tdLookup, ref BufferLookup<WESubTextRef> subTextLookup, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, ParentEntityMode childTargetMode = ParentEntityMode.TARGET_IS_TARGET, bool selfTarget = false)
        {
            var newEntity = cmd.CreateEntity(unfilteredChunkIndex);
            var childTarget = CommonDataSetup(toCopy, parentEntity, targetEntity, childTargetMode, selfTarget, newEntity, out WETextData weData);
            cmd.AddComponent(unfilteredChunkIndex, newEntity, weData);
            cmd.AddComponent<WEWaitingPostInstantiation>(unfilteredChunkIndex, newEntity);
            if (weData.TextType != WESimulationTextType.Placeholder && toCopy.children.Length > 0)
            {
                var buff = cmd.AddBuffer<WESubTextRef>(unfilteredChunkIndex, newEntity);
                for (int i = 0; i < toCopy.children.Length; i++)
                {
                    buff.Add(new WESubTextRef { m_weTextData = DoCreateLayoutItem(toCopy.children[i], newEntity, childTarget, ref tdLookup, ref subTextLookup, unfilteredChunkIndex, cmd) });
                }
            }
            return newEntity;
        }

        private static Entity CommonDataSetup(WETextDataTreeStruct toCopy, Entity parentEntity, Entity targetEntity, ParentEntityMode childTargetMode, bool selfTarget, Entity newEntity, out WETextData weData)
        {
            if (selfTarget) targetEntity = newEntity;
            weData = WETextData.FromDataStruct(toCopy.self, newEntity, targetEntity);
            var childTarget = childTargetMode switch
            {
                ParentEntityMode.TARGET_IS_SELF => newEntity,
                ParentEntityMode.TARGET_IS_PARENT => parentEntity,
                _ => targetEntity
            };
            weData.SetNewParentForced(parentEntity);
            return childTarget;
        }
    }
}