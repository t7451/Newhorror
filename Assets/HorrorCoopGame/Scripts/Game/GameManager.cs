using HorrorCoopGame.Player;
using HorrorCoopGame.Vehicle;
using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Game
{
    /// <summary>
    /// Server-authoritative game state coordinator. Tracks the current
    /// <see cref="GamePhase"/>, listens for win/loss conditions, and exposes
    /// hooks for the HUD and end-screen UI.
    ///
    /// Victory: any registered <see cref="VehicleRepair"/> reports
    /// <see cref="VehicleRepair.IsRepaired"/> true.
    /// Defeat:  every connected player's <see cref="PlayerStats.Health"/>
    ///          is depleted (0 or below) and at least one player has connected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References (optional, auto-discovered if null)")]
        [SerializeField] private VehicleRepair vehicleRepair;

        [Header("Server Tuning")]
        [Tooltip("How often (in seconds) the server polls win/loss conditions.")]
        [SerializeField] private float conditionCheckInterval = 0.5f;

        [Tooltip("Seconds players are given to connect after host start before defeat checks engage.")]
        [SerializeField] private float defeatGracePeriod = 3f;

        public readonly NetworkVariable<GamePhase> Phase = new(
            GamePhase.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Server time at which the current round began. -1 if not started.
        /// </summary>
        public readonly NetworkVariable<float> RoundStartTime = new(
            -1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float nextConditionCheckTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Phase.Value = GamePhase.Lobby;
                RoundStartTime.Value = -1f;
            }
        }

        /// <summary>
        /// Server entry point: transition from Lobby to Playing. Safe to call
        /// from any client via the matching ServerRpc.
        /// </summary>
        public void StartGame()
        {
            if (!IsServer)
            {
                StartGameServerRpc();
                return;
            }

            if (Phase.Value != GamePhase.Lobby)
            {
                return;
            }

            Phase.Value = GamePhase.Playing;
            RoundStartTime.Value = (float)NetworkManager.ServerTime.Time;
        }

        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc()
        {
            StartGame();
        }

        /// <summary>
        /// Server entry point: return to Lobby (e.g. after a victory/defeat screen).
        /// </summary>
        public void ResetToLobby()
        {
            if (!IsServer)
            {
                ResetToLobbyServerRpc();
                return;
            }

            Phase.Value = GamePhase.Lobby;
            RoundStartTime.Value = -1f;
        }

        [ServerRpc(RequireOwnership = false)]
        private void ResetToLobbyServerRpc()
        {
            ResetToLobby();
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            if (Phase.Value != GamePhase.Playing)
            {
                return;
            }

            if (Time.unscaledTime < nextConditionCheckTime)
            {
                return;
            }

            nextConditionCheckTime = Time.unscaledTime + Mathf.Max(0.05f, conditionCheckInterval);
            EvaluateEndConditions();
        }

        private void EvaluateEndConditions()
        {
            if (HasVictoryCondition())
            {
                Phase.Value = GamePhase.Victory;
                return;
            }

            if (HasDefeatCondition())
            {
                Phase.Value = GamePhase.Defeat;
            }
        }

        private bool HasVictoryCondition()
        {
            VehicleRepair repair = vehicleRepair;
            if (repair == null)
            {
                // Late-bind the repair point so designers can spawn it after the manager.
                repair = FindObjectOfType<VehicleRepair>();
                vehicleRepair = repair;
            }

            return repair != null && repair.IsRepaired.Value;
        }

        private bool HasDefeatCondition()
        {
            if (NetworkManager == null)
            {
                return false;
            }

            if (RoundStartTime.Value < 0f)
            {
                return false;
            }

            if ((float)NetworkManager.ServerTime.Time - RoundStartTime.Value < defeatGracePeriod)
            {
                return false;
            }

            int totalPlayers = 0;
            int downedPlayers = 0;

            foreach (var kvp in NetworkManager.ConnectedClients)
            {
                NetworkObject playerObject = kvp.Value.PlayerObject;
                if (playerObject == null)
                {
                    continue;
                }

                if (!playerObject.TryGetComponent(out PlayerStats stats))
                {
                    continue;
                }

                totalPlayers++;
                if (stats.Health.Value <= 0f)
                {
                    downedPlayers++;
                }
            }

            return totalPlayers > 0 && downedPlayers >= totalPlayers;
        }
    }
}
