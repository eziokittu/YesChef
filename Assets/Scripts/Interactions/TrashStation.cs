using UnityEngine;

namespace YesChef
{
    public sealed class TrashStation : MonoBehaviour, IInteractable
    {
        public TrashLidAnimator lidAnimator;
        public string GetPrompt(PlayerController player) => player.Inventory.HasItem ? "[E] Throw away held ingredient" : "TRASH: Hands are empty";
        public void Interact(PlayerController player)
        {
            var discarded = player.Inventory.ConsumeHeld();
            if (discarded == null) return;
            lidAnimator?.OpenOnce();
            KitchenActivityEffects.Instance?.OnTrashDiscard();
            AudioDirector.Instance?.PlayTrash();
            Destroy(discarded.gameObject);
        }
    }
}
