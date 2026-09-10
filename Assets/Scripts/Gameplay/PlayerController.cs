using System.Linq;
using UnityEngine;

namespace YesChef
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInventory))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.5f;
        public float interactionRadius = 1.65f;
        public Transform visualRoot;
        public CharacterIdleMotion characterMotion;

        public PlayerInventory Inventory { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsActionLocked { get; private set; }

        private CharacterController characterController;
        private IInteractable currentInteractable;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            Inventory = GetComponent<PlayerInventory>();
            StartPosition = transform.position;
        }

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                game.TogglePause();
            }

            if (game.Phase != GamePhase.Playing)
            {
                IsMoving = false;
                game.SetInteractionPrompt(string.Empty);
                return;
            }

            if (IsActionLocked)
            {
                IsMoving = false;
                characterController.SimpleMove(Vector3.zero);
                return;
            }

            Move();
            FindInteraction();

            if (currentInteractable is RefrigeratorStation refrigerator && refrigerator.menu != null && refrigerator.menu.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) refrigerator.TryTake(this, IngredientType.Vegetable);
                if (Input.GetKeyDown(KeyCode.Alpha2)) refrigerator.TryTake(this, IngredientType.Cheese);
                if (Input.GetKeyDown(KeyCode.Alpha3)) refrigerator.TryTake(this, IngredientType.Meat);
            }

            if (IsActionLocked) return;

            if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
            {
                currentInteractable.Interact(this);
            }
        }

        public void ResetPlayer()
        {
            Inventory.Clear();
            IsActionLocked = false;
            characterController.enabled = false;
            transform.position = StartPosition;
            transform.rotation = Quaternion.identity;
            characterController.enabled = true;
        }

        public void BeginFridgeAction(Vector3 fridgePosition)
        {
            IsActionLocked = true;
            IsMoving = false;
            var direction = fridgePosition - transform.position;
            direction.y = 0f;
            if (visualRoot != null && direction.sqrMagnitude > 0.01f)
                visualRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void PlayFridgeReach(float duration) => characterMotion?.PlayReach(duration);

        public void EndFridgeAction() => IsActionLocked = false;

        private void Move()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            IsMoving = input.sqrMagnitude > 0.01f;
            characterController.SimpleMove(input * moveSpeed);

            if (IsMoving && visualRoot != null)
            {
                visualRoot.rotation = Quaternion.Slerp(
                    visualRoot.rotation,
                    Quaternion.LookRotation(input, Vector3.up),
                    14f * Time.deltaTime);
            }
        }

        private void FindInteraction()
        {
            // A station may have several child components or colliders. Distinct()
            // collapses those into one option before selecting the nearest one.
            currentInteractable = Physics.OverlapSphere(transform.position, interactionRadius)
                .SelectMany(collider => collider.GetComponentsInParent<MonoBehaviour>())
                .OfType<IInteractable>()
                .Distinct()
                .OrderBy(interactable => Vector3.SqrMagnitude(((MonoBehaviour)interactable).transform.position - transform.position))
                .FirstOrDefault();

            var prompt = currentInteractable?.GetPrompt(this);
            GameManager.Instance.SetInteractionPrompt(string.IsNullOrEmpty(prompt) ? Inventory.GetGuidance() : prompt);
        }
    }
}
