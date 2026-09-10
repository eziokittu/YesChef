#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesChef.Editor
{
    /// <summary>
    /// Builds the editable demonstration scene. This runs only in the editor;
    /// the shipped game uses normal serialized references and never rebuilds itself.
    /// </summary>
    public static class YesChefSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Kitchen.unity";

        private static readonly Color Cream = new(0.96f, 0.91f, 0.78f, 1f);
        private static readonly Color Ink = new(0.07f, 0.09f, 0.11f, 1f);
        private static readonly Color Tomato = new(0.90f, 0.20f, 0.15f, 1f);
        private static readonly Color Panel = new(0.045f, 0.06f, 0.075f, 0.96f);
        private static TMP_FontAsset defaultFont;

        [MenuItem("Tools/Yes Chef/Build Top-Down World")]
        public static void BuildPlayableKitchen()
        {
            EnsureTextMeshProResources();
            ConfigureBrandTexture();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var models = LoadModels();

            CreateEnvironment(models);
            var dayNightCycle = CreateRoomLighting();

            var gameRoot = new GameObject("GAMEPLAY");
            gameRoot.AddComponent<RuntimeVerifier>();
            var factory = CreateIngredientFactory(gameRoot, models);
            var player = CreatePlayer(models["Chef"]);
            var refrigerator = CreateRefrigerator(models["Refrigerator"], factory);
            var table = CreateChoppingTable(models["ChoppingTable"]);
            var stoves = new[]
            {
                CreateStove("Stove A", new Vector3(0.25f, 0f, -0.75f), models),
                CreateStove("Stove B", new Vector3(3.35f, 0f, -0.75f), models)
            };
            var trash = CreateTrash(models["TrashBin"]);
            var windows = CreateCustomerTables(models);
            var audio = CreateAudioDirector();
            CreateAmbientAudio(player);
            CreateKitchenActivityEffects(models, refrigerator, table, stoves, trash);

            refrigerator.transform.SetParent(gameRoot.transform);
            table.transform.SetParent(gameRoot.transform);
            foreach (var stove in stoves) stove.transform.SetParent(gameRoot.transform);
            trash.transform.SetParent(gameRoot.transform);
            foreach (var window in windows) window.transform.SetParent(gameRoot.transform);

            var camera = CreateCinemachineCamera(player);
            var ui = CreateScreenUi(refrigerator, player, audio);

            var manager = gameRoot.AddComponent<GameManager>();
            manager.player = player;
            manager.windows = windows.ToArray();
            manager.choppingTable = table;
            manager.stoves = stoves;
            manager.fridgeMenu = ui.fridgeMenu;
            manager.adaptiveCamera = camera.controller;
            manager.dayNightCycle = dayNightCycle;
            manager.timerText = ui.timer;
            manager.scoreText = ui.score;
            manager.highScoreText = ui.highScore;
            manager.heldItemText = ui.heldItem;
            manager.heldItemColor = ui.heldColor;
            manager.interactionText = ui.prompt;
            manager.controlsStrip = ui.controlsStrip;
            manager.instructionsPanel = ui.instructions;
            manager.pausePanel = ui.pause;
            manager.quitConfirmationPanel = ui.quitConfirmation;
            manager.resultsPanel = ui.results;
            manager.resultScoreText = ui.resultScore;
            manager.newHighScoreText = ui.newHighScore;
            manager.pauseDetailsText = ui.pauseDetails;

            refrigerator.menu = ui.fridgeMenu;
            UnityEventTools.AddPersistentListener(ui.startButton.onClick, manager.BeginGame);
            UnityEventTools.AddPersistentListener(ui.pauseButton.onClick, manager.TogglePause);
            UnityEventTools.AddPersistentListener(ui.resumeButton.onClick, manager.TogglePause);
            UnityEventTools.AddPersistentListener(ui.restartButton.onClick, manager.BeginGame);
            UnityEventTools.AddPersistentListener(ui.quitButton.onClick, manager.RequestQuit);
            UnityEventTools.AddPersistentListener(ui.pauseQuitButton.onClick, manager.RequestQuit);
            UnityEventTools.AddPersistentListener(ui.cancelQuitButton.onClick, manager.CancelQuit);
            UnityEventTools.AddPersistentListener(ui.confirmQuitButton.onClick, manager.QuitGame);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Yes Chef top-down perspective world created and saved to " + ScenePath);
        }

        private static void EnsureTextMeshProResources()
        {
            const string fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (defaultFont != null) return;

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (package == null) throw new FileNotFoundException("The Unity UI/TextMesh Pro package is not installed.");
            var resourcesPackage = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(resourcesPackage, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (defaultFont == null) throw new MissingReferenceException("TMP essential resources could not be imported.");
        }

        private static void ConfigureBrandTexture()
        {
            ConfigureSprite("Assets/UI/Brand/GlitchbongLogo.png");
            ConfigureSprite("Assets/UI/Brand/GitHub_Invertocat_White.png");
        }

        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException($"UI image is missing: {path}");
            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, GameObject> LoadModels()
        {
            var names = new[]
            {
                "Refrigerator", "ChoppingTable", "SingleStove", "TrashBin", "Chef", "Customer",
                "VegetableRaw", "VegetableChopped", "Cheese", "MeatRaw", "MeatCooked",
                "Tree", "Flower", "Lotus", "Rat", "Bush"
            };
            return names.ToDictionary(name => name, name =>
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{name}.fbx");
                if (model == null) throw new MissingReferenceException($"Missing Blender model: Assets/Art/Models/{name}.fbx");
                return model;
            });
        }

        private static IngredientFactory CreateIngredientFactory(GameObject root, IReadOnlyDictionary<string, GameObject> models)
        {
            var factory = root.AddComponent<IngredientFactory>();
            factory.vegetableRawPrefab = models["VegetableRaw"];
            factory.vegetablePreparedPrefab = models["VegetableChopped"];
            factory.cheesePrefab = models["Cheese"];
            factory.meatRawPrefab = models["MeatRaw"];
            factory.meatPreparedPrefab = models["MeatCooked"];
            return factory;
        }

        private static void CreateEnvironment(IReadOnlyDictionary<string, GameObject> models)
        {
            var environment = new GameObject("ENVIRONMENT").transform;
            var grass = GetOrCreateMaterial("Outdoor Grass", new Color(0.18f, 0.48f, 0.20f));
            var kitchenFloor = GetOrCreateMaterial("Kitchen Floor", new Color(0.20f, 0.27f, 0.30f));
            var wall = GetOrCreateMaterial("Kitchen Walls", new Color(0.93f, 0.84f, 0.67f));
            var road = GetOrCreateMaterial("Road", new Color(0.13f, 0.15f, 0.17f));
            var roadLine = GetOrCreateMaterial("Road Line", new Color(0.95f, 0.76f, 0.16f));
            var water = GetOrCreateMaterial("Marsh Water", new Color(0.07f, 0.38f, 0.42f));
            var stone = GetOrCreateMaterial("Building", new Color(0.33f, 0.36f, 0.43f));
            var glass = GetOrCreateTransparentMaterial("Building Windows", new Color(0.12f, 0.62f, 0.86f, .28f));

            CreateVisualBox("Outdoor Ground", new Vector3(0, -0.24f, 0), new Vector3(42f, 0.25f, 32f), grass, environment);
            CreateVisualBox("Kitchen Floor", new Vector3(0, -0.08f, 0), new Vector3(12f, 0.18f, 10f), kitchenFloor, environment);
            CreateVisualBox("North Kitchen Wall", new Vector3(0, 0.55f, 5f), new Vector3(12f, 1.1f, 0.25f), wall, environment);
            CreateWindowedKitchenWalls(environment, wall, glass);
            CreateVisualBox("West Service Wall", new Vector3(-6f, 0.42f, 0), new Vector3(0.25f, 0.84f, 10f), wall, environment);
            CreateKitchenSurfacePatterns(environment);

            CreateBoundary("Kitchen Floor Collider", new Vector3(0, -0.12f, 0), new Vector3(12f, 0.24f, 10f), environment);
            CreateBoundary("North Boundary", new Vector3(0, 1.1f, 5f), new Vector3(12f, 2.2f, 0.3f), environment);
            CreateBoundary("South Boundary", new Vector3(0, 1.1f, -5f), new Vector3(12f, 2.2f, 0.3f), environment);
            CreateBoundary("East Boundary", new Vector3(6f, 1.1f, 0), new Vector3(0.3f, 2.2f, 10f), environment);
            CreateBoundary("West Boundary", new Vector3(-6f, 1.1f, 0), new Vector3(0.3f, 2.2f, 10f), environment);

            CreateVisualBox("Road", new Vector3(-9.2f, -0.08f, 0), new Vector3(4.1f, 0.12f, 28f), road, environment);
            CreateVisualBox("Road Centre Line", new Vector3(-9.2f, -0.005f, 0), new Vector3(0.12f, 0.03f, 28f), roadLine, environment);
            for (var z = -12f; z <= 12f; z += 3f)
            {
                CreateVisualBox("Road Dash", new Vector3(-9.2f, 0.02f, z), new Vector3(0.18f, 0.035f, 1.2f), CreamMaterial(), environment);
            }

            CreateVisualBox("Marsh Bed", new Vector3(11f, -0.15f, 0), new Vector3(8f, 0.08f, 28f), water, environment);
            for (var x = 8f; x <= 14f; x += 1.5f)
                for (var z = -12f; z <= 12f; z += 2f)
                {
                    var tile = CreateVisualBoxObject("Moving Water Facet", new Vector3(x, -0.09f, z), new Vector3(1.55f, 0.04f, 2.05f), water, environment);
                    tile.AddComponent<WaterSurfaceAnimator>().waveSpeed = 0.9f + Mathf.Abs((x + z) % 3f) * 0.08f;
                }
            CreateBuilding(environment, stone, glass);
            CreateNature(models, environment);
            CreateMarshPlants(models, environment);
        }

        private static void CreateBuilding(Transform parent, Material stone, Material glass)
        {
            CreateVisualBox("North Building", new Vector3(0, 3.5f, 10f), new Vector3(15f, 7f, 4f), stone, parent);
            CreateVisualBox("Building Roof", new Vector3(0, 7.1f, 10f), new Vector3(15.5f, 0.25f, 4.5f), InkMaterial(), parent);
            for (var floor = 0; floor < 3; floor++)
            {
                for (var column = -3; column <= 3; column++)
                {
                    CreateVisualBox($"Window {floor}-{column}", new Vector3(column * 1.75f, 1.55f + floor * 1.85f, 7.95f),
                        new Vector3(0.92f, 1.05f, 0.08f), glass, parent);
                }
            }
        }

        private static void CreateKitchenSurfacePatterns(Transform parent)
        {
            var ivory = GetOrCreateMaterial("Kitchen Floor Accent", new Color(.62f, .66f, .63f));
            var brass = GetOrCreateMaterial("Kitchen Floor Brass", new Color(.73f, .47f, .14f));
            var jade = GetOrCreateMaterial("Kitchen Floor Jade", new Color(.27f, .39f, .39f));
            var wallTrim = GetOrCreateMaterial("Kitchen Wall Trim", new Color(.18f, .43f, .39f));
            var wallMotif = GetOrCreateMaterial("Kitchen Wall Motif", new Color(.72f, .27f, .17f));

            // A neutral 9-by-7 tiled layout mirrors exactly across both kitchen
            // axes. The slight gaps reveal the dark base as consistent grout.
            // Every decorative piece remains flat and collider-free.
            CreateVisualBox("Floor Border North", new Vector3(0,.021f,4.55f), new Vector3(10.9f,.028f,.12f), brass, parent);
            CreateVisualBox("Floor Border South", new Vector3(0,.021f,-4.55f), new Vector3(10.9f,.028f,.12f), brass, parent);
            CreateVisualBox("Floor Border East", new Vector3(5.48f,.021f,0), new Vector3(.12f,.028f,9.2f), brass, parent);
            CreateVisualBox("Floor Border West", new Vector3(-5.48f,.021f,0), new Vector3(.12f,.028f,9.2f), brass, parent);
            for (var row = -3; row <= 3; row++)
            {
                for (var column = -4; column <= 4; column++)
                {
                    var material = (Mathf.Abs(row) + Mathf.Abs(column)) % 2 == 0 ? ivory : jade;
                    CreateVisualBox("Symmetric Floor Tile", new Vector3(column * 1.2f,.024f,row * 1.2f),
                        new Vector3(1.14f,.025f,1.14f), material, parent);
                }
            }

            // Wainscot, cap rails, and repeating small motifs break up the old
            // solid-colour walls without relying on external texture files.
            CreateVisualBox("North Wall Wainscot", new Vector3(0,.27f,4.855f), new Vector3(11.7f,.36f,.035f), wallTrim, parent);
            CreateVisualBox("North Wall Cap Rail", new Vector3(0,.51f,4.83f), new Vector3(11.7f,.055f,.065f), brass, parent);
            CreateVisualBox("West Wall Wainscot", new Vector3(-5.855f,.25f,0), new Vector3(.035f,.34f,9.7f), wallTrim, parent);
            CreateVisualBox("West Wall Cap Rail", new Vector3(-5.83f,.49f,0), new Vector3(.065f,.055f,9.7f), brass, parent);
            for (var x = -5f; x <= 5f; x += 2f)
                CreateWallDiamond(new Vector3(x,.77f,4.855f), wallMotif, parent, false);
            for (var z = -4f; z <= 4f; z += 2f)
                CreateWallDiamond(new Vector3(-5.855f,.70f,z), wallMotif, parent, true);
        }

        private static void CreateWallDiamond(Vector3 position, Material material, Transform parent, bool westWall)
        {
            var motif = CreateVisualBoxObject("Decorative Wall Diamond", position,
                westWall ? new Vector3(.035f,.22f,.22f) : new Vector3(.22f,.22f,.035f), material, parent);
            motif.transform.rotation = westWall ? Quaternion.Euler(45f,0f,0f) : Quaternion.Euler(0f,0f,45f);
        }

        private static void CreateNature(IReadOnlyDictionary<string, GameObject> models, Transform parent)
        {
            var treePositions = new[]
            {
                new Vector3(-5.0f, 0, -8.4f), new Vector3(-1.2f, 0, -9.6f), new Vector3(2.6f, 0, -7.7f),
                new Vector3(5.4f, 0, -10.1f), new Vector3(7.3f, 0, 7.5f)
            };
            for (var index = 0; index < treePositions.Length; index++)
            {
                var tree = AddModel(models["Tree"], "Windy Low Poly Tree", treePositions[index], Quaternion.Euler(0, index * 47f, 0), Vector3.one * (0.85f + index % 3 * 0.12f), parent);
                tree.AddComponent<WindSway>().phase = index * 1.37f;
            }

            var flowers = new[]
            {
                new Vector3(-5.4f,0,-6.3f),new Vector3(-4.1f,0,-7.1f),new Vector3(-2.7f,0,-6.5f),new Vector3(-0.5f,0,-7.4f),
                new Vector3(0.4f,0,-6.2f),new Vector3(2.1f,0,-7.0f),new Vector3(3.8f,0,-6.4f),new Vector3(5.5f,0,-7.3f),
                new Vector3(-3.3f,0,-9.0f),new Vector3(1.2f,0,-9.4f),new Vector3(4.4f,0,-8.7f)
            };
            for (var index = 0; index < flowers.Length; index++)
            {
                var flower = AddModel(models["Flower"], "Windy Garden Flower", flowers[index], Quaternion.Euler(0, index * 61f, 0), Vector3.one * (0.65f + index % 4 * 0.08f), parent);
                var sway = flower.AddComponent<WindSway>();
                sway.angle = 4f;
                sway.speed = 1.4f;
            }
            foreach (var position in new[] { new Vector3(-5.2f,0,-9.7f), new Vector3(-2.1f,0,-7.9f), new Vector3(3.2f,0,-9.3f), new Vector3(5.8f,0,-8.1f) })
                AddModel(models["Bush"], "Garden Bush", position, Quaternion.Euler(0, position.x * 23f, 0), Vector3.one * 1.1f, parent).AddComponent<WindSway>();
            CreateGrassClumps(parent);
        }

        private static void CreateMarshPlants(IReadOnlyDictionary<string, GameObject> models, Transform parent)
        {
            foreach (var position in new[] { new Vector3(8.5f, 0, -3.5f), new Vector3(10.8f, 0, 2.2f), new Vector3(12.7f, 0, -0.5f), new Vector3(9.4f, 0, 5.8f) })
            {
                CreateLilyPad(position + new Vector3(.14f,-.015f,.08f), parent);
                var lotus = AddModel(models["Lotus"], "Windy Lotus", position, Quaternion.identity, Vector3.one * 1.2f, parent);
                lotus.AddComponent<WindSway>().angle = 1.4f;
            }
        }

        private static void CreateWindowedKitchenWalls(Transform parent, Material wall, Material glass)
        {
            // Visual wall segments leave genuine openings toward the garden and
            // marsh. The full-height boundary remains for predictable gameplay.
            CreateVisualBox("South Wall Left", new Vector3(-4.65f, .55f, -5f), new Vector3(2.7f, 1.1f, .25f), wall, parent);
            CreateVisualBox("South Wall Centre", new Vector3(0f, .55f, -5f), new Vector3(3.8f, 1.1f, .25f), wall, parent);
            CreateVisualBox("South Wall Right", new Vector3(4.65f, .55f, -5f), new Vector3(2.7f, 1.1f, .25f), wall, parent);
            CreateVisualBox("South Garden Window", new Vector3(-2.55f, .68f, -5.02f), new Vector3(1.45f, .7f, .05f), glass, parent);
            CreateVisualBox("South Garden Window 2", new Vector3(2.55f, .68f, -5.02f), new Vector3(1.45f, .7f, .05f), glass, parent);

            CreateVisualBox("East Wall North", new Vector3(6f, .55f, 3.75f), new Vector3(.25f, 1.1f, 2.5f), wall, parent);
            CreateVisualBox("East Wall Centre", new Vector3(6f, .55f, 0f), new Vector3(.25f, 1.1f, 2.7f), wall, parent);
            CreateVisualBox("East Wall South", new Vector3(6f, .55f, -3.75f), new Vector3(.25f, 1.1f, 2.5f), wall, parent);
            CreateVisualBox("East Marsh Window", new Vector3(6.02f, .68f, 2.0f), new Vector3(.05f, .7f, 1.35f), glass, parent);
            CreateVisualBox("East Marsh Window 2", new Vector3(6.02f, .68f, -2.0f), new Vector3(.05f, .7f, 1.35f), glass, parent);

            CreateWindowLight("Daylight Window Light - South Left", new Vector3(-2.55f, 2.8f, -6.25f), Quaternion.Euler(32f, 0f, 0f), parent, true);
            CreateWindowLight("Daylight Window Light - South Right", new Vector3(2.55f, 2.8f, -6.25f), Quaternion.Euler(32f, 0f, 0f), parent, true);
            CreateWindowLight("Daylight Window Light - East North", new Vector3(7.25f, 2.8f, 2f), Quaternion.Euler(32f, -90f, 0f), parent, true);
            CreateWindowLight("Daylight Window Light - East South", new Vector3(7.25f, 2.8f, -2f), Quaternion.Euler(32f, -90f, 0f), parent, true);

            CreateWindowLight("Night Window Spill - South Left", new Vector3(-2.55f, 2.35f, -4.45f), Quaternion.Euler(28f, 180f, 0f), parent, false);
            CreateWindowLight("Night Window Spill - South Right", new Vector3(2.55f, 2.35f, -4.45f), Quaternion.Euler(28f, 180f, 0f), parent, false);
            CreateWindowLight("Night Window Spill - East North", new Vector3(5.45f, 2.35f, 2f), Quaternion.Euler(28f, 90f, 0f), parent, false);
            CreateWindowLight("Night Window Spill - East South", new Vector3(5.45f, 2.35f, -2f), Quaternion.Euler(28f, 90f, 0f), parent, false);
        }

        private static void CreateLilyPad(Vector3 position, Transform parent)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Low Poly Lotus Leaf";
            pad.transform.SetParent(parent);
            pad.transform.position = position;
            pad.transform.localScale = new Vector3(.72f,.018f,.58f);
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            pad.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Lotus Leaf", new Color(.16f,.48f,.18f));
            var sway = pad.AddComponent<WindSway>(); sway.angle = .8f; sway.speed = .7f;
        }

        private static void CreateWindowLight(string name, Vector3 position, Quaternion rotation, Transform parent, bool daylight)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(position, rotation);
            var light = root.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = daylight ? new Color(1f, .91f, .70f) : new Color(1f, .68f, .38f);
            light.intensity = daylight ? 2.8f : 0f;
            light.range = daylight ? 10f : 8f;
            light.spotAngle = 42f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateGrassClumps(Transform parent)
        {
            var material = GetOrCreateMaterial("Garden Grass Blades", new Color(.12f, .38f, .13f));
            var positions = new[] { new Vector3(-4.8f,0,-6.8f), new Vector3(-3.7f,0,-8.2f), new Vector3(-.8f,0,-8.6f), new Vector3(.8f,0,-7.8f), new Vector3(2.8f,0,-8.8f), new Vector3(4.9f,0,-6.8f) };
            for (var index = 0; index < positions.Length; index++)
            {
                var clump = new GameObject("Windy Grass Clump");
                clump.transform.SetParent(parent);
                clump.transform.position = positions[index];
                for (var blade = 0; blade < 5; blade++)
                    CreateVisualBox("Grass Blade", positions[index] + new Vector3((blade - 2) * .08f, .22f + blade % 2 * .06f, blade % 3 * .06f), new Vector3(.035f, .44f + blade % 2 * .12f, .035f), material, clump.transform);
                var sway = clump.AddComponent<WindSway>(); sway.angle = 5f; sway.phase = index * .8f;
            }
        }

        private static DayNightCycle CreateRoomLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.62f, 0.56f);
            var cycleRoot = new GameObject("DAY NIGHT LIGHTING");
            var cycle = cycleRoot.AddComponent<DayNightCycle>();
            var lightRoot = new GameObject("SEQUENTIAL KITCHEN LIGHTS").transform;
            lightRoot.SetParent(cycleRoot.transform);
            var kitchenLights = new List<Light>();
            foreach (var position in new[]
                     {
                         new Vector3(-3.5f, 4.5f, -2.5f), new Vector3(0, 4.5f, -2.5f), new Vector3(3.5f, 4.5f, -2.5f),
                         new Vector3(-3.5f, 4.5f, 2.5f), new Vector3(0, 4.5f, 2.5f), new Vector3(3.5f, 4.5f, 2.5f)
                     })
            {
                var fixture = new GameObject("Warm Ceiling Point Light");
                fixture.transform.SetParent(lightRoot);
                fixture.transform.position = position;
                var light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.82f, 0.58f);
                light.intensity = .22f;
                light.range = 7f;
                light.shadows = LightShadows.Soft;
                kitchenLights.Add(light);
            }
            var morningRoot = new GameObject("Global Morning Point Light");
            morningRoot.transform.SetParent(cycleRoot.transform);
            morningRoot.transform.position = new Vector3(1f, 13f, -2f);
            var morning = morningRoot.AddComponent<Light>();
            morning.type = LightType.Point;
            morning.color = new Color(1f, .94f, .78f);
            morning.intensity = cycle.globalMorningIntensity;
            morning.range = 42f;
            morning.shadows = LightShadows.Soft;
            cycle.globalMorningLight = morning;
            cycle.kitchenLights = kitchenLights.ToArray();
            cycle.exteriorWindowLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(light => light.type == LightType.Spot && light.name.Contains("Daylight Window Light"))
                .ToArray();
            cycle.interiorWindowSpillLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(light => light.type == LightType.Spot && light.name.Contains("Night Window Spill"))
                .ToArray();
            cycle.ApplyLighting(0f);
            return cycle;
        }

        private static AudioDirector CreateAudioDirector()
        {
            var root = new GameObject("AUDIO - DROP CLIPS HERE");
            var director = root.AddComponent<AudioDirector>();
            director.musicSource = root.AddComponent<AudioSource>();
            director.sfxSource = root.AddComponent<AudioSource>();
            director.musicSource.playOnAwake = false;
            director.sfxSource.playOnAwake = false;
            return director;
        }

        private static void CreateAmbientAudio(PlayerController player)
        {
            CreateAmbientZone("Garden Ambience - Crickets Birds Wind", new Vector3(0, 0, -5f), player, 3);
            CreateAmbientZone("Marsh Ambience - Water Paddle Frog Hiss", new Vector3(6f, 0, 0), player, 4);
        }

        private static void CreateAmbientZone(string name, Vector3 position, PlayerController player, int sourceCount)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var zone = root.AddComponent<AmbientAudioZone>();
            zone.listener = player.transform;
            zone.loops = new AudioSource[sourceCount];
            for (var index = 0; index < sourceCount; index++)
            {
                var sourceObject = new GameObject($"Optional Loop {index + 1}");
                sourceObject.transform.SetParent(root.transform, false);
                var source = sourceObject.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = .35f;
                zone.loops[index] = source;
            }
        }

        private static void CreateKitchenActivityEffects(IReadOnlyDictionary<string, GameObject> models,
            RefrigeratorStation refrigerator, ChoppingTableStation table, StoveStation[] stoves, TrashStation trash)
        {
            var root = new GameObject("POOLED KITCHEN MICRO EFFECTS");
            var effects = root.AddComponent<KitchenActivityEffects>();
            effects.cheeseCrumbs = CreateMessParticles("Cheese or Meat Crumbs", refrigerator.transform.position + new Vector3(-.8f,.05f,-.6f), new Color(1f,.72f,.08f), 18, root.transform);
            effects.meatCrumbs = CreateMessParticles("Meat Fridge Spill", refrigerator.transform.position + new Vector3(-.7f,.05f,-.5f), new Color(.55f,.05f,.04f), 16, root.transform);
            effects.choppingSpill = CreateMessParticles("Chopping Green Spill", table.transform.position + new Vector3(0,.04f,-.8f), new Color(.2f,.7f,.13f), 14, root.transform);
            effects.meatSpill = CreateMessParticles("Cooking Meat Spill", (stoves[0].transform.position + stoves[1].transform.position) * .5f + new Vector3(0,.04f,-.9f), new Color(.55f,.05f,.04f), 12, root.transform);
            SetParticleLoop(effects.choppingSpill, true);
            SetParticleLoop(effects.meatSpill, true);
            effects.trashAnts = CreateMessParticles("Delayed Red Ants", trash.transform.position + new Vector3(0,.04f,-.6f), new Color(.45f,.035f,.025f), 22, root.transform);
            effects.trashFlies = CreateMessParticles("Delayed Black Flies", trash.transform.position + Vector3.up * .8f, new Color(.025f,.02f,.018f), 12, root.transform, .65f);
            ConfigureTrashVisitors(effects.trashAnts, effects.trashFlies);
            effects.ratEntrances = new[]
            {
                CreatePoint(root.transform, "Rat Entrance North West", new Vector3(-5.65f,.02f,4.55f)),
                CreatePoint(root.transform, "Rat Entrance North East", new Vector3(5.65f,.02f,4.55f)),
                CreatePoint(root.transform, "Rat Entrance South West", new Vector3(-5.65f,.02f,-4.55f)),
                CreatePoint(root.transform, "Rat Entrance South East", new Vector3(5.65f,.02f,-4.55f))
            };
            effects.ratFood = CreatePoint(root.transform, "Rat Food Target", refrigerator.transform.position + new Vector3(-.8f,.02f,-.6f));
            effects.rat = AddModel(models["Rat"], "Small Visiting Rat", effects.ratEntrances[0].position, Quaternion.identity, Vector3.one * .85f, root.transform, true);
            effects.rat.SetActive(false);
        }

        private static ParticleSystem CreateMessParticles(string name, Vector3 position, Color color, int maxParticles, Transform parent, float radius = .35f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            var particles = root.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 2.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.02f, .15f);
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .07f);
            main.startColor = color;
            main.maxParticles = maxParticles;
            var emission = particles.emission;
            emission.rateOverTime = maxParticles * .45f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.rotation = new Vector3(90f, 0, 0);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetOrCreateParticleMaterial("Micro_Effects", Color.white);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static void ConfigureTrashVisitors(ParticleSystem ants, ParticleSystem flies)
        {
            var antMain = ants.main;
            antMain.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            antMain.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .12f);
            antMain.startSize = new ParticleSystem.MinMaxCurve(.025f, .04f);
            var antShape = ants.shape;
            antShape.radius = .48f;
            antShape.radiusThickness = .2f;
            var antVelocity = ants.velocityOverLifetime;
            antVelocity.enabled = true;
            antVelocity.orbitalY = .72f;
            antVelocity.radial = .04f;

            var flyMain = flies.main;
            flyMain.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 4.5f);
            flyMain.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .28f);
            flyMain.startSize = new ParticleSystem.MinMaxCurve(.018f, .035f);
            var flyShape = flies.shape;
            flyShape.shapeType = ParticleSystemShapeType.Sphere;
            flyShape.radius = .62f;
            var flyVelocity = flies.velocityOverLifetime;
            flyVelocity.enabled = true;
            flyVelocity.orbitalY = 2.1f;
            flyVelocity.orbitalX = .38f;
            flyVelocity.radial = .1f;
            var noise = flies.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(.18f, .42f);
            noise.frequency = 1.8f;
            noise.scrollSpeed = .7f;
        }

        private static void SetParticleLoop(ParticleSystem particles, bool loop)
        {
            var main = particles.main;
            main.loop = loop;
        }

        private static PlayerController CreatePlayer(GameObject model)
        {
            var root = new GameObject("Player Chef");
            root.transform.position = new Vector3(0, 0, -2.2f);
            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.42f;
            controller.height = 2.25f;
            controller.center = new Vector3(0, 1.12f, 0);

            var inventory = root.AddComponent<PlayerInventory>();
            var player = root.AddComponent<PlayerController>();
            var facingRoot = new GameObject("Chef Facing Root").transform;
            facingRoot.SetParent(root.transform, false);
            var visual = AddModel(model, "Chef Visual", Vector3.zero, Quaternion.identity, Vector3.one * 0.86f, facingRoot);
            var idle = visual.AddComponent<CharacterIdleMotion>();
            idle.player = player;
            player.visualRoot = facingRoot;

            var hand = new GameObject("Hand Anchor").transform;
            hand.SetParent(facingRoot, false);
            hand.localPosition = new Vector3(0, 1.42f, 0.62f);
            inventory.handAnchor = hand;
            return player;
        }

        private static RefrigeratorStation CreateRefrigerator(GameObject model, IngredientFactory factory)
        {
            var root = CreateStationRoot("Refrigerator Station", new Vector3(4.85f, 0, 3.65f), new Vector3(1.9f, 2.9f, 1.5f));
            var visual = AddModel(model, "Refrigerator Visual", Vector3.zero, Quaternion.Euler(0, 180, 0), Vector3.one, root.transform);
            var station = root.AddComponent<RefrigeratorStation>();
            station.factory = factory;
            station.labelFader = CreateStationLabel(root.transform, "Fridge Label", new Vector3(0, 3.05f, 0), "FRIDGE", 30, new Vector2(270, 78));
            var animator = root.AddComponent<RefrigeratorAnimator>();
            animator.doorHinge = FindChild(visual.transform, "MainDoorHinge");
            var lightObject = new GameObject("Refrigerator Interior Light");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(0, 1.25f, -0.9f);
            var interiorLight = lightObject.AddComponent<Light>();
            interiorLight.type = LightType.Point;
            interiorLight.color = new Color(0.78f, 0.90f, 1f);
            interiorLight.range = 3.2f;
            interiorLight.intensity = 0f;
            animator.interiorLight = interiorLight;
            station.animator = animator;
            return station;
        }

        private static ChoppingTableStation CreateChoppingTable(GameObject model)
        {
            var root = CreateStationRoot("Chopping Table Station", new Vector3(0.65f, 0, 2.65f), new Vector3(2.6f, 1.5f, 1.5f));
            AddModel(model, "Chopping Table Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            var anchor = CreatePoint(root.transform, "Ingredient Anchor", new Vector3(.65f, 1.28f, 2.65f));
            var station = root.AddComponent<ChoppingTableStation>();
            station.itemAnchor = anchor;
            station.statusText = CreateWorldText(root.transform, "Table Status", new Vector3(0, 1.85f, 0), "CHOPPING TABLE", 30, new Vector2(360, 82), new Color(0.03f, 0.04f, 0.05f, 0.88f));
            station.labelFader = ConfigureStationLabel(station.statusText);
            station.progressFill = CreateWorldProgressBar(root.transform, "Chopping Progress", new Vector3(0, 1.98f, 0), 650);
            return station;
        }

        private static StoveStation CreateStove(string name, Vector3 position, IReadOnlyDictionary<string, GameObject> models)
        {
            var root = CreateStationRoot(name, position, new Vector3(2.05f, 1.7f, 1.6f));
            AddModel(models["ChoppingTable"], name + " Table", Vector3.zero, Quaternion.identity, new Vector3(0.78f, 0.9f, 0.82f), root.transform);
            AddModel(models["SingleStove"], name + " Hotplate", new Vector3(0, 1.0f, 0), Quaternion.identity, Vector3.one * 0.82f, root.transform);
            var anchor = CreatePoint(root.transform, "Cooking Anchor", position + new Vector3(0, 1.68f, 0));

            var station = root.AddComponent<StoveStation>();
            station.itemAnchor = anchor;
            station.statusText = CreateWorldText(root.transform, name + " Status", new Vector3(0, 2.14f, 0), name.ToUpperInvariant(), 30, new Vector2(250, 78), new Color(0.03f, 0.04f, 0.05f, 0.88f));
            station.labelFader = ConfigureStationLabel(station.statusText);
            station.progressFill = CreateWorldProgressBar(root.transform, name + " Progress", new Vector3(0, 2.42f, 0), 570);
            CreateStoveEffects(root.transform, new Vector3(0, 1.58f, 0), station);
            return station;
        }

        private static void CreateStoveEffects(Transform parent, Vector3 localPosition, StoveStation station)
        {
            var effects = new GameObject("Flame Effects");
            effects.transform.SetParent(parent, false);
            effects.transform.localPosition = localPosition;
            var particles = effects.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.55f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.18f, 0.02f), new Color(1f, 0.75f, 0.08f));
            main.maxParticles = 40;
            var emission = particles.emission;
            emission.rateOverTime = 18f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.34f;
            var particleRenderer = effects.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = GetOrCreateParticleMaterial();
            var meshSource = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = meshSource.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(meshSource);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var light = effects.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.28f, 0.04f);
            light.range = 4f;
            light.enabled = false;
            station.flameParticles = particles;
            station.cookingLight = light;
        }

        private static TrashStation CreateTrash(GameObject model)
        {
            var root = CreateStationRoot("Trash Station", new Vector3(4.8f, 0, -3.55f), new Vector3(1.5f, 1.5f, 1.5f));
            var visual = AddModel(model, "Trash Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            CreateStationLabel(root.transform, "Trash Label", new Vector3(0, 1.68f, 0), "TRASH", 30, new Vector2(230, 76));
            var station = root.AddComponent<TrashStation>();
            var animator = root.AddComponent<TrashLidAnimator>();
            animator.lid = FindChild(visual.transform, "TrashLidHinge");
            station.lidAnimator = animator;
            return station;
        }

        private static List<CustomerWindow> CreateCustomerTables(IReadOnlyDictionary<string, GameObject> models)
        {
            var result = new List<CustomerWindow>();
            var positions = new[] { -3.55f, -1.2f, 1.2f, 3.55f };
            for (var index = 0; index < positions.Length; index++)
            {
                var z = positions[index];
                var root = CreateStationRoot($"Serving Table {index + 1}", new Vector3(-5.35f, 0, z), new Vector3(1.35f, 1.5f, 1.75f));
                AddModel(models["ChoppingTable"], "Serving Table Visual", Vector3.zero, Quaternion.Euler(0, 90, 0), new Vector3(0.62f, 0.78f, 0.75f), root.transform);

                var customerRoot = new GameObject("Customer").transform;
                customerRoot.SetParent(root.transform);
                customerRoot.position = new Vector3(-6.75f, 0, z);
                var customerVisual = AddModel(models["Customer"], "Customer Visual", Vector3.zero, Quaternion.identity, Vector3.one * 0.85f, customerRoot);
                customerVisual.AddComponent<CharacterIdleMotion>();
                var avatar = customerRoot.gameObject.AddComponent<CustomerAvatar>();

                var spawnZ = index % 2 == 0 ? 12f : -12f;
                var exitZ = -spawnZ;
                var window = root.AddComponent<CustomerWindow>();
                window.customerRoot = customerRoot;
                window.avatar = avatar;
                window.spawnPoint = CreatePoint(root.transform, "Spawn Point", new Vector3(-9.2f, 0, spawnZ));
                window.roadPoint = CreatePoint(root.transform, "Road Turn", new Vector3(-9.2f, 0, z));
                window.servicePoint = CreatePoint(root.transform, "Customer Service Point", new Vector3(-6.75f, 0, z));
                window.exitPoint = CreatePoint(root.transform, "Exit Point", new Vector3(-9.2f, 0, exitZ));
                window.orderText = CreateWorldText(root.transform, "Table Order Card", new Vector3(0.3f, 1.92f, 0), "ORDER", 32, new Vector2(570, 125));
                window.scorePopupText = CreateWorldText(root.transform, "Score Popup", new Vector3(0.3f, 3.05f, 0), string.Empty, 52, new Vector2(420, 100));
                window.dialogueText = CreateCloudText(customerRoot, "Customer Dialogue", new Vector3(-1.1f, 3.3f, 0), "Welcome!", 35, new Vector2(500, 170));
                window.dialogueDisplaySeconds = 6f;
                window.dialogueBubble = window.dialogueText.transform.parent.gameObject;
                result.Add(window);
            }
            return result;
        }

        private static (CinemachineVirtualCamera virtualCamera, AdaptiveCinemachineCamera controller) CreateCinemachineCamera(PlayerController player)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.34f, 0.62f, 0.76f);
            cameraObject.AddComponent<CinemachineBrain>();

            var virtualCameraObject = new GameObject("CM Player Follow Camera");
            var virtualCamera = virtualCameraObject.AddComponent<CinemachineVirtualCamera>();
            virtualCamera.Follow = player.transform;
            virtualCamera.LookAt = null;
            virtualCamera.m_Lens.FieldOfView = 52f;
            virtualCamera.m_Lens.NearClipPlane = 0.15f;
            virtualCamera.m_Lens.FarClipPlane = 120f;
            virtualCamera.transform.position = player.transform.position + new Vector3(0, 12.8f, -6.0f);
            virtualCamera.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            var framing = virtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
            framing.m_CameraDistance = 14.5f;
            framing.m_TrackedObjectOffset = new Vector3(0, 0.9f, 0);
            framing.m_XDamping = 0.45f;
            framing.m_YDamping = 0.45f;
            framing.m_ZDamping = 0.45f;
            framing.m_ScreenX = 0.5f;
            framing.m_ScreenY = 0.48f;

            var controller = virtualCameraObject.AddComponent<AdaptiveCinemachineCamera>();
            controller.virtualCamera = virtualCamera;
            controller.player = player;
            controller.movingFieldOfView = 52f;
            controller.idleFieldOfView = 42f;
            controller.idleDelay = 3f;
            controller.zoomSmoothTime = 1.8f;
            return (virtualCamera, controller);
        }

        private static (TMP_Text timer, TMP_Text score, TMP_Text highScore, TMP_Text heldItem, Image heldColor,
            TMP_Text prompt, GameObject controlsStrip, GameObject instructions, GameObject pause, GameObject quitConfirmation, GameObject results,
            TMP_Text resultScore, TMP_Text newHighScore, TMP_Text pauseDetails, Button startButton, Button pauseButton, Button resumeButton,
            Button restartButton, Button quitButton, Button pauseQuitButton, Button cancelQuitButton, Button confirmQuitButton,
            FridgeMenuController fridgeMenu) CreateScreenUi(
                RefrigeratorStation refrigerator, PlayerController player, AudioDirector audio)
        {
            var canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.55f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var hudPanel = CreateAnchoredPanel(canvas.transform, "Score HUD", new Vector2(0, 1), new Vector2(24, -24), new Vector2(390, 180), new Vector2(0, 1));
            var score = CreateScreenText(hudPanel.transform, "Score", "SCORE  0", 38, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(340, 52));
            var highScore = CreateScreenText(hudPanel.transform, "High Score", "BEST  0", 27, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -66), new Vector2(340, 45));
            var heldColor = CreateColorSwatch(hudPanel.transform, new Vector2(24, -126));
            var heldItem = CreateScreenText(hudPanel.transform, "Held Item", "HANDS  EMPTY", 26, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -116), new Vector2(290, 46));

            var timerPanel = CreateAnchoredPanel(canvas.transform, "Timer HUD", new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(280, 88), new Vector2(0.5f, 1));
            var timer = CreateScreenText(timerPanel.transform, "Timer", "03:00", 52, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 75));

            var pauseButton = CreateButton(canvas.transform, "Pause Button", string.Empty, new Vector2(1, 1), new Vector2(-106, -28), new Vector2(64, 64));
            CreatePauseGlyph(pauseButton.transform);
            var quitButton = CreateButton(canvas.transform, "Quit Button", "X", new Vector2(1, 1), new Vector2(-28, -28), new Vector2(64, 64));

            var controlsStrip = CreateAnchoredPanel(canvas.transform, "Controls Strip", new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(1250, 92), new Vector2(0.5f, 0));
            CreateScreenText(controlsStrip.transform, "Controls", "<b>WASD / ARROWS</b> Move     <b>E</b> Interact     <b>1 / 2 / 3</b> Quick fridge pick     <b>ESC</b> Pause",
                25, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(1200, 38));
            var prompt = CreateScreenText(controlsStrip.transform, "Context Guidance", "NEXT: Visit the fridge", 30, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(1200, 46), new Color(1f, 0.78f, 0.20f));

            var fridgeMenu = CreateFridgeMenu(canvas.transform, refrigerator, player);

            var instructions = CreatePanel(canvas.transform, "Instructions Panel", new Vector2(1040, 860));
            CreateScreenText(instructions.transform, "Title", "YES CHEF!", 76, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(900, 94), Tomato);
            CreateScreenText(instructions.transform, "Subtitle", "3 MINUTES  |  4 TABLES  |  ONE ITEM AT A TIME", 26, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -115), new Vector2(900, 46), new Color(.73f,.78f,.78f,1));
            CreateScreenText(instructions.transform, "How To Play",
                "<color=#63D66B><b>[1] FRIDGE</b></color>  Pick one ingredient with E or 1 / 2 / 3.\n\n" +
                "<color=#FFD04A><b>[2] PREPARE</b></color>  Chop vegetables for 2s; cook meat for 6s; cheese is ready.\n\n" +
                "<color=#63B8FF><b>[3] SERVE</b></color>  Match the table card. Faster complete orders score more.\n\n" +
                "<color=#FF6655><b>[4] CLEAN UP</b></color>  Wrong items stay in hand—discard them at the trash.\n\n" +
                "<b>WASD / ARROWS</b> Move     <b>E</b> Interact     <b>ESC</b> Pause",
                26, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(55, 4), new Vector2(800, 430), Cream);
            CreateInstructionIcons(instructions.transform);
            var startButton = CreateButton(instructions.transform, "Start Button", "START COOKING", new Vector2(0.5f, 0), new Vector2(0, 154), new Vector2(350, 76));
            CreateBrandCredits(instructions.transform, new Vector2(0, 22));

            var pause = CreateScreenOverlay(canvas.transform, "Pause Overlay");
            var pauseCard = CreatePanel(pause.transform, "Pause Card", new Vector2(760, 570));
            CreateScreenText(pauseCard.transform, "Paused", "PAUSED", 60, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(650, 88), Tomato);
            var pauseDetails = CreateScreenText(pauseCard.transform, "Pause Details", "CURRENT SCORE  0      BEST  0", 25, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(650, 150), Cream);
            var resumeButton = CreateButton(pauseCard.transform, "Resume Button", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(300, 68));
            var pauseQuitButton = CreateButton(pauseCard.transform, "Pause Quit Button", "QUIT GAME", new Vector2(0.5f, 0.5f), new Vector2(0, -102), new Vector2(280, 60));
            var musicToggle = CreateToggle(pauseCard.transform, "Music Toggle", "MUSIC", new Vector2(-145, 48), audio.MusicEnabled);
            var sfxToggle = CreateToggle(pauseCard.transform, "SFX Toggle", "SOUND EFFECTS", new Vector2(145, 48), audio.SfxEnabled);
            UnityEventTools.AddPersistentListener(musicToggle.onValueChanged, audio.SetMusicEnabled);
            UnityEventTools.AddPersistentListener(sfxToggle.onValueChanged, audio.SetSfxEnabled);
            CreateBrandCredits(pause.transform, new Vector2(0, 22));

            var quitConfirmation = CreateScreenOverlay(canvas.transform, "Quit Confirmation Overlay");
            var quitCard = CreatePanel(quitConfirmation.transform, "Quit Confirmation Card", new Vector2(720, 500));
            CreateScreenText(quitCard.transform, "Quit Title", "ONE MORE ORDER?", 54, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(620, 78), Tomato);
            CreateScreenText(quitCard.transform, "Quit Message", "The kitchen is still warm and your customers are hungry.\nStay for one more delicious service?", 28,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(610, 105), Cream);
            var cancelQuitButton = CreateButton(quitCard.transform, "Keep Cooking Button", "KEEP COOKING", new Vector2(0.5f, 0.5f), new Vector2(-155, -40), new Vector2(280, 68));
            var confirmQuitButton = CreateButton(quitCard.transform, "Confirm Quit Button", "YES, QUIT", new Vector2(0.5f, 0.5f), new Vector2(155, -40), new Vector2(230, 68));
            CreateBrandCredits(quitConfirmation.transform, new Vector2(0, 22));

            var results = CreatePanel(canvas.transform, "Results Panel", new Vector2(700, 610));
            CreateScreenText(results.transform, "Game Over", "SERVICE OVER!", 60, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(540, 90), Tomato);
            var newHighScore = CreateScreenText(results.transform, "New High Score", string.Empty, 35, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -145), new Vector2(540, 60), new Color(1f, 0.78f, 0.2f));
            var resultScore = CreateScreenText(results.transform, "Result Score", "Final score: 0", 38, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(540, 120), Cream);
            var restartButton = CreateButton(results.transform, "Restart Button", "PLAY AGAIN", new Vector2(0.5f, 0), new Vector2(0, 174), new Vector2(290, 72));
            CreateBrandCredits(results.transform, new Vector2(0, 22));

            controlsStrip.SetActive(false);
            pause.SetActive(false);
            quitConfirmation.SetActive(false);
            results.SetActive(false);
            return (timer, score, highScore, heldItem, heldColor, prompt, controlsStrip, instructions, pause, quitConfirmation, results,
                resultScore, newHighScore, pauseDetails, startButton, pauseButton, resumeButton, restartButton, quitButton,
                pauseQuitButton, cancelQuitButton, confirmQuitButton, fridgeMenu);
        }

        private static FridgeMenuController CreateFridgeMenu(Transform canvas, RefrigeratorStation refrigerator, PlayerController player)
        {
            var panel = CreateAnchoredPanel(canvas, "Fridge Catalogue", new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(430, 590), new Vector2(1, 0.5f));
            CreateScreenText(panel.transform, "Title", "FRIDGE", 46, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(370, 65), Tomato);
            CreateScreenText(panel.transform, "Hint", "Click an ingredient to take it", 22, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -82), new Vector2(370, 42));

            var scrollObject = new GameObject("Scrollable Ingredient List", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.sizeDelta = new Vector2(380, 390);
            scrollRectTransform.anchoredPosition = new Vector2(0, -30);
            scrollObject.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.55f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            StretchToParent(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vegetable = CreateCatalogueButton(content.transform, "Vegetable", "VEGETABLE", "Raw - chop for 2 seconds", IngredientRules.Color(IngredientType.Vegetable));
            var cheese = CreateCatalogueButton(content.transform, "Cheese", "CHEESE", "Ready to serve immediately", IngredientRules.Color(IngredientType.Cheese));
            var meat = CreateCatalogueButton(content.transform, "Meat", "MEAT", "Raw - cook for 6 seconds", IngredientRules.Color(IngredientType.Meat));

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var close = CreateButton(panel.transform, "Close", "CLOSE", new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(180, 58));
            var controller = canvas.gameObject.AddComponent<FridgeMenuController>();
            controller.panel = panel;
            controller.refrigerator = refrigerator;
            controller.player = player;
            UnityEventTools.AddPersistentListener(vegetable.onClick, controller.TakeVegetable);
            UnityEventTools.AddPersistentListener(cheese.onClick, controller.TakeCheese);
            UnityEventTools.AddPersistentListener(meat.onClick, controller.TakeMeat);
            UnityEventTools.AddPersistentListener(close.onClick, controller.Close);
            panel.SetActive(false);
            return controller;
        }

        private static Button CreateCatalogueButton(Transform parent, string name, string title, string description, Color color)
        {
            var buttonObject = new GameObject(name + " Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 112;
            buttonObject.GetComponent<Image>().color = new Color(color.r * 0.42f, color.g * 0.42f, color.b * 0.42f, 0.98f);
            var button = buttonObject.GetComponent<Button>();
            CreateScreenText(buttonObject.transform, "Title", title, 31, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -14), new Vector2(300, 42), Color.white).raycastTarget = false;
            CreateScreenText(buttonObject.transform, "Description", description, 20, TextAlignmentOptions.Left,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(22, 13), new Vector2(330, 34), Cream).raycastTarget = false;
            return button;
        }

        private static Image CreateColorSwatch(Transform parent, Vector2 position)
        {
            var swatch = new GameObject("Held Item Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            swatch.transform.SetParent(parent, false);
            var rect = swatch.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(34, 34);
            var image = swatch.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = new Color(0.25f, 0.28f, 0.30f, 0.7f);
            return image;
        }

        private static TMP_Text CreateCloudText(Transform parent, string name, Vector3 localPosition, string value, float fontSize, Vector2 size)
        {
            var canvasObject = new GameObject(name + " Canvas", typeof(Canvas), typeof(Billboard));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localScale = Vector3.one * 0.0052f;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<Canvas>().sortingOrder = 14;
            canvasObject.GetComponent<RectTransform>().sizeDelta = size;
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            foreach (var data in new[]
                     {
                         (new Vector2(-170, 4), new Vector2(205, 150)),
                         (new Vector2(-65, 24), new Vector2(230, 170)),
                         (new Vector2(55, 22), new Vector2(230, 170)),
                         (new Vector2(170, 0), new Vector2(205, 150)),
                         (new Vector2(0, -22), new Vector2(360, 145)),
                         (new Vector2(180, -102), new Vector2(46, 46)),
                         (new Vector2(205, -132), new Vector2(25, 25))
                     })
            {
                var puff = new GameObject("Cloud Puff", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                puff.transform.SetParent(canvasObject.transform, false);
                var rect = puff.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = data.Item1;
                rect.sizeDelta = data.Item2;
                puff.GetComponent<Image>().sprite = sprite;
                // Fully opaque overlapping puffs read as one cloud silhouette;
                // translucency created distracting internal overlap lines.
                puff.GetComponent<Image>().color = new Color(1f, 0.98f, 0.91f, 1f);
            }

            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Ink;
            text.richText = true;
            text.enableWordWrapping = true;
            StretchToParent(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(38, 25);
            text.rectTransform.offsetMax = new Vector2(-38, -20);
            return text;
        }

        private static WorldLabelFader CreateStationLabel(Transform parent, string name, Vector3 localPosition,
            string value, float fontSize, Vector2 size)
        {
            return ConfigureStationLabel(CreateWorldText(parent, name, localPosition, value, fontSize, size,
                new Color(0.03f, 0.04f, 0.05f, 0.52f)));
        }

        private static WorldLabelFader ConfigureStationLabel(TMP_Text text)
        {
            var canvasObject = text.transform.parent.gameObject;
            canvasObject.AddComponent<CanvasGroup>();
            return canvasObject.AddComponent<WorldLabelFader>();
        }

        private static TMP_Text CreateWorldText(Transform parent, string name, Vector3 localPosition, string value, float fontSize,
            Vector2 size, Color? background = null, Color? textColor = null)
        {
            var canvasObject = new GameObject(name + " Canvas", typeof(Canvas), typeof(Billboard));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localScale = Vector3.one * 0.0042f;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvasObject.GetComponent<RectTransform>().sizeDelta = size;

            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(canvasObject.transform, false);
            StretchToParent(backdrop.GetComponent<RectTransform>());
            var backdropImage = backdrop.GetComponent<Image>();
            backdropImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            backdropImage.type = Image.Type.Sliced;
            backdropImage.color = background ?? new Color(0.03f, 0.04f, 0.05f, 0.94f);

            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor ?? Cream;
            text.richText = true;
            text.enableWordWrapping = true;
            StretchToParent(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18, 12);
            text.rectTransform.offsetMax = new Vector2(-18, -12);
            return text;
        }

        private static Image CreateWorldProgressBar(Transform parent, string name, Vector3 localPosition, float width)
        {
            var canvasObject = new GameObject(name + " Canvas", typeof(Canvas), typeof(Billboard));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localScale = Vector3.one * 0.0042f;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<Canvas>().sortingOrder = 11;
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 32);

            var background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            StretchToParent(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.95f);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(canvasObject.transform, false);
            StretchToParent(fillObject.GetComponent<RectTransform>());
            fillObject.GetComponent<RectTransform>().offsetMin = new Vector2(4, 4);
            fillObject.GetComponent<RectTransform>().offsetMax = new Vector2(-4, -4);
            var fill = fillObject.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.color = new Color(0.35f, 0.9f, 0.42f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            return fill;
        }

        private static GameObject CreateStationRoot(string name, Vector3 position, Vector3 colliderSize)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = colliderSize;
            collider.center = new Vector3(0, colliderSize.y * 0.5f, 0);
            return root;
        }

        private static Transform CreatePoint(Transform parent, string name, Vector3 worldPosition)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent);
            point.position = worldPosition;
            return point;
        }

        private static void CreateBoundary(string name, Vector3 position, Vector3 size, Transform parent)
        {
            var boundary = new GameObject(name);
            boundary.transform.SetParent(parent);
            boundary.transform.position = position;
            boundary.AddComponent<BoxCollider>().size = size;
        }

        private static GameObject AddModel(GameObject prefab, string name, Vector3 position, Quaternion rotation, Vector3 scale,
            Transform parent, bool worldSpace = false, bool applyBlenderAxisCorrection = true)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            if (worldSpace)
            {
                instance.transform.position = position;
                instance.transform.rotation = applyBlenderAxisCorrection ? rotation * Quaternion.Euler(-90f, 0f, 0f) : rotation;
            }
            else
            {
                instance.transform.localPosition = position;
                instance.transform.localRotation = applyBlenderAxisCorrection ? rotation * Quaternion.Euler(-90f, 0f, 0f) : rotation;
            }
            instance.transform.localScale = scale;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            return instance;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == childName);
        }

        private static void CreateVisualBox(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            CreateVisualBoxObject(name, position, size, material, parent);
        }

        private static GameObject CreateVisualBoxObject(string name, Vector3 position, Vector3 size, Material material, Transform parent, bool worldPosition = true)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(parent, worldPosition);
            if (worldPosition) visual.transform.position = position;
            else visual.transform.localPosition = position;
            visual.transform.localScale = size;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = material;
            return visual;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"Assets/Materials/{name.Replace(' ', '_')}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateTransparentMaterial(string name, Color color)
        {
            var material = GetOrCreateMaterial(name, color);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material InkMaterial() => GetOrCreateMaterial("Deep Ink", Ink);
        private static Material CreamMaterial() => GetOrCreateMaterial("Warm Cream", Cream);

        private static Material GetOrCreateParticleMaterial()
        {
            return GetOrCreateParticleMaterial("Stove_Flame", new Color(1f, 0.28f, 0.02f, 0.9f));
        }

        private static Material GetOrCreateParticleMaterial(string assetName, Color color)
        {
            var path = $"Assets/Materials/{assetName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) throw new MissingReferenceException("Unity's Particles/Standard Unlit shader is unavailable.");
            if (material == null)
            {
                material = new Material(shader) { name = "Stove Flame" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            return CreateAnchoredPanel(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, size, new Vector2(0.5f, 0.5f));
        }

        private static GameObject CreateScreenOverlay(Transform parent, string name)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(parent, false);
            StretchToParent(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.025f, 0.58f);
            return overlay;
        }

        private static void CreateBrandCredits(Transform parent, Vector2 bottomPosition)
        {
            var links = parent.gameObject.AddComponent<ExternalLinkButton>();
            var contact = CreateButton(parent, "Glitchbong Contact Link", "        DEVELOPED BY GLITCHBONG", new Vector2(0.5f, 0), bottomPosition + new Vector2(0, 54), new Vector2(540, 46));
            var repository = CreateButton(parent, "GitHub Source Link", "        VIEW SOURCE ON GITHUB", new Vector2(0.5f, 0), bottomPosition, new Vector2(540, 46));
            contact.GetComponent<Image>().color = new Color(0.14f, 0.48f, 0.18f, 0.94f);
            repository.GetComponent<Image>().color = new Color(0.13f, 0.15f, 0.18f, 0.96f);
            var logoObject = new GameObject("Glitchbong Logo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.transform.SetParent(contact.transform, false);
            var logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0, .5f);
            logoRect.pivot = new Vector2(0, .5f);
            logoRect.anchoredPosition = new Vector2(12, 0);
            logoRect.sizeDelta = new Vector2(40, 40);
            var logo = logoObject.GetComponent<Image>();
            logo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GlitchbongLogo.png");
            logo.raycastTarget = false;
            var githubIcon = CreateVisualUiImage(repository.transform, "GitHub Invertocat", new Vector2(-238, 0), new Vector2(34, 34), Color.white);
            githubIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GitHub_Invertocat_White.png");
            githubIcon.preserveAspect = true;
            githubIcon.raycastTarget = false;
            UnityEventTools.AddPersistentListener(contact.onClick, links.OpenGlitchbongContact);
            UnityEventTools.AddPersistentListener(repository.onClick, links.OpenRepository);
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 position, bool initialValue)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(250, 44);
            var background = CreateVisualUiImage(root.transform, "Track", new Vector2(-90, 0), new Vector2(52, 28), new Color(.12f,.15f,.17f,1));
            var check = CreateVisualUiImage(background.transform, "Checkmark", Vector2.zero, new Vector2(20,20), new Color(.35f,.9f,.42f,1));
            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = initialValue;
            CreateScreenText(root.transform, "Label", label, 19, TextAlignmentOptions.Left,
                new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(30,0), new Vector2(190,40), Cream).raycastTarget = false;
            return toggle;
        }

        private static Image CreateVisualUiImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = root.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static void CreateInstructionIcons(Transform parent)
        {
            var colors = new[] { new Color(.2f,.7f,.3f), new Color(1f,.72f,.12f), new Color(.2f,.62f,1f), Tomato };
            for (var index = 0; index < 4; index++)
            {
                var image = CreateVisualUiImage(parent, $"Rule {index + 1} Icon", new Vector2(-430, 135 - index * 63), new Vector2(42,42), colors[index]);
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            }
        }

        private static GameObject CreateAnchoredPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = Panel;
            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .42f);
            shadow.effectDistance = new Vector2(0f, -7f);
            return panel;
        }

        private static TMP_Text CreateScreenText(Transform parent, string name, string value, float fontSize,
            TextAlignmentOptions alignment, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color? color = null)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color ?? Cream;
            text.richText = true;
            text.enableWordWrapping = true;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = buttonObject.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = Tomato;
            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Tomato;
            colors.highlightedColor = new Color(1f, 0.38f, 0.24f);
            colors.pressedColor = new Color(0.68f, 0.08f, 0.06f);
            button.colors = colors;
            var shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .34f);
            shadow.effectDistance = new Vector2(0f, -4f);
            var text = CreateScreenText(buttonObject.transform, "Label", label, 26, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);
            text.raycastTarget = false;
            return button;
        }

        private static void CreatePauseGlyph(Transform parent)
        {
            foreach (var x in new[] { -8f, 8f })
            {
                var bar = new GameObject("Pause Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bar.transform.SetParent(parent, false);
                var rect = bar.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0);
                rect.sizeDelta = new Vector2(7, 25);
                bar.GetComponent<Image>().color = Color.white;
            }
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
