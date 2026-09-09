using TMPro;
using UnityEngine;

namespace YesChef
{
    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public IngredientFactory factory;

        public string GetPrompt(PlayerController player)
        {
            return player.Inventory.HasItem
                ? "Hands full - use another station or the trash"
                : "FRIDGE: [1] Vegetable  [2] Cheese  [3] Meat  ([E] Vegetable)";
        }

        public void Interact(PlayerController player) => TryTake(player, IngredientType.Vegetable);

        public void TryTake(PlayerController player, IngredientType type)
        {
            if (player.Inventory.HasItem)
            {
                return;
            }

            player.Inventory.TryTake(factory.Create(type, PreparationState.Raw, player.Inventory.handAnchor));
        }
    }

    public sealed class ChoppingTableStation : MonoBehaviour, IInteractable
    {
        public Transform itemAnchor;
        public TMP_Text statusText;
        public float preparationSeconds = 2f;

        private IngredientItem item;
        private float remaining;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing)
            {
                RefreshText();
                return;
            }

            if (item != null && item.State == PreparationState.Raw)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f)
                {
                    remaining = 0f;
                    item.SetPrepared();
                }
            }

            RefreshText();
        }

        public string GetPrompt(PlayerController player)
        {
            if (item == null)
            {
                return player.Inventory.HeldItem != null && player.Inventory.HeldItem.Type == IngredientType.Vegetable
                    ? "[E] Place vegetable on chopping table"
                    : "TABLE: Needs a raw vegetable";
            }

            if (item.State == PreparationState.Prepared && !player.Inventory.HasItem)
            {
                return "[E] Pick up chopped vegetable";
            }

            return item.State == PreparationState.Raw ? $"Chopping... {remaining:0.0}s" : "Table occupied";
        }

        public void Interact(PlayerController player)
        {
            if (item == null)
            {
                var held = player.Inventory.HeldItem;
                if (held == null || held.Type != IngredientType.Vegetable || held.State != PreparationState.Raw)
                {
                    return;
                }

                item = player.Inventory.ReleaseTo(itemAnchor);
                remaining = preparationSeconds;
                RefreshText();
                return;
            }

            if (item.State == PreparationState.Prepared && !player.Inventory.HasItem && player.Inventory.TryTake(item))
            {
                item = null;
                RefreshText();
            }
        }

        public void ResetStation()
        {
            if (item != null) Destroy(item.gameObject);
            item = null;
            remaining = 0f;
            RefreshText();
        }

        private void RefreshText()
        {
            if (statusText == null) return;
            statusText.text = item == null
                ? "<b>CHOPPING TABLE</b>\nEmpty"
                : item.State == PreparationState.Prepared
                    ? "<b>CHOPPING TABLE</b>\n<color=#7DFF72>Chopped - ready!</color>"
                    : $"<b>CHOPPING TABLE</b>\nChopping {remaining:0.0}s";
        }
    }

    public sealed class StoveStation : MonoBehaviour, IInteractable
    {
        public Transform[] slotAnchors = new Transform[2];
        public TMP_Text statusText;
        public float cookingSeconds = 6f;

        private readonly IngredientItem[] items = new IngredientItem[2];
        private readonly float[] remaining = new float[2];

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing)
            {
                RefreshText();
                return;
            }

            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] == null || items[i].State != PreparationState.Raw) continue;
                remaining[i] -= Time.deltaTime;
                if (remaining[i] <= 0f)
                {
                    remaining[i] = 0f;
                    items[i].SetPrepared();
                }
            }
            RefreshText();
        }

        public string GetPrompt(PlayerController player)
        {
            if (!player.Inventory.HasItem && FirstCookedSlot() >= 0) return "[E] Pick up cooked meat";
            if (player.Inventory.HeldItem != null && player.Inventory.HeldItem.Type == IngredientType.Meat && FirstEmptySlot() >= 0)
                return "[E] Put raw meat on stove";
            return "STOVE: Two meat cooking slots";
        }

        public void Interact(PlayerController player)
        {
            if (!player.Inventory.HasItem)
            {
                var cooked = FirstCookedSlot();
                if (cooked >= 0 && player.Inventory.TryTake(items[cooked]))
                {
                    items[cooked] = null;
                    remaining[cooked] = 0f;
                }
                RefreshText();
                return;
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
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] != null) Destroy(items[i].gameObject);
                items[i] = null;
                remaining[i] = 0f;
            }
            RefreshText();
        }

        private int FirstEmptySlot()
        {
            for (var i = 0; i < items.Length; i++) if (items[i] == null) return i;
            return -1;
        }

        private int FirstCookedSlot()
        {
            for (var i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].State == PreparationState.Prepared) return i;
            return -1;
        }

        private void RefreshText()
        {
            if (statusText == null) return;
            statusText.text = $"<b>STOVE</b>\n1: {SlotText(0)}   2: {SlotText(1)}";
        }

        private string SlotText(int index)
        {
            if (items[index] == null) return "Empty";
            return items[index].State == PreparationState.Prepared
                ? "<color=#7DFF72>Cooked!</color>"
                : $"{remaining[index]:0.0}s";
        }
    }

    public sealed class TrashStation : MonoBehaviour, IInteractable
    {
        public string GetPrompt(PlayerController player) => player.Inventory.HasItem ? "[E] Throw away held ingredient" : "TRASH: Hands are empty";

        public void Interact(PlayerController player)
        {
            var discarded = player.Inventory.ConsumeHeld();
            if (discarded != null) Destroy(discarded.gameObject);
        }
    }
}
