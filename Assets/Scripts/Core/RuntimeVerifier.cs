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
                StartCoroutine(CaptureAndQuit(true, false, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-fridge-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, true, false, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-stove-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, true, false, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-pause-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, true, false));
            }
            else if (Array.IndexOf(arguments, "-yeschef-quit-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true, false, false, false, true));
            }
            else if (Array.IndexOf(arguments, "-yeschef-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(false, false, false, false, false));
            }
        }

        private static IEnumerator CaptureAndQuit(bool beginGame, bool openFridge, bool lightStove, bool pauseMenu, bool quitMenu)
        {
            yield return null;
            if (beginGame) GameManager.Instance.BeginGame();
            if (openFridge)
            {
                GameManager.Instance.fridgeMenu.closeDistance = 100f;
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
            yield return new WaitForSecondsRealtime(0.35f);
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
            var fileName = pauseMenu ? "YesChef_Pause_Verification.png"
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
    }
}
