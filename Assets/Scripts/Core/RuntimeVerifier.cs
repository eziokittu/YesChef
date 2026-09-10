using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace YesChef
{
    public sealed class RuntimeVerifier : MonoBehaviour
    {
        private void Start()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "-yeschef-gameplay-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, false, false, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-fridge-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, true, false, false, false, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-stove-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, true, false, false, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-pause-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, true, false, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-quit-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, false, true, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-exterior-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, false, false, true, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-customer-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, false, false, false, true, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-night-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, false, false, false, false, true));
            }
            else if (Array.IndexOf(arguments, "-yeschef-effects-probe") >= 0)
            {
                StartCoroutine(ProbeKitchenEffectsAndQuit());
            }
            else if (Array.IndexOf(arguments, "-yeschef-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(false, false, false, false, false, false, false, false));
            }
        }

        private static IEnumerator CaptureAndQuit(bool beginGame, bool openFridge, bool lightStove, bool pauseMenu,
            bool quitMenu, bool exterior, bool customerSide, bool night)
        {
            yield return null;
            if (beginGame) GameManager.Instance.BeginGame();
            if (openFridge)
            {
                GameManager.Instance.fridgeMenu.closeDistance = 100f;
                GameManager.Instance.fridgeMenu.Open(GameManager.Instance.player);
                var refrigerator = GameManager.Instance.fridgeMenu.refrigerator;
                refrigerator.TryTake(GameManager.Instance.player, IngredientType.Cheese);
                refrigerator.Interact(GameManager.Instance.player);
                Debug.Log($"YES_CHEF_FRIDGE_E_CLOSE_WITH_HELD_ITEM: {!GameManager.Instance.fridgeMenu.IsOpen}");
                GameManager.Instance.player.Inventory.Clear();
                GameManager.Instance.fridgeMenu.Open(GameManager.Instance.player);
            }
            if (lightStove)
            {
                var game = GameManager.Instance;
                game.fridgeMenu.refrigerator.TryTake(game.player, IngredientType.Meat);
                game.stoves[0].Interact(game.player);
            }
            if (pauseMenu) GameManager.Instance.TogglePause();
            if (quitMenu) GameManager.Instance.RequestQuit();
            if (exterior)
            {
                var chef = GameManager.Instance.player;
                var controller = chef.GetComponent<CharacterController>();
                controller.enabled = false;
                chef.transform.position = new Vector3(4.7f, 0f, 0f);
                controller.enabled = true;
            }
            if (customerSide)
            {
                var chef = GameManager.Instance.player;
                var controller = chef.GetComponent<CharacterController>();
                controller.enabled = false;
                chef.transform.position = new Vector3(-4.7f, 0f, 0f);
                controller.enabled = true;
            }
            if (night)
            {
                GameManager.Instance.dayNightCycle.ApplyLighting(80f);
                Debug.Log($"YES_CHEF_RUNTIME_NIGHT: daylight={GameManager.Instance.dayNightCycle.DaylightAmount:0.00}, " +
                          $"sun={GameManager.Instance.dayNightCycle.globalMorningLight.intensity:0.00}, " +
                          $"kitchenMin={Mathf.Min(Array.ConvertAll(GameManager.Instance.dayNightCycle.kitchenLights, light => light.intensity)):0.00}, " +
                          $"dayWindows={Mathf.Max(Array.ConvertAll(GameManager.Instance.dayNightCycle.exteriorWindowLights, light => light.intensity)):0.00}, " +
                          $"nightWindows={Mathf.Min(Array.ConvertAll(GameManager.Instance.dayNightCycle.interiorWindowSpillLights, light => light.intensity)):0.00}");
            }
            yield return new WaitForSecondsRealtime(exterior || customerSide ? 4.5f : .35f);
            if (beginGame)
            {
                var chef = GameManager.Instance.player;
                Debug.Log($"YES_CHEF_RUNTIME_PLAYER: world={chef.transform.position}, screen={Camera.main.WorldToScreenPoint(chef.transform.position + Vector3.up)}");
                foreach (var renderer in chef.GetComponentsInChildren<Renderer>(true))
                {
                    Debug.Log($"YES_CHEF_RUNTIME_RENDERER: {renderer.name}, active={renderer.gameObject.activeInHierarchy}, enabled={renderer.enabled}, bounds={renderer.bounds.center}");
                }
            }
            yield return new WaitForEndOfFrame();
            var fileName = exterior ? "YesChef_Exterior_Verification.png"
                : customerSide ? "YesChef_Customer_Camera_Verification.png"
                : night ? "YesChef_Night_Verification.png"
                : pauseMenu ? "YesChef_Pause_Verification.png"
                : quitMenu ? "YesChef_Quit_Verification.png"
                : openFridge ? "YesChef_Fridge_Verification.png"
                : lightStove ? "YesChef_Stove_Verification.png"
                : beginGame ? "YesChef_Gameplay_Verification.png"
                : "YesChef_Verification.png";
            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fileName));
            ScreenCapture.CaptureScreenshot(outputPath, 1);
            yield return new WaitForSecondsRealtime(2f);
            if (beginGame)
            {
                Debug.Log($"YES_CHEF_RUNTIME_IDLE_FOV_BEFORE_DELAY: {GameManager.Instance.adaptiveCamera.virtualCamera.m_Lens.FieldOfView:0.00}");
                yield return new WaitForSecondsRealtime(2f);
                Debug.Log($"YES_CHEF_RUNTIME_IDLE_FOV_AFTER_DELAY: {GameManager.Instance.adaptiveCamera.virtualCamera.m_Lens.FieldOfView:0.00}");
            }
            Application.Quit(0);
        }

        private static IEnumerator ProbeKitchenEffectsAndQuit()
        {
            yield return null;
            GameManager.Instance.BeginGame();
            var effects = KitchenActivityEffects.Instance;
            Debug.Log($"YES_CHEF_EFFECTS_INITIAL: chop={effects.choppingSpill.isPlaying}, stove={effects.meatSpill.isPlaying}, " +
                      $"ants={effects.trashAnts.isPlaying}, flies={effects.trashFlies.isPlaying}, rat={effects.rat.activeSelf}");

            // Choose a deterministic state whose next value triggers the normal
            // 50% cheese spill path; the production method remains unchanged.
            for (var seed = 0; seed < 100; seed++)
            {
                UnityEngine.Random.InitState(seed);
                var state = UnityEngine.Random.state;
                if (UnityEngine.Random.value <= .5f)
                {
                    UnityEngine.Random.state = state;
                    break;
                }
            }
            effects.OnFridgeTake(IngredientType.Cheese);
            effects.OnTrashDiscard();
            effects.OnChoppingStarted();
            effects.OnCookingStarted();
            Debug.Log($"YES_CHEF_EFFECTS_TRIGGERED: chop={effects.choppingSpill.isPlaying}, stove={effects.meatSpill.isPlaying}");
            yield return new WaitForSecondsRealtime(3.1f);
            Debug.Log($"YES_CHEF_RAT_VISIT: active={effects.rat.activeSelf}, entrance={effects.LastRatEntranceIndex}, position={effects.rat.transform.position}");
            yield return new WaitForSecondsRealtime(3.2f);
            Debug.Log($"YES_CHEF_TRASH_VISITORS: ants={effects.trashAnts.particleCount}, flies={effects.trashFlies.particleCount}");
            Application.Quit(0);
        }
    }
}
