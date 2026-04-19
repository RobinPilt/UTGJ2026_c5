using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to each of the 4 signal button Images (not the GlowOverlay child).
/// Replaces the old alpha-swap glow with a genuine shader bloom.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class SimonSaysGlowController : MonoBehaviour
{
    [Header("Material — SimonSaysGlow shader")]
    [SerializeField] private Material glowMaterial;

    [Header("Glow Feel")]
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private float glowRadius = 0.22f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float activateSpeed = 8f;  // how fast glow fades in
    [SerializeField] private float deactivateSpeed = 3f;  // how fast glow fades out

    private RawImage _image;
    private Material _instance;
    private float _targetStrength = 0f;
    private float _currentStrength = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        _image = GetComponent<RawImage>();
        _instance = Instantiate(glowMaterial);
        _image.material = _instance;

        _instance.SetColor("_GlowColor", glowColor);
        _instance.SetFloat("_GlowRadius", glowRadius);
        _instance.SetFloat("_PulseSpeed", pulseSpeed);
        _instance.SetFloat("_GlowStrength", 0f);
    }

    private void Update()
    {
        _instance.SetFloat("_Time2", Time.time);

        // Smooth strength toward target
        float speed = _targetStrength > _currentStrength ? activateSpeed : deactivateSpeed;
        _currentStrength = Mathf.MoveTowards(_currentStrength, _targetStrength,
                                              speed * Time.deltaTime);
        _instance.SetFloat("_GlowStrength", _currentStrength);
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
    }

    // ── Public API ────────────────────────────────────────────────────

    public void SetGlow(bool on) => _targetStrength = on ? 1f : 0f;
}