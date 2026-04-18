using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton. Fades a full-screen black UI panel in and out.
/// Requires a Canvas (Screen Space - Overlay) with a black Image child.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadePanel;
    [SerializeField] private float defaultDuration = 0.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start fully transparent
        SetAlpha(0f);
    }

    /// <summary>Fade to black, run action, fade back in.</summary>
    public void FadeOutThenIn(Action onBlack, float duration = -1f)
    {
        StartCoroutine(FadeSequence(onBlack, duration < 0 ? defaultDuration : duration));
    }

    public void FadeOut(Action onComplete = null, float duration = -1f)
    {
        StartCoroutine(FadeTo(1f, duration < 0 ? defaultDuration : duration, onComplete));
    }

    public void FadeIn(Action onComplete = null, float duration = -1f)
    {
        StartCoroutine(FadeTo(0f, duration < 0 ? defaultDuration : duration, onComplete));
    }

    // ── Internal ─────────────────────────────────────────────────────
    private IEnumerator FadeSequence(Action onBlack, float duration)
    {
        yield return FadeTo(1f, duration);
        onBlack?.Invoke();
        yield return new WaitForSeconds(0.1f); // one frame buffer
        yield return FadeTo(0f, duration);
    }

    private IEnumerator FadeTo(float target, float duration, Action onComplete = null)
    {
        float start = fadePanel.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(start, target, elapsed / duration));
            yield return null;
        }

        SetAlpha(target);
        onComplete?.Invoke();
    }

    private void SetAlpha(float a)
    {
        Color c = fadePanel.color;
        c.a = a;
        fadePanel.color = c;

        // Block raycasts only when visible so UI beneath stays interactive
        fadePanel.raycastTarget = a > 0.01f;
    }
}