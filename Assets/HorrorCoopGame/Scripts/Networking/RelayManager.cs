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
        public static RelayManager Instance { get; private set; }

        [SerializeField] private int maxConnections = 4;
        [SerializeField] private NetworkManager networkManager;

        private bool isInitialized;

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
        }

        private async void Start()
        {
            await InitializeServicesAsync();
        }

        public async Task<string> CreateRelayAsync()
        {
            if (!await InitializeServicesAsync())
            {
                return string.Empty;
            }

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager reference is missing.");
                return string.Empty;
            }

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                UnityTransport transport = GetTransportOrLogError();
                if (transport == null)
                {
                    return string.Empty;
                }

                transport.UseWebSockets = true;
                transport.SetRelayServerData(new RelayServerData(allocation, "wss"));

                if (!networkManager.StartHost())
                {
                    Debug.LogError("Failed to start host.");
                    return string.Empty;
                }

                return joinCode;
            }
            catch (Exception exception)
            {
                Debug.LogError($"CreateRelayAsync failed: {exception}");
                return string.Empty;
            }
        }

        public async Task<bool> JoinRelayAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogWarning("Join code was empty.");
                return false;
            }

            if (!await InitializeServicesAsync())
            {
                return false;
            }

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager reference is missing.");
                return false;
            }

            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

                UnityTransport transport = GetTransportOrLogError();
                if (transport == null)
                {
                    return false;
                }

                transport.UseWebSockets = true;
                transport.SetRelayServerData(new RelayServerData(joinAllocation, "wss"));

                return networkManager.StartClient();
            }
            catch (Exception exception)
            {
                Debug.LogError($"JoinRelayAsync failed for code '{joinCode}': {exception}");
                return false;
            }
        }

        private UnityTransport GetTransportOrLogError()
        {
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
                return false;
            }
        }
    }
}
