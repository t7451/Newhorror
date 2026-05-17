using HorrorCoopGame.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Building
{
    public sealed class StructureHealth : NetworkBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public NetworkVariable<float> Health = new(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Health.Value = maxHealth;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float amount)
        {
            if (!IsServer || amount <= 0f)
            {
                return;
            }

            Health.Value = Mathf.Max(0f, Health.Value - amount);
            if (Health.Value <= 0f)
            {
                if (NetworkedPoolManager.Instance != null)
                {
                    NetworkedPoolManager.Instance.ReturnToPool(NetworkObject);
                }
                else
                {
                    NetworkObject.Despawn(true);
                }
            }
        }
    }
}
