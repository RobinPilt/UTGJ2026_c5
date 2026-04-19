using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the menu Background RawImage.
/// Drives the glitch shader over time with occasional burst spikes.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MenuGlitchController : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material glitchMaterial;

    [Header("Baseline — always-on subtle glitch")]
    [SerializeField] private float baseChromaStrength = 0.003f;
    [SerializeField] private float baseGlitchIntensity = 0.04f;

    [Header("Burst — occasional strong glitch spike")]
    [SerializeField] private float burstChromaStrength = 0.018f;
    [SerializeField] private float burstGlitchIntensity = 0.35f;
    [SerializeField] private float burstDuration = 0.12f;
    [SerializeField] private float burstMinInterval = 2f;
    [SerializeField] private float burstMaxInterval = 6f;

    [Header("Speed")]
    [SerializeField] private float glitchSpeed = 1.2f;

    private RawImage _image;
    private Material _instance;
    private float _nextBurstTime;
    private bool _bursting;
    private float _burstEndTime;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        _image = GetComponent<RawImage>();
        _instance = Instantiate(glitchMaterial);
        _image.material = _instance;

        ScheduleNextBurst();
    }

    private void Update()
    {
        _instance.SetFloat("_Time2", Time.time);
        _instance.SetFloat("_GlitchSpeed", glitchSpeed);

        // Check if burst should start
        if (!_bursting && Time.time >= _nextBurstTime)
        {
            _bursting = true;
            _burstEndTime = Time.time + burstDuration;
        }

        // Check if burst should end
        if (_bursting && Time.time >= _burstEndTime)
        {
            _bursting = false;
            ScheduleNextBurst();
        }

        // Set shader params based on burst state
        float chroma = _bursting ? burstChromaStrength : baseChromaStrength;
        float intensity = _bursting ? burstGlitchIntensity : baseGlitchIntensity;

        _instance.SetFloat("_ChromaStrength", chroma);
        _instance.SetFloat("_GlitchIntensity", intensity);
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
    }

    private void ScheduleNextBurst()
    {
        _nextBurstTime = Time.time + Random.Range(burstMinInterval, burstMaxInterval);
    }
}