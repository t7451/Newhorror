using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.Networking
{
    public sealed class NetworkMenuUI : MonoBehaviour
    {
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TextMeshProUGUI statusText;

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
        }

        private async void OnHostClicked()
        {
            if (statusText == null || RelayManager.Instance == null)
            {
                return;
            }

            statusText.text = "Creating relay room...";

            string joinCode = await RelayManager.Instance.CreateRelayAsync();
            if (string.IsNullOrEmpty(joinCode))
            {
                statusText.text = "Failed to create room.";
                return;
            }

            GUIUtility.systemCopyBuffer = joinCode;
            statusText.text = $"Room created. Code: {joinCode}";
            gameObject.SetActive(false);
        }

        private async void OnJoinClicked()
        {
            if (statusText == null || RelayManager.Instance == null)
            {
                return;
            }

            string joinCode = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                statusText.text = "Enter a join code.";
                return;
            }

            statusText.text = "Joining relay room...";

            bool joined = await RelayManager.Instance.JoinRelayAsync(joinCode);
            if (!joined)
            {
                statusText.text = "Join failed.";
                return;
            }

            statusText.text = "Connected.";
            gameObject.SetActive(false);
        }
    }
}
