using HorrorCoopGame.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Vehicle
{
    /// <summary>
    /// Networked repair point. Requires three key components installed
    /// (CarBattery, Alternator, SparkPlugs by default) to trigger escape.
    /// </summary>
    public sealed class VehicleRepair : NetworkBehaviour, IInteractable
    {
        [SerializeField]
        private string[] requiredComponents =
        {
            "CarBattery",
            "Alternator",
            "SparkPlugs"
        };

        [SerializeField] private Transform escapeDriveTarget;
        [SerializeField] private float escapeDriveDuration = 6f;

        public NetworkVariable<int> InstalledCount = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsRepaired = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly System.Collections.Generic.HashSet<string> installedServer = new();
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool escapeSequenceStarted;
        private Coroutine escapeRoutine;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ResetRepairStateServer();
            }
        }

        public string GetInteractPrompt()
        {
            if (IsRepaired.Value)
            {
                return "Escape!";
            }

            return $"Repair Vehicle ({InstalledCount.Value}/{requiredComponents.Length})";
        }

        public void Interact(ulong playerNetworkId)
        {
            RequestInstallServerRpc(playerNetworkId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestInstallServerRpc(ulong playerNetworkId)
        {
            if (IsRepaired.Value)
            {
                TryStartEscapeSequenceServer();
                return;
            }

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerNetworkId, out var client) ||
                client.PlayerObject == null)
            {
                return;
            }

            if (!client.PlayerObject.TryGetComponent(out InventorySystem inventory))
            {
                return;
            }

            for (int i = 0; i < requiredComponents.Length; i++)
            {
                string componentName = requiredComponents[i];
                if (installedServer.Contains(componentName))
                {
                    continue;
                }

                if (!inventory.HasItemQuantity(componentName, 1))
                {
                    continue;
                }

                if (!inventory.RemoveItemQuantity(componentName, 1))
                {
                    continue;
                }

                installedServer.Add(componentName);
                InstalledCount.Value = installedServer.Count;

                if (installedServer.Count >= requiredComponents.Length)
                {
                    IsRepaired.Value = true;
                    TryStartEscapeSequenceServer();
                }

                return;
            }
        }

        public void ResetRepairStateServer()
        {
            if (!IsServer)
            {
                return;
            }

            installedServer.Clear();
            InstalledCount.Value = 0;
            IsRepaired.Value = false;
            escapeSequenceStarted = false;

            if (escapeRoutine != null)
            {
                StopCoroutine(escapeRoutine);
                escapeRoutine = null;
            }

            transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (IsSpawned)
            {
                ApplyResetPoseClientRpc(initialPosition, initialRotation);
            }
        }

        private void TryStartEscapeSequenceServer()
        {
            if (!IsServer || escapeSequenceStarted)
            {
                return;
            }

            escapeSequenceStarted = true;
            StartEscapeClientRpc();
        }

        [ClientRpc]
        private void StartEscapeClientRpc()
        {
            if (escapeRoutine != null)
            {
                StopCoroutine(escapeRoutine);
            }

            escapeRoutine = StartCoroutine(PlayEscapeSequence());
        }

        [ClientRpc]
        private void ApplyResetPoseClientRpc(Vector3 position, Quaternion rotation)
        {
            if (escapeRoutine != null)
            {
                StopCoroutine(escapeRoutine);
                escapeRoutine = null;
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        private System.Collections.IEnumerator PlayEscapeSequence()
        {
            if (escapeDriveTarget == null)
            {
                yield break;
            }

            Vector3 start = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < escapeDriveDuration)
            {
                float t = elapsed / escapeDriveDuration;
                transform.position = Vector3.Lerp(start, escapeDriveTarget.position, t);
                transform.rotation = Quaternion.Slerp(startRot, escapeDriveTarget.rotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.SetPositionAndRotation(escapeDriveTarget.position, escapeDriveTarget.rotation);
        }
    }
}
