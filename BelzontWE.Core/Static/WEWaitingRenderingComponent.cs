#define BURST
//#define VERBOSE 
using Unity.Entities;

namespace BelzontWE
{
    public struct WEWaitingRenderingComponent : IBufferElementData
    {
        public WESimulationTextComponent src;

        public static WEWaitingRenderingComponent From(WESimulationTextComponent src)
        {
            var result = new WEWaitingRenderingComponent
            {
                src = src
            };
            return result;
        }
    }

}