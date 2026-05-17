using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.Interaction
{
    /// <summary>
    /// Lightweight scalable grid UI that mirrors the local player's
    /// NetworkList-backed inventory. Avoids re-instantiating slots
    /// when only data changes.
    /// </summary>
    public sealed class InventoryGridUI : MonoBehaviour
    {
        [SerializeField] private RectTransform slotsParent;
        [SerializeField] private GameObject slotPrefab;

        private readonly List<SlotView> slotViews = new();
        private InventorySystem trackedInventory;

        private struct SlotView
        {
            public Image Icon;
            public TextMeshProUGUI Quantity;
        }

        private void OnEnable()
        {
            TryBindLocalInventory();
        }

        private void OnDisable()
        {
            UnbindInventory();
        }

        private void Update()
        {
            if (trackedInventory == null)
            {
                TryBindLocalInventory();
            }
        }

        private void TryBindLocalInventory()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            {
                return;
            }

            NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObject == null)
            {
                return;
            }

            if (!playerObject.TryGetComponent(out InventorySystem inventory))
            {
                return;
            }

            trackedInventory = inventory;
            inventory.Slots.OnListChanged += OnSlotsChanged;
            Rebuild();
        }

        private void UnbindInventory()
        {
            if (trackedInventory != null && trackedInventory.Slots != null)
            {
                trackedInventory.Slots.OnListChanged -= OnSlotsChanged;
            }

            trackedInventory = null;
        }

        private void OnSlotsChanged(NetworkListEvent<InventorySystem.InventorySlot> _)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (trackedInventory == null || slotsParent == null || slotPrefab == null)
            {
                return;
            }

            EnsureSlotViews(trackedInventory.Slots.Count);

            for (int i = 0; i < trackedInventory.Slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = trackedInventory.Slots[i];
                SlotView view = slotViews[i];

                bool empty = slot.Quantity <= 0 || slot.ItemName.Length == 0;

                if (view.Quantity != null)
                {
                    view.Quantity.text = empty ? string.Empty : slot.Quantity.ToString();
                }

                if (view.Icon != null)
                {
                    view.Icon.enabled = !empty;
                }
            }
        }

        private void EnsureSlotViews(int desiredCount)
        {
            while (slotViews.Count < desiredCount)
            {
                GameObject instance = Instantiate(slotPrefab, slotsParent);
                SlotView view = new SlotView
                {
                    Icon = instance.GetComponentInChildren<Image>(includeInactive: true),
                    Quantity = instance.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
                };
                slotViews.Add(view);
            }

            while (slotViews.Count > desiredCount)
            {
                int lastIndex = slotViews.Count - 1;
                SlotView view = slotViews[lastIndex];
                if (view.Icon != null)
                {
                    Destroy(view.Icon.transform.parent.gameObject);
                }
                slotViews.RemoveAt(lastIndex);
            }
        }
    }
}
