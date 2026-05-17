using HorrorCoopGame.Environment;
using HorrorCoopGame.Game;
using HorrorCoopGame.Player;
using HorrorCoopGame.Vehicle;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HorrorCoopGame.UI
{
    /// <summary>
    /// In-world HUD shown while the player is connected. Mirrors the local
    /// player's <see cref="PlayerStats"/>, the world <see cref="DayNightCycle"/>,
    /// the <see cref="VehicleRepair"/> progress, and the <see cref="GameManager"/>
    /// phase. Also owns the inventory and pause panel toggles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHUD : MonoBehaviour
    {
        [Header("Stat Bars")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image staminaBarFill;
        [SerializeField] private Image sanityBarFill;
        [SerializeField] private CanvasGroup sanityVignette;

        [Header("Status Readouts")]
        [SerializeField] private TextMeshProUGUI repairProgressText;
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private TextMeshProUGUI phaseBannerText;
        [SerializeField] private CanvasGroup phaseBannerGroup;

        [Header("Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject endScreenPanel;
        [SerializeField] private TextMeshProUGUI endScreenText;

        [Header("Tuning")]
        [Tooltip("Multiplier (1 / dayLengthSeconds * 24h) for converting normalized time to a clock string.")]
        [SerializeField] private bool show24HourClock = true;

        private PlayerStats trackedStats;
        private VehicleRepair trackedRepair;
        private DayNightCycle trackedCycle;
        private GameManager trackedGame;

        private bool inventoryVisible;
        private bool paused;

        private void OnEnable()
        {
            HidePanel(inventoryPanel);
            HidePanel(pausePanel);
            HidePanel(endScreenPanel);
            SetBannerVisible(false);
        }

        private void Update()
        {
            EnsureBindings();

            UpdateStatBars();
            UpdateRepairText();
            UpdateClockText();
            UpdatePhaseBanner();
        }

        // ---------- Input hooks (wire from PlayerInput "Send Messages" / UI buttons) ----------

        /// <summary>Toggle the inventory panel. Bound to the <c>Inventory</c> action.</summary>
        public void OnInventory(InputValue value)
        {
            if (value.isPressed)
            {
                ToggleInventory();
            }
        }

        /// <summary>Toggle the pause panel. Bound to the <c>Pause</c> action.</summary>
        public void OnPause(InputValue value)
        {
            if (value.isPressed)
            {
                TogglePause();
            }
        }

        // ---------- Public UI Button hooks ----------

        public void ToggleInventory()
        {
            inventoryVisible = !inventoryVisible;
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(inventoryVisible);
            }
        }

        public void TogglePause()
        {
            paused = !paused;
            if (pausePanel != null)
            {
                pausePanel.SetActive(paused);
            }
        }

        /// <summary>Disconnects the local client and returns the UI to lobby state.</summary>
        public void LeaveSession()
        {
            paused = false;
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        // ---------- Internals ----------

        private void EnsureBindings()
        {
            if (trackedStats == null)
            {
                trackedStats = FindLocalPlayerStats();
            }

            if (trackedRepair == null)
            {
                trackedRepair = FindObjectOfType<VehicleRepair>();
            }

            if (trackedCycle == null)
            {
                trackedCycle = FindObjectOfType<DayNightCycle>();
            }

            if (trackedGame == null)
            {
                trackedGame = GameManager.Instance;
            }
        }

        private static PlayerStats FindLocalPlayerStats()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            {
                return null;
            }

            NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObject == null)
            {
                return null;
            }

            return playerObject.GetComponent<PlayerStats>();
        }

        private void UpdateStatBars()
        {
            if (trackedStats == null)
            {
                return;
            }

            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = trackedStats.Health.Value / 100f;
            }

            if (staminaBarFill != null)
            {
                staminaBarFill.fillAmount = trackedStats.Stamina.Value / 100f;
            }

            if (sanityBarFill != null)
            {
                sanityBarFill.fillAmount = trackedStats.Sanity.Value / 100f;
            }

            if (sanityVignette != null)
            {
                sanityVignette.alpha = 1f - (trackedStats.Sanity.Value / 100f);
            }
        }

        private void UpdateRepairText()
        {
            if (repairProgressText == null)
            {
                return;
            }

            if (trackedRepair == null)
            {
                repairProgressText.text = string.Empty;
                return;
            }

            repairProgressText.text = trackedRepair.IsRepaired.Value
                ? "Vehicle ready — escape!"
                : $"Vehicle parts: {trackedRepair.InstalledCount.Value}/3";
        }

        private void UpdateClockText()
        {
            if (clockText == null)
            {
                return;
            }

            if (trackedCycle == null)
            {
                clockText.text = string.Empty;
                return;
            }

            float normalized = Mathf.Repeat(trackedCycle.TimeOfDay.Value, 1f);
            float totalMinutes = normalized * 24f * 60f;
            int hours = Mathf.FloorToInt(totalMinutes / 60f);
            int minutes = Mathf.FloorToInt(totalMinutes % 60f);

            if (show24HourClock)
            {
                clockText.text = $"{hours:00}:{minutes:00}";
            }
            else
            {
                int hour12 = hours % 12;
                if (hour12 == 0)
                {
                    hour12 = 12;
                }
                string suffix = hours < 12 ? "AM" : "PM";
                clockText.text = $"{hour12}:{minutes:00} {suffix}";
            }
        }

        private void UpdatePhaseBanner()
        {
            if (trackedGame == null)
            {
                SetBannerVisible(false);
                HidePanel(endScreenPanel);
                return;
            }

            GamePhase phase = trackedGame.Phase.Value;
            switch (phase)
            {
                case GamePhase.Lobby:
                    SetBannerText("Waiting to start...");
                    SetBannerVisible(true);
                    HidePanel(endScreenPanel);
                    break;
                case GamePhase.Playing:
                    SetBannerVisible(false);
                    HidePanel(endScreenPanel);
                    break;
                case GamePhase.Victory:
                    SetBannerVisible(false);
                    ShowEndScreen("You escaped.");
                    break;
                case GamePhase.Defeat:
                    SetBannerVisible(false);
                    ShowEndScreen("You did not make it.");
                    break;
            }
        }

        private void SetBannerText(string text)
        {
            if (phaseBannerText != null)
            {
                phaseBannerText.text = text;
            }
        }

        private void SetBannerVisible(bool visible)
        {
            if (phaseBannerGroup != null)
            {
                phaseBannerGroup.alpha = visible ? 1f : 0f;
                phaseBannerGroup.blocksRaycasts = visible;
                phaseBannerGroup.interactable = visible;
            }
            else if (phaseBannerText != null)
            {
                phaseBannerText.gameObject.SetActive(visible);
            }
        }

        private void ShowEndScreen(string message)
        {
            if (endScreenText != null)
            {
                endScreenText.text = message;
            }

            if (endScreenPanel != null && !endScreenPanel.activeSelf)
            {
                endScreenPanel.SetActive(true);
            }
        }

        private static void HidePanel(GameObject panel)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }
        }
    }
}
