

using Unity.Entities;

namespace BelzontWE
{
    public struct WEWaitingRendering : IQueryTypeParameter, IComponentData { }
    public struct WEWaitingPostInstantiation : IQueryTypeParameter, IComponentData { }
    public struct WEWaitingRenderingPlaceholder : IQueryTypeParameter, IComponentData { }
}