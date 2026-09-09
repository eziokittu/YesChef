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
            CreateRoomLighting();

            var gameRoot = new GameObject("GAMEPLAY");
            gameRoot.AddComponent<RuntimeVerifier>();
            var factory = CreateIngredientFactory(gameRoot, models);
            var player = CreatePlayer(models["Chef"]);
            var refrigerator = CreateRefrigerator(models["Refrigerator"], factory);
            var table = CreateChoppingTable(models["ChoppingTable"]);
            var stoves = new[]
            {
                CreateStove("Stove A", new Vector3(1.25f, 0f, -0.35f), models),
                CreateStove("Stove B", new Vector3(3.75f, 0f, -0.35f), models)
            };
            var trash = CreateTrash(models["TrashBin"]);
            var windows = CreateCustomerTables(models);

            refrigerator.transform.SetParent(gameRoot.transform);
            table.transform.SetParent(gameRoot.transform);
            foreach (var stove in stoves) stove.transform.SetParent(gameRoot.transform);
            trash.transform.SetParent(gameRoot.transform);
            foreach (var window in windows) window.transform.SetParent(gameRoot.transform);

            var camera = CreateCinemachineCamera(player);
            var ui = CreateScreenUi(refrigerator, player);

            var manager = gameRoot.AddComponent<GameManager>();
            manager.player = player;
            manager.windows = windows.ToArray();
            manager.choppingTable = table;
            manager.stoves = stoves;
            manager.fridgeMenu = ui.fridgeMenu;
            manager.adaptiveCamera = camera.controller;
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
            const string path = "Assets/UI/Brand/GlitchbongLogo.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException($"Brand logo is missing: {path}");
            if (importer.textureType == TextureImporterType.Sprite) return;
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
                "Tree", "Flower", "Lotus", "Frog", "Fish", "Snake"
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
            var kitchenFloor = GetOrCreateMaterial("Kitchen Floor", new Color(0.55f, 0.59f, 0.57f));
            var wall = GetOrCreateMaterial("Kitchen Walls", new Color(0.92f, 0.88f, 0.72f));
            var road = GetOrCreateMaterial("Road", new Color(0.13f, 0.15f, 0.17f));
            var roadLine = GetOrCreateMaterial("Road Line", new Color(0.95f, 0.76f, 0.16f));
            var water = GetOrCreateMaterial("Marsh Water", new Color(0.07f, 0.38f, 0.42f));
            var stone = GetOrCreateMaterial("Building", new Color(0.33f, 0.36f, 0.43f));
            var glass = GetOrCreateMaterial("Building Windows", new Color(0.12f, 0.62f, 0.86f));

            CreateVisualBox("Outdoor Ground", new Vector3(0, -0.24f, 0), new Vector3(32f, 0.25f, 28f), grass, environment);
            CreateVisualBox("Kitchen Floor", new Vector3(0, -0.08f, 0), new Vector3(12f, 0.18f, 10f), kitchenFloor, environment);
            CreateVisualBox("North Kitchen Wall", new Vector3(0, 0.55f, 5f), new Vector3(12f, 1.1f, 0.25f), wall, environment);
            CreateVisualBox("South Kitchen Wall", new Vector3(0, 0.55f, -5f), new Vector3(12f, 1.1f, 0.25f), wall, environment);
            CreateVisualBox("East Kitchen Wall", new Vector3(6f, 0.55f, 0), new Vector3(0.25f, 1.1f, 10f), wall, environment);
            CreateVisualBox("West Service Wall", new Vector3(-6f, 0.42f, 0), new Vector3(0.25f, 0.84f, 10f), wall, environment);

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

            CreateVisualBox("Marsh", new Vector3(11f, -0.10f, 0), new Vector3(8f, 0.10f, 28f), water, environment);
            CreateBuilding(environment, stone, glass);
            CreateNature(models, environment);
            CreateMarshLife(models, environment);
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

        private static void CreateNature(IReadOnlyDictionary<string, GameObject> models, Transform parent)
        {
            var treePositions = new[]
            {
                new Vector3(-4.5f, 0, -8f), new Vector3(-1.5f, 0, -9.2f), new Vector3(2f, 0, -8.3f),
                new Vector3(5.2f, 0, -9.5f), new Vector3(7.5f, 0, 7.2f)
            };
            foreach (var position in treePositions) AddModel(models["Tree"], "Low Poly Tree", position, Quaternion.identity, Vector3.one, parent);

            for (var index = 0; index < 16; index++)
            {
                var x = -5.5f + (index % 8) * 1.5f;
                var z = -6.5f - (index / 8) * 1.3f;
                AddModel(models["Flower"], "Garden Flower", new Vector3(x, 0, z), Quaternion.Euler(0, index * 37f, 0), Vector3.one * 0.8f, parent);
            }
        }

        private static void CreateMarshLife(IReadOnlyDictionary<string, GameObject> models, Transform parent)
        {
            foreach (var position in new[] { new Vector3(8.5f, 0, -3.5f), new Vector3(10.8f, 0, 2.2f), new Vector3(12.7f, 0, -0.5f), new Vector3(9.4f, 0, 5.8f) })
            {
                AddModel(models["Lotus"], "Lotus", position, Quaternion.identity, Vector3.one * 1.2f, parent);
            }
            AddModel(models["Frog"], "Frog", new Vector3(9f, 0.04f, 1.2f), Quaternion.Euler(0, 35, 0), Vector3.one, parent);
            AddModel(models["Frog"], "Frog", new Vector3(12.5f, 0.04f, -4f), Quaternion.Euler(0, -45, 0), Vector3.one * 0.85f, parent);

            CreateWildlife(models["Fish"], "Swimming Fish A", new Vector3(8.2f, 0.02f, -5f), new Vector3(13.3f, 0.02f, -5f), 1.0f, false, parent);
            CreateWildlife(models["Fish"], "Swimming Fish B", new Vector3(12.8f, 0.02f, 3.8f), new Vector3(8.5f, 0.02f, 3.8f), 0.75f, false, parent);
            CreateWildlife(models["Snake"], "Rare Marsh Snake", new Vector3(8.3f, 0.05f, 6f), new Vector3(13.2f, 0.05f, -6f), 1.25f, true, parent);
        }

        private static void CreateWildlife(GameObject model, string name, Vector3 pointA, Vector3 pointB, float speed, bool rare, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pointA;
            AddModel(model, name + " Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            var mover = root.AddComponent<WildlifeMover>();
            mover.pointA = pointA;
            mover.pointB = pointB;
            mover.speed = speed;
            mover.rareAppearance = rare;
        }

        private static void CreateRoomLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.27f);
            var lightRoot = new GameObject("ROOM POINT LIGHTS").transform;
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
                light.intensity = 2.2f;
                light.range = 7f;
                light.shadows = LightShadows.Soft;
            }
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
            var root = CreateStationRoot("Refrigerator Station", new Vector3(4.85f, 0, 3.45f), new Vector3(1.9f, 2.9f, 1.5f));
            var visual = AddModel(model, "Refrigerator Visual", Vector3.zero, Quaternion.Euler(0, 180, 0), Vector3.one, root.transform);
            var station = root.AddComponent<RefrigeratorStation>();
            station.factory = factory;
            station.labelFader = CreateStationLabel(root.transform, "Fridge Label", new Vector3(0, 2.82f, 0), "FRIDGE", 27, new Vector2(250, 72));
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
            var root = CreateStationRoot("Chopping Table Station", new Vector3(1.6f, 0, 2.45f), new Vector3(2.6f, 1.5f, 1.5f));
            AddModel(model, "Chopping Table Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            var anchor = CreatePoint(root.transform, "Ingredient Anchor", new Vector3(1.6f, 1.28f, 2.45f));
            var station = root.AddComponent<ChoppingTableStation>();
            station.itemAnchor = anchor;
            station.statusText = CreateWorldText(root.transform, "Table Status", new Vector3(0, 1.62f, 0), "CHOPPING TABLE", 27, new Vector2(330, 76), new Color(0.03f, 0.04f, 0.05f, 0.52f));
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
            station.statusText = CreateWorldText(root.transform, name + " Status", new Vector3(0, 1.92f, 0), name.ToUpperInvariant(), 27, new Vector2(230, 72), new Color(0.03f, 0.04f, 0.05f, 0.52f));
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
            var root = CreateStationRoot("Trash Station", new Vector3(4.9f, 0, -3.6f), new Vector3(1.5f, 1.5f, 1.5f));
            AddModel(model, "Trash Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            CreateStationLabel(root.transform, "Trash Label", new Vector3(0, 1.44f, 0), "TRASH", 26, new Vector2(210, 70));
            return root.AddComponent<TrashStation>();
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
                window.orderText = CreateWorldText(root.transform, "Table Order Card", new Vector3(0.3f, 2.15f, 0), "ORDER", 40, new Vector2(720, 165));
                window.scorePopupText = CreateWorldText(root.transform, "Score Popup", new Vector3(0.3f, 3.05f, 0), string.Empty, 52, new Vector2(420, 100));
                window.dialogueText = CreateCloudText(customerRoot, "Customer Dialogue", new Vector3(-1.1f, 3.3f, 0), "Welcome!", 38, new Vector2(520, 180));
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
                RefrigeratorStation refrigerator, PlayerController player)
        {
            var canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
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

            var instructions = CreatePanel(canvas.transform, "Instructions Panel", new Vector2(900, 700));
            CreateScreenText(instructions.transform, "Title", "YES CHEF!", 76, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(780, 100), Tomato);
            CreateScreenText(instructions.transform, "Subtitle", "Top-down kitchen service - three minutes, four tables", 31, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(800, 55), Cream);
            CreateScreenText(instructions.transform, "How To Play",
                "<b>1.</b> Visit the fridge and press <b>E</b> to browse, click an item, or use 1 / 2 / 3.\n" +
                "<b>2.</b> Chop raw vegetables for 2s. Cook raw meat on either stove for 6s. Cheese is ready.\n" +
                "<b>3.</b> Carry only one item and serve it at the matching named customer table.\n" +
                "<b>4.</b> Faster orders score more. Wrong items stay in your hands; trash unwanted items.\n\n" +
                "The bottom bar always tells you the controls and your best next action.",
                28, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(760, 350), Cream);
            var startButton = CreateButton(instructions.transform, "Start Button", "START COOKING", new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(330, 76));

            var pause = CreateScreenOverlay(canvas.transform, "Pause Overlay");
            var pauseCard = CreatePanel(pause.transform, "Pause Card", new Vector2(760, 690));
            CreateScreenText(pauseCard.transform, "Paused", "PAUSED", 60, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(650, 88), Tomato);
            var pauseDetails = CreateScreenText(pauseCard.transform, "Pause Details", "CURRENT SCORE  0      BEST  0", 25, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(650, 150), Cream);
            var resumeButton = CreateButton(pauseCard.transform, "Resume Button", ">  RESUME", new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(280, 68));
            var pauseQuitButton = CreateButton(pauseCard.transform, "Pause Quit Button", "QUIT GAME", new Vector2(0.5f, 0.5f), new Vector2(0, -102), new Vector2(280, 60));
            CreateBrandCredits(pauseCard.transform, new Vector2(0, 82));

            var quitConfirmation = CreateScreenOverlay(canvas.transform, "Quit Confirmation Overlay");
            var quitCard = CreatePanel(quitConfirmation.transform, "Quit Confirmation Card", new Vector2(720, 650));
            CreateScreenText(quitCard.transform, "Quit Title", "ONE MORE ORDER?", 54, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(620, 78), Tomato);
            CreateScreenText(quitCard.transform, "Quit Message", "The kitchen is still warm and your customers are hungry.\nStay for one more delicious service?", 28,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(610, 105), Cream);
            var cancelQuitButton = CreateButton(quitCard.transform, "Keep Cooking Button", ">  KEEP COOKING", new Vector2(0.5f, 0.5f), new Vector2(-155, -40), new Vector2(280, 68));
            var confirmQuitButton = CreateButton(quitCard.transform, "Confirm Quit Button", "YES, QUIT", new Vector2(0.5f, 0.5f), new Vector2(155, -40), new Vector2(230, 68));
            CreateBrandCredits(quitCard.transform, new Vector2(0, 78));

            var results = CreatePanel(canvas.transform, "Results Panel", new Vector2(620, 480));
            CreateScreenText(results.transform, "Game Over", "SERVICE OVER!", 60, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(540, 90), Tomato);
            var newHighScore = CreateScreenText(results.transform, "New High Score", string.Empty, 35, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -145), new Vector2(540, 60), new Color(1f, 0.78f, 0.2f));
            var resultScore = CreateScreenText(results.transform, "Result Score", "Final score: 0", 38, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(540, 120), Cream);
            var restartButton = CreateButton(results.transform, "Restart Button", "PLAY AGAIN", new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(270, 72));

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
                puff.GetComponent<Image>().color = new Color(1f, 0.98f, 0.91f, 0.96f);
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
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(parent);
            visual.transform.position = position;
            visual.transform.localScale = size;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = material;
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

        private static Material InkMaterial() => GetOrCreateMaterial("Deep Ink", Ink);
        private static Material CreamMaterial() => GetOrCreateMaterial("Warm Cream", Cream);

        private static Material GetOrCreateParticleMaterial()
        {
            const string path = "Assets/Materials/Stove_Flame.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) throw new MissingReferenceException("Unity's Particles/Standard Unlit shader is unavailable.");
            if (material == null)
            {
                material = new Material(shader) { name = "Stove Flame" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = new Color(1f, 0.28f, 0.02f, 0.9f);
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
            overlay.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.025f, 0.72f);
            return overlay;
        }

        private static void CreateBrandCredits(Transform parent, Vector2 bottomPosition)
        {
            var logoObject = new GameObject("Glitchbong Logo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.transform.SetParent(parent, false);
            var logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0);
            logoRect.pivot = new Vector2(0.5f, 0);
            logoRect.anchoredPosition = bottomPosition + new Vector2(-245, 0);
            logoRect.sizeDelta = new Vector2(78, 78);
            logoObject.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Brand/GlitchbongLogo.png");

            var links = parent.gameObject.AddComponent<ExternalLinkButton>();
            var contact = CreateButton(parent, "Glitchbong Contact Link", "DEVELOPED BY GLITCHBONG  ->", new Vector2(0.5f, 0), bottomPosition + new Vector2(35, 40), new Vector2(430, 48));
            var repository = CreateButton(parent, "GitHub Source Link", "GH  CHECK GITHUB SOURCE CODE  ->", new Vector2(0.5f, 0), bottomPosition + new Vector2(35, -18), new Vector2(430, 48));
            contact.GetComponent<Image>().color = new Color(0.14f, 0.48f, 0.18f, 0.94f);
            repository.GetComponent<Image>().color = new Color(0.13f, 0.15f, 0.18f, 0.96f);
            UnityEventTools.AddPersistentListener(contact.onClick, links.OpenGlitchbongContact);
            UnityEventTools.AddPersistentListener(repository.onClick, links.OpenRepository);
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
