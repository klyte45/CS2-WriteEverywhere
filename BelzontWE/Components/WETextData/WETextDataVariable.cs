using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataVariable : IBufferElementData
    {
        private const uint VERSION = 0;
        private FixedString32Bytes key;
        private FixedString32Bytes value;

        public FixedString32Bytes Key { readonly get => key; set => key = value; }
        public FixedString32Bytes Value { readonly get => value; set => this.value = value; }
  
    }
}