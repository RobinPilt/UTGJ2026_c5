using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;

/// <summary>
/// Attach to a UI Image. Call Glitch() to trigger a burst of corruption.
/// Uses RectTransform jitter + color flicker — no shader required.
/// </summary>
public class GlitchEffect : MonoBehaviour
{
    [Header("Jitter")]
    [SerializeField] private float maxOffset = 12f;  // max pixel displacement
    [SerializeField] private float jitterSpeed = 0.03f; // seconds between jitter frames

    [Header("Color Flicker")]
    [SerializeField] private Color glitchTint = new Color(0.6f, 1f, 0.9f, 1f); // cyan-ish

    private RectTransform _rect;
    private Image _image;
    private Vector2 _originPos;
    private Color _originColor;
    private Coroutine _glitchRoutine;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _originPos = _rect.anchoredPosition;
        _originColor = _image != null ? _image.color : Color.white;
    }

    /// <summary>
    /// Runs a glitch burst for [duration] seconds then snaps back to normal.
    /// </summary>
    public void Glitch(float duration, Action onComplete = null)
    {
        if (_glitchRoutine != null) StopCoroutine(_glitchRoutine);
        _glitchRoutine = StartCoroutine(GlitchRoutine(duration, onComplete));
    }

    private IEnumerator GlitchRoutine(float duration, Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Random position jitter
            _rect.anchoredPosition = _originPos + new Vector2(
                Random.Range(-maxOffset, maxOffset),
                Random.Range(-maxOffset * 0.4f, maxOffset * 0.4f)
            );

            // Flicker between normal and glitch tint
            if (_image != null)
                _image.color = Random.value > 0.5f ? glitchTint : _originColor;

            elapsed += jitterSpeed;
            yield return new WaitForSeconds(jitterSpeed);
        }

        // Snap back clean
        _rect.anchoredPosition = _originPos;
        if (_image != null) _image.color = _originColor;

        onComplete?.Invoke();
    }
}