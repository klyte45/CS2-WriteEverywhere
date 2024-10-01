﻿using Colossal.Entities;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataMain : IComponentData
    {

        private FixedString32Bytes itemName;
        private Entity targetEntity;
        private Entity parentEntity;

        public FixedString32Bytes ItemName { get => itemName; set => itemName = value; }
        public Entity TargetEntity { get => targetEntity; set => targetEntity = value; }
        public Entity ParentEntity { get => parentEntity; set => parentEntity = value; }

        public static WETextDataMain CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                targetEntity = target,
                parentEntity = parent ?? target,
                itemName = "New item",
            };
        public bool SetNewParent(Entity e, EntityManager em)
        {
            if ((e != TargetEntity && e != Entity.Null && (!em.TryGetComponent<WETextDataMesh>(e, out var mesh) || !em.TryGetComponent<WETextDataMain>(e, out var mainData) || mesh.TextType == WESimulationTextType.Placeholder || (mainData.TargetEntity != Entity.Null && mainData.TargetEntity != TargetEntity))))
            {
                return false;
            }
            ParentEntity = e;
            return true;
        }
        public void SetNewParentForced(Entity e)
        {
            ParentEntity = e;
        }
    }
}