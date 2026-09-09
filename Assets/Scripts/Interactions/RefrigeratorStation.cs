using UnityEngine;

namespace YesChef
{
    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public IngredientFactory factory;
        public FridgeMenuController menu;

        public string GetPrompt(PlayerController player) => player.Inventory.HasItem
            ? "Hands full - use another station or the trash"
            : "[E] Open fridge menu   |   [1] Vegetable  [2] Cheese  [3] Meat";

        public void Interact(PlayerController player)
        {
            if (!player.Inventory.HasItem) menu?.Toggle(player);
        }

        public bool TryTake(PlayerController player, IngredientType type)
        {
            if (player.Inventory.HasItem) return false;
            return player.Inventory.TryTake(factory.Create(type, PreparationState.Raw, player.Inventory.handAnchor));
        }
    }
}
