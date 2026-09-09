#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesChef.Editor
{
    public static class YesChefProjectValidator
    {
        public static void BuildAndValidate()
        {
            YesChefSceneBuilder.BuildPlayableKitchen();
            Validate();
        }

        public static void BuildWindowsPlayer()
        {
            var outputPath = Path.GetFullPath("Builds/Windows/YesChef.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Kitchen.unity" },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
            }
            Debug.Log($"YES_CHEF_WINDOWS_BUILD_COMPLETE: {outputPath} ({report.summary.totalSize} bytes)");
        }

        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity", OpenSceneMode.Single);
            Require(scene.IsValid(), "Kitchen scene could not be opened.");

            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Require(manager != null, "GameManager is missing.");
            Require(manager.player != null, "Player reference is missing.");
            Require(manager.windows != null && manager.windows.Length == 4, "Exactly four customer windows are required.");
            Require(manager.windows.All(window => window.orderText != null && window.scorePopupText != null), "A customer window UI reference is missing.");
            Require(manager.choppingTable != null,
                "Chopping table reference is missing from GameManager.");
            Require(manager.choppingTable.statusText != null,
                $"Chopping table status text is missing. Table object: {manager.choppingTable.name}; child components: " +
                string.Join(", ", manager.choppingTable.GetComponentsInChildren<Component>(true).Select(component => component.GetType().FullName)));
            Require(manager.choppingTable.progressFill != null, "The chopping table progress bar is missing.");
            Require(manager.stove != null && manager.stove.slotAnchors.Length == 2 && manager.stove.statusText != null, "The stove must have two slots and status UI.");
            Require(manager.stove.progressFills.Length == 2 && manager.stove.progressFills.All(fill => fill != null), "Both stove progress bars are required.");
            Require(manager.timerText != null && manager.scoreText != null && manager.highScoreText != null && manager.heldItemText != null, "HUD references are incomplete.");
            Require(manager.instructionsPanel != null && manager.pausePanel != null && manager.resultsPanel != null, "Game-state panels are incomplete.");
            Require(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null,
                "TextMesh Pro essential resources are missing.");
            Require(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(text => text.font != null),
                "Every TextMesh Pro label must have an assigned font asset.");
            Require(OrderScoring.Calculate(new[] { IngredientType.Cheese, IngredientType.Meat }, 14.99f) == 26,
                "The PDF scoring example must produce 26 points.");
            Require(OrderScoring.Calculate(new[] { IngredientType.Cheese, IngredientType.Cheese }, 25.2f) == -5,
                "Orders must be able to produce a negative score.");

            var variations = OrderGenerator.CreateAllRecipeVariations();
            Require(variations.Count == 36, "All 36 ordered two- and three-ingredient recipe variations must be supported.");
            Require(variations.Any(recipe => recipe.Length == 3 && recipe.All(type => type == IngredientType.Meat)),
                "Duplicate ingredient recipes must be supported.");
            foreach (var recipe in variations)
            {
                var ticket = new OrderTicket(recipe);
                foreach (var ingredient in recipe)
                {
                    Require(ticket.TryDeliver(ingredient), "A generated recipe ingredient could not be delivered.");
                }
                Require(ticket.IsComplete, "A generated recipe did not complete after every required delivery.");
            }

            var expectedModels = new[]
            {
                "KitchenFloor", "KitchenWall", "Refrigerator", "ChoppingTable", "Stove", "TrashBin",
                "CustomerWindow", "Chef", "VegetableRaw", "VegetableChopped", "Cheese", "MeatRaw", "MeatCooked"
            };
            foreach (var model in expectedModels)
            {
                Require(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{model}.fbx") != null, $"Model {model} did not import.");
            }

            Debug.Log("YES_CHEF_VALIDATION_COMPLETE: scene, references, models, UI, and scoring rules passed.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
