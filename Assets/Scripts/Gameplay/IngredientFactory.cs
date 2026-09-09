using UnityEngine;

namespace YesChef
{
    public sealed class IngredientFactory : MonoBehaviour
    {
        public GameObject vegetableRawPrefab;
        public GameObject vegetablePreparedPrefab;
        public GameObject cheesePrefab;
        public GameObject meatRawPrefab;
        public GameObject meatPreparedPrefab;

        public IngredientItem Create(IngredientType type, PreparationState state, Transform parent)
        {
            var root = new GameObject($"{state} {type}");
            var item = root.AddComponent<IngredientItem>();
            item.Initialize(this, type, state);
            item.PlaceAt(parent);
            return item;
        }

        public GameObject CreateVisual(IngredientType type, PreparationState state, Transform parent)
        {
            var prefab = type switch
            {
                IngredientType.Vegetable when state == PreparationState.Prepared => vegetablePreparedPrefab,
                IngredientType.Vegetable => vegetableRawPrefab,
                IngredientType.Cheese => cheesePrefab,
                IngredientType.Meat when state == PreparationState.Prepared => meatPreparedPrefab,
                IngredientType.Meat => meatRawPrefab,
                _ => null
            };

            if (prefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = "MissingIngredientVisual";
                fallback.transform.SetParent(parent, false);
                fallback.transform.localScale = Vector3.one * 0.5f;
                return fallback;
            }

            var instance = Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
            return instance;
        }
    }
}
