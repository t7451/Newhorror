using HorrorCoopGame.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorCoopGame.Building
{
    /// <summary>
    /// Owner-only build mode controller with snap-to-grid ghost placement.
    /// Confirmation fires a ServerRpc which pulls a pooled NetworkObject.
    /// </summary>
    public sealed class BuildingManager : NetworkBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private BuildableData currentBuildable;
        [SerializeField] private float placementDistance = 4f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private InventorySystem inventory;

        private GameObject ghostInstance;
        private bool isBuildMode;
        private float ghostYaw;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            DestroyGhost();
        }

        public void OnBuildMode(InputValue value)
        {
            if (!value.isPressed)
            {
                return;
            }

            isBuildMode = !isBuildMode;
            if (isBuildMode)
            {
                CreateGhost();
            }
            else
            {
                DestroyGhost();
            }
        }

        /// <summary>
        /// Invoked by the on-screen "Confirm Build" button or keyboard binding.
        /// </summary>
        public void OnConfirmBuild(InputValue value)
        {
            if (!value.isPressed || !isBuildMode || currentBuildable == null)
            {
                return;
            }

            if (!TryGetPlacementPose(out Vector3 position, out Quaternion rotation))
            {
                return;
            }

            RequestPlaceServerRpc(position, rotation);
        }

        private void Update()
        {
            if (!isBuildMode || ghostInstance == null || currentBuildable == null)
            {
                return;
            }

            if (!TryGetPlacementPose(out Vector3 position, out Quaternion rotation))
            {
                ghostInstance.SetActive(false);
                return;
            }

            ghostInstance.SetActive(true);
            ghostInstance.transform.SetPositionAndRotation(position, rotation);
        }

        public void RotateGhost(float deltaDegrees)
        {
            if (currentBuildable == null)
            {
                return;
            }

            ghostYaw += deltaDegrees;
            ghostYaw = Mathf.Round(ghostYaw / currentBuildable.yawSnap) * currentBuildable.yawSnap;
        }

        private bool TryGetPlacementPose(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;

            if (cameraTransform == null || currentBuildable == null)
            {
                return false;
            }

            Vector3 origin = cameraTransform.position;
            Vector3 forward = cameraTransform.forward;

            Vector3 rawPoint;
            if (Physics.Raycast(origin, forward, out RaycastHit hit, placementDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                rawPoint = hit.point;
            }
            else
            {
                rawPoint = origin + (forward * placementDistance);
            }

            float grid = Mathf.Max(0.05f, currentBuildable.gridSize);
            position = new Vector3(
                Mathf.Round(rawPoint.x / grid) * grid,
                Mathf.Round(rawPoint.y / grid) * grid,
                Mathf.Round(rawPoint.z / grid) * grid);

            rotation = Quaternion.Euler(0f, ghostYaw, 0f);
            return true;
        }

        private void CreateGhost()
        {
            if (currentBuildable == null || currentBuildable.ghostPrefab == null)
            {
                return;
            }

            DestroyGhost();
            ghostInstance = Instantiate(currentBuildable.ghostPrefab);
            ghostInstance.SetActive(false);
        }

        private void DestroyGhost()
        {
            if (ghostInstance != null)
            {
                Destroy(ghostInstance);
                ghostInstance = null;
            }
        }

        [ServerRpc]
        private void RequestPlaceServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            if (currentBuildable == null || currentBuildable.prefab == null)
            {
                return;
            }

            ulong senderId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client) ||
                client.PlayerObject == null)
            {
                return;
            }

            InventorySystem requesterInventory = client.PlayerObject.GetComponent<InventorySystem>();
            if (requesterInventory == null)
            {
                return;
            }

            if (!requesterInventory.HasItemQuantity(currentBuildable.requiredItemName, currentBuildable.requiredItemAmount))
            {
                return;
            }

            NetworkObject spawned = NetworkedPoolManager.Instance != null
                ? NetworkedPoolManager.Instance.SpawnFromPool(currentBuildable.prefab, position, rotation)
                : null;

            if (spawned == null)
            {
                return;
            }

            requesterInventory.RemoveItemQuantity(currentBuildable.requiredItemName, currentBuildable.requiredItemAmount);
        }
    }
}
