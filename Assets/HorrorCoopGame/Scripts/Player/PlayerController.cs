using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorCoopGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float gravity = -19.62f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseLookSensitivity = 0.12f;
        [SerializeField] private float touchLookSensitivity = 0.08f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Smoothing")]
        [Tooltip("Seconds of exponential smoothing applied to move input. Removes touch-stick jitter and per-frame input noise.")]
        [SerializeField] private float moveSmoothTime = 0.08f;
        [Tooltip("Seconds of exponential smoothing applied to look input on mouse. Lower = snappier.")]
        [SerializeField] private float mouseLookSmoothTime = 0.02f;
        [Tooltip("Seconds of exponential smoothing applied to look input on touch. Higher = smoother but slightly laggier.")]
        [SerializeField] private float touchLookSmoothTime = 0.06f;
        [Tooltip("Seconds of smoothing for camera pitch/yaw. 0 to disable. Smooths jittery frames on WebGL.")]
        [SerializeField] private float cameraSmoothTime = 0.04f;

        [Header("Touch UI")]
        [SerializeField] private Canvas touchControlsCanvas;

        private CharacterController characterController;
        private PlayerInput playerInput;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool sprintPressed;
        private bool jumpQueued;

        // Smoothing state
        private Vector2 smoothedMoveInput;
        private Vector2 moveInputVelocity;
        private Vector2 smoothedLookInput;
        private Vector2 lookInputVelocity;
        private float displayedPitch;
        private float pitchVelocity;
        private float displayedYaw;
        private float yawVelocity;

        private float verticalVelocity;
        private float pitch;
        private bool isMobile;

        public override void OnNetworkSpawn()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            if (!IsOwner)
            {
                if (cameraPivot != null)
                {
                    cameraPivot.gameObject.SetActive(false);
                }

                if (playerInput != null)
                {
                    playerInput.enabled = false;
                }

                enabled = false;
                return;
            }

            isMobile = Application.isMobilePlatform;

            if (touchControlsCanvas != null)
            {
                touchControlsCanvas.enabled = isMobile;
            }

            if (!isMobile)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            displayedYaw = transform.eulerAngles.y;
            displayedPitch = pitch;
        }

        public override void OnNetworkDespawn()
        {
            if (!isMobile)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        public void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        public void OnSprint(InputValue value)
        {
            sprintPressed = value.isPressed;
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                jumpQueued = true;
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            ApplyLook();
            ApplyMovement();
        }

        private void ApplyLook()
        {
            float sensitivity = isMobile ? touchLookSensitivity : mouseLookSensitivity;
            float lookSmoothTime = isMobile ? touchLookSmoothTime : mouseLookSmoothTime;

            // Low-pass filter the raw look input. Smooths jittery touch deltas
            // and frame-spike spikes on WebGL without adding perceptible lag.
            smoothedLookInput = Vector2.SmoothDamp(
                smoothedLookInput,
                lookInput,
                ref lookInputVelocity,
                Mathf.Max(0f, lookSmoothTime),
                Mathf.Infinity,
                Time.deltaTime);

            Vector2 scaledLook = smoothedLookInput * sensitivity;

            pitch = Mathf.Clamp(pitch - scaledLook.y, minPitch, maxPitch);

            // Drive yaw via an internal accumulator and a smoothed displayed
            // yaw so character rotation interpolates cleanly between frames.
            float targetYaw = transform.eulerAngles.y + scaledLook.x;

            if (cameraSmoothTime > 0f)
            {
                displayedPitch = Mathf.SmoothDampAngle(displayedPitch, pitch, ref pitchVelocity, cameraSmoothTime, Mathf.Infinity, Time.deltaTime);
                displayedYaw = Mathf.SmoothDampAngle(displayedYaw, targetYaw, ref yawVelocity, cameraSmoothTime, Mathf.Infinity, Time.deltaTime);
            }
            else
            {
                displayedPitch = pitch;
                displayedYaw = targetYaw;
            }

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(displayedPitch, 0f, 0f);
            }

            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(currentEuler.x, displayedYaw, currentEuler.z);
        }

        private void ApplyMovement()
        {
            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            // Smooth raw move input so direction changes ease in/out instead
            // of snapping. Helps both touch-stick precision and WebGL frame jitter.
            smoothedMoveInput = Vector2.SmoothDamp(
                smoothedMoveInput,
                moveInput,
                ref moveInputVelocity,
                Mathf.Max(0f, moveSmoothTime),
                Mathf.Infinity,
                Time.deltaTime);

            float speed = sprintPressed ? sprintSpeed : walkSpeed;
            Vector3 planarMove = (transform.right * smoothedMoveInput.x + transform.forward * smoothedMoveInput.y) * speed;
            characterController.Move(planarMove * Time.deltaTime);

            if (jumpQueued && grounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            jumpQueued = false;
            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }
    }
}
