using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace HorrorCoopGame.Networking
{
    [DisallowMultipleComponent]
    public sealed class RelayManager : MonoBehaviour
    {
        public enum RelayConnectionState
        {
            Offline,
            Initializing,
            Ready,
            CreatingHost,
            Joining,
            Connected,
            Failed
        }

        public static RelayManager Instance { get; private set; }

        [SerializeField] private int maxConnections = 4;
        [SerializeField] private NetworkManager networkManager;

        public event Action<RelayConnectionState, string> ConnectionStateChanged;
        public event Action<int, int> PlayerCountChanged;

        public RelayConnectionState CurrentState { get; private set; } = RelayConnectionState.Offline;
        public string CurrentJoinCode { get; private set; } = string.Empty;
        public string StatusMessage { get; private set; } = "Offline";
        public int ConnectedPlayerCount { get; private set; }
        public int MaxPlayerCount => Mathf.Max(1, maxConnections + 1);

        private bool isInitialized;
        private bool callbacksRegistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (networkManager == null)
            {
                networkManager = NetworkManager.Singleton;
            }

            RegisterNetworkCallbacks();
        }

        private async void Start()
        {
            if (await InitializeServicesAsync() && CurrentState == RelayConnectionState.Initializing)
            {
                SetState(RelayConnectionState.Ready, "Ready to host or join.");
            }
        }

        public async Task<string> CreateRelayAsync()
        {
            if (IsBusyOrConnected())
            {
                return string.Empty;
            }

            SetState(RelayConnectionState.CreatingHost, "Creating relay room...");

            if (!await InitializeServicesAsync())
            {
                SetState(RelayConnectionState.Failed, "Unity Services initialization failed.");
                return string.Empty;
            }

            SetState(RelayConnectionState.CreatingHost, "Creating relay room...");

            if (!EnsureNetworkManager())
            {
                SetState(RelayConnectionState.Failed, "NetworkManager reference is missing.");
                return string.Empty;
            }

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                UnityTransport transport = GetTransportOrLogError();
                if (transport == null)
                {
                    SetState(RelayConnectionState.Failed, "UnityTransport is required.");
                    return string.Empty;
                }

                transport.UseWebSockets = true;
                transport.SetRelayServerData(new RelayServerData(allocation, "wss"));

                if (!networkManager.StartHost())
                {
                    Debug.LogError("Failed to start host.");
                    SetState(RelayConnectionState.Failed, "Failed to start host.");
                    return string.Empty;
                }

                CurrentJoinCode = joinCode;
                UpdatePlayerCount();
                SetState(RelayConnectionState.Connected, $"Room created. Code: {joinCode}");
                return joinCode;
            }
            catch (Exception exception)
            {
                Debug.LogError($"CreateRelayAsync failed: {exception}");
                SetState(RelayConnectionState.Failed, "Failed to create room.");
                return string.Empty;
            }
        }

        public async Task<bool> JoinRelayAsync(string joinCode)
        {
            if (IsBusyOrConnected())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogWarning("Join code was empty.");
                SetState(RelayConnectionState.Failed, "Enter a join code.");
                return false;
            }

            SetState(RelayConnectionState.Joining, "Joining relay room...");

            if (!await InitializeServicesAsync())
            {
                SetState(RelayConnectionState.Failed, "Unity Services initialization failed.");
                return false;
            }

            SetState(RelayConnectionState.Joining, "Joining relay room...");

            if (!EnsureNetworkManager())
            {
                SetState(RelayConnectionState.Failed, "NetworkManager reference is missing.");
                return false;
            }

            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

                UnityTransport transport = GetTransportOrLogError();
                if (transport == null)
                {
                    SetState(RelayConnectionState.Failed, "UnityTransport is required.");
                    return false;
                }

                transport.UseWebSockets = true;
                transport.SetRelayServerData(new RelayServerData(joinAllocation, "wss"));

                bool started = networkManager.StartClient();
                if (!started)
                {
                    SetState(RelayConnectionState.Failed, "Failed to start client.");
                    return false;
                }

                CurrentJoinCode = joinCode.Trim().ToUpperInvariant();
                SetState(RelayConnectionState.Connected, "Connected.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"JoinRelayAsync failed for code '{joinCode}': {exception}");
                SetState(RelayConnectionState.Failed, "Join failed.");
                return false;
            }
        }

        public void Disconnect()
        {
            if (EnsureNetworkManager() && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            CurrentJoinCode = string.Empty;
            ConnectedPlayerCount = 0;
            PlayerCountChanged?.Invoke(ConnectedPlayerCount, MaxPlayerCount);
            SetState(isInitialized ? RelayConnectionState.Ready : RelayConnectionState.Offline, isInitialized ? "Ready to host or join." : "Offline");
        }

        private UnityTransport GetTransportOrLogError()
        {
            if (!EnsureNetworkManager())
            {
                return null;
            }

            UnityTransport transport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("NetworkConfig.NetworkTransport must be UnityTransport.");
            }

            return transport;
        }

        private async Task<bool> InitializeServicesAsync()
        {
            if (isInitialized)
            {
                return true;
            }

            SetState(RelayConnectionState.Initializing, "Initializing Unity Services...");

            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                isInitialized = true;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Unity Services initialization failed: {exception}");
                SetState(RelayConnectionState.Failed, "Unity Services initialization failed.");
                return false;
            }
        }

        private bool EnsureNetworkManager()
        {
            if (networkManager == null)
            {
                networkManager = NetworkManager.Singleton;
                RegisterNetworkCallbacks();
            }

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager reference is missing.");
                return false;
            }

            return true;
        }

        private bool IsBusyOrConnected()
        {
            return CurrentState == RelayConnectionState.CreatingHost ||
                   CurrentState == RelayConnectionState.Joining ||
                   (networkManager != null && networkManager.IsListening);
        }

        private void RegisterNetworkCallbacks()
        {
            if (callbacksRegistered || networkManager == null)
            {
                return;
            }

            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            callbacksRegistered = true;
        }

        private void OnDestroy()
        {
            if (callbacksRegistered && networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                callbacksRegistered = false;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnClientConnected(ulong _)
        {
            UpdatePlayerCount();

            if (CurrentState == RelayConnectionState.Joining || CurrentState == RelayConnectionState.CreatingHost)
            {
                SetState(RelayConnectionState.Connected, string.IsNullOrEmpty(CurrentJoinCode) ? "Connected." : $"Connected. Code: {CurrentJoinCode}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            UpdatePlayerCount();

            if (networkManager != null && clientId == networkManager.LocalClientId && !networkManager.IsHost)
            {
                CurrentJoinCode = string.Empty;
                SetState(isInitialized ? RelayConnectionState.Ready : RelayConnectionState.Offline, "Disconnected.");
            }
        }

        private void UpdatePlayerCount()
        {
            ConnectedPlayerCount = networkManager != null && networkManager.IsListening
                ? networkManager.ConnectedClientsIds.Count
                : 0;
            PlayerCountChanged?.Invoke(ConnectedPlayerCount, MaxPlayerCount);
        }

        private void SetState(RelayConnectionState state, string message)
        {
            CurrentState = state;
            StatusMessage = message;
            ConnectionStateChanged?.Invoke(CurrentState, StatusMessage);
        }
    }
}
