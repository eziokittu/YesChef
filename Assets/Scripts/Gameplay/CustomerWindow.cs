using System.Linq;
using TMPro;
using UnityEngine;

namespace YesChef
{
    public sealed class CustomerWindow : MonoBehaviour, IInteractable
    {
        [Header("TextMesh Pro display")]
        [Tooltip("Shows the current ingredients and how long this order has been open.")]
        public TMP_Text orderText;

        [Tooltip("Shows the points awarded near the customer window.")]
        public TMP_Text scorePopupText;

        [Header("Timing")]
        [Min(0f)] public float respawnSeconds = 5f;
        [Min(0.1f)] public float popupSeconds = 2.5f;
        [Min(0.1f)] public float popupFadeSeconds = 1f;

        private OrderTicket currentOrder;
        private float respawnRemaining;
        private float popupRemaining;

        private bool HasOrder => currentOrder != null;

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || game.Phase != GamePhase.Playing) return;

            if (HasOrder)
            {
                currentOrder.Tick(Time.deltaTime);
            }
            else
            {
                respawnRemaining -= Time.deltaTime;
                if (respawnRemaining <= 0f) CreateOrder();
            }

            if (popupRemaining > 0f)
            {
                popupRemaining -= Time.deltaTime;
                RefreshPopup();
            }

            RefreshText();
        }

        public string GetPrompt(PlayerController player)
        {
            if (!HasOrder) return $"Window waiting for next order ({Mathf.Max(0f, respawnRemaining):0.0}s)";
            if (!player.Inventory.HasItem) return "WINDOW: Bring a required prepared ingredient";
            var item = player.Inventory.HeldItem;
            if (!IngredientRules.IsDeliveryReady(item.Type, item.State)) return "That ingredient still needs preparation";
            return currentOrder.RemainingIngredients.Contains(item.Type)
                ? $"[E] Deliver {item.Type}"
                : "This order does not need that ingredient";
        }

        public void Interact(PlayerController player)
        {
            if (!HasOrder || !player.Inventory.HasItem) return;
            var item = player.Inventory.HeldItem;
            if (!IngredientRules.IsDeliveryReady(item.Type, item.State)) return;

            if (!currentOrder.TryDeliver(item.Type)) return;

            var delivered = player.Inventory.ConsumeHeld();
            if (delivered != null) Destroy(delivered.gameObject);

            if (currentOrder.IsComplete) CompleteOrder();
            RefreshText();
        }

        public void CreateOrder()
        {
            currentOrder = OrderGenerator.CreateRandom();
            respawnRemaining = 0f;
            RefreshText();
        }

        public void ResetWindow()
        {
            popupRemaining = 0f;
            ClearPopup();
            CreateOrder();
        }

        private void CompleteOrder()
        {
            var awarded = currentOrder.CalculateScore();
            GameManager.Instance.AddScore(awarded);
            if (scorePopupText != null)
            {
                scorePopupText.text = awarded >= 0
                    ? $"<color=#7DFF72>+{awarded}</color>"
                    : $"<color=#FF6868>{awarded}</color>";
                scorePopupText.alpha = 1f;
            }
            popupRemaining = popupSeconds;
            currentOrder = null;
            respawnRemaining = respawnSeconds;
        }

        private void RefreshText()
        {
            if (orderText == null) return;
            if (!HasOrder)
            {
                orderText.text = $"<b>NEXT ORDER</b>\n{Mathf.Max(0f, respawnRemaining):0.0}s";
                return;
            }

            var required = currentOrder.RequiredIngredients;
            var remaining = currentOrder.RemainingIngredients;
            var ingredients = string.Join("  ", required.Select((type, index) =>
            {
                var deliveredCount = required.Count(x => x == type) - remaining.Count(x => x == type);
                var occurrenceNumber = required.Take(index + 1).Count(x => x == type);
                var done = occurrenceNumber <= deliveredCount;
                var color = ColorUtility.ToHtmlStringRGB(IngredientRules.Color(type));
                return done ? $"<s>{IngredientRules.ShortName(type)}</s>" : $"<color=#{color}>{IngredientRules.ShortName(type)}</color>";
            }));
            orderText.text = $"<b>ORDER</b>  {currentOrder.SecondsOpen:0}s\n{ingredients}";
        }

        private void RefreshPopup()
        {
            if (scorePopupText == null) return;
            scorePopupText.alpha = Mathf.Clamp01(popupRemaining / popupFadeSeconds);
            if (popupRemaining <= 0f) ClearPopup();
        }

        private void ClearPopup()
        {
            if (scorePopupText == null) return;
            scorePopupText.text = string.Empty;
            scorePopupText.alpha = 1f;
        }
    }
}
