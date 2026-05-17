using UnityEngine;

namespace HorrorCoopGame.Environment
{
    /// <summary>
    /// Runtime performance tuner. Place this on a bootstrap GameObject in the
    /// first-loaded scene. Auto-detects mobile / WebGL browser and applies
    /// quality, frame-rate and screen settings tuned for smooth play on
    /// low-end devices without rebuilding per-platform.
    ///
    /// Mobile (and mobile browsers) get aggressive cuts (shadows off, lower
    /// texture quality, lower LOD bias, capped pixel-light count, reduced
    /// fixed-timestep cost). Desktop browser/WebGL gets balanced defaults
    /// (60 FPS target, shadows on, vSync off so WebGL rAF drives pacing).
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class PerformanceBootstrap : MonoBehaviour
    {
        [Header("Frame Rate")]
        [Tooltip("Target FPS on desktop / WebGL desktop. WebGL ignores values above the browser's rAF rate.")]
        [SerializeField] private int desktopTargetFrameRate = 60;
        [Tooltip("Target FPS on mobile / mobile browsers. 60 if device can sustain it, otherwise drop to 30 for smoother frame pacing.")]
        [SerializeField] private int mobileTargetFrameRate = 60;

        [Header("Physics")]
        [Tooltip("Mobile fixed timestep in seconds. 0.02 = 50 Hz (default), 0.0333 = 30 Hz (cheaper, still smooth with interpolation).")]
        [SerializeField] private float mobileFixedTimestep = 0.02f;
        [Tooltip("Maximum allowed delta time. Caps physics catch-up cost during hitches so a stall does not snowball.")]
        [SerializeField] private float maximumDeltaTime = 0.066f; // ~15 FPS floor

        [Header("Quality")]
        [SerializeField] private bool disableShadowsOnMobile = true;
        [SerializeField, Range(0f, 2f)] private float mobileLodBias = 1f;
        [SerializeField, Range(0f, 2f)] private float desktopLodBias = 1.5f;
        [SerializeField, Range(0, 4)] private int mobilePixelLightCount = 1;
        [SerializeField, Range(0, 8)] private int desktopPixelLightCount = 2;

        [Header("Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private static PerformanceBootstrap instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            Apply();
        }

        private void Apply()
        {
            bool isMobile = IsMobileRuntime();

            // vSync must be off for Application.targetFrameRate to take effect.
            // WebGL ignores vSync entirely (browser controls swap via requestAnimationFrame).
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = isMobile ? mobileTargetFrameRate : desktopTargetFrameRate;

            // Keep the screen awake during gameplay on mobile/web.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Cap catch-up so a single long frame doesn't cause a physics death-spiral on slow browsers.
            Time.maximumDeltaTime = maximumDeltaTime;

            if (isMobile)
            {
                Time.fixedDeltaTime = mobileFixedTimestep;
                QualitySettings.lodBias = mobileLodBias;
                QualitySettings.pixelLightCount = mobilePixelLightCount;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 25f);
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.globalTextureMipmapLimit = Mathf.Max(QualitySettings.globalTextureMipmapLimit, 1);
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.antiAliasing = 0;

                if (disableShadowsOnMobile)
                {
                    QualitySettings.shadows = ShadowQuality.Disable;
                }
            }
            else
            {
                QualitySettings.lodBias = desktopLodBias;
                QualitySettings.pixelLightCount = desktopPixelLightCount;
            }
        }

        private static bool IsMobileRuntime()
        {
            if (Application.isMobilePlatform)
            {
                return true;
            }

            // WebGL on a mobile browser reports platform = WebGLPlayer but deviceType = Handheld.
            return SystemInfo.deviceType == DeviceType.Handheld;
        }
    }
}
