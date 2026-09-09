#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
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
    public static class YesChefSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Kitchen.unity";

        private static readonly Color Cream = new(0.96f, 0.91f, 0.78f, 1f);
        private static readonly Color Ink = new(0.08f, 0.10f, 0.13f, 1f);
        private static readonly Color Tomato = new(0.90f, 0.20f, 0.15f, 1f);
        private static readonly Color Panel = new(0.055f, 0.07f, 0.09f, 0.94f);

        [MenuItem("Tools/Yes Chef/Build Playable Kitchen")]
        public static void BuildPlayableKitchen()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var models = LoadModels();
            CreateEnvironment(models);
            CreateCameraAndLighting();

            var gameRoot = new GameObject("GAMEPLAY");
            var factory = gameRoot.AddComponent<IngredientFactory>();
            factory.vegetableRawPrefab = models["VegetableRaw"];
            factory.vegetablePreparedPrefab = models["VegetableChopped"];
            factory.cheesePrefab = models["Cheese"];
            factory.meatRawPrefab = models["MeatRaw"];
            factory.meatPreparedPrefab = models["MeatCooked"];

            var player = CreatePlayer(models["Chef"]);
            var refrigerator = CreateRefrigerator(models["Refrigerator"], factory);
            var table = CreateTable(models["ChoppingTable"]);
            var stove = CreateStove(models["Stove"]);
            var trash = CreateTrash(models["TrashBin"]);
            var windows = CreateWindows(models["CustomerWindow"]);

            refrigerator.transform.SetParent(gameRoot.transform);
            table.transform.SetParent(gameRoot.transform);
            stove.transform.SetParent(gameRoot.transform);
            trash.transform.SetParent(gameRoot.transform);
            foreach (var window in windows) window.transform.SetParent(gameRoot.transform);

            var ui = CreateScreenUi();
            var manager = gameRoot.AddComponent<GameManager>();
            manager.player = player;
            manager.windows = windows.ToArray();
            manager.choppingTable = table;
            manager.stove = stove;
            manager.timerText = ui.timer;
            manager.scoreText = ui.score;
            manager.highScoreText = ui.highScore;
            manager.interactionText = ui.prompt;
            manager.instructionsPanel = ui.instructions;
            manager.pausePanel = ui.pause;
            manager.resultsPanel = ui.results;
            manager.resultScoreText = ui.resultScore;
            manager.newHighScoreText = ui.newHighScore;

            UnityEventTools.AddPersistentListener(ui.startButton.onClick, manager.BeginGame);
            UnityEventTools.AddPersistentListener(ui.pauseButton.onClick, manager.TogglePause);
            UnityEventTools.AddPersistentListener(ui.resumeButton.onClick, manager.TogglePause);
            UnityEventTools.AddPersistentListener(ui.restartButton.onClick, manager.BeginGame);
            UnityEventTools.AddPersistentListener(ui.quitButton.onClick, manager.QuitGame);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Yes Chef playable kitchen created and saved to " + ScenePath);
        }

        private static Dictionary<string, GameObject> LoadModels()
        {
            var names = new[]
            {
                "KitchenFloor", "KitchenWall", "Refrigerator", "ChoppingTable", "Stove", "TrashBin",
                "CustomerWindow", "Chef", "VegetableRaw", "VegetableChopped", "Cheese", "MeatRaw", "MeatCooked"
            };
            var models = new Dictionary<string, GameObject>();
            foreach (var name in names)
            {
                var path = $"Assets/Art/Models/{name}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new MissingReferenceException($"Missing generated model: {path}");
                models.Add(name, model);
            }
            return models;
        }

        private static void CreateEnvironment(Dictionary<string, GameObject> models)
        {
            var environment = new GameObject("ENVIRONMENT").transform;
            AddModel(models["KitchenFloor"], "Kitchen Floor", Vector3.zero, Quaternion.identity, new Vector3(7f, 1f, 5f), environment);
            AddModel(models["KitchenWall"], "North Wall", new Vector3(0, 0, 5f), Quaternion.identity, new Vector3(3.5f, 1f, 1f), environment);
            AddModel(models["KitchenWall"], "South Wall", new Vector3(0, 0, -5f), Quaternion.Euler(0, 180, 0), new Vector3(3.5f, 1f, 1f), environment);
            AddModel(models["KitchenWall"], "East Wall", new Vector3(7f, 0, 0), Quaternion.Euler(0, 90, 0), new Vector3(2.5f, 1f, 1f), environment);
            AddModel(models["KitchenWall"], "West Wall", new Vector3(-7f, 0, 0), Quaternion.Euler(0, -90, 0), new Vector3(2.5f, 1f, 1f), environment);

            CreateBoundary("North Boundary", new Vector3(0, 1.25f, 5f), new Vector3(14f, 2.5f, 0.3f), environment);
            CreateBoundary("South Boundary", new Vector3(0, 1.25f, -5f), new Vector3(14f, 2.5f, 0.3f), environment);
            CreateBoundary("East Boundary", new Vector3(7f, 1.25f, 0), new Vector3(0.3f, 2.5f, 10f), environment);
            CreateBoundary("West Boundary", new Vector3(-7f, 1.25f, 0), new Vector3(0.3f, 2.5f, 10f), environment);
        }

        private static void CreateCameraAndLighting()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.085f);
            cameraObject.transform.position = new Vector3(0, 13.5f, -10.5f);
            cameraObject.transform.LookAt(new Vector3(0, 0, 0.4f));

            var lightObject = new GameObject("Sun Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.94f, 0.83f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.51f, 0.55f);
        }

        private static PlayerController CreatePlayer(GameObject model)
        {
            var root = new GameObject("Player Chef");
            root.transform.position = new Vector3(0, 0, -3.0f);
            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.42f;
            controller.height = 2.25f;
            controller.center = new Vector3(0, 1.12f, 0);

            var inventory = root.AddComponent<PlayerInventory>();
            var player = root.AddComponent<PlayerController>();
            var visual = AddModel(model, "Chef Visual", Vector3.zero, Quaternion.Euler(0, 180, 0), Vector3.one * 0.86f, root.transform);
            player.visualRoot = visual.transform;

            var hand = new GameObject("Hand Anchor").transform;
            hand.SetParent(visual.transform, false);
            hand.localPosition = new Vector3(0, 1.35f, 0.62f);
            inventory.handAnchor = hand;
            return player;
        }

        private static RefrigeratorStation CreateRefrigerator(GameObject model, IngredientFactory factory)
        {
            var root = CreateStationRoot("Refrigerator Station", new Vector3(5.45f, 0, 3.5f), new Vector3(1.9f, 2.9f, 1.4f));
            AddModel(model, "Refrigerator Visual", Vector3.zero, Quaternion.Euler(0, 180, 0), Vector3.one, root.transform);
            var station = root.AddComponent<RefrigeratorStation>();
            station.factory = factory;
            CreateWorldText(root.transform, "Ingredient Menu", new Vector3(0, 3.15f, 0), "<b>FRIDGE</b>\n1 Veg   2 Cheese   3 Meat", 32, new Vector2(720, 150));
            return station;
        }

        private static ChoppingTableStation CreateTable(GameObject model)
        {
            var root = CreateStationRoot("Chopping Table Station", new Vector3(1.65f, 0, 2.35f), new Vector3(2.6f, 1.5f, 1.5f));
            AddModel(model, "Table Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            var anchor = new GameObject("Ingredient Anchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0, 1.28f, 0);
            var station = root.AddComponent<ChoppingTableStation>();
            station.itemAnchor = anchor;
            station.statusText = CreateWorldText(root.transform, "Table Status", new Vector3(0, 2.12f, 0), "<b>CHOPPING TABLE</b>\nEmpty", 30, new Vector2(650, 140));
            return station;
        }

        private static StoveStation CreateStove(GameObject model)
        {
            var root = CreateStationRoot("Stove Station", new Vector3(1.65f, 0, -0.2f), new Vector3(2.6f, 1.6f, 1.7f));
            AddModel(model, "Stove Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            var anchors = new Transform[2];
            for (var i = 0; i < 2; i++)
            {
                anchors[i] = new GameObject($"Stove Slot {i + 1}").transform;
                anchors[i].SetParent(root.transform, false);
                anchors[i].localPosition = new Vector3(i == 0 ? -0.58f : 0.58f, 1.38f, 0);
            }
            var station = root.AddComponent<StoveStation>();
            station.slotAnchors = anchors;
            station.statusText = CreateWorldText(root.transform, "Stove Status", new Vector3(0, 2.15f, 0), "<b>STOVE</b>\n1: Empty   2: Empty", 30, new Vector2(650, 140));
            return station;
        }

        private static TrashStation CreateTrash(GameObject model)
        {
            var root = CreateStationRoot("Trash Station", new Vector3(5.45f, 0, -3.35f), new Vector3(1.5f, 1.5f, 1.5f));
            AddModel(model, "Trash Visual", Vector3.zero, Quaternion.identity, Vector3.one, root.transform);
            CreateWorldText(root.transform, "Trash Label", new Vector3(0, 1.75f, 0), "<b>TRASH</b>", 30, new Vector2(360, 80));
            return root.AddComponent<TrashStation>();
        }

        private static List<CustomerWindow> CreateWindows(GameObject model)
        {
            var result = new List<CustomerWindow>();
            var positions = new[] { -3.55f, -1.2f, 1.2f, 3.55f };
            for (var i = 0; i < positions.Length; i++)
            {
                var root = CreateStationRoot($"Customer Window {i + 1}", new Vector3(-6.25f, 0, positions[i]), new Vector3(1.4f, 2.8f, 2.15f));
                AddModel(model, "Window Visual", Vector3.zero, Quaternion.Euler(0, 90, 0), new Vector3(0.82f, 0.82f, 0.82f), root.transform);
                var window = root.AddComponent<CustomerWindow>();
                window.orderText = CreateWorldText(root.transform, "Order Display", new Vector3(1.05f, 2.85f, 0), "ORDER", 27, new Vector2(700, 145));
                window.scorePopupText = CreateWorldText(root.transform, "Score Popup", new Vector3(1.05f, 3.55f, 0), string.Empty, 42, new Vector2(360, 90));
                result.Add(window);
            }
            return result;
        }

        private static (TMP_Text timer, TMP_Text score, TMP_Text highScore, TMP_Text prompt, GameObject instructions,
            GameObject pause, GameObject results, TMP_Text resultScore, TMP_Text newHighScore, Button startButton,
            Button pauseButton, Button resumeButton, Button restartButton, Button quitButton) CreateScreenUi()
        {
            var canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var score = CreateScreenText(canvas.transform, "Score", "SCORE  0", 38, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -24), new Vector2(350, 70));
            var highScore = CreateScreenText(canvas.transform, "High Score", "BEST  0", 28, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -86), new Vector2(350, 55));
            var timer = CreateScreenText(canvas.transform, "Timer", "03:00", 52, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(300, 80));
            var prompt = CreateScreenText(canvas.transform, "Interaction Prompt", string.Empty, 32, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(1100, 70));
            AddTextBackdrop(prompt.rectTransform, new Color(0.02f, 0.025f, 0.03f, 0.82f));

            var pauseButton = CreateButton(canvas.transform, "Pause Button", "PAUSE", new Vector2(1, 1), new Vector2(-180, -42), new Vector2(150, 54));
            var quitButton = CreateButton(canvas.transform, "Quit Button", "QUIT", new Vector2(1, 1), new Vector2(-28, -42), new Vector2(120, 54));

            var instructions = CreatePanel(canvas.transform, "Instructions Panel", new Vector2(760, 610));
            CreateScreenText(instructions.transform, "Title", "YES CHEF!", 72, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(650, 100), Tomato);
            CreateScreenText(instructions.transform, "Subtitle", "Three minutes. Four hungry customers. One chef.", 28, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -145), new Vector2(680, 55), Cream);
            CreateScreenText(instructions.transform, "Controls",
                "<b>WASD / Arrow Keys</b>  Move\n<b>E</b>  Interact / pick up / place / deliver\n<b>1 / 2 / 3</b>  Choose Vegetable / Cheese / Meat at fridge\n<b>Esc</b>  Pause\n\nVegetables: chop 2s   |   Meat: cook 6s   |   Cheese: ready now",
                29, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(680, 285), Cream);
            var startButton = CreateButton(instructions.transform, "Start Button", "START COOKING", new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(310, 72));

            var pause = CreatePanel(canvas.transform, "Pause Panel", new Vector2(520, 330));
            CreateScreenText(pause.transform, "Paused", "PAUSED", 64, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -55), new Vector2(440, 100), Tomato);
            var resumeButton = CreateButton(pause.transform, "Resume Button", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(260, 68));

            var results = CreatePanel(canvas.transform, "Results Panel", new Vector2(600, 470));
            CreateScreenText(results.transform, "Game Over", "SERVICE OVER!", 58, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(520, 90), Tomato);
            var newHighScore = CreateScreenText(results.transform, "New High Score", string.Empty, 34, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(520, 60), new Color(1f, 0.78f, 0.2f));
            var resultScore = CreateScreenText(results.transform, "Result Score", "Final score: 0", 36, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(520, 120), Cream);
            var restartButton = CreateButton(results.transform, "Restart Button", "PLAY AGAIN", new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(260, 68));

            pause.SetActive(false);
            results.SetActive(false);
            return (timer, score, highScore, prompt, instructions, pause, results, resultScore, newHighScore,
                startButton, pauseButton, resumeButton, restartButton, quitButton);
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

        private static void CreateBoundary(string name, Vector3 position, Vector3 size, Transform parent)
        {
            var boundary = new GameObject(name);
            boundary.transform.SetParent(parent);
            boundary.transform.position = position;
            boundary.AddComponent<BoxCollider>().size = size;
        }

        private static GameObject AddModel(GameObject prefab, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            return instance;
        }

        private static TMP_Text CreateWorldText(Transform parent, string name, Vector3 localPosition, string value, float fontSize, Vector2 size)
        {
            var canvasObject = new GameObject(name + " Canvas", typeof(Canvas), typeof(Billboard));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localScale = Vector3.one * 0.0045f;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(canvasObject.transform, false);
            var backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.88f);

            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Cream;
            text.richText = true;
            text.enableWordWrapping = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 8);
            textRect.offsetMax = new Vector2(-12, -8);
            return text;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = Panel;
            return panel;
        }

        private static TMP_Text CreateScreenText(Transform parent, string name, string value, float fontSize,
            TextAlignmentOptions alignment, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color? color = null)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.alignment = alignment;
            text.color = color ?? Cream;
            text.richText = true;
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
            image.color = Tomato;
            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Tomato;
            colors.highlightedColor = new Color(1f, 0.34f, 0.22f);
            colors.pressedColor = new Color(0.72f, 0.10f, 0.08f);
            button.colors = colors;

            var text = CreateScreenText(buttonObject.transform, "Label", label, 26, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);
            text.raycastTarget = false;
            return button;
        }

        private static void AddTextBackdrop(RectTransform target, Color color)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(target.parent, false);
            backdrop.transform.SetSiblingIndex(target.GetSiblingIndex());
            var rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = target.anchorMin;
            rect.anchorMax = target.anchorMax;
            rect.pivot = target.pivot;
            rect.anchoredPosition = target.anchoredPosition;
            rect.sizeDelta = target.sizeDelta + new Vector2(30, 10);
            backdrop.GetComponent<Image>().color = color;
        }
    }
}
#endif
