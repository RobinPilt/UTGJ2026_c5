using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MinigameTransitionManager : MonoBehaviour
{
    public static MinigameTransitionManager Instance { get; private set; }

    [Header("Swirl Surface — fullscreen RawImage")]
    [SerializeField] private RawImage swirlSurface;
    [SerializeField] private Material swirlMaterial;

    [Header("Black Overlay")]
    [SerializeField] private Image blackOverlay;

    [Header("Swirl Tuning")]
    [SerializeField] private float swirlDuration = 1.1f;
    [SerializeField] private float maxSwirlStrength = 9f;
    [SerializeField]
    private AnimationCurve swirlCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float holdOnBlackDuration = 0.9f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.45f;

    private CanvasGroup _activePanelGroup;
    private Texture2D _capturedFrame;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        swirlSurface.gameObject.SetActive(false);
        SetOverlayAlpha(0f);
    }

    // ── Public API ────────────────────────────────────────────────────

    public void TransitionIn(CanvasGroup panelGroup, Action onBlack, Action onComplete)
    {
        _activePanelGroup = panelGroup;
        StartCoroutine(SwirlInSequence(onBlack, onComplete));
    }

    public void TransitionOut(CanvasGroup panelGroup, Action onBlack, Action onComplete)
    {
        _activePanelGroup = panelGroup;
        StartCoroutine(FadeOutSequence(onBlack, onComplete));
    }

    // ── Swirl in ──────────────────────────────────────────────────────

    private IEnumerator SwirlInSequence(Action onBlack, Action onComplete)
    {
        // Capture current frame
        yield return new WaitForEndOfFrame();
        CaptureScreen();

        // Show swirl surface
        swirlSurface.texture = _capturedFrame;
        swirlSurface.material = swirlMaterial;
        swirlSurface.gameObject.SetActive(true);
        SetSwirlParams(0f, 0f, 0f);
        SetOverlayAlpha(0f);

        // Animate swirl to black
        float elapsed = 0f;
        while (elapsed < swirlDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swirlDuration);
            float curved = swirlCurve.Evaluate(t);
            SetSwirlParams(curved * maxSwirlStrength, curved, curved);
            yield return null;
        }

        // Overlay covers first, then hide swirl — no flash
        SetOverlayAlpha(1f);
        yield return null;
        swirlSurface.gameObject.SetActive(false);
        SetSwirlParams(0f, 0f, 0f);

        // Show panel hidden behind black
        onBlack?.Invoke();
        if (_activePanelGroup != null) _activePanelGroup.alpha = 0f;

        // Hold on black
        yield return new WaitForSeconds(holdOnBlackDuration);

        // Fade panel in — overlay stays black the whole time
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            if (_activePanelGroup != null) _activePanelGroup.alpha = t;
            yield return null;
        }

        if (_activePanelGroup != null) _activePanelGroup.alpha = 1f;

        // Now fade overlay out — panel covers the room so no flash possible
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            SetOverlayAlpha(1f - t);
            yield return null;
        }

        SetOverlayAlpha(0f);

        Destroy(_capturedFrame);
        _capturedFrame = null;

        onComplete?.Invoke();
    }

    // ── Fade out ──────────────────────────────────────────────────────

    private IEnumerator FadeOutSequence(Action onBlack, Action onComplete)
    {
        // Fade overlay to black while panel still fully visible
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            SetOverlayAlpha(t);
            yield return null;
        }

        SetOverlayAlpha(1f);

        // Fully black — safe to disable panel and fire onBlack
        onBlack?.Invoke();

        if (_activePanelGroup != null)
        {
            _activePanelGroup.alpha = 0f;
            _activePanelGroup.interactable = false;
            _activePanelGroup.blocksRaycasts = false;
            _activePanelGroup.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.08f);

        // Fade overlay out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            SetOverlayAlpha(1f - t);
            yield return null;
        }

        SetOverlayAlpha(0f);
        _activePanelGroup = null;
        onComplete?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void CaptureScreen()
    {
        if (_capturedFrame != null) Destroy(_capturedFrame);
        _capturedFrame = ScreenCapture.CaptureScreenshotAsTexture();
    }

    private void SetSwirlParams(float swirl, float darkness, float pull)
    {
        if (swirlMaterial == null) return;
        swirlMaterial.SetFloat("_SwirlStrength", swirl);
        swirlMaterial.SetFloat("_Darkness", darkness);
        swirlMaterial.SetFloat("_Pull", pull);
    }

    private void SetOverlayAlpha(float a)
    {
        if (blackOverlay == null) return;
        Color c = blackOverlay.color;
        c.a = a;
        blackOverlay.color = c;
        blackOverlay.raycastTarget = a > 0.01f; // only block raycasts when actually visible
    }
}