using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Screen-space fridge catalogue. The visible buttons call the three named
    /// methods below, which makes the Inspector wiring easy to understand.
    /// </summary>
    public sealed class FridgeMenuController : MonoBehaviour
    {
        public GameObject panel;
        public RefrigeratorStation refrigerator;
        public PlayerController player;
        public float closeDistance = 3f;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Update()
        {
            if (!IsOpen || refrigerator == null || player == null) return;
            if (Vector3.Distance(player.transform.position, refrigerator.transform.position) > closeDistance)
            {
                Close();
            }
        }

        public void Open(PlayerController interactingPlayer)
        {
            player = interactingPlayer;
            if (panel != null) panel.SetActive(true);
            refrigerator?.SetOpen(true);
        }

        public void Close()
        {
            HidePanel();
            refrigerator?.SetOpen(false);
        }

        public void HidePanel()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Toggle(PlayerController interactingPlayer)
        {
            if (IsOpen) Close();
            else Open(interactingPlayer);
        }

        public void TakeVegetable() => Take(IngredientType.Vegetable);
        public void TakeCheese() => Take(IngredientType.Cheese);
        public void TakeMeat() => Take(IngredientType.Meat);

        private void Take(IngredientType type)
        {
            if (player == null || refrigerator == null) return;
            refrigerator.TryTake(player, type);
        }
    }
}
