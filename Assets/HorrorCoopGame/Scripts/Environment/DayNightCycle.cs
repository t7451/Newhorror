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

        [Header("Optimization")]
        [Tooltip("Seconds between gradient evaluations on clients. Color/rotation are smoothly lerped between samples for cheap, smooth lighting on WebGL/mobile.")]
        [SerializeField] private float clientUpdateInterval = 0.25f;
        [Tooltip("Smoothing speed (per second) for sun/ambient color and sun rotation between samples.")]
        [SerializeField] private float colorLerpSpeed = 4f;

        public NetworkVariable<float> TimeOfDay = new(
            0.25f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float nextLightingSampleTime;
        private Color targetAmbient;
        private Color targetSunColor;
        private Quaternion targetSunRotation = Quaternion.identity;
        private bool hasSampledOnce;

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

            float now = Time.time;
            if (!hasSampledOnce || now >= nextLightingSampleTime)
            {
                nextLightingSampleTime = now + Mathf.Max(0f, clientUpdateInterval);
                SampleLighting(TimeOfDay.Value);
                hasSampledOnce = true;
            }

            ApplyLighting();
        }

        private void SampleLighting(float normalizedTime)
        {
            float sunAngle = (normalizedTime * 360f) - 90f;
            targetSunRotation = Quaternion.Euler(sunAngle, 170f, 0f);

            if (sunColorOverDay != null)
            {
                targetSunColor = sunColorOverDay.Evaluate(normalizedTime);
            }

            if (ambientColorOverDay != null)
            {
                targetAmbient = ambientColorOverDay.Evaluate(normalizedTime);
            }
        }

        private void ApplyLighting()
        {
            float t = 1f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime);

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Slerp(sunLight.transform.rotation, targetSunRotation, t);

                if (sunColorOverDay != null)
                {
                    sunLight.color = Color.Lerp(sunLight.color, targetSunColor, t);
                }
            }

            if (ambientColorOverDay != null)
            {
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, t);
            }
        }
    }
}
