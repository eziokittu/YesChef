using System.Collections;
using UnityEngine;

namespace YesChef
{
    /// <summary>Small pooled mess/visitor effects; no per-frame spawning or unbounded debris.</summary>
    public sealed class KitchenActivityEffects : MonoBehaviour
    {
        public static KitchenActivityEffects Instance { get; private set; }
        public ParticleSystem cheeseCrumbs;
        public ParticleSystem meatCrumbs;
        public ParticleSystem choppingSpill;
        public ParticleSystem meatSpill;
        public ParticleSystem trashAnts;
        public ParticleSystem trashFlies;
        public GameObject rat;
        public Transform[] ratEntrances;
        public Transform ratFood;
        private Coroutine ratVisit;
        private int activeCookingStations;
        private bool trashVisitorsScheduled;
        public int LastRatEntranceIndex { get; private set; } = -1;
        private void Awake()
        {
            Instance = this;
            ResetEffects();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OnFridgeTake(IngredientType type)
        {
            if (type != IngredientType.Cheese && type != IngredientType.Meat) return;
            if (Random.value > 0.5f) return;
            if (type == IngredientType.Cheese)
            {
                cheeseCrumbs?.Play();
                if (ratVisit != null) StopCoroutine(ratVisit);
                ratVisit = StartCoroutine(RatVisit());
            }
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
            ratVisit = null;
            activeCookingStations = 0;
            trashVisitorsScheduled = false;
            LastRatEntranceIndex = -1;
            StopAndClear(cheeseCrumbs);
            StopAndClear(meatCrumbs);
            StopAndClear(choppingSpill);
            StopAndClear(meatSpill);
            StopAndClear(trashAnts);
            StopAndClear(trashFlies);
            if (rat != null) rat.SetActive(false);
        }

        private IEnumerator DelayedTrashVisitors()
        {
            yield return new WaitForSeconds(Random.Range(5f, 6f));
            trashAnts?.Play(); trashFlies?.Play();
        }
        private IEnumerator RatVisit()
        {
            yield return new WaitForSeconds(Random.Range(2f, 3f));
            if (rat == null || ratEntrances == null || ratEntrances.Length == 0 || ratFood == null) yield break;
            LastRatEntranceIndex = Random.Range(0, ratEntrances.Length);
            var entrance = ratEntrances[LastRatEntranceIndex];
            if (entrance == null) yield break;
            rat.transform.position = entrance.position;
            rat.SetActive(true);
            yield return MoveRat(ratFood.position, 1.5f);
            yield return new WaitForSeconds(1.4f);
            yield return MoveRat(entrance.position, 1.8f);
            rat.SetActive(false);
        }
        private IEnumerator MoveRat(Vector3 target, float speed)
        {
            while (Vector3.Distance(rat.transform.position, target) > 0.03f)
            {
                var direction = target - rat.transform.position; direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f) rat.transform.rotation = Quaternion.LookRotation(direction);
                rat.transform.position = Vector3.MoveTowards(rat.transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
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
