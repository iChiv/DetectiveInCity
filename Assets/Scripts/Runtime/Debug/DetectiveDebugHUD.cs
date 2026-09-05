using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Detective
{
    public sealed class DetectiveDebugHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private DetectiveClickMover mover;
        [SerializeField] private InteractionController interactionController;
        [SerializeField] private bool showInteractableLabels = true;
        [SerializeField] private bool allowLabelToggle = true;

        private void Update()
        {
            if (allowLabelToggle && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                showInteractableLabels = !showInteractableLabels;
            }

            ApplyLabelVisibility();

            if (statusText == null || mover == null || interactionController == null)
            {
                return;
            }

            Vector3 destination = mover.Destination;
            string agentState = mover.Agent != null && mover.Agent.isOnNavMesh ? "On NavMesh" : "Off NavMesh";
            string collectedClues = DetectiveInvestigationState.CollectedClueCount == 0
                ? "None"
                : string.Join(", ", DetectiveInvestigationState.CollectedClueIds.OrderBy(value => value));

            statusText.text = $"<b>Detective Test</b>\n" +
                              $"Move Target: {destination.x:0.0}, {destination.y:0.0}, {destination.z:0.0}\n" +
                              $"Moving: {mover.IsMoving}\n" +
                              $"Interact Target: {interactionController.CurrentTargetName}\n" +
                              $"Interaction Prompt: {interactionController.InteractionPrompt}\n" +
                              $"Awaiting Confirm: {interactionController.IsAwaitingConfirmation}\n" +
                              $"Last Interaction: {interactionController.LastInteractionMessage}\n" +
                              $"Collected Clues: {collectedClues}\n" +
                              $"Labels: {(showInteractableLabels ? "ON" : "OFF")} (F3)\n" +
                              $"Agent: {agentState}";
        }

        private void ApplyLabelVisibility()
        {
            TestInteractable[] interactables = FindObjectsByType<TestInteractable>(FindObjectsSortMode.None);
            foreach (TestInteractable interactable in interactables)
            {
                interactable.SetDebugLabelVisible(showInteractableLabels);
            }
        }
    }
}