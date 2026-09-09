using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YesChef
{
    /// <summary>
    /// Two independent cooking slots. Each owns its ingredient and timer, so
    /// both pieces of meat cook while the player performs other tasks.
    /// </summary>
    public sealed class StoveStation : MonoBehaviour, IInteractable
    {
        private const int SlotCount = 2;

        [Header("Scene references")]
        public Transform[] slotAnchors = new Transform[SlotCount];
        public TMP_Text statusText;
        public Image[] progressFills = new Image[SlotCount];

        [Header("Rule")]
        [Min(0.1f)] public float cookingSeconds = 6f;

        private readonly IngredientItem[] items = new IngredientItem[SlotCount];
        private readonly float[] secondsRemaining = new float[SlotCount];

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Playing)
            {
                AdvanceCookingTimers();
            }

            RefreshDisplay();
        }

        public string GetPrompt(PlayerController player)
        {
            if (!player.Inventory.HasItem && FindFirstCookedSlot() >= 0) return "[E] Pick up cooked meat";
            if (IsHoldingRawMeat(player) && FindFirstEmptySlot() >= 0) return "[E] Put raw meat on stove";
            if (IsHoldingRawMeat(player)) return "STOVE: Both slots are occupied";
            return "STOVE: Two independent meat slots";
        }

        public void Interact(PlayerController player)
        {
            if (player.Inventory.HasItem) TryPlaceMeat(player);
            else TryCollectCookedMeat(player);
            RefreshDisplay();
        }

        public void ResetStation()
        {
            for (var slot = 0; slot < SlotCount; slot++)
            {
                if (items[slot] != null) Destroy(items[slot].gameObject);
                items[slot] = null;
                secondsRemaining[slot] = 0f;
            }
            RefreshDisplay();
        }

        private void AdvanceCookingTimers()
        {
            for (var slot = 0; slot < SlotCount; slot++)
            {
                if (items[slot] == null || items[slot].State != PreparationState.Raw) continue;
                secondsRemaining[slot] = Mathf.Max(0f, secondsRemaining[slot] - Time.deltaTime);
                if (secondsRemaining[slot] <= 0f) items[slot].SetPrepared();
            }
        }

        private void TryPlaceMeat(PlayerController player)
        {
            var emptySlot = FindFirstEmptySlot();
            if (!IsHoldingRawMeat(player) || emptySlot < 0) return;
            items[emptySlot] = player.Inventory.ReleaseTo(slotAnchors[emptySlot], 0.48f);
            secondsRemaining[emptySlot] = cookingSeconds;
        }

        private void TryCollectCookedMeat(PlayerController player)
        {
            var cookedSlot = FindFirstCookedSlot();
            if (cookedSlot < 0) return;
            if (player.Inventory.TryTake(items[cookedSlot]))
            {
                items[cookedSlot] = null;
                secondsRemaining[cookedSlot] = 0f;
            }
        }

        private static bool IsHoldingRawMeat(PlayerController player)
        {
            var held = player.Inventory.HeldItem;
            return held != null && held.Type == IngredientType.Meat && held.State == PreparationState.Raw;
        }

        private int FindFirstEmptySlot()
        {
            for (var slot = 0; slot < SlotCount; slot++) if (items[slot] == null) return slot;
            return -1;
        }

        private int FindFirstCookedSlot()
        {
            for (var slot = 0; slot < SlotCount; slot++)
            {
                if (items[slot] != null && items[slot].State == PreparationState.Prepared) return slot;
            }
            return -1;
        }

        private string GetSlotText(int slot)
        {
            if (items[slot] == null) return "Empty";
            return items[slot].State == PreparationState.Prepared
                ? "<color=#7DFF72>Cooked!</color>"
                : $"{secondsRemaining[slot]:0.0}s";
        }

        private void RefreshDisplay()
        {
            if (statusText != null)
            {
                statusText.text = $"<b>STOVE</b>\n1: {GetSlotText(0)}   2: {GetSlotText(1)}";
            }

            for (var slot = 0; slot < progressFills.Length && slot < SlotCount; slot++)
            {
                if (progressFills[slot] == null) continue;
                progressFills[slot].fillAmount = items[slot] == null ? 0f
                    : items[slot].State == PreparationState.Prepared ? 1f
                    : 1f - secondsRemaining[slot] / cookingSeconds;
            }
        }
    }
}
