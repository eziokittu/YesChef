using TMPro;
using UnityEngine;

namespace YesChef
{
    public sealed class ChoppingTableStation : MonoBehaviour, IInteractable
    {
        public Transform itemAnchor;
        public TMP_Text statusText;
        public float preparationSeconds = 2f;
        private IngredientItem item;
        private float remaining;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing) { RefreshText(); return; }
            if (item != null && item.State == PreparationState.Raw)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f) { remaining = 0f; item.SetPrepared(); }
            }
            RefreshText();
        }

        public string GetPrompt(PlayerController player)
        {
            if (item == null) return player.Inventory.HeldItem != null && player.Inventory.HeldItem.Type == IngredientType.Vegetable
                ? "[E] Place vegetable on chopping table" : "TABLE: Needs a raw vegetable";
            if (item.State == PreparationState.Prepared && !player.Inventory.HasItem) return "[E] Pick up chopped vegetable";
            return item.State == PreparationState.Raw ? $"Chopping... {remaining:0.0}s" : "Table occupied";
        }

        public void Interact(PlayerController player)
        {
            if (item == null)
            {
                var held = player.Inventory.HeldItem;
                if (held == null || held.Type != IngredientType.Vegetable || held.State != PreparationState.Raw) return;
                item = player.Inventory.ReleaseTo(itemAnchor);
                remaining = preparationSeconds;
                RefreshText();
                return;
            }
            if (item.State == PreparationState.Prepared && !player.Inventory.HasItem && player.Inventory.TryTake(item)) item = null;
            RefreshText();
        }

        public void ResetStation()
        {
            if (item != null) Destroy(item.gameObject);
            item = null; remaining = 0f; RefreshText();
        }

        private void RefreshText()
        {
            if (statusText == null) return;
            statusText.text = item == null ? "<b>CHOPPING TABLE</b>\nEmpty" : item.State == PreparationState.Prepared
                ? "<b>CHOPPING TABLE</b>\n<color=#7DFF72>Chopped - ready!</color>"
                : $"<b>CHOPPING TABLE</b>\nChopping {remaining:0.0}s";
        }
    }
}
