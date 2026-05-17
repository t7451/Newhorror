using UnityEngine;
using UnityEngine.UI;

namespace HorrorCoopGame.UI
{
    /// <summary>
    /// Forces a Canvas Scaler to "Scale With Screen Size" at 1920x1080 and
    /// auto-selects width/height matching based on the current aspect ratio
    /// so the UI is readable on phones (portrait/landscape) and WebGL.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);

        private CanvasScaler scaler;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            UpdateMatch();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                UpdateMatch();
            }
        }

        private void UpdateMatch()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            float referenceAspect = referenceResolution.x / referenceResolution.y;
            float screenAspect = lastHeight > 0 ? (float)lastWidth / lastHeight : referenceAspect;

            // Narrower than reference (e.g. portrait phones) -> match width.
            // Wider than reference (e.g. ultra-wide) -> match height.
            scaler.matchWidthOrHeight = screenAspect < referenceAspect ? 0f : 1f;
        }
    }
}
