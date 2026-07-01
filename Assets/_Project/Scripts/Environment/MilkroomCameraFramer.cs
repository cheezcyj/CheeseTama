using UnityEngine;

namespace CheeseTama.Environment
{
    // Keeps the CheeseTama character and milkroom visible above the bottom UI
    // bars (care buttons and the always-visible status message bar) at any aspect ratio.
    //
    // The milkroom camera is perspective with a fixed vertical FOV, so a world
    // point's vertical screen position is invariant to aspect ratio; the only
    // thing that changes on a short/ultra-wide screen (e.g. 19:6) is that the
    // bottom bars cover a larger *fraction* of the screen. But even at 16:9 the
    // character's feet sit slightly behind the bars, so this component always
    // pans the camera down just enough to lift the character's feet to (and a
    // little above) the top edge of the always-visible bottom bar, at every
    // aspect ratio. The camera never pans *up* past its authored baseline.
    [RequireComponent(typeof(Camera))]
    public sealed class MilkroomCameraFramer : MonoBehaviour
    {
        [SerializeField] private Canvas referenceCanvas;

        // Fallback top edge in canvas reference pixels, used only if the named
        // bottom bars cannot be found. In normal scenes this is resolved from
        // the visible RectTransforms so hidden bars do not push the room upward.
        [SerializeField] private float uiSafeReferenceHeight = 270f;

        // World-space bottom of the content that must stay visible (character
        // feet rest on the floor around y = -2.13).
        [SerializeField] private float contentBottomWorldY = -2.13f;

        // Z plane of the content (character/props front) used for the projection.
        [SerializeField] private float contentPlaneZ = 0.08f;

        // Extra clearance above the bar, as a fraction of screen height.
        [SerializeField] private float extraClearanceFraction = 0.02f;

        [SerializeField] private float maxLift = 4f;
        [SerializeField] private float smoothing = 10f;

        private Camera cam;
        private float baselineY;
        private bool hasBaseline;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            CaptureBaseline();
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (referenceCanvas == null)
            {
                var canvasObject = GameObject.Find("Milkroom Canvas");
                if (canvasObject != null)
                {
                    referenceCanvas = canvasObject.GetComponent<Canvas>();
                }
            }
        }

        private void CaptureBaseline()
        {
            if (!hasBaseline)
            {
                baselineY = transform.position.y;
                hasBaseline = true;
            }
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            CaptureBaseline();

            if (Screen.height <= 0 || cam == null || cam.orthographic)
            {
                return;
            }

            var scaleFactor = referenceCanvas != null ? referenceCanvas.scaleFactor : 1f;
            if (scaleFactor <= 0f)
            {
                scaleFactor = 1f;
            }

            var safeReferenceHeight = ResolveVisibleBottomBarTop();
            if (safeReferenceHeight <= 0f)
            {
                MoveCameraY(baselineY);
                return;
            }

            // Screen-height fraction covered by the currently visible bottom bars.
            var barTopFraction = Mathf.Clamp01((safeReferenceHeight * scaleFactor) / Screen.height);

            // Viewport height (0..1) we want the content bottom to sit at.
            var desiredBottomViewport = Mathf.Clamp01(barTopFraction + extraClearanceFraction);

            // Half viewport height in world units at the content plane.
            var distance = Mathf.Abs(contentPlaneZ - transform.position.z);
            var halfWorld = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // vp = 0.5 + 0.5 * (worldY - camY) / halfWorld  =>  solve camY.
            var requiredCamY = contentBottomWorldY - (desiredBottomViewport - 0.5f) * 2f * halfWorld;

            // Only ever pan down from the baseline, and clamp the maximum lift.
            var targetY = Mathf.Clamp(requiredCamY, baselineY - maxLift, baselineY);

            MoveCameraY(targetY);
        }

        private float ResolveVisibleBottomBarTop()
        {
            if (referenceCanvas == null)
            {
                return 0f;
            }

            var canvasRoot = referenceCanvas.transform;
            var top = 0f;
            top = Mathf.Max(top, GetVisibleBottomPanelTop(canvasRoot, "Bottom Action Bar"));
            top = Mathf.Max(top, GetVisibleBottomPanelTop(canvasRoot, "Message Bar"));
            return top > 0f ? top : Mathf.Max(0f, uiSafeReferenceHeight);
        }

        private static float GetVisibleBottomPanelTop(Transform canvasRoot, string panelName)
        {
            var panel = canvasRoot.Find(panelName);
            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                return 0f;
            }

            if (!panel.TryGetComponent(out RectTransform rect))
            {
                return 0f;
            }

            return rect.anchoredPosition.y + rect.sizeDelta.y * (1f - rect.pivot.y);
        }

        private void MoveCameraY(float targetY)
        {
            var pos = transform.position;
            var t = 1f - Mathf.Exp(-smoothing * Mathf.Max(0.0001f, Time.unscaledDeltaTime));
            pos.y = Mathf.Lerp(pos.y, targetY, t);
            transform.position = pos;
        }
    }
}
