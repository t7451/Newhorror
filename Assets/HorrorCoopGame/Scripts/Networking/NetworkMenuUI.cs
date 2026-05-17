using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.Networking
{
    public sealed class NetworkMenuUI : MonoBehaviour
    {
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI joinCodeText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private bool hideMenuWhenConnected = true;

        private void Awake()
        {
            if (hostButton != null)
            {
                hostButton.onClick.AddListener(OnHostClicked);
            }

            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinClicked);
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
            }
        }

        private void OnEnable()
        {
            if (RelayManager.Instance == null)
            {
                SetStatus("Networking backend not found.");
                SetControlsInteractable(false);
                return;
            }

            RelayManager.Instance.ConnectionStateChanged += OnConnectionStateChanged;
            RelayManager.Instance.PlayerCountChanged += OnPlayerCountChanged;

            OnConnectionStateChanged(RelayManager.Instance.CurrentState, RelayManager.Instance.StatusMessage);
            OnPlayerCountChanged(RelayManager.Instance.ConnectedPlayerCount, RelayManager.Instance.MaxPlayerCount);
        }

        private void OnDisable()
        {
            if (RelayManager.Instance == null)
            {
                return;
            }

            RelayManager.Instance.ConnectionStateChanged -= OnConnectionStateChanged;
            RelayManager.Instance.PlayerCountChanged -= OnPlayerCountChanged;
        }

        private void OnDestroy()
        {
            if (hostButton != null)
            {
                hostButton.onClick.RemoveListener(OnHostClicked);
            }

            if (joinButton != null)
            {
                joinButton.onClick.RemoveListener(OnJoinClicked);
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
            }
        }

        private async void OnHostClicked()
        {
            if (RelayManager.Instance == null)
            {
                return;
            }

            string joinCode = await RelayManager.Instance.CreateRelayAsync();
            if (!string.IsNullOrEmpty(joinCode))
            {
                GUIUtility.systemCopyBuffer = joinCode;
            }
        }

        private async void OnJoinClicked()
        {
            if (RelayManager.Instance == null)
            {
                return;
            }

            string joinCode = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                SetStatus("Enter a join code.");
                return;
            }

            await RelayManager.Instance.JoinRelayAsync(joinCode);
        }

        private void OnDisconnectClicked()
        {
            if (RelayManager.Instance == null)
            {
                return;
            }

            RelayManager.Instance.Disconnect();
        }

        private void OnConnectionStateChanged(RelayManager.RelayConnectionState state, string message)
        {
            bool busy = state == RelayManager.RelayConnectionState.Initializing ||
                        state == RelayManager.RelayConnectionState.CreatingHost ||
                        state == RelayManager.RelayConnectionState.Joining;
            bool connected = state == RelayManager.RelayConnectionState.Connected;

            SetStatus(message);
            SetControlsInteractable(!busy && !connected);

            if (disconnectButton != null)
            {
                disconnectButton.gameObject.SetActive(connected);
                disconnectButton.interactable = connected;
            }

            if (joinCodeText != null)
            {
                string joinCode = RelayManager.Instance != null ? RelayManager.Instance.CurrentJoinCode : string.Empty;
                joinCodeText.text = string.IsNullOrEmpty(joinCode) ? string.Empty : $"Join Code: {joinCode}";
            }

            if (hideMenuWhenConnected && connected)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnPlayerCountChanged(int connectedPlayers, int maxPlayers)
        {
            if (playerCountText != null)
            {
                playerCountText.text = maxPlayers > 0 ? $"Players: {connectedPlayers}/{maxPlayers}" : string.Empty;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (hostButton != null)
            {
                hostButton.interactable = interactable;
            }

            if (joinButton != null)
            {
                joinButton.interactable = interactable;
            }

            if (joinCodeInput != null)
            {
                joinCodeInput.interactable = interactable;
            }
        }
    }
}
