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

        [Header("Touch UI")]
        [SerializeField] private Canvas touchControlsCanvas;

        private CharacterController characterController;
        private PlayerInput playerInput;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool sprintPressed;
        private bool jumpQueued;

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
            Vector2 scaledLook = lookInput * sensitivity;

            pitch = Mathf.Clamp(pitch - scaledLook.y, minPitch, maxPitch);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            transform.Rotate(Vector3.up * scaledLook.x);
        }

        private void ApplyMovement()
        {
            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            float speed = sprintPressed ? sprintSpeed : walkSpeed;
            Vector3 planarMove = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;
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
