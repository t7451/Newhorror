using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.Environment
{
    /// <summary>
    /// Baked-lighting friendly day/night cycle: rotates a single directional
    /// light and lerps ambient color. No realtime GI updates.
    /// </summary>
    public sealed class DayNightCycle : NetworkBehaviour
    {
        [SerializeField] private Light sunLight;
        [SerializeField] private Gradient ambientColorOverDay;
        [SerializeField] private Gradient sunColorOverDay;
        [SerializeField] private float dayLengthSeconds = 600f;
        [SerializeField, Range(0f, 1f)] private float startTimeOfDay = 0.25f;

        public NetworkVariable<float> TimeOfDay = new(
            0.25f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                TimeOfDay.Value = Mathf.Repeat(startTimeOfDay, 1f);
            }
        }

        private void Update()
        {
            if (IsServer && dayLengthSeconds > 0f)
            {
                TimeOfDay.Value = Mathf.Repeat(TimeOfDay.Value + (Time.deltaTime / dayLengthSeconds), 1f);
            }

            ApplyLighting(TimeOfDay.Value);
        }

        private void ApplyLighting(float normalizedTime)
        {
            if (sunLight != null)
            {
                float sunAngle = (normalizedTime * 360f) - 90f;
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

                if (sunColorOverDay != null)
                {
                    sunLight.color = sunColorOverDay.Evaluate(normalizedTime);
                }
            }

            if (ambientColorOverDay != null)
            {
                RenderSettings.ambientLight = ambientColorOverDay.Evaluate(normalizedTime);
            }
        }
    }
}
