using System.Collections;
using UnityEngine;

namespace YesChef
{
    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public IngredientFactory factory;
        public FridgeMenuController menu;
        public RefrigeratorAnimator animator;
        public WorldLabelFader labelFader;
        [Min(0.2f)] public float pickupReachSeconds = 0.6f;

        private Coroutine takeRoutine;
        private PlayerController activePlayer;

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
            if (player == null || player.Inventory.HasItem || takeRoutine != null || menu == null || !menu.IsOpen)
                return false;

            activePlayer = player;
            menu.HidePanel();
            player.BeginFridgeAction(transform.position);
            takeRoutine = StartCoroutine(TakeAfterDoorOpens(player, type));
            return true;
        }

        public void SetOpen(bool value)
        {
            if (!value && takeRoutine != null) CancelPendingTake();
            animator?.SetOpen(value);
            labelFader?.SetSuppressed(value);
        }

        private IEnumerator TakeAfterDoorOpens(PlayerController player, IngredientType type)
        {
            while (animator != null && !animator.IsPickupReady) yield return null;

            var reachSeconds = Mathf.Max(0.2f, pickupReachSeconds);
            player.PlayFridgeReach(reachSeconds);
            yield return new WaitForSeconds(reachSeconds * 0.52f);

            if (!player.Inventory.HasItem)
            {
                var item = factory.Create(type, PreparationState.Raw, player.Inventory.handAnchor);
                if (player.Inventory.TryTake(item)) KitchenActivityEffects.Instance?.OnFridgeTake(type);
                else Destroy(item.gameObject);
            }

            yield return new WaitForSeconds(reachSeconds * 0.48f);
            takeRoutine = null;
            activePlayer = null;
            menu.Close();
            player.EndFridgeAction();
        }

        private void CancelPendingTake()
        {
            StopCoroutine(takeRoutine);
            takeRoutine = null;
            activePlayer?.EndFridgeAction();
            activePlayer = null;
        }
    }
}
