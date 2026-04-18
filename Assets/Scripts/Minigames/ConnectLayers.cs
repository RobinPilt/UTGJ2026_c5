using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectLayers : Minigame
{
    [Header("Layer Images (assign Sky, Earth, Underworld panels)")]
    [SerializeField] private Image skyLayer;
    [SerializeField] private Image earthLayer;
    [SerializeField] private Image underworldLayer;

    [Header("Intro Audio Cues (played in randomised order before puzzle)")]
    [SerializeField] private AudioClip skyIntroClip;
    [SerializeField] private AudioClip earthIntroClip;
    [SerializeField] private AudioClip underworldIntroClip;

    [Header("Hover Ambient Sounds (soft loop while hovering)")]
    [SerializeField] private AudioClip skyHoverClip;
    [SerializeField] private AudioClip earthHoverClip;
    [SerializeField] private AudioClip underworldHoverClip;

    [Header("Glow Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.4f, 1f, 0.55f, 1f);
    [SerializeField] private Color successColor = new Color(0f, 1f, 0.3f, 1f);
    [SerializeField] private Color failColor = Color.red;

    // ── Internal ──────────────────────────────────────────────────────

    public enum LayerType { Sky = 0, Earth = 1, Underworld = 2 }

    private List<LayerType> _correctOrder = new();
    private List<LayerType> _playerInput = new();
    private Dictionary<LayerType, Image> _images;
    private Dictionary<LayerType, AudioClip> _introCues;
    private Dictionary<LayerType, AudioClip> _hoverCues;
    private Dictionary<LayerType, Outline> _outlines;

    private AudioSource _sfxSource;
    private AudioSource _ambientSource;
    private bool _inputEnabled;

    // ── Minigame interface ────────────────────────────────────────────

    protected override void OnBegin()
    {
        BuildLookups();
        SetupOutlines();
        SetupAudio();
        Shuffle(_correctOrder);
        _playerInput.Clear();
        _inputEnabled = false;
        StartCoroutine(PlayIntroThenEnable());
    }

    protected override void OnCleanup()
    {
        StopAllCoroutines();
        _inputEnabled = false;
        _playerInput.Clear();
        _ambientSource.Stop();

        foreach (var kv in _outlines)
            ApplyOutline(kv.Key, idleColor, 3f);
    }

    // ── Called by EventTrigger on each Image ──────────────────────────

    public void OnHoverEnter(int layerIndex)
    {
        var layer = (LayerType)layerIndex;
        if (!_inputEnabled || _playerInput.Contains(layer)) return;
        ApplyOutline(layer, hoverColor, 10f);
        PlayAmbient(_hoverCues[layer]);
    }

    public void OnHoverExit(int layerIndex)
    {
        var layer = (LayerType)layerIndex;
        if (!_inputEnabled || _playerInput.Contains(layer)) return;
        ApplyOutline(layer, idleColor, 3f);
        _ambientSource.Stop();
    }

    public void OnLayerClicked(int layerIndex)
    {
        var layer = (LayerType)layerIndex;
        if (!_inputEnabled || _playerInput.Contains(layer)) return;

        _playerInput.Add(layer);
        int idx = _playerInput.Count - 1;
        ApplyOutline(layer, selectedColor, 6f);

        if (_playerInput[idx] != _correctOrder[idx])
        {
            StartCoroutine(FailAndReset());
            return;
        }

        if (_playerInput.Count == _correctOrder.Count)
            StartCoroutine(WinSequence());
    }

    // ── Coroutines ────────────────────────────────────────────────────

    private IEnumerator PlayIntroThenEnable()
    {
        yield return new WaitForSeconds(0.6f);

        foreach (var layer in _correctOrder)
        {
            var clip = _introCues[layer];
            float duration = clip != null ? clip.length : 1f;
            if (clip != null) _sfxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(duration + 0.35f);
        }

        _inputEnabled = true;
    }

    private IEnumerator WinSequence()
    {
        _inputEnabled = false;
        _ambientSource.Stop();
        foreach (var kv in _outlines) ApplyOutline(kv.Key, successColor, 14f);
        yield return new WaitForSeconds(1.4f);
        Complete(true);
    }

    private IEnumerator FailAndReset()
    {
        _inputEnabled = false;
        _ambientSource.Stop();
        foreach (var kv in _outlines) ApplyOutline(kv.Key, failColor, 10f);
        yield return new WaitForSeconds(1f);

        _playerInput.Clear();
        foreach (var kv in _outlines) ApplyOutline(kv.Key, idleColor, 3f);
        Shuffle(_correctOrder);
        StartCoroutine(PlayIntroThenEnable());
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void BuildLookups()
    {
        _images = new()
        {
            { LayerType.Sky,        skyLayer },
            { LayerType.Earth,      earthLayer },
            { LayerType.Underworld, underworldLayer }
        };
        _introCues = new()
        {
            { LayerType.Sky,        skyIntroClip },
            { LayerType.Earth,      earthIntroClip },
            { LayerType.Underworld, underworldIntroClip }
        };
        _hoverCues = new()
        {
            { LayerType.Sky,        skyHoverClip },
            { LayerType.Earth,      earthHoverClip },
            { LayerType.Underworld, underworldHoverClip }
        };
        _correctOrder = new() { LayerType.Sky, LayerType.Earth, LayerType.Underworld };
    }

    private void SetupOutlines()
    {
        _outlines = new();
        foreach (var kv in _images)
        {
            var o = kv.Value.GetComponent<Outline>() ?? kv.Value.gameObject.AddComponent<Outline>();
            o.effectColor = idleColor;
            o.effectDistance = new Vector2(3, 3);
            _outlines[kv.Key] = o;
        }
    }

    private void SetupAudio()
    {
        _sfxSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        var sources = GetComponents<AudioSource>();
        _ambientSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        _ambientSource.loop = true;
        _ambientSource.volume = 0.25f;
    }

    private void ApplyOutline(LayerType layer, Color color, float size)
    {
        _outlines[layer].effectColor = color;
        _outlines[layer].effectDistance = new Vector2(size, size);
    }

    private void PlayAmbient(AudioClip clip)
    {
        if (clip == null) return;
        _ambientSource.clip = clip;
        _ambientSource.Play();
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
