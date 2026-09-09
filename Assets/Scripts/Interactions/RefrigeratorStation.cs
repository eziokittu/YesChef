using UnityEngine;

namespace YesChef
{
    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public IngredientFactory factory;

        public string GetPrompt(PlayerController player) => player.Inventory.HasItem
            ? "Hands full - use another station or the trash"
            : "FRIDGE: [1] Vegetable  [2] Cheese  [3] Meat  ([E] Vegetable)";

        public void Interact(PlayerController player) => TryTake(player, IngredientType.Vegetable);

        public void TryTake(PlayerController player, IngredientType type)
        {
            if (player.Inventory.HasItem) return;
            player.Inventory.TryTake(factory.Create(type, PreparationState.Raw, player.Inventory.handAnchor));
        }
    }
}
