using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace HorrorCoopGame.Networking
{
    [RequireComponent(typeof(NetworkManager))]
    [DisallowMultipleComponent]
    public sealed class NetworkWebSocketSetup : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        private void Reset()
        {
            networkManager = GetComponent<NetworkManager>();
        }

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = GetComponent<NetworkManager>();
            }

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is required on this GameObject.");
                return;
            }

            UnityTransport transport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("UnityTransport is required for Relay + WebSocket support.");
                return;
            }

            transport.UseWebSockets = true;
        }
    }
}
