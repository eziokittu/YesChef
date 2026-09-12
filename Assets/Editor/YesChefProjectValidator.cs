#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
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
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
            }
            Debug.Log($"YES_CHEF_WINDOWS_BUILD_COMPLETE: {outputPath} ({report.summary.totalSize} bytes)");
        }

        public static void BuildWebGLPlayer()
        {
            var outputPath = Path.GetFullPath("Builds/WebGL");
            var archivePath = Path.GetFullPath("Builds/YesChef-WebGL.zip");
            Directory.CreateDirectory(outputPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Kitchen.unity" },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }
            CreatePortableZip(outputPath, archivePath);
            Debug.Log($"YES_CHEF_WEBGL_BUILD_COMPLETE: {outputPath} ({report.summary.totalSize} bytes), itch.io archive: {archivePath}");
        }

        private static void CreatePortableZip(string sourceDirectory, string archivePath)
        {
            if (File.Exists(archivePath)) File.Delete(archivePath);
            var sourceRoot = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            var hasRootIndex = false;
            foreach (var filePath in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var entryName = filePath.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
                if (entryName.Contains('\\'))
                    throw new InvalidOperationException("The itch.io WebGL archive contains non-portable Windows path separators.");
                if (entryName == "index.html") hasRootIndex = true;
                archive.CreateEntryFromFile(filePath, entryName, System.IO.Compression.CompressionLevel.Optimal);
            }

            if (!hasRootIndex)
                throw new InvalidOperationException("The itch.io WebGL archive must contain index.html at its root.");
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
            var characterMotions = UnityEngine.Object.FindObjectsByType<CharacterIdleMotion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(characterMotions.Length >= 5 && characterMotions.All(motion =>
                    motion.GetComponentsInChildren<Transform>(true).Count(part => part.name.StartsWith("ArmPivot_")) == 2 &&
                    motion.GetComponentsInChildren<Transform>(true).Count(part => part.name.StartsWith("LegPivot_")) == 2),
                "The chef and all four customers need animation roots with two shoulder and two hip pivots.");
            Require(manager.player.characterMotion != null,
                "The chef needs an assigned motion controller for idle, walking, and fridge-reaching animation.");
            Require(manager.choppingTable != null,
                "Chopping table reference is missing from GameManager.");
            Require(manager.choppingTable.statusText != null,
                $"Chopping table status text is missing. Table object: {manager.choppingTable.name}; child components: " +
                string.Join(", ", manager.choppingTable.GetComponentsInChildren<Component>(true).Select(component => component.GetType().FullName)));
            Require(manager.choppingTable.progressFill != null, "The chopping table progress bar is missing.");
            Require(manager.stoves != null && manager.stoves.Length == 2, "Exactly two separate stove stations are required.");
            Require(manager.stoves.All(stove => stove.itemAnchor != null && stove.progressFill != null && stove.flameParticles != null && stove.cookingLight != null),
                "A stove is missing its cooking anchor, progress UI, flame particles, or pulsing light.");
            Require(manager.stoves.All(stove => stove.labelFader != null),
                "Every stove label needs the same proximity fading used by the other stations.");
            Require(manager.stoves.All(stove => stove.labelFader.nearbyAlpha <= .05f),
                "Stove labels must become nearly invisible beside the chef.");
            Require(manager.stoves.All(stove => stove.flameParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial != null &&
                                                stove.flameParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name != "Hidden/InternalErrorShader"),
                "Every stove flame needs a valid particle material.");
            Require(manager.timerText != null && manager.scoreText != null && manager.highScoreText != null && manager.heldItemText != null && manager.heldItemColor != null, "HUD references are incomplete.");
            Require(manager.controlsStrip != null && manager.fridgeMenu != null, "Controls strip or fridge catalogue is missing.");
            Require(manager.fridgeMenu.refrigerator.animator != null && manager.fridgeMenu.refrigerator.animator.doorHinge != null &&
                    manager.fridgeMenu.refrigerator.animator.interiorLight != null,
                "The refrigerator needs an authored door hinge and fading interior light.");
            Require(Mathf.Approximately(manager.fridgeMenu.refrigerator.pickupReachSeconds, .6f) &&
                    Mathf.Approximately(manager.fridgeMenu.refrigerator.animator.animationSpeed, 4.8f),
                "The refrigerator door and pickup reach must run at the 1.5x timing.");
            Require(manager.instructionsPanel != null && manager.pausePanel != null && manager.quitConfirmationPanel != null &&
                    manager.resultsPanel != null && manager.pauseDetailsText != null, "Game-state panels are incomplete.");
            Require(UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>() != null, "The Main Camera needs a Cinemachine Brain.");
            var audioListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(audioListeners.Length == 1 && audioListeners[0].gameObject == Camera.main.gameObject && audioListeners[0].enabled,
                "Exactly one enabled AudioListener must be attached to the Main Camera.");
            var virtualCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineVirtualCamera>();
            Require(virtualCamera != null && virtualCamera.Follow == manager.player.transform && Camera.main != null && !Camera.main.orthographic,
                "A perspective Cinemachine camera must follow the player.");
            Require(manager.adaptiveCamera != null, "The idle/movement Cinemachine zoom controller is missing.");
            Require(Mathf.Approximately(Camera.main.fieldOfView, 36f) &&
                    Mathf.Approximately(virtualCamera.m_Lens.FieldOfView, 36f) &&
                    Mathf.Approximately(manager.adaptiveCamera.movingFieldOfView, 36f) &&
                    Mathf.Approximately(manager.adaptiveCamera.idleFieldOfView, 28f) &&
                    Mathf.Approximately(manager.adaptiveCamera.idleRevealFieldOfView, 28f),
                "The camera must use a 36-degree gameplay FOV and a 28-degree idle FOV.");
            Require(manager.adaptiveCamera.idleDelay >= 3f &&
                    manager.adaptiveCamera.marshRevealScreenX < .5f && manager.adaptiveCamera.customerRevealScreenX > .5f &&
                    manager.adaptiveCamera.gardenRevealScreenY > .5f,
                "Idle camera framing must reveal the marsh, garden, and customer road edges.");
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
            var audio = UnityEngine.Object.FindFirstObjectByType<AudioDirector>(FindObjectsInactive.Include);
            Require(audio.musicTracks?.Length == 2 && audio.musicTracks.All(clip => clip != null && clip.length >= 30f) &&
                    new[] { audio.chopping, audio.cooking, audio.fridgeOpen, audio.fridgeClose, audio.foodPrepared,
                            audio.newOrder, audio.orderItemReceived, audio.customerArrival, audio.customerHappy,
                            audio.trash }.All(clip => clip != null),
                "Both calm music tracks and all kitchen/customer sound effects must be assigned.");
            Require(audio.musicSource != null && audio.sfxSource != null && audio.musicVolume >= .7f &&
                    audio.sfxVolume < audio.musicVolume && audio.sfxVolume <= .25f &&
                    audio.musicSource.spatialBlend == 0f && audio.sfxSource.spatialBlend == 0f,
                "Music must be clearly audible and every kitchen sound effect must remain quieter than the music bed.");
            Require(UnityEngine.Object.FindObjectsByType<AmbientAudioZone>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .All(zone => zone.continuousLoops?.Length == 1 &&
                                 zone.continuousRelativeVolumes?.Length == zone.continuousLoops.Length &&
                                 zone.randomOneShots?.Length >= 2 &&
                                 zone.randomRelativeVolumes?.Length == zone.randomOneShots.Length &&
                                 zone.minimumIntervals?.Length == zone.randomOneShots.Length &&
                                 zone.maximumIntervals?.Length == zone.randomOneShots.Length &&
                                 zone.maximumVolume < audio.musicVolume &&
                                 zone.continuousLoops.All(source => source != null && source.clip != null && source.loop) &&
                                 zone.randomOneShots.All(source => source != null && source.clip != null && !source.loop)),
                "Exterior ambience needs one soft loop plus independently scheduled wildlife details per zone.");
            Require(UnityEngine.Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 2,
                "Pause UI needs music and sound-effect toggles.");
            var mobileInput = UnityEngine.Object.FindFirstObjectByType<MobileInputController>(FindObjectsInactive.Include);
            Require(mobileInput != null && manager.player.mobileInput == mobileInput && mobileInput.controlsRoot != null &&
                    mobileInput.visibilityToggle != null && mobileInput.visibilityToggle.isOn &&
                    mobileInput.controlsRoot.GetComponentsInChildren<TouchControlButton>(true)
                        .Count(control => control.isAction) == 1 &&
                    mobileInput.controlsRoot.GetComponentsInChildren<TouchControlButton>(true)
                        .All(control => control.isAction),
                "WebGL needs one action button and an enabled-by-default pause toggle.");
            var circularController = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(image => image.name == "Circular Glass Controller");
            var joystick = circularController != null ? circularController.GetComponent<VirtualJoystick>() : null;
            var screenControlsRect = mobileInput?.controlsRoot?.GetComponent<RectTransform>();
            Require(circularController != null && circularController.sprite != null && circularController.raycastTarget &&
                    joystick != null && joystick.input == mobileInput && joystick.handle != null &&
                    joystick.handle.name == "Controller Hub" && joystick.movementRadius >= 75f &&
                    joystick.deadZone >= .05f && joystick.deadZone <= .25f &&
                    UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Count(item => item.name == "Chevron Stroke") == 8 &&
                    screenControlsRect != null && screenControlsRect.anchoredPosition.y >= 120f,
                "Screen movement needs one raised drag joystick with a returning hub and four geometry-based chevrons.");
            Require(UnityEngine.Object.FindObjectsByType<WindSway>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 15 &&
                    UnityEngine.Object.FindObjectsByType<WaterSurfaceAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 50,
                "Garden wind motion and faceted water movement are incomplete.");
            Require(UnityEngine.Object.FindObjectsByType<WaterSurfaceAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .All(water => water.waveHeight >= .1f && water.tiltAngle > 1f && water.colorShift > 0f),
                "Water facets need visible vertical waves and gentle surface tilt.");
            Require(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Count(item => item.name == "Tall Grass Blade") >= 100 &&
                    UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Count(item => item.name == "Patchy Garden Ground") >= 6 &&
                    UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Count(item => item.name == "Low Poly Lotus Leaf") >= 10 &&
                    GameObject.Find("Fallen Pond Edge Log") != null &&
                    GameObject.Find("Fallen Pond Edge Log").transform.position.x < 8f,
                "Patchy grass, tall blades, extra round pond leaves, or the edge log are missing.");
            Require(UnityEngine.Object.FindObjectsByType<ButterflyFlight>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<FrogHopper>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<WildlifeMover>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "Fish, snake, frog, and butterfly visuals must be completely absent while their audio slots remain available.");
            var activityEffects = UnityEngine.Object.FindFirstObjectByType<KitchenActivityEffects>(FindObjectsInactive.Include);
            Require(activityEffects != null && UnityEngine.Object.FindFirstObjectByType<TrashLidAnimator>(FindObjectsInactive.Include) != null,
                "Optimized kitchen mess, insects, or trash-lid mechanics are missing.");
            Require(activityEffects.trashAnts.main.loop && activityEffects.trashFlies.main.loop &&
                    activityEffects.trashFlyAudio != null && activityEffects.trashFlyAudio.clip != null &&
                    activityEffects.trashFlyAudio.loop && !activityEffects.trashFlyAudio.playOnAwake &&
                    activityEffects.trashFlyVolume < audio.musicVolume,
                "Trash ants, flies, and proximity buzz must persist after the first discard until a reset.");
            Require(new[] { activityEffects.cheeseCrumbs, activityEffects.meatCrumbs, activityEffects.choppingSpill,
                            activityEffects.meatSpill, activityEffects.trashAnts, activityEffects.trashFlies }
                    .All(particles => particles != null && !particles.main.playOnAwake),
                "Kitchen mess and visitor particles must stay hidden until their gameplay event.");
            Require(GameObject.Find("Floor Border North") != null &&
                    UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Count(item => item.name == "Symmetric Floor Tile") == 63 &&
                    GameObject.Find("North Wall Wainscot") != null && GameObject.Find("Decorative Wall Diamond") != null,
                "The symmetric tiled floor and decorative wall treatment are missing.");
            var trashAnimator = UnityEngine.Object.FindFirstObjectByType<TrashLidAnimator>(FindObjectsInactive.Include);
            Require(trashAnimator != null && trashAnimator.lid != null && trashAnimator.openAngle < 0f &&
                    trashAnimator.lid.position.x > trashAnimator.transform.position.x,
                "The rotated trash lid must open upward from its right-side hinge.");
            var dialogueWindows = UnityEngine.Object.FindObjectsByType<CustomerWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(UnityEngine.Object.FindObjectsByType<DialogueBubbleAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 4 &&
                    !UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Any(item => item.name == "Cloud Puff") &&
                    dialogueWindows.All(window => window.dialogueAnimator != null &&
                                       window.dialogueLayout != null && !window.dialogueText.enableAutoSizing &&
                                       window.dialogueLayout.minimumFontSize <= 18f &&
                                       window.dialogueLayout.extraLongSize.y >= 330f &&
                                       window.dialogueBubble.transform.localPosition.y < 3f &&
                                       window.dialogueText.rectTransform.parent.GetComponent<RectTransform>().sizeDelta.y >= 225f &&
                                       window.dialogueText.transform.parent.GetComponentInChildren<Image>().preserveAspect),
                "Each customer needs one lowered, dynamically sized, aspect-preserved speech bubble and bounce animator.");
            if (dialogueWindows.Length > 0)
            {
                var layout = dialogueWindows[0].dialogueLayout;
                layout.Configure("Thanks!");
                var shortCloud = ((RectTransform)layout.transform).sizeDelta;
                var shortFont = layout.dialogueText.fontSize;
                const string fitProbe = "Could I please have a freshly prepared vegetable burger with cheese, and make sure every step is finished before serving it to me?";
                layout.Configure(fitProbe);
                var longCloud = ((RectTransform)layout.transform).sizeDelta;
                var longFont = layout.dialogueText.fontSize;
                var available = new Vector2(longCloud.x - 112f, longCloud.y - 100f);
                var preferred = layout.dialogueText.GetPreferredValues(fitProbe, available.x, Mathf.Infinity);
                Require(longCloud.x > shortCloud.x && longCloud.y > shortCloud.y && longFont < shortFont &&
                        preferred.x <= available.x + 1f && preferred.y <= available.y + 1f,
                    "Long dialogue must grow its cloud and shrink its text until the complete message fits inside the safe area.");
                layout.Configure("Welcome!");
            }
            var pauseCredits = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == "Glitchbong Contact Link" && item.parent.name == "Pause Overlay");
            Require(pauseCredits != null && pauseCredits.anchoredPosition.y >= 170f,
                "Pause credits must sit above the persistent controls strip.");
            var quitCredits = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == "Glitchbong Contact Link" && item.parent.name == "Quit Confirmation Overlay");
            Require(quitCredits != null && quitCredits.anchoredPosition.y >= 170f,
                "Quit-confirmation credits must sit above the persistent controls strip.");
            Require(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(light => light.type == LightType.Spot) >= 8,
                "Four daylight and four night window light cones are required.");
            Require(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null,
                "TextMesh Pro essential resources are missing.");
            var menuTitles = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(text => text.text != "FRIDGE"
                    ? new[] { "YES CHEF!", "PAUSED", "ONE MORE ORDER?", "SERVICE OVER!" }.Contains(text.text)
                    : text.transform.parent.name == "Fridge Catalogue")
                .ToArray();
            Require(menuTitles.Length == 5 && menuTitles.All(text => (text.fontStyle & FontStyles.Bold) != 0 && text.outlineWidth > .1f),
                "Every main menu title needs the original bold, outlined cover-style treatment.");
            Require(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GlitchbongLogo.png") != null,
                "The Glitchbong credit logo is missing or is not imported as a sprite.");
            Require(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GitHub_Invertocat_White.png") != null &&
                    GameObject.Find("GitHub Invertocat")?.GetComponent<Image>()?.sprite != null,
                "Every credit menu needs the official GitHub Invertocat sprite instead of placeholder letters.");
            var cover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/YesChefCover.png");
            var startupSplash = UnityEngine.Object.FindFirstObjectByType<StartupSplashController>(FindObjectsInactive.Include);
            Require(cover != null && startupSplash != null && startupSplash.splashRoot != null &&
                    Mathf.Approximately(startupSplash.displaySeconds, 2f) &&
                    startupSplash.splashRoot.GetComponentsInChildren<Image>(true).Any(image => image.sprite == cover),
                "The supplied cover artwork must fill a two-second startup splash before the instruction screen.");
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
                "Tree", "Flower", "Lotus", "Bush"
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
