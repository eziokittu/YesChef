using UnityEngine;

namespace YesChef
{
    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public IngredientFactory factory;
        public FridgeMenuController menu;
        public RefrigeratorAnimator animator;
        public WorldLabelFader labelFader;

        public string GetPrompt(PlayerController player)
        {
            if (menu != null && menu.IsOpen) return "[E] Close fridge";
            return player.Inventory.HasItem
                ? "Hands full - use another station or the trash"
                : "[E] Open fridge menu   |   [1] Vegetable  [2] Cheese  [3] Meat";
        }

        public void Interact(PlayerController player)
        {
            // Closing the door must always be allowed, even after an item was
            // taken through a quick-pick or while the chef's hands are full.
            if (menu != null && menu.IsOpen) menu.Close();
            else if (!player.Inventory.HasItem) menu?.Open(player);
        }

        public bool TryTake(PlayerController player, IngredientType type)
        {
            if (player.Inventory.HasItem) return false;
            var taken = player.Inventory.TryTake(factory.Create(type, PreparationState.Raw, player.Inventory.handAnchor));
            if (taken) KitchenActivityEffects.Instance?.OnFridgeTake(type);
            return taken;
        }

        public void SetOpen(bool value)
        {
            animator?.SetOpen(value);
            labelFader?.SetSuppressed(value);
        }
    }
}
