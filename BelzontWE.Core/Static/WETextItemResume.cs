using Unity.Entities;

namespace BelzontWE
{
    public partial class WEWorldPickerController
    {
        public struct WETextItemResume
        {
            public string name;
            public int type;
            public Entity id;
            public WETextItemResume[] children;
        }
    }

}