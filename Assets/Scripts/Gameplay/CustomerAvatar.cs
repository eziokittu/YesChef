using System;
using System.Linq;
using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Applies a random name, clothing palette, skin tone and hairstyle to the
    /// reusable low-poly customer model whenever a new customer arrives.
    /// </summary>
    public sealed class CustomerAvatar : MonoBehaviour
    {
        private static readonly string[] Names =
        {
            "Aarav", "Aditi", "Akash", "Ananya", "Arjun", "Diya", "Ishaan", "Kabir", "Kavya", "Meera",
            "Neha", "Nikhil", "Priya", "Rahul", "Riya", "Rohan", "Saanvi", "Tara", "Vihaan", "Zoya",
            "Amelia", "Benjamin", "Charlotte", "Daniel", "Eleanor", "Ethan", "Eva", "Grace", "Henry", "Isla",
            "Jack", "James", "Leo", "Lily", "Lucas", "Maya", "Mia", "Noah", "Oliver", "Olivia",
            "Oscar", "Ruby", "Samuel", "Sofia", "Theo", "Thomas", "Victoria", "William", "Zachary", "Zoe"
        };

        private static readonly Color[] ShirtColors =
        {
            new(0.12f, 0.45f, 0.80f), new(0.82f, 0.20f, 0.24f), new(0.22f, 0.65f, 0.32f),
            new(0.66f, 0.28f, 0.76f), new(0.95f, 0.55f, 0.12f), new(0.12f, 0.70f, 0.72f)
        };

        private static readonly Color[] SkinColors =
        {
            new(0.96f, 0.78f, 0.62f), new(0.82f, 0.57f, 0.39f), new(0.62f, 0.38f, 0.24f), new(0.38f, 0.22f, 0.14f)
        };

        private static readonly Color[] HairColors =
        {
            new(0.07f, 0.045f, 0.03f), new(0.20f, 0.09f, 0.04f), new(0.48f, 0.28f, 0.10f), new(0.72f, 0.58f, 0.30f)
        };

        public string CustomerName { get; private set; }

        public void Randomize()
        {
            CustomerName = Names[UnityEngine.Random.Range(0, Names.Length)];
            TintParts("Shirt", ShirtColors[UnityEngine.Random.Range(0, ShirtColors.Length)]);
            TintParts("Skin", SkinColors[UnityEngine.Random.Range(0, SkinColors.Length)]);
            TintParts("Hair", HairColors[UnityEngine.Random.Range(0, HairColors.Length)]);

            var hairstyles = GetComponentsInChildren<Transform>(true)
                .Where(child => child.name.StartsWith("HairStyle", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hairstyles.Length == 0) return;
            var selected = UnityEngine.Random.Range(0, hairstyles.Length);
            for (var index = 0; index < hairstyles.Length; index++) hairstyles[index].gameObject.SetActive(index == selected);
        }

        private void TintParts(string nameFragment, Color color)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.gameObject.name.Contains(nameFragment, StringComparison.OrdinalIgnoreCase)) continue;
                renderer.material.color = color;
            }
        }
    }
}
