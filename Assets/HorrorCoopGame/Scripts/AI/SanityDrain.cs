using HorrorCoopGame.Player;
using Unity.Netcode;
using UnityEngine;

namespace HorrorCoopGame.AI
{
    /// <summary>
    /// Drains the player's Sanity NetworkVariable while in darkness.
    /// Uses light probes / sampling at a low cadence to avoid hot-loop costs.
    /// Audio hallucinations are played client-side via a ClientRpc.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public sealed class SanityDrain : NetworkBehaviour
    {
        [SerializeField] private float drainPerSecond = 1.5f;
        [SerializeField] private float regenPerSecond = 0.75f;
        [SerializeField] private float darknessThreshold = 0.25f;
        [SerializeField] private float evaluationInterval = 0.5f;
        [SerializeField] private AudioClip[] hallucinationClips;
        [SerializeField] private AudioSource hallucinationSource;
        [SerializeField] private float hallucinationMinInterval = 8f;
        [SerializeField] private float hallucinationMaxInterval = 18f;

        private PlayerStats stats;
        private float nextEvaluationTime;
        private float nextHallucinationTime;

        public override void OnNetworkSpawn()
        {
            stats = GetComponent<PlayerStats>();

            if (!IsServer && !IsOwner)
            {
                enabled = false;
            }

            ScheduleNextHallucination();
        }

        private void Update()
        {
            if (IsServer && Time.time >= nextEvaluationTime)
            {
                nextEvaluationTime = Time.time + evaluationInterval;
                EvaluateSanityServer();
            }

            if (IsOwner)
            {
                TryPlayHallucination();
            }
        }

        private void EvaluateSanityServer()
        {
            float light = SampleAmbientLight();
            float delta = light < darknessThreshold
                ? -drainPerSecond * evaluationInterval
                : regenPerSecond * evaluationInterval;

            stats.Sanity.Value = Mathf.Clamp(stats.Sanity.Value + delta, 0f, 100f);
        }

        private float SampleAmbientLight()
        {
            // Cheap, allocation-free probe of baked ambient + sky light intensity.
            Color ambient = RenderSettings.ambientLight;
            float intensity = (ambient.r + ambient.g + ambient.b) / 3f;
            intensity = Mathf.Max(intensity, RenderSettings.ambientIntensity);

            if (RenderSettings.sun != null)
            {
                intensity += RenderSettings.sun.intensity * Mathf.Clamp01(Vector3.Dot(Vector3.up, -RenderSettings.sun.transform.forward));
            }

            return Mathf.Clamp01(intensity);
        }

        private void TryPlayHallucination()
        {
            if (Time.time < nextHallucinationTime || hallucinationSource == null || hallucinationClips == null || hallucinationClips.Length == 0)
            {
                return;
            }

            if (stats.Sanity.Value > 60f)
            {
                ScheduleNextHallucination();
                return;
            }

            AudioClip clip = hallucinationClips[Random.Range(0, hallucinationClips.Length)];
            if (clip != null)
            {
                hallucinationSource.PlayOneShot(clip);
            }

            ScheduleNextHallucination();
        }

        private void ScheduleNextHallucination()
        {
            nextHallucinationTime = Time.time + Random.Range(hallucinationMinInterval, hallucinationMaxInterval);
        }
    }
}
