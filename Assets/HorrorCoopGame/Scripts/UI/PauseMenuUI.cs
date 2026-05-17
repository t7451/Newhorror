using HorrorCoopGame.Game;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.UI
{
    /// <summary>
    /// Simple pause / lobby panel. Hosts a "Start Game" button (visible only
    /// to the host while the game is in <see cref="GamePhase.Lobby"/>),
    /// a "Resume" button, and a "Leave" button that shuts the local client down.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        private void Update()
        {
            RefreshStartButton();
            RefreshStatus();
        }

        private void RefreshStartButton()
        {
            if (startButton == null)
            {
                return;
            }

            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            bool inLobby = GameManager.Instance != null && GameManager.Instance.Phase.Value == GamePhase.Lobby;

            startButton.gameObject.SetActive(isHost && inLobby);
        }

        private void RefreshStatus()
        {
            if (statusText == null)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                statusText.text = string.Empty;
                return;
            }

            switch (GameManager.Instance.Phase.Value)
            {
                case GamePhase.Lobby:
                    statusText.text = "Waiting in lobby";
                    break;
                case GamePhase.Playing:
                    statusText.text = "Round in progress";
                    break;
                case GamePhase.Victory:
                    statusText.text = "Victory";
                    break;
                case GamePhase.Defeat:
                    statusText.text = "Defeat";
                    break;
            }
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnResumeClicked()
        {
            gameObject.SetActive(false);
        }

        private void OnLeaveClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            gameObject.SetActive(false);
        }
    }
}
