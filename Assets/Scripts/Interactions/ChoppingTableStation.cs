using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YesChef
{
    /// <summary>
    /// Accepts one raw vegetable, prepares it over time, then lets the player
    /// collect it. The player is free to walk away while preparation continues.
    /// </summary>
    public sealed class ChoppingTableStation : MonoBehaviour, IInteractable
    {
        [Header("Scene references")]
        public Transform itemAnchor;
        public TMP_Text statusText;
        public Image progressFill;

        [Header("Rule")]
        [Min(0.1f)] public float preparationSeconds = 2f;

        private IngredientItem item;
        private float secondsRemaining;

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Playing)
            {
                AdvancePreparation();
            }

            RefreshDisplay();
        }

        public string GetPrompt(PlayerController player)
        {
            if (item == null)
            {
                return IsHoldingRawVegetable(player)
                    ? "[E] Place vegetable on chopping table"
                    : "TABLE: Needs a raw vegetable";
            }

            if (item.State == PreparationState.Prepared && !player.Inventory.HasItem)
            {
                return "[E] Pick up chopped vegetable";
            }

            return item.State == PreparationState.Raw
                ? $"Chopping... {secondsRemaining:0.0}s"
                : "Table occupied - empty your hands first";
        }

        public void Interact(PlayerController player)
        {
            if (item == null) TryPlaceVegetable(player);
            else TryCollectVegetable(player);
            RefreshDisplay();
        }

        public void ResetStation()
        {
            if (item != null) Destroy(item.gameObject);
            item = null;
            secondsRemaining = 0f;
            RefreshDisplay();
        }

        private void AdvancePreparation()
        {
            if (item == null || item.State != PreparationState.Raw) return;
            secondsRemaining = Mathf.Max(0f, secondsRemaining - Time.deltaTime);
            if (secondsRemaining <= 0f) item.SetPrepared();
        }

        private void TryPlaceVegetable(PlayerController player)
        {
            if (!IsHoldingRawVegetable(player)) return;
            item = player.Inventory.ReleaseTo(itemAnchor);
            secondsRemaining = preparationSeconds;
        }

        private void TryCollectVegetable(PlayerController player)
        {
            if (item.State != PreparationState.Prepared || player.Inventory.HasItem) return;
            if (player.Inventory.TryTake(item)) item = null;
        }

        private static bool IsHoldingRawVegetable(PlayerController player)
        {
            var held = player.Inventory.HeldItem;
            return held != null && held.Type == IngredientType.Vegetable && held.State == PreparationState.Raw;
        }

        private void RefreshDisplay()
        {
            if (statusText != null)
            {
                statusText.text = item == null
                    ? "<b>CHOPPING TABLE</b>\nEmpty"
                    : item.State == PreparationState.Prepared
                        ? "<b>CHOPPING TABLE</b>\n<color=#7DFF72>Chopped - ready!</color>"
                        : $"<b>CHOPPING TABLE</b>\nChopping  {secondsRemaining:0.0}s";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = item == null ? 0f
                    : item.State == PreparationState.Prepared ? 1f
                    : 1f - secondsRemaining / preparationSeconds;
            }
        }
    }
}
