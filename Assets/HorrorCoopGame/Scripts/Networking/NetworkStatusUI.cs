using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.Networking
{
    public sealed class NetworkStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI joinCodeText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private GameObject menuPanel;

        private void Awake()
        {
            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
            }
        }

        private void OnEnable()
        {
            if (RelayManager.Instance == null)
            {
                SetStatus("Offline");
                SetPlayerCount(0, 0);
                SetJoinCode(string.Empty);
                SetDisconnectVisible(false);
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
            if (disconnectButton != null)
            {
                disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
            }
        }

        private void OnConnectionStateChanged(RelayManager.RelayConnectionState state, string message)
        {
            bool connected = state == RelayManager.RelayConnectionState.Connected;

            SetStatus(message);
            SetJoinCode(RelayManager.Instance != null ? RelayManager.Instance.CurrentJoinCode : string.Empty);
            SetDisconnectVisible(connected);
        }

        private void OnPlayerCountChanged(int connectedPlayers, int maxPlayers)
        {
            SetPlayerCount(connectedPlayers, maxPlayers);
        }

        private void OnDisconnectClicked()
        {
            if (RelayManager.Instance != null)
            {
                RelayManager.Instance.Disconnect();
            }

            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetJoinCode(string joinCode)
        {
            if (joinCodeText != null)
            {
                joinCodeText.text = string.IsNullOrEmpty(joinCode) ? string.Empty : $"Join Code: {joinCode}";
            }
        }

        private void SetPlayerCount(int connectedPlayers, int maxPlayers)
        {
            if (playerCountText != null)
            {
                playerCountText.text = maxPlayers > 0 ? $"Players: {connectedPlayers}/{maxPlayers}" : string.Empty;
            }
        }

        private void SetDisconnectVisible(bool visible)
        {
            if (disconnectButton != null)
            {
                disconnectButton.gameObject.SetActive(visible);
                disconnectButton.interactable = visible;
            }
        }
    }
}
