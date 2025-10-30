﻿

using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WEWaitingRendering : IQueryTypeParameter, IComponentData, IEnableableComponent { }
    public struct WEWaitingPostInstantiation : IQueryTypeParameter, IComponentData { }
    public struct WEPlaceholderToBeProcessedInMain : IQueryTypeParameter, IComponentData {
        public FixedString128Bytes layoutName;
    }
}