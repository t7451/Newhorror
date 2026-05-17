using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.Player
{
    public sealed class PlayerStats : NetworkBehaviour
    {
        public readonly NetworkVariable<float> Health = new(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> Stamina = new(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> Sanity = new(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [Header("Local UI")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image staminaBarFill;
        [SerializeField] private Image sanityBarFill;
        [SerializeField] private CanvasGroup sanityVignette;

        [Header("Server Tuning")]
        [SerializeField] private float staminaRegenPerSecond = 5f;

        private void Update()
        {
            if (IsOwner)
            {
                UpdateLocalUi();
            }

            if (IsServer)
            {
                RegenerateServerStamina();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float amount)
        {
            float clampedAmount = Mathf.Max(0f, amount);
            Health.Value = Mathf.Clamp(Health.Value - clampedAmount, 0f, 100f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ModifySanityServerRpc(float delta)
        {
            Sanity.Value = Mathf.Clamp(Sanity.Value + delta, 0f, 100f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UseStaminaServerRpc(float amount)
        {
            float clampedAmount = Mathf.Max(0f, amount);
            Stamina.Value = Mathf.Clamp(Stamina.Value - clampedAmount, 0f, 100f);
        }

        private void UpdateLocalUi()
        {
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = Health.Value / 100f;
            }

            if (staminaBarFill != null)
            {
                staminaBarFill.fillAmount = Stamina.Value / 100f;
            }

            if (sanityBarFill != null)
            {
                sanityBarFill.fillAmount = Sanity.Value / 100f;
            }

            if (sanityVignette != null)
            {
                sanityVignette.alpha = 1f - (Sanity.Value / 100f);
            }
        }

        private void RegenerateServerStamina()
        {
            if (Stamina.Value >= 100f)
            {
                return;
            }

            Stamina.Value = Mathf.Clamp(Stamina.Value + (staminaRegenPerSecond * Time.deltaTime), 0f, 100f);
        }
    }
}
