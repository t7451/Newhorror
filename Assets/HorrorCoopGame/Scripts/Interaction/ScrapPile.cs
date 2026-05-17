using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Interaction
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class ScrapPile : NetworkBehaviour, IInteractable
    {
        [SerializeField] private ItemData resourceItem;
        [SerializeField] private int yieldAmount = 3;

        public string GetInteractPrompt()
        {
            if (resourceItem == null)
            {
                return "Collect";
            }

            return $"Collect {resourceItem.itemName} (+{yieldAmount})";
        }

        public void Interact(ulong playerNetworkId)
        {
            RequestCollectServerRpc(playerNetworkId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestCollectServerRpc(ulong playerNetworkId)
        {
            if (resourceItem == null)
            {
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

            if (!inventory.AddItem(resourceItem, yieldAmount))
            {
                return;
            }

            if (NetworkedPoolManager.Instance != null)
            {
                NetworkedPoolManager.Instance.ReturnToPool(NetworkObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
