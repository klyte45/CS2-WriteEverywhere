using Belzont.Utils;
using Colossal.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public partial class WETextDataTransformController : WETextDataBaseController
    {
        private const string PREFIX = "dataTransform.";

        public MultiUIValueBinding<float3, float[]> CurrentScale { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentRotation { get; private set; }
        public MultiUIValueBinding<float3, float[]> CurrentPosition { get; private set; }
        public MultiUIValueBinding<bool> UseAbsoluteSizeEditing { get; private set; }
        protected override void DoInitValueBindings()
        {

            CurrentScale = new(default, $"{PREFIX}{nameof(CurrentScale)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentRotation = new(default, $"{PREFIX}{nameof(CurrentRotation)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            CurrentPosition = new(default, $"{PREFIX}{nameof(CurrentPosition)}", EventCaller, CallBinder, (x, _) => new[] { x.x, x.y, x.z }, (x, _) => new float3(x[0], x[1], x[2]));
            UseAbsoluteSizeEditing = new(default, $"{PREFIX}{nameof(UseAbsoluteSizeEditing)}", EventCaller, CallBinder);


            CurrentScale.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.scale = x; return currentItem; });
            CurrentRotation.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.offsetRotation = KMathUtils.UnityEulerToQuaternion(x); return currentItem; });
            CurrentPosition.OnScreenValueChanged += (x) => PickerController.EnqueueModification<float3, WETextDataTransform>(x, (x, currentItem) => { currentItem.offsetPosition = x; return currentItem; });
            UseAbsoluteSizeEditing.OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataTransform>(x, (x, currentItem) => { currentItem.useAbsoluteSizeEditing = x; return currentItem; });
        }

        public override void OnCurrentItemChanged(Entity entity)
        {
            EntityManager.TryGetComponent<WETextDataTransform>(entity, out var transform);

            CurrentPosition.Value = transform.offsetPosition;
            CurrentRotation.Value = KMathUtils.UnityQuaternionToEuler(transform.offsetRotation);
            CurrentScale.Value = transform.scale;
            UseAbsoluteSizeEditing.Value = transform.useAbsoluteSizeEditing;
        }
    }
}