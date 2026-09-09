using UnityEngine;

namespace YesChef
{
    public sealed class IngredientItem : MonoBehaviour
    {
        public IngredientType Type { get; private set; }
        public PreparationState State { get; private set; }

        private IngredientFactory factory;
        private GameObject visual;

        public void Initialize(IngredientFactory sourceFactory, IngredientType type, PreparationState state)
        {
            factory = sourceFactory;
            Type = type;
            State = state;
            name = $"{state} {type}";
            RefreshVisual();
        }

        public void SetPrepared()
        {
            State = PreparationState.Prepared;
            name = $"Prepared {Type}";
            RefreshVisual();
        }

        public void PlaceAt(Transform anchor, float scale = 0.55f)
        {
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * scale;
        }

        private void RefreshVisual()
        {
            if (visual != null)
            {
                Destroy(visual);
            }

            visual = factory.CreateVisual(Type, State, transform);
        }
    }

}
