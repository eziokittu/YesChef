using UnityEngine;
using UnityEngine.UI;

namespace YesChef
{
    /// <summary>
    /// Bridges pointer-held screen controls into the existing player controller.
    /// The preference defaults on for WebGL and touch devices, while ordinary
    /// desktop builds keep the overlay out of the way.
    /// </summary>
    public sealed class MobileInputController : MonoBehaviour
    {
        private const string PreferenceKey = "YesChef.ScreenControls";

        public GameObject controlsRoot;
        public Toggle visibilityToggle;
        public bool showInEditor = true;
        [HideInInspector] public bool forceVisibleForTesting;

        public Vector2 Movement { get; private set; }
        public bool ControlsEnabled { get; private set; }

        private readonly bool[] heldDirections = new bool[4];
        private bool actionPressed;

        private void Awake()
        {
            ControlsEnabled = PlayerPrefs.GetInt(PreferenceKey, 1) != 0;
            visibilityToggle?.SetIsOnWithoutNotify(ControlsEnabled);
            RefreshVisibility();
        }

        private void Update()
        {
            RefreshVisibility();
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        public void SetControlsEnabled(bool enabled)
        {
            ControlsEnabled = enabled;
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) ReleaseAll();
            RefreshVisibility();
        }

        public void SetDirection(MobileDirection direction, bool held)
        {
            heldDirections[(int)direction] = held;
            Movement = new Vector2(
                (heldDirections[(int)MobileDirection.Right] ? 1f : 0f) - (heldDirections[(int)MobileDirection.Left] ? 1f : 0f),
                (heldDirections[(int)MobileDirection.Up] ? 1f : 0f) - (heldDirections[(int)MobileDirection.Down] ? 1f : 0f));
            Movement = Vector2.ClampMagnitude(Movement, 1f);
        }

        public void SetMovement(Vector2 movement)
        {
            for (var index = 0; index < heldDirections.Length; index++) heldDirections[index] = false;
            Movement = Vector2.ClampMagnitude(movement, 1f);
        }

        public void PressAction()
        {
            actionPressed = true;
        }

        public bool ConsumeActionPressed()
        {
            if (!actionPressed) return false;
            actionPressed = false;
            return true;
        }

        private void RefreshVisibility()
        {
            if (controlsRoot == null) return;
            var playing = GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Playing;
            var shouldShow = ControlsEnabled && playing && SupportsScreenControls();
            if (controlsRoot.activeSelf != shouldShow) controlsRoot.SetActive(shouldShow);
            if (!shouldShow) ReleaseAll();
        }

        private bool SupportsScreenControls()
        {
            if (forceVisibleForTesting) return true;
#if UNITY_WEBGL
            return true;
#elif UNITY_EDITOR
            return showInEditor;
#else
            return Input.touchSupported;
#endif
        }

        private void ReleaseAll()
        {
            for (var index = 0; index < heldDirections.Length; index++) heldDirections[index] = false;
            Movement = Vector2.zero;
            actionPressed = false;
        }
    }

    public enum MobileDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
