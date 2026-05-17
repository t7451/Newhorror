using TMPro;
using HorrorCoopGame.Game;
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

        [Header("Optimization")]
        [Tooltip("Seconds between target raycasts. ~12 Hz is plenty for prompts and saves significant CPU on mobile/WebGL.")]
        [SerializeField] private float targetUpdateInterval = 0.08f;

        private readonly Collider[] overlapBuffer = new Collider[8];
        private IInteractable currentTarget;
        private bool interactPressed;
        private float nextTargetUpdateTime;

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

            // Throttle raycasts/overlap queries; prompt UI is updated every frame
            // off the cached target so it stays responsive without per-frame physics queries.
            if (!IsRoundPlaying())
            {
                currentTarget = null;
                interactPressed = false;
                UpdatePromptUi();
                return;
            }

            if (Time.time >= nextTargetUpdateTime)
            {
                nextTargetUpdateTime = Time.time + Mathf.Max(0f, targetUpdateInterval);
                UpdateTarget();
            }

            UpdatePromptUi();

            if (interactPressed)
            {
                interactPressed = false;
                // Refresh target immediately on press so a freshly-aimed item
                // can be interacted with even if the throttled tick is pending.
                UpdateTarget();
                if (currentTarget != null)
                {
                    currentTarget.Interact(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        private static bool IsRoundPlaying()
        {
            return GameManager.Instance == null || GameManager.Instance.Phase.Value == GamePhase.Playing;
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
