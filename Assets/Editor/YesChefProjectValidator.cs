#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
            var chefRenderers = manager.player.GetComponentsInChildren<Renderer>(true);
            Require(chefRenderers.Length > 0, "The chef model has no renderers.");
            var chefBounds = chefRenderers[0].bounds;
            foreach (var renderer in chefRenderers.Skip(1)) chefBounds.Encapsulate(renderer.bounds);
            Require(chefBounds.size.y > 1.5f && chefBounds.size.y > chefBounds.size.z,
                "The Blender chef must be upright on Unity's Y axis for the top-down camera.");
            Require(GameObject.Find("Kitchen Floor Collider")?.GetComponent<BoxCollider>() != null,
                "The chef needs a solid kitchen floor collider.");
            Require(manager.windows != null && manager.windows.Length == 4, "Exactly four customer windows are required.");
            Require(manager.windows.All(window => window.orderText != null && window.dialogueText != null && window.scorePopupText != null), "A customer table UI reference is missing.");
            Require(manager.windows.All(window => window.avatar != null && window.customerRoot != null && window.spawnPoint != null && window.servicePoint != null), "Customer models or walking routes are incomplete.");
            Require(UnityEngine.Object.FindObjectsByType<CharacterIdleMotion>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 5,
                "The chef and all four customers need subtle idle motion.");
            Require(manager.choppingTable != null,
                "Chopping table reference is missing from GameManager.");
            Require(manager.choppingTable.statusText != null,
                $"Chopping table status text is missing. Table object: {manager.choppingTable.name}; child components: " +
                string.Join(", ", manager.choppingTable.GetComponentsInChildren<Component>(true).Select(component => component.GetType().FullName)));
            Require(manager.choppingTable.progressFill != null, "The chopping table progress bar is missing.");
            Require(manager.stoves != null && manager.stoves.Length == 2, "Exactly two separate stove stations are required.");
            Require(manager.stoves.All(stove => stove.itemAnchor != null && stove.progressFill != null && stove.flameParticles != null && stove.cookingLight != null),
                "A stove is missing its cooking anchor, progress UI, flame particles, or pulsing light.");
            Require(manager.stoves.All(stove => stove.flameParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial != null &&
                                                stove.flameParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name != "Hidden/InternalErrorShader"),
                "Every stove flame needs a valid particle material.");
            Require(manager.timerText != null && manager.scoreText != null && manager.highScoreText != null && manager.heldItemText != null && manager.heldItemColor != null, "HUD references are incomplete.");
            Require(manager.controlsStrip != null && manager.fridgeMenu != null, "Controls strip or fridge catalogue is missing.");
            Require(manager.fridgeMenu.refrigerator.animator != null && manager.fridgeMenu.refrigerator.animator.doorHinge != null &&
                    manager.fridgeMenu.refrigerator.animator.interiorLight != null,
                "The refrigerator needs an authored door hinge and fading interior light.");
            Require(manager.instructionsPanel != null && manager.pausePanel != null && manager.quitConfirmationPanel != null &&
                    manager.resultsPanel != null && manager.pauseDetailsText != null, "Game-state panels are incomplete.");
            Require(UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>() != null, "The Main Camera needs a Cinemachine Brain.");
            var virtualCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineVirtualCamera>();
            Require(virtualCamera != null && virtualCamera.Follow == manager.player.transform && Camera.main != null && !Camera.main.orthographic,
                "A perspective Cinemachine camera must follow the player.");
            Require(manager.adaptiveCamera != null, "The idle/movement Cinemachine zoom controller is missing.");
            Require(manager.adaptiveCamera.idleDelay >= 3f && manager.adaptiveCamera.zoomSmoothTime >= 1.5f,
                "The adaptive camera must wait three seconds and zoom gradually.");
            Require(manager.adaptiveCamera.idleRevealFieldOfView > manager.adaptiveCamera.movingFieldOfView &&
                    manager.adaptiveCamera.marshRevealScreenX < .5f && manager.adaptiveCamera.customerRevealScreenX > .5f &&
                    manager.adaptiveCamera.gardenRevealScreenY > .5f,
                "Idle camera reveals must frame the marsh, garden, and customer road edges.");
            Require(UnityEngine.Object.FindObjectsByType<WorldLabelFader>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 5,
                "Kitchen station labels need proximity fading.");
            Require(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(light => light.type == LightType.Point) >= 8,
                "Room point lights and stove lights are required.");
            Require(manager.dayNightCycle != null && manager.dayNightCycle.globalMorningLight != null &&
                    manager.dayNightCycle.kitchenLights?.Length == 6 && manager.dayNightCycle.exteriorWindowLights?.Length == 4 &&
                    manager.dayNightCycle.interiorWindowSpillLights?.Length == 4,
                "The day/night controller must own the global morning, kitchen, and eight independent window lights.");
            manager.dayNightCycle.ApplyLighting(0f);
            Require(manager.dayNightCycle.DaylightAmount > .99f && manager.dayNightCycle.globalMorningLight.intensity > 1f &&
                    manager.dayNightCycle.exteriorWindowLights.All(light => light.intensity > 2f) &&
                    manager.dayNightCycle.interiorWindowSpillLights.All(light => light.intensity < .01f),
                "The match must begin in bright morning light.");
            manager.dayNightCycle.ApplyLighting(80f);
            Require(manager.dayNightCycle.DaylightAmount < .01f && manager.dayNightCycle.globalMorningLight.intensity < .01f &&
                    manager.dayNightCycle.exteriorWindowLights.All(light => light.intensity < .01f) &&
                    manager.dayNightCycle.interiorWindowSpillLights.All(light => light.intensity > 1f) &&
                    manager.dayNightCycle.kitchenLights.All(light => light.intensity > 1.5f),
                "Night must disable exterior light and fully switch on the kitchen fixtures.");
            manager.dayNightCycle.ApplyLighting(120f);
            Require(manager.dayNightCycle.DaylightAmount > .45f && manager.dayNightCycle.DaylightAmount < .55f,
                "Sunrise must be halfway complete at 120 seconds.");
            manager.dayNightCycle.ResetCycle();
            Require(UnityEngine.Object.FindFirstObjectByType<ScrollRect>(FindObjectsInactive.Include) != null,
                "The fridge ingredient list must use a ScrollRect.");
            Require(UnityEngine.Object.FindFirstObjectByType<AudioDirector>(FindObjectsInactive.Include) != null &&
                    UnityEngine.Object.FindObjectsByType<AmbientAudioZone>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 2,
                "Persistent music/SFX routing and both exterior ambience zones are required.");
            Require(UnityEngine.Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 2,
                "Pause UI needs music and sound-effect toggles.");
            Require(UnityEngine.Object.FindObjectsByType<WindSway>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 15 &&
                    UnityEngine.Object.FindObjectsByType<WaterSurfaceAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 50,
                "Garden wind motion and faceted water movement are incomplete.");
            Require(UnityEngine.Object.FindObjectsByType<WaterSurfaceAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .All(water => water.waveHeight >= .08f && water.tiltAngle > 0f),
                "Water facets need visible vertical waves and gentle surface tilt.");
            Require(UnityEngine.Object.FindObjectsByType<ButterflyFlight>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<FrogHopper>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<WildlifeMover>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "Fish, snake, frog, and butterfly visuals must be completely absent while their audio slots remain available.");
            var activityEffects = UnityEngine.Object.FindFirstObjectByType<KitchenActivityEffects>(FindObjectsInactive.Include);
            Require(activityEffects != null && UnityEngine.Object.FindFirstObjectByType<TrashLidAnimator>(FindObjectsInactive.Include) != null,
                "Optimized kitchen mess, rat, insects, or trash-lid mechanics are missing.");
            Require(activityEffects.rat != null && activityEffects.ratEntrances?.Length == 4 && activityEffects.ratFood != null,
                "The cheese-seeking rat must be able to enter from all four kitchen corners.");
            Require(new[] { activityEffects.cheeseCrumbs, activityEffects.meatCrumbs, activityEffects.choppingSpill,
                            activityEffects.meatSpill, activityEffects.trashAnts, activityEffects.trashFlies }
                    .All(particles => particles != null && !particles.main.playOnAwake),
                "Kitchen mess and visitor particles must stay hidden until their gameplay event.");
            Require(GameObject.Find("Floor Border North") != null &&
                    UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Count(item => item.name == "Symmetric Floor Tile") == 63 &&
                    GameObject.Find("North Wall Wainscot") != null && GameObject.Find("Decorative Wall Diamond") != null,
                "The symmetric tiled floor and decorative wall treatment are missing.");
            Require(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(light => light.type == LightType.Spot) >= 8,
                "Four daylight and four night window light cones are required.");
            Require(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null,
                "TextMesh Pro essential resources are missing.");
            Require(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GlitchbongLogo.png") != null,
                "The Glitchbong credit logo is missing or is not imported as a sprite.");
            Require(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GitHub_Invertocat_White.png") != null &&
                    GameObject.Find("GitHub Invertocat")?.GetComponent<Image>()?.sprite != null,
                "Every credit menu needs the official GitHub Invertocat sprite instead of placeholder letters.");
            Require(!UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(text => text.text == "GH"),
                "The GH text placeholder must not remain in the UI.");
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
                "Refrigerator", "ChoppingTable", "SingleStove", "TrashBin", "Chef", "Customer",
                "VegetableRaw", "VegetableChopped", "Cheese", "MeatRaw", "MeatCooked",
                "Tree", "Flower", "Lotus", "Rat", "Bush"
            };
            foreach (var model in expectedModels)
            {
                Require(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{model}.fbx") != null, $"Model {model} did not import.");
            }

            Debug.Log("YES_CHEF_VALIDATION_COMPLETE: perspective Cinemachine, customers, environment, effects, models, UI, and gameplay rules passed.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
