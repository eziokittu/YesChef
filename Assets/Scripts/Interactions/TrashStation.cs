using UnityEngine;

namespace YesChef
{
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
