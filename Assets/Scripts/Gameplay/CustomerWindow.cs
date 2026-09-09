using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace YesChef
{
    public sealed class CustomerWindow : MonoBehaviour, IInteractable
    {
        public TMP_Text orderText;
        public TMP_Text scorePopupText;
        public float respawnSeconds = 5f;

        private readonly List<IngredientType> required = new();
        private readonly List<IngredientType> remaining = new();
        private bool hasOrder;
        private float orderAge;
        private float respawnRemaining;
        private float popupRemaining;

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || game.Phase != GamePhase.Playing) return;

            if (hasOrder)
            {
                orderAge += Time.deltaTime;
            }
            else
            {
                respawnRemaining -= Time.deltaTime;
                if (respawnRemaining <= 0f) CreateOrder();
            }

            if (popupRemaining > 0f)
            {
                popupRemaining -= Time.deltaTime;
                if (popupRemaining <= 0f && scorePopupText != null) scorePopupText.text = string.Empty;
            }

            RefreshText();
        }

        public string GetPrompt(PlayerController player)
        {
            if (!hasOrder) return $"Window waiting for next order ({Mathf.Max(0f, respawnRemaining):0.0}s)";
            if (!player.Inventory.HasItem) return "WINDOW: Bring a required prepared ingredient";
            var item = player.Inventory.HeldItem;
            if (!IngredientRules.IsDeliveryReady(item.Type, item.State)) return "That ingredient still needs preparation";
            return remaining.Contains(item.Type) ? $"[E] Deliver {item.Type}" : "This order does not need that ingredient";
        }

        public void Interact(PlayerController player)
        {
            if (!hasOrder || !player.Inventory.HasItem) return;
            var item = player.Inventory.HeldItem;
            if (!IngredientRules.IsDeliveryReady(item.Type, item.State)) return;

            var index = remaining.IndexOf(item.Type);
            if (index < 0) return;

            remaining.RemoveAt(index);
            var delivered = player.Inventory.ConsumeHeld();
            if (delivered != null) Destroy(delivered.gameObject);

            if (remaining.Count == 0) CompleteOrder();
            RefreshText();
        }

        public void CreateOrder()
        {
            required.Clear();
            remaining.Clear();
            var ingredientCount = Random.value < 0.5f ? 2 : 3;
            for (var i = 0; i < ingredientCount; i++)
            {
                var ingredient = (IngredientType)Random.Range(0, 3);
                required.Add(ingredient);
                remaining.Add(ingredient);
            }
            orderAge = 0f;
            respawnRemaining = 0f;
            hasOrder = true;
            RefreshText();
        }

        public void ResetWindow()
        {
            popupRemaining = 0f;
            if (scorePopupText != null) scorePopupText.text = string.Empty;
            CreateOrder();
        }

        private void CompleteOrder()
        {
            var baseScore = required.Sum(IngredientRules.Score);
            var awarded = baseScore - Mathf.FloorToInt(orderAge);
            GameManager.Instance.AddScore(awarded);
            if (scorePopupText != null)
            {
                scorePopupText.text = awarded >= 0
                    ? $"<color=#7DFF72>+{awarded}</color>"
                    : $"<color=#FF6868>{awarded}</color>";
            }
            popupRemaining = 2.5f;
            hasOrder = false;
            respawnRemaining = respawnSeconds;
            required.Clear();
            remaining.Clear();
        }

        private void RefreshText()
        {
            if (orderText == null) return;
            if (!hasOrder)
            {
                orderText.text = $"<b>NEXT ORDER</b>\n{Mathf.Max(0f, respawnRemaining):0.0}s";
                return;
            }

            var ingredients = string.Join("  ", required.Select((type, index) =>
            {
                var deliveredCount = required.Count(x => x == type) - remaining.Count(x => x == type);
                var occurrenceNumber = required.Take(index + 1).Count(x => x == type);
                var done = occurrenceNumber <= deliveredCount;
                var color = ColorUtility.ToHtmlStringRGB(IngredientRules.Color(type));
                return done ? $"<s>{IngredientRules.ShortName(type)}</s>" : $"<color=#{color}>{IngredientRules.ShortName(type)}</color>";
            }));
            orderText.text = $"<b>ORDER</b>  {orderAge:0}s\n{ingredients}";
        }
    }
}
