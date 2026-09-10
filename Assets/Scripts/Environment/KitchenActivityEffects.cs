using System.Collections;
using UnityEngine;

namespace YesChef
{
    /// <summary>Small pooled mess and insect effects; no per-frame spawning or unbounded debris.</summary>
    public sealed class KitchenActivityEffects : MonoBehaviour
    {
        public static KitchenActivityEffects Instance { get; private set; }
        public ParticleSystem cheeseCrumbs;
        public ParticleSystem meatCrumbs;
        public ParticleSystem choppingSpill;
        public ParticleSystem meatSpill;
        public ParticleSystem trashAnts;
        public ParticleSystem trashFlies;
        public AudioSource trashFlyAudio;
        [Range(0f, 1f)] public float trashFlyVolume = .07f;
        private int activeCookingStations;
        private bool trashVisitorsScheduled;
        private void Awake()
        {
            Instance = this;
            ResetEffects();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (trashFlyAudio != null)
                trashFlyAudio.volume = AudioDirector.Instance != null && AudioDirector.Instance.SfxEnabled ? trashFlyVolume : 0f;
        }

        public void OnFridgeTake(IngredientType type)
        {
            if (type != IngredientType.Cheese && type != IngredientType.Meat) return;
            if (Random.value > 0.5f) return;
            if (type == IngredientType.Cheese)
                cheeseCrumbs?.Play();
            else meatCrumbs?.Play();
        }
        public void OnTrashDiscard()
        {
            if (trashVisitorsScheduled) return;
            trashVisitorsScheduled = true;
            StartCoroutine(DelayedTrashVisitors());
        }
        public void OnChoppingStarted() => choppingSpill?.Play();
        public void OnChoppingFinished() => StopGradually(choppingSpill);
        public void OnCookingStarted()
        {
            activeCookingStations++;
            if (activeCookingStations == 1) meatSpill?.Play();
        }
        public void OnCookingFinished()
        {
            activeCookingStations = Mathf.Max(0, activeCookingStations - 1);
            if (activeCookingStations == 0) StopGradually(meatSpill);
        }

        public void ResetEffects()
        {
            StopAllCoroutines();
            activeCookingStations = 0;
            trashVisitorsScheduled = false;
            StopAndClear(cheeseCrumbs);
            StopAndClear(meatCrumbs);
            StopAndClear(choppingSpill);
            StopAndClear(meatSpill);
            StopAndClear(trashAnts);
            StopAndClear(trashFlies);
            if (trashFlyAudio != null) trashFlyAudio.Stop();
        }

        private IEnumerator DelayedTrashVisitors()
        {
            yield return new WaitForSeconds(Random.Range(5f, 6f));
            trashAnts?.Play(); trashFlies?.Play();
            if (trashFlyAudio != null && trashFlyAudio.clip != null) trashFlyAudio.Play();
        }
        private void StopGradually(ParticleSystem particles)
        {
            if (particles == null) return;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }


        private static void StopAndClear(ParticleSystem particles)
        {
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
