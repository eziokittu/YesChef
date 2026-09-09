using UnityEngine;

namespace YesChef
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        public Transform handAnchor;
        public IngredientItem HeldItem { get; private set; }
        public bool HasItem => HeldItem != null;

        public string GetGuidance()
        {
            if (!HasItem) return "NEXT: Visit the fridge and choose an ingredient";

            return (HeldItem.Type, HeldItem.State) switch
            {
                (IngredientType.Vegetable, PreparationState.Raw) => "NEXT: Take the vegetable to a chopping table",
                (IngredientType.Meat, PreparationState.Raw) => "NEXT: Put the meat on either stove",
                _ => "NEXT: Deliver this prepared ingredient to a matching customer table"
            };
        }

        public bool TryTake(IngredientItem item)
        {
            if (item == null || HasItem)
            {
                return false;
            }

            HeldItem = item;
            HeldItem.PlaceAt(handAnchor, 0.62f);
            return true;
        }

        public IngredientItem ReleaseTo(Transform destination, float scale = 0.55f)
        {
            if (!HasItem)
            {
                return null;
            }

            var released = HeldItem;
            HeldItem = null;
            released.PlaceAt(destination, scale);
            return released;
        }

        public IngredientItem ConsumeHeld()
        {
            if (!HasItem)
            {
                return null;
            }

            var consumed = HeldItem;
            HeldItem = null;
            consumed.transform.SetParent(null);
            return consumed;
        }

        public void Clear()
        {
            if (!HasItem)
            {
                return;
            }

            Destroy(HeldItem.gameObject);
            HeldItem = null;
        }
    }
}
