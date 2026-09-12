using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace YesChef
{
    [DefaultExecutionOrder(-10000)]
    public sealed class RuntimeVerifier : MonoBehaviour
    {
        private bool listenerRepairProbe;

        private void Awake()
        {
            listenerRepairProbe = Array.IndexOf(Environment.GetCommandLineArgs(), "-yeschef-listener-repair-probe") >= 0;
            if (!listenerRepairProbe) return;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                DestroyImmediate(listener);
        }

        private void Start()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (listenerRepairProbe)
            {
                StartCoroutine(ProbeAudioListenerRepairAndQuit());
            }
            else if (Array.IndexOf(arguments, "-yeschef-gameplay-capture") >= 0)
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
            else if (Array.IndexOf(arguments, "-yeschef-character-animation-probe") >= 0)
            {
                StartCoroutine(ProbeCharacterAnimationAndQuit());
            }
            else if (Array.IndexOf(arguments, "-yeschef-camera-fov-probe") >= 0)
            {
                StartCoroutine(ProbeCameraFieldOfViewAndQuit());
            }
            else if (Array.IndexOf(arguments, "-yeschef-quit-modal-probe") >= 0)
            {
                StartCoroutine(ProbeQuitModalAndQuit());
            }
            else if (Array.IndexOf(arguments, "-yeschef-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(false, false, false, false, false, false, false, false));
            }
        }

        private static IEnumerator ProbeAudioListenerRepairAndQuit()
        {
            yield return null;
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var music = AudioDirector.Instance.musicSource;
            var startSample = music.timeSamples;
            yield return new WaitForSecondsRealtime(.75f);
            var host = listeners.Length == 1 ? listeners[0].gameObject : null;
            Debug.Log($"YES_CHEF_LISTENER_REPAIR_PROBE: count={listeners.Length}, host={(host != null ? host.name : "NONE")}, " +
                      $"music={music.isPlaying}, advanced={music.timeSamples > startSample}");
            Application.Quit(listeners.Length == 1 && host == Camera.main.gameObject &&
                             music.isPlaying && music.timeSamples > startSample ? 0 : 1);
        }

        private static IEnumerator ProbeCameraFieldOfViewAndQuit()
        {
            yield return null;
            var game = GameManager.Instance;
            var adaptiveCamera = game.adaptiveCamera;
            adaptiveCamera.idleDelay = 100f;
            game.BeginGame();
            yield return new WaitForEndOfFrame();

            var gameplayFieldOfView = adaptiveCamera.virtualCamera.m_Lens.FieldOfView;
            adaptiveCamera.idleDelay = 0f;
            adaptiveCamera.zoomSmoothTime = .05f;
            yield return new WaitForSecondsRealtime(.5f);
            var idleFieldOfView = adaptiveCamera.virtualCamera.m_Lens.FieldOfView;

            var passed = Mathf.Abs(adaptiveCamera.movingFieldOfView - 36f) < .01f &&
                         Mathf.Abs(adaptiveCamera.idleFieldOfView - 28f) < .01f &&
                         Mathf.Abs(adaptiveCamera.idleRevealFieldOfView - 28f) < .01f &&
                         Mathf.Abs(gameplayFieldOfView - 36f) < .05f &&
                         Mathf.Abs(idleFieldOfView - 28f) < .15f;
            Debug.Log($"YES_CHEF_CAMERA_FOV_PROBE: gameplay={gameplayFieldOfView:0.00}, " +
                      $"idle={idleFieldOfView:0.00}, configuredMoving={adaptiveCamera.movingFieldOfView:0.00}, " +
                      $"configuredIdle={adaptiveCamera.idleFieldOfView:0.00}, passed={passed}");
            Application.Quit(passed ? 0 : 1);
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
                var queued = refrigerator.TryTake(GameManager.Instance.player, IngredientType.Cheese);
                yield return new WaitForSecondsRealtime(.35f);
                Debug.Log($"YES_CHEF_FRIDGE_REACH: queued={queued}, door={refrigerator.animator.OpenAmount:0.00}, " +
                          $"reaching={GameManager.Instance.player.characterMotion.IsPerformingAction}, " +
                          $"held={GameManager.Instance.player.Inventory.HasItem}");
                yield return new WaitForSecondsRealtime(.55f);
                Debug.Log($"YES_CHEF_FRIDGE_PICKUP_COMPLETE: held={GameManager.Instance.player.Inventory.HasItem}, " +
                          $"locked={GameManager.Instance.player.IsActionLocked}, menu={GameManager.Instance.fridgeMenu.IsOpen}");
                GameManager.Instance.player.Inventory.Clear();
                GameManager.Instance.fridgeMenu.Open(GameManager.Instance.player);
            }
            if (lightStove)
            {
                var game = GameManager.Instance;
                game.fridgeMenu.Open(game.player);
                game.fridgeMenu.refrigerator.TryTake(game.player, IngredientType.Meat);
                yield return new WaitForSecondsRealtime(1.30f);
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
            var music = AudioDirector.Instance.musicSource;
            var initialMusicSample = music.timeSamples;
            var activeListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"YES_CHEF_AUDIO_LISTENER_COUNT: {activeListeners.Length}");
            Debug.Log($"YES_CHEF_EFFECTS_INITIAL: chop={effects.choppingSpill.isPlaying}, stove={effects.meatSpill.isPlaying}, " +
                      $"ants={effects.trashAnts.isPlaying}, flies={effects.trashFlies.isPlaying}, " +
                      $"music={music.isPlaying}, track={music.clip?.name}, volume={music.volume:0.00}");

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
            var trash = FindFirstObjectByType<TrashStation>();
            trash?.lidAnimator?.OpenOnce();
            Debug.Log($"YES_CHEF_EFFECTS_TRIGGERED: chop={effects.choppingSpill.isPlaying}, stove={effects.meatSpill.isPlaying}");
            yield return new WaitForSecondsRealtime(.25f);
            if (trash != null && trash.lidAnimator != null && trash.lidAnimator.lid != null)
                Debug.Log($"YES_CHEF_TRASH_LID_UP: localEuler={trash.lidAnimator.lid.localEulerAngles}");

            var chef = GameManager.Instance.player;
            var controller = chef.GetComponent<CharacterController>();
            controller.enabled = false;
            chef.transform.position = GameManager.Instance.stoves[0].transform.position + Vector3.back * .4f;
            controller.enabled = true;
            yield return new WaitForSecondsRealtime(.5f);
            var stoveLabelAlpha = GameManager.Instance.stoves[0].labelFader.GetComponent<CanvasGroup>().alpha;
            Debug.Log($"YES_CHEF_STOVE_LABEL_NEARBY_ALPHA: {stoveLabelAlpha:0.000}");
            Debug.Log($"YES_CHEF_MUSIC_ADVANCING: playing={music.isPlaying}, startSample={initialMusicSample}, " +
                      $"currentSample={music.timeSamples}, advanced={music.timeSamples > initialMusicSample}");
            yield return new WaitForSecondsRealtime(5.55f);
            Debug.Log($"YES_CHEF_TRASH_VISITORS: ants={effects.trashAnts.particleCount}, flies={effects.trashFlies.particleCount}");
            yield return new WaitForSecondsRealtime(8f);
            Debug.Log($"YES_CHEF_TRASH_VISITORS_PERSIST: ants={effects.trashAnts.particleCount}, flies={effects.trashFlies.particleCount}, " +
                      $"buzz={effects.trashFlyAudio != null && effects.trashFlyAudio.isPlaying}");
            Application.Quit(0);
        }

        private static IEnumerator ProbeCharacterAnimationAndQuit()
        {
            yield return null;
            GameManager.Instance.BeginGame();
            var motions = FindObjectsByType<CharacterIdleMotion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var chefMotion = GameManager.Instance.player.characterMotion;
            var idleStart = chefMotion.transform.localPosition;
            yield return new WaitForSeconds(.35f);
            var idleDelta = Vector3.Distance(idleStart, chefMotion.transform.localPosition);

            var observedCustomerWalk = false;
            var deadline = Time.time + 9f;
            while (Time.time < deadline)
            {
                foreach (var motion in motions)
                {
                    if (motion.player == null && motion.IsMoving) observedCustomerWalk = true;
                }
                yield return null;
            }

            Debug.Log($"YES_CHEF_CHARACTER_ANIMATION: count={motions.Length}, idleDelta={idleDelta:0.000}, " +
                      $"customerWalk={observedCustomerWalk}, armPivots={chefMotion.ArmPivotCount}, legPivots={chefMotion.LegPivotCount}");
            Application.Quit(motions.Length >= 5 && idleDelta > .005f && observedCustomerWalk &&
                             chefMotion.ArmPivotCount == 2 && chefMotion.LegPivotCount == 2 ? 0 : 1);
        }

        private static IEnumerator ProbeQuitModalAndQuit()
        {
            yield return null;
            var game = GameManager.Instance;

            game.BeginGame();
            game.TogglePause();
            game.RequestQuit();
            var pauseHidden = !game.pausePanel.activeSelf && game.quitConfirmationPanel.activeSelf;
            game.CancelQuit();
            var pauseRestored = game.Phase == GamePhase.Paused && game.pausePanel.activeSelf &&
                                !game.quitConfirmationPanel.activeSelf;

            game.TogglePause();
            game.matchSeconds = .02f;
            game.BeginGame();
            yield return new WaitForSecondsRealtime(.08f);
            var reachedResults = game.Phase == GamePhase.Results && game.resultsPanel.activeSelf;
            game.RequestQuit();
            var resultsHidden = !game.resultsPanel.activeSelf && game.quitConfirmationPanel.activeSelf;
            game.CancelQuit();
            var resultsRestored = game.Phase == GamePhase.Results && game.resultsPanel.activeSelf &&
                                  !game.quitConfirmationPanel.activeSelf;

            Debug.Log($"YES_CHEF_QUIT_MODAL: pauseHidden={pauseHidden}, pauseRestored={pauseRestored}, " +
                      $"reachedResults={reachedResults}, resultsHidden={resultsHidden}, resultsRestored={resultsRestored}");
            Application.Quit(pauseHidden && pauseRestored && reachedResults && resultsHidden && resultsRestored ? 0 : 1);
        }
    }
}
