using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorCoopGame.Environment
{
    /// <summary>
    /// Owner-controlled, networked flashlight. Uses a spotlight with
    /// shadows explicitly disabled for mobile/WebGL performance.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class PerformantFlashlight : NetworkBehaviour
    {
        [SerializeField] private Light spotLight;

        public NetworkVariable<bool> IsOn = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Reset()
        {
            spotLight = GetComponent<Light>();
        }

        public override void OnNetworkSpawn()
        {
            if (spotLight == null)
            {
                spotLight = GetComponent<Light>();
            }

            if (spotLight != null)
            {
                spotLight.shadows = LightShadows.None;
                spotLight.type = LightType.Spot;
                spotLight.enabled = IsOn.Value;
            }

            IsOn.OnValueChanged += HandleStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            IsOn.OnValueChanged -= HandleStateChanged;
        }

        public void OnToggleFlashlight(InputValue value)
        {
            if (!value.isPressed || !IsOwner)
            {
                return;
            }

            ToggleServerRpc();
        }

        [ServerRpc]
        private void ToggleServerRpc()
        {
            IsOn.Value = !IsOn.Value;
        }

        private void HandleStateChanged(bool _, bool newValue)
        {
            if (spotLight != null)
            {
                spotLight.enabled = newValue;
            }
        }
    }
}
