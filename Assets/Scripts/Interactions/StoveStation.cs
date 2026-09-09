using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YesChef
{
    /// <summary>
    /// One physical stove with one cooking position. The scene contains two of
    /// these components on separate tables, so both pieces of meat cook independently.
    /// </summary>
    public sealed class StoveStation : MonoBehaviour, IInteractable
    {
        [Header("Scene references")]
        public Transform itemAnchor;
        public TMP_Text statusText;
        public Image progressFill;
        public WorldLabelFader labelFader;
        public ParticleSystem flameParticles;
        public Light cookingLight;

        [Header("Rule")]
        [Min(0.1f)] public float cookingSeconds = 6f;

        private IngredientItem item;
        private float secondsRemaining;

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Playing)
            {
                AdvanceCooking();
            }

            UpdateEffects();
            RefreshDisplay();
        }

        public string GetPrompt(PlayerController player)
        {
            if (item == null)
            {
                return IsHoldingRawMeat(player) ? "[E] Put raw meat on this stove" : "STOVE: Needs raw meat";
            }

            if (item.State == PreparationState.Prepared && !player.Inventory.HasItem) return "[E] Pick up cooked meat";
            return item.State == PreparationState.Raw
                ? $"Cooking... {secondsRemaining:0.0}s"
                : "Cooked meat ready - empty your hands first";
        }

        public void Interact(PlayerController player)
        {
            if (item == null) TryPlaceMeat(player);
            else TryCollectMeat(player);
            RefreshDisplay();
        }

        public void ResetStation()
        {
            if (item != null) Destroy(item.gameObject);
            item = null;
            secondsRemaining = 0f;
            UpdateEffects();
            RefreshDisplay();
        }

        private void AdvanceCooking()
        {
            if (item == null || item.State != PreparationState.Raw) return;
            secondsRemaining = Mathf.Max(0f, secondsRemaining - Time.deltaTime);
            if (secondsRemaining <= 0f) item.SetPrepared();
        }

        private void TryPlaceMeat(PlayerController player)
        {
            if (!IsHoldingRawMeat(player)) return;
            item = player.Inventory.ReleaseTo(itemAnchor, 0.48f);
            secondsRemaining = cookingSeconds;
        }

        private void TryCollectMeat(PlayerController player)
        {
            if (item.State != PreparationState.Prepared || player.Inventory.HasItem) return;
            if (player.Inventory.TryTake(item)) item = null;
        }

        private static bool IsHoldingRawMeat(PlayerController player)
        {
            var held = player.Inventory.HeldItem;
            return held != null && held.Type == IngredientType.Meat && held.State == PreparationState.Raw;
        }

        private void UpdateEffects()
        {
            var stoveIsOn = item != null && item.State == PreparationState.Raw;
            if (flameParticles != null)
            {
                if (stoveIsOn && !flameParticles.isPlaying) flameParticles.Play();
                else if (!stoveIsOn && flameParticles.isPlaying) flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (cookingLight != null)
            {
                cookingLight.enabled = stoveIsOn;
                if (stoveIsOn) cookingLight.intensity = 2.1f + Mathf.Sin(Time.time * 12f) * 0.45f;
            }
        }

        private void RefreshDisplay()
        {
            if (statusText != null)
            {
                statusText.text = item == null ? $"<b>{gameObject.name.ToUpperInvariant()}</b>"
                    : item.State == PreparationState.Prepared ? "<b>STOVE</b>\n<color=#7DFF72>Cooked - ready!</color>"
                    : $"<b>STOVE</b>\nCooking  {secondsRemaining:0.0}s";
            }

            if (progressFill != null)
            {
                progressFill.transform.parent.gameObject.SetActive(item != null && item.State == PreparationState.Raw);
                progressFill.fillAmount = item == null ? 0f
                    : item.State == PreparationState.Prepared ? 1f
                    : 1f - secondsRemaining / cookingSeconds;
            }

            labelFader?.SetSuppressed(item != null);
        }
    }
}
