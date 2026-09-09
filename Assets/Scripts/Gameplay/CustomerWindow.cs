using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Owns one serving table and one reusable customer. It coordinates arrival,
    /// order display, delivery, time-sensitive dialogue, departure and respawn.
    /// </summary>
    public sealed class CustomerWindow : MonoBehaviour, IInteractable
    {
        private enum VisitState
        {
            Serving,
            Thanking,
            LeavingToRoad,
            LeavingRoad,
            Waiting,
            ArrivingOnRoad,
            ArrivingToTable
        }

        private static readonly string[] FastDialogue =
        {
            "What a lovely kitchen!", "That smells amazing!", "Beautiful day for a meal.",
            "The service here is wonderful!", "I love the chef's energy!"
        };

        private static readonly string[] NormalDialogue =
        {
            "I wonder what the special is.", "The garden looks peaceful today.", "I hope the stove is hot!",
            "This road is always lively.", "Fresh ingredients make everything better."
        };

        private static readonly string[] SlowDialogue =
        {
            "Is my order coming soon?", "I'm getting rather hungry...", "Did the chef forget my table?",
            "This is taking a while.", "Please hurry, chef!"
        };

        private static readonly string[] ThanksDialogue =
        {
            "Delicious - thank you!", "Exactly what I wanted!", "Wonderful cooking!", "Perfect. Have a great day!"
        };

        [Header("Customer and route")]
        public Transform customerRoot;
        public CustomerAvatar avatar;
        public Transform spawnPoint;
        public Transform roadPoint;
        public Transform servicePoint;
        public Transform exitPoint;
        public float walkingSpeed = 2.4f;

        [Header("TextMesh Pro displays")]
        public TMP_Text orderText;
        public TMP_Text dialogueText;
        public TMP_Text scorePopupText;
        public GameObject dialogueBubble;

        [Header("Timing")]
        [Min(0f)] public float respawnMinimum = 2.5f;
        [Min(0f)] public float respawnMaximum = 6f;
        [Min(0.1f)] public float popupSeconds = 2.5f;
        [Min(0.1f)] public float popupFadeSeconds = 1f;

        private OrderTicket currentOrder;
        private VisitState state;
        private float stateSeconds;
        private float dialogueSeconds;
        private float bubbleVisibleSeconds;
        private float popupRemaining;
        private float waitDuration;

        private bool HasOrder => state == VisitState.Serving && currentOrder != null;

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || game.Phase != GamePhase.Playing) return;

            UpdateVisit();
            UpdateScorePopup();
            RefreshOrderDisplay();
        }

        public string GetPrompt(PlayerController player)
        {
            if (!HasOrder) return "TABLE: Waiting for the next customer";
            if (!player.Inventory.HasItem) return $"{avatar.CustomerName}: Check the order card on this serving table";

            var item = player.Inventory.HeldItem;
            if (!IngredientRules.IsDeliveryReady(item.Type, item.State)) return "Prepare that ingredient before serving it";
            return currentOrder.RemainingIngredients.Contains(item.Type)
                ? $"[E] Serve {item.Type} to {avatar.CustomerName}"
                : $"{avatar.CustomerName} did not order that ingredient";
        }

        public void Interact(PlayerController player)
        {
            if (!HasOrder || !player.Inventory.HasItem) return;
            var item = player.Inventory.HeldItem;
            if (!IngredientRules.IsDeliveryReady(item.Type, item.State)) return;
            if (!currentOrder.TryDeliver(item.Type)) return;

            var delivered = player.Inventory.ConsumeHeld();
            if (delivered != null) Destroy(delivered.gameObject);
            ShowDialogue("That is one item closer - thank you!");

            if (currentOrder.IsComplete) CompleteOrder();
            RefreshOrderDisplay();
        }

        public void ResetWindow(bool startServing, float initialDelay)
        {
            popupRemaining = 0f;
            ClearPopup();
            stateSeconds = 0f;
            if (startServing)
            {
                customerRoot.gameObject.SetActive(true);
                customerRoot.position = servicePoint.position;
                FaceCounter();
                avatar.Randomize();
                CreateOrder();
            }
            else
            {
                currentOrder = null;
                waitDuration = initialDelay;
                state = VisitState.Waiting;
                customerRoot.gameObject.SetActive(false);
                if (dialogueBubble != null) dialogueBubble.SetActive(false);
                RefreshOrderDisplay();
            }
        }

        private void UpdateVisit()
        {
            if (bubbleVisibleSeconds > 0f)
            {
                bubbleVisibleSeconds -= Time.deltaTime;
                if (bubbleVisibleSeconds <= 0f && state == VisitState.Serving && dialogueBubble != null)
                {
                    dialogueBubble.SetActive(false);
                }
            }

            switch (state)
            {
                case VisitState.Serving:
                    currentOrder.Tick(Time.deltaTime);
                    dialogueSeconds -= Time.deltaTime;
                    if (dialogueSeconds <= 0f) ShowTimedDialogue();
                    break;
                case VisitState.Thanking:
                    CountDownThen(2.4f, BeginLeavingToRoad);
                    break;
                case VisitState.LeavingToRoad:
                    MoveCustomer(roadPoint.position, VisitState.LeavingRoad);
                    break;
                case VisitState.LeavingRoad:
                    MoveCustomer(exitPoint.position, VisitState.Waiting);
                    break;
                case VisitState.Waiting:
                    customerRoot.gameObject.SetActive(false);
                    CountDownThen(waitDuration, BeginArrival);
                    break;
                case VisitState.ArrivingOnRoad:
                    MoveCustomer(roadPoint.position, VisitState.ArrivingToTable);
                    break;
                case VisitState.ArrivingToTable:
                    MoveCustomer(servicePoint.position, VisitState.Serving, () =>
                    {
                        FaceCounter();
                        CreateOrder();
                    });
                    break;
            }
        }

        private void CreateOrder()
        {
            currentOrder = OrderGenerator.CreateRandom();
            state = VisitState.Serving;
            stateSeconds = 0f;
            dialogueSeconds = Random.Range(5f, 9f);
            var request = string.Join(" + ", currentOrder.RequiredIngredients.Select(IngredientRules.ShortName));
            ShowDialogue($"Hi, I'm <b>{avatar.CustomerName}</b>. May I order\n<b>{request}</b>?");
            RefreshOrderDisplay();
        }

        private void CompleteOrder()
        {
            var awarded = currentOrder.CalculateScore();
            var orderAge = currentOrder.SecondsOpen;
            GameManager.Instance.AddScore(awarded);

            if (scorePopupText != null)
            {
                scorePopupText.transform.parent.gameObject.SetActive(true);
                scorePopupText.text = awarded >= 0
                    ? $"<color=#7DFF72>+{awarded}</color>"
                    : $"<color=#FF6868>{awarded}</color>";
                scorePopupText.alpha = 1f;
            }

            popupRemaining = popupSeconds;
            ShowDialogue(orderAge < 35f
                ? Pick(ThanksDialogue)
                : "Thanks - I was getting very hungry!");
            currentOrder = null;
            state = VisitState.Thanking;
            stateSeconds = 0f;
            waitDuration = Random.Range(respawnMinimum, respawnMaximum);
        }

        private void BeginLeavingToRoad()
        {
            state = VisitState.LeavingToRoad;
            stateSeconds = 0f;
            if (dialogueBubble != null) dialogueBubble.SetActive(false);
        }

        private void BeginArrival()
        {
            avatar.Randomize();
            customerRoot.position = spawnPoint.position;
            customerRoot.gameObject.SetActive(true);
            state = VisitState.ArrivingOnRoad;
            stateSeconds = 0f;
        }

        private void FaceCounter()
        {
            var counterDirection = transform.position - customerRoot.position;
            counterDirection.y = 0f;
            if (counterDirection.sqrMagnitude > 0.01f)
                customerRoot.rotation = Quaternion.LookRotation(counterDirection, Vector3.up);
        }

        private void MoveCustomer(Vector3 target, VisitState nextState, System.Action arrived = null)
        {
            var direction = target - customerRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                customerRoot.rotation = Quaternion.Slerp(customerRoot.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            }
            customerRoot.position = Vector3.MoveTowards(customerRoot.position, target, walkingSpeed * Time.deltaTime);
            if (Vector3.Distance(customerRoot.position, target) > 0.04f) return;
            state = nextState;
            stateSeconds = 0f;
            arrived?.Invoke();
        }

        private void CountDownThen(float duration, System.Action action)
        {
            stateSeconds += Time.deltaTime;
            if (stateSeconds >= duration) action();
        }

        private void ShowTimedDialogue()
        {
            var dialogue = currentOrder.SecondsOpen < 20f ? Pick(FastDialogue)
                : currentOrder.SecondsOpen < 45f ? Pick(NormalDialogue)
                : Pick(SlowDialogue);
            ShowDialogue(dialogue);
            dialogueSeconds = Random.Range(7f, 12f);
        }

        private void ShowDialogue(string message)
        {
            if (dialogueBubble != null) dialogueBubble.SetActive(true);
            if (dialogueText != null) dialogueText.text = message;
            bubbleVisibleSeconds = 3.5f;
        }

        private void RefreshOrderDisplay()
        {
            if (orderText == null) return;
            if (!HasOrder)
            {
                orderText.transform.parent.gameObject.SetActive(false);
                return;
            }

            orderText.transform.parent.gameObject.SetActive(true);

            var required = currentOrder.RequiredIngredients;
            var remaining = currentOrder.RemainingIngredients;
            var ingredients = string.Join("  ", required.Select((type, index) =>
            {
                var deliveredCount = required.Count(candidate => candidate == type) - remaining.Count(candidate => candidate == type);
                var occurrenceNumber = required.Take(index + 1).Count(candidate => candidate == type);
                var complete = occurrenceNumber <= deliveredCount;
                var color = ColorUtility.ToHtmlStringRGB(IngredientRules.Color(type));
                return complete ? $"<s>{IngredientRules.ShortName(type)}</s>" : $"<color=#{color}>{IngredientRules.ShortName(type)}</color>";
            }));
            orderText.text = $"<b>{avatar.CustomerName.ToUpperInvariant()}</b>  {currentOrder.SecondsOpen:0}s\n{ingredients}";
        }

        private void UpdateScorePopup()
        {
            if (popupRemaining <= 0f) return;
            popupRemaining -= Time.deltaTime;
            if (scorePopupText == null) return;
            scorePopupText.alpha = Mathf.Clamp01(popupRemaining / popupFadeSeconds);
            if (popupRemaining <= 0f) ClearPopup();
        }

        private void ClearPopup()
        {
            if (scorePopupText == null) return;
            scorePopupText.text = string.Empty;
            scorePopupText.alpha = 1f;
            scorePopupText.transform.parent.gameObject.SetActive(false);
        }

        private static string Pick(IReadOnlyList<string> values) => values[Random.Range(0, values.Count)];
    }
}
