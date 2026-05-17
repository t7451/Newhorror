using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Interaction
{
    public sealed class InventorySystem : NetworkBehaviour
    {
        public struct InventorySlot : INetworkSerializable, System.IEquatable<InventorySlot>
        {
            public FixedString32Bytes ItemName;
            public int Quantity;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ItemName);
                serializer.SerializeValue(ref Quantity);
            }

            public bool Equals(InventorySlot other)
            {
                return ItemName.Equals(other.ItemName) && Quantity == other.Quantity;
            }
        }

        [SerializeField] private int slotCount = 6;

        public NetworkList<InventorySlot> Slots;

        private void Awake()
        {
            Slots = new NetworkList<InventorySlot>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && Slots.Count == 0)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    Slots.Add(new InventorySlot { ItemName = default, Quantity = 0 });
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Slots?.Dispose();
        }

        /// <summary>
        /// Server-only. Returns true when the full amount was added.
        /// </summary>
        public bool AddItem(ItemData item, int amount)
        {
            if (!IsServer || item == null || amount <= 0)
            {
                return false;
            }

            FixedString32Bytes itemName = new FixedString32Bytes(item.itemName);

            for (int i = 0; i < Slots.Count && amount > 0; i++)
            {
                InventorySlot slot = Slots[i];
                if (slot.ItemName.Equals(itemName) && slot.Quantity < item.maxStack)
                {
                    int space = item.maxStack - slot.Quantity;
                    int toAdd = Mathf.Min(space, amount);
                    slot.Quantity += toAdd;
                    Slots[i] = slot;
                    amount -= toAdd;
                }
            }

            for (int i = 0; i < Slots.Count && amount > 0; i++)
            {
                InventorySlot slot = Slots[i];
                if (slot.Quantity == 0)
                {
                    int toAdd = Mathf.Min(item.maxStack, amount);
                    Slots[i] = new InventorySlot { ItemName = itemName, Quantity = toAdd };
                    amount -= toAdd;
                }
            }

            return amount <= 0;
        }

        public bool HasItemQuantity(string itemName, int requiredAmount)
        {
            FixedString32Bytes target = new FixedString32Bytes(itemName);
            int total = 0;
            foreach (InventorySlot slot in Slots)
            {
                if (slot.ItemName.Equals(target))
                {
                    total += slot.Quantity;
                    if (total >= requiredAmount)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Server-only. Removes up to <paramref name="amountToRemove"/> items.
        /// Returns true if the full requested amount was removed.
        /// </summary>
        public bool RemoveItemQuantity(string itemName, int amountToRemove)
        {
            if (!IsServer || amountToRemove <= 0)
            {
                return false;
            }

            FixedString32Bytes target = new FixedString32Bytes(itemName);

            for (int i = Slots.Count - 1; i >= 0 && amountToRemove > 0; i--)
            {
                InventorySlot slot = Slots[i];
                if (!slot.ItemName.Equals(target))
                {
                    continue;
                }

                if (slot.Quantity > amountToRemove)
                {
                    slot.Quantity -= amountToRemove;
                    Slots[i] = slot;
                    amountToRemove = 0;
                }
                else
                {
                    amountToRemove -= slot.Quantity;
                    Slots[i] = new InventorySlot { ItemName = default, Quantity = 0 };
                }
            }

            return amountToRemove <= 0;
        }
    }
}
