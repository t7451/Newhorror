using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Interaction
{
    /// <summary>
    /// Server-authoritative object pool for NetworkObjects. Avoids
    /// Instantiate/Destroy churn during play sessions on WebGL/mobile.
    /// </summary>
    public sealed class NetworkedPoolManager : NetworkBehaviour
    {
        public static NetworkedPoolManager Instance { get; private set; }

        [System.Serializable]
        public struct PoolConfig
        {
            public GameObject prefab;
            public int initialSize;
        }

        [SerializeField] private List<PoolConfig> poolConfigurations = new();

        private readonly Dictionary<int, Queue<NetworkObject>> pooledObjects = new();
        private readonly Dictionary<int, GameObject> prefabLookup = new();
        private readonly Dictionary<ulong, int> spawnedToPrefabId = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            foreach (PoolConfig config in poolConfigurations)
            {
                if (config.prefab == null)
                {
                    continue;
                }

                int prefabId = GetPrefabId(config.prefab);
                prefabLookup[prefabId] = config.prefab;
                pooledObjects[prefabId] = new Queue<NetworkObject>();

                for (int i = 0; i < config.initialSize; i++)
                {
                    GameObject instance = Instantiate(config.prefab);
                    instance.SetActive(false);
                    NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                    if (networkObject == null)
                    {
                        Debug.LogError($"Pool prefab '{config.prefab.name}' is missing a NetworkObject.");
                        Destroy(instance);
                        continue;
                    }

                    networkObject.Spawn(false);
                    pooledObjects[prefabId].Enqueue(networkObject);
                }
            }
        }

        public NetworkObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!IsServer || prefab == null)
            {
                return null;
            }

            int prefabId = GetPrefabId(prefab);

            if (!pooledObjects.TryGetValue(prefabId, out Queue<NetworkObject> queue))
            {
                queue = new Queue<NetworkObject>();
                pooledObjects[prefabId] = queue;
                prefabLookup[prefabId] = prefab;
            }

            NetworkObject networkObject;
            if (queue.Count > 0)
            {
                networkObject = queue.Dequeue();
                networkObject.transform.SetPositionAndRotation(position, rotation);
                networkObject.gameObject.SetActive(true);
            }
            else
            {
                GameObject instance = Instantiate(prefab, position, rotation);
                networkObject = instance.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    Debug.LogError($"Prefab '{prefab.name}' is missing a NetworkObject.");
                    Destroy(instance);
                    return null;
                }

                networkObject.Spawn(true);
            }

            spawnedToPrefabId[networkObject.NetworkObjectId] = prefabId;
            return networkObject;
        }

        public void ReturnToPool(NetworkObject networkObject)
        {
            if (!IsServer || networkObject == null)
            {
                return;
            }

            if (!spawnedToPrefabId.TryGetValue(networkObject.NetworkObjectId, out int prefabId))
            {
                networkObject.gameObject.SetActive(false);
                return;
            }

            networkObject.gameObject.SetActive(false);

            if (!pooledObjects.TryGetValue(prefabId, out Queue<NetworkObject> queue))
            {
                queue = new Queue<NetworkObject>();
                pooledObjects[prefabId] = queue;
            }

            queue.Enqueue(networkObject);
        }

        private static int GetPrefabId(GameObject prefab)
        {
            return prefab.name.GetHashCode();
        }
    }
}
