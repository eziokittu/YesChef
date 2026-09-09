using TMPro;
using UnityEngine;

namespace YesChef
{
    public sealed class StoveStation : MonoBehaviour, IInteractable
    {
        public Transform[] slotAnchors = new Transform[2];
        public TMP_Text statusText;
        public float cookingSeconds = 6f;
        private readonly IngredientItem[] items = new IngredientItem[2];
        private readonly float[] remaining = new float[2];

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing) { RefreshText(); return; }
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] == null || items[i].State != PreparationState.Raw) continue;
                remaining[i] -= Time.deltaTime;
                if (remaining[i] <= 0f) { remaining[i] = 0f; items[i].SetPrepared(); }
            }
            RefreshText();
        }

        public string GetPrompt(PlayerController player)
        {
            if (!player.Inventory.HasItem && FirstCookedSlot() >= 0) return "[E] Pick up cooked meat";
            if (player.Inventory.HeldItem != null && player.Inventory.HeldItem.Type == IngredientType.Meat && FirstEmptySlot() >= 0) return "[E] Put raw meat on stove";
            return "STOVE: Two meat cooking slots";
        }

        public void Interact(PlayerController player)
        {
            if (!player.Inventory.HasItem)
            {
                var cooked = FirstCookedSlot();
                if (cooked >= 0 && player.Inventory.TryTake(items[cooked])) { items[cooked] = null; remaining[cooked] = 0f; }
                RefreshText(); return;
            }
            var held = player.Inventory.HeldItem;
            var empty = FirstEmptySlot();
            if (held.Type != IngredientType.Meat || held.State != PreparationState.Raw || empty < 0) return;
            items[empty] = player.Inventory.ReleaseTo(slotAnchors[empty], 0.48f);
            remaining[empty] = cookingSeconds;
            RefreshText();
        }

        public void ResetStation()
        {
            for (var i = 0; i < items.Length; i++) { if (items[i] != null) Destroy(items[i].gameObject); items[i] = null; remaining[i] = 0f; }
            RefreshText();
        }

        private int FirstEmptySlot() { for (var i = 0; i < items.Length; i++) if (items[i] == null) return i; return -1; }
        private int FirstCookedSlot() { for (var i = 0; i < items.Length; i++) if (items[i] != null && items[i].State == PreparationState.Prepared) return i; return -1; }
        private string SlotText(int index) => items[index] == null ? "Empty" : items[index].State == PreparationState.Prepared ? "<color=#7DFF72>Cooked!</color>" : $"{remaining[index]:0.0}s";
        private void RefreshText() { if (statusText != null) statusText.text = $"<b>STOVE</b>\n1: {SlotText(0)}   2: {SlotText(1)}"; }
    }
}
