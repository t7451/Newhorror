using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorCoopGame.Interaction
{
    /// <summary>
    /// Owner-local interaction targeting. Uses an overlap sphere
    /// at the camera forward point so mobile aiming is forgiving.
    /// </summary>
    public sealed class PlayerInteractionRaycast : NetworkBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private float aimAssistRadius = 0.4f;
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private TextMeshProUGUI promptText;

        private readonly Collider[] overlapBuffer = new Collider[8];
        private IInteractable currentTarget;
        private bool interactPressed;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed)
            {
                interactPressed = true;
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            UpdateTarget();
            UpdatePromptUi();

            if (interactPressed)
            {
                interactPressed = false;
                if (currentTarget != null)
                {
                    currentTarget.Interact(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        private void UpdateTarget()
        {
            currentTarget = null;

            if (cameraTransform == null)
            {
                return;
            }

            Vector3 origin = cameraTransform.position;
            Vector3 forward = cameraTransform.forward;

            // Prioritize a direct raycast hit
            if (Physics.Raycast(origin, forward, out RaycastHit hit, interactRange, interactableLayer, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent(out IInteractable directHit))
                {
                    currentTarget = directHit;
                    return;
                }
            }

            // Fallback: overlap sphere at the look point for mobile leniency
            Vector3 sphereCenter = origin + (forward * interactRange);
            int count = Physics.OverlapSphereNonAlloc(sphereCenter, aimAssistRadius, overlapBuffer, interactableLayer, QueryTriggerInteraction.Collide);

            float bestDot = -1f;
            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null || !col.TryGetComponent(out IInteractable candidate))
                {
                    continue;
                }

                Vector3 toCandidate = (col.transform.position - origin).normalized;
                float dot = Vector3.Dot(forward, toCandidate);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    currentTarget = candidate;
                }
            }
        }

        private void UpdatePromptUi()
        {
            if (promptText == null)
            {
                return;
            }

            promptText.text = currentTarget != null ? currentTarget.GetInteractPrompt() : string.Empty;
        }
    }
}
