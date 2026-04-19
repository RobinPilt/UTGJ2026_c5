using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StitchSeam
{
    public List<RectTransform> pointsA;
    public List<RectTransform> pointsB;
}

public class ConnectLayers : Minigame
{
    public static ConnectLayers Instance { get; private set; }
    public enum LayerType { Sky = 0, Earth = 1, Underworld = 2 }

    private enum GamePhase { Intro, Hover, Stitching }

    [Header("Layer Images")]
    [SerializeField] private Image skyLayer;
    [SerializeField] private Image earthLayer;
    [SerializeField] private Image underworldLayer;

    [Header("Distortion")]
    [SerializeField] private LayerDistortionEffect skyDistortion;
    [SerializeField] private LayerDistortionEffect earthDistortion;
    [SerializeField] private LayerDistortionEffect underworldDistortion;
    [SerializeField] private float distortionRampSpeed = 2f;

    [Header("Seams")]
    [SerializeField] private StitchSeam skyEarthSeam;
    [SerializeField] private StitchSeam earthUnderworldSeam;

    [Header("Thread")]
    [SerializeField] private RectTransform threadContainer;
    [SerializeField] private Sprite threadSprite;

    [Header("Layer Audio (used for both intro sequence and hover)")]
    [SerializeField] private AudioClip skyClip;
    [SerializeField] private AudioClip earthClip;
    [SerializeField] private AudioClip underworldClip;

    [Header("Canvas (leave null for Overlay)")]
    [SerializeField] private Canvas canvas;

    [Header("Colors")]
    [SerializeField] private Color idleColor      = Color.white;
    [SerializeField] private Color hoverPlayColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color selectedColor  = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color successColor   = new Color(0.5f, 0.65f, 0.5f, 1f);
    [SerializeField] private Color failColor      = Color.red;
    [SerializeField] private Color activeColor    = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color completedColor = new Color(0.5f, 0.65f, 0.5f, 1f);

    // ── Internal ──────────────────────────────────────────────────────

    private GamePhase _phase;
    private List<LayerType> _correctOrder = new();
    private List<LayerType> _playerSelection = new();

    private Dictionary<LayerType, Image>                 _images;
    private Dictionary<LayerType, AudioClip>             _clips;
    private Dictionary<LayerType, LayerDistortionEffect> _distortions;

    private AudioSource _sfxSource;
    private AudioSource _hoverSource;
    private bool        _hoverAudioBusy;
    private bool        _inputEnabled;
    private bool        _finished;

    private int           _connectionIndex;
    private int           _stitchIndex;
    private bool          _isDragging;
    private RectTransform _dragOrigin;
    private Image         _activeThread;
    private List<Image>   _lockedThreads = new();
    private Coroutine     _distortionCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Minigame overrides ────────────────────────────────────────────

    protected override void OnBegin()
    {
        BuildLookups();
        SetupAudio();
        RandomiseOrder();

        _connectionIndex = 0;
        _stitchIndex     = 0;
        _isDragging      = false;
        _inputEnabled    = false;
        _finished        = false;
        _hoverAudioBusy  = false;
        _playerSelection.Clear();

        ClearThreads();
        SetAllAnchorsActive(false);
        ResetAllLayerColors();

        skyLayer.alphaHitTestMinimumThreshold        = 0.1f;
        earthLayer.alphaHitTestMinimumThreshold      = 0.1f;
        underworldLayer.alphaHitTestMinimumThreshold = 0.1f;

        StartCoroutine(IntroSequence());
    }

    protected override void OnCleanup()
    {
        StopAllCoroutines();
        _inputEnabled   = false;
        _isDragging     = false;
        _finished       = false;
        _hoverAudioBusy = false;
        _sfxSource.Stop();
        _hoverSource.Stop();
        ClearThreads();
        SetAllAnchorsActive(false);
        ResetAllLayerColors();
        StopDistortion();
        ResetAllDistortions();
    }

    // ── Phase 1: Intro ────────────────────────────────────────────────

    private IEnumerator IntroSequence()
    {
        _phase = GamePhase.Intro;
        yield return new WaitForSeconds(0.5f);

        foreach (LayerType layer in _correctOrder)
        {
            AudioClip clip = _clips[layer];
            if (clip != null) _sfxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip != null ? clip.length + 0.2f : 0.8f);
        }

        yield return new WaitForSeconds(0.3f);
        _phase        = GamePhase.Hover;
        _inputEnabled = true;
    }

    // ── Phase 2: Hover ────────────────────────────────────────────────

    public void OnLayerHoverEnter(LayerType layer)
    {
        if (_phase != GamePhase.Hover || _finished) return;
        if (_hoverAudioBusy) return;
        StartCoroutine(PlayHoverSound(layer));
    }

    private IEnumerator PlayHoverSound(LayerType layer)
    {
        AudioClip clip = _clips[layer];
        if (clip == null) yield break;

        _hoverAudioBusy   = true;
        _hoverSource.clip = clip;
        _hoverSource.Play();
        HighlightLayer(layer, hoverPlayColor);

        yield return new WaitForSeconds(clip.length);

        HighlightLayer(layer, idleColor);
        _hoverAudioBusy = false;
    }

    // ── Phase 2: Selection ────────────────────────────────────────────

    public void OnLayerClicked(LayerType clicked)
    {
        if (_phase != GamePhase.Hover || !_inputEnabled || _finished) return;
        if (_playerSelection.Contains(clicked)) return;

        int index = _playerSelection.Count;

        if (clicked != _correctOrder[index])
        {
            StartCoroutine(HandleSelectionFail());
            return;
        }

        _playerSelection.Add(clicked);
        HighlightLayer(clicked, selectedColor);

        if (_playerSelection.Count >= 2)
        {
            _inputEnabled = false;
            StartCoroutine(TransitionToStitching());
        }
    }

    private IEnumerator HandleSelectionFail()
    {
        _inputEnabled   = false;
        _hoverAudioBusy = false;
        _hoverSource.Stop();

        foreach (LayerType l in new[] { LayerType.Sky, LayerType.Earth, LayerType.Underworld })
            HighlightLayer(l, failColor);

        yield return new WaitForSeconds(1f);

        TriggerComplete(false);
    }

    // ── Phase 3: Stitching ────────────────────────────────────────────

    private IEnumerator TransitionToStitching()
    {
        yield return new WaitForSeconds(0.4f);
        _phase = GamePhase.Stitching;
        StartSeamDistortion();
        ActivateCurrentAnchors();
        _inputEnabled = true;
    }

    public void OnStitchPointerDown(RectTransform point)
    {
        if (_phase != GamePhase.Stitching || !_inputEnabled || _finished) return;

        var seam = CurrentSeam;
        if (point != seam.pointsA[_stitchIndex] &&
            point != seam.pointsB[_stitchIndex]) return;

        _isDragging   = true;
        _dragOrigin   = point;
        _activeThread = CreateThreadLine();
        SetAnchorColor(point, activeColor);
    }

    public void OnStitchPointerUp(Vector2 screenPos)
    {
        if (!_isDragging || _finished) return;
        _isDragging = false;

        var seam   = CurrentSeam;
        var target = _dragOrigin == seam.pointsA[_stitchIndex]
            ? seam.pointsB[_stitchIndex]
            : seam.pointsA[_stitchIndex];

        var  cam = canvas != null ? canvas.worldCamera : null;
        bool hit = Vector2.Distance(screenPos,
            RectTransformUtility.WorldToScreenPoint(cam, target.position)) < 40f;

        if (hit)
        {
            LockThread(_dragOrigin, target);
            SetAnchorColor(_dragOrigin, completedColor);
            SetAnchorColor(target,      completedColor);
            _stitchIndex++;

            if (_stitchIndex >= CurrentSeam.pointsA.Count)
                OnSeamComplete();
            else
                ActivateCurrentAnchors();
        }
        else
        {
            Destroy(_activeThread.gameObject);
            _activeThread = null;
            SetAnchorColor(_dragOrigin, activeColor);
        }
    }

    public void OnDrag(Vector2 screenPos)
    {
        if (!_isDragging || _activeThread == null || _finished) return;
        var     cam  = canvas != null ? canvas.worldCamera : null;
        Vector2 from = ToLocal(RectTransformUtility.WorldToScreenPoint(cam, _dragOrigin.position));
        DrawThread(_activeThread, from, ToLocal(screenPos));
    }

    // ── Seam progression ──────────────────────────────────────────────

    private void OnSeamComplete()
    {
        StopDistortion();
        ResetAllDistortions();
        _connectionIndex++;
        _stitchIndex = 0;

        if (_connectionIndex >= 2)
            StartCoroutine(WinSequence());
        else
            StartCoroutine(TransitionToStitching());
    }

    private StitchSeam CurrentSeam
    {
        get
        {
            bool topDown = _correctOrder[0] == LayerType.Sky;
            return _connectionIndex == 0
                ? (topDown ? skyEarthSeam : earthUnderworldSeam)
                : (topDown ? earthUnderworldSeam : skyEarthSeam);
        }
    }

    // ── Completion ────────────────────────────────────────────────────

    private IEnumerator WinSequence()
    {
        _inputEnabled = false;
        foreach (var kv in _images) kv.Value.color = successColor;
        yield return new WaitForSeconds(1.4f);
        TriggerComplete(true);
    }

    private void TriggerComplete(bool success)
    {
        if (_finished) return;
        _finished = true;
        Complete(success);
    }

    // ── Thread drawing ────────────────────────────────────────────────

    private Image CreateThreadLine()
    {
        var go  = new GameObject("Thread", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(threadContainer, false);
        var img = go.GetComponent<Image>();
        img.color = completedColor;
        if (threadSprite != null) img.sprite = threadSprite;
        img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return img;
    }

    private void DrawThread(Image line, Vector2 localFrom, Vector2 localTo)
    {
        Vector2 dir  = localTo - localFrom;
        float   dist = dir.magnitude;
        line.rectTransform.sizeDelta        = new Vector2(dist, 4f);
        line.rectTransform.anchoredPosition = (localFrom + localTo) * 0.5f;
        line.rectTransform.rotation         = Quaternion.Euler(0, 0,
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void LockThread(RectTransform a, RectTransform b)
    {
        var     cam  = canvas != null ? canvas.worldCamera : null;
        Vector2 posA = ToLocal(RectTransformUtility.WorldToScreenPoint(cam, a.position));
        Vector2 posB = ToLocal(RectTransformUtility.WorldToScreenPoint(cam, b.position));
        DrawThread(_activeThread, posA, posB);
        _lockedThreads.Add(_activeThread);
        _activeThread = null;
    }

    private void ClearThreads()
    {
        foreach (var t in _lockedThreads) if (t != null) Destroy(t.gameObject);
        _lockedThreads.Clear();
        if (_activeThread != null) { Destroy(_activeThread.gameObject); _activeThread = null; }
    }

    private Vector2 ToLocal(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            threadContainer, screenPos,
            canvas != null ? canvas.worldCamera : null,
            out Vector2 local);
        return local;
    }

    // ── Anchor helpers ────────────────────────────────────────────────

    private void ActivateCurrentAnchors()
    {
        SetAllAnchorsActive(false);
        var seam = CurrentSeam;
        seam.pointsA[_stitchIndex].gameObject.SetActive(true);
        seam.pointsB[_stitchIndex].gameObject.SetActive(true);
        SetAnchorColor(seam.pointsA[_stitchIndex], activeColor);
        SetAnchorColor(seam.pointsB[_stitchIndex], activeColor);
    }

    private void SetAllAnchorsActive(bool active)
    {
        foreach (var seam in new[] { skyEarthSeam, earthUnderworldSeam })
        {
            foreach (var p in seam.pointsA) if (p != null) p.gameObject.SetActive(active);
            foreach (var p in seam.pointsB) if (p != null) p.gameObject.SetActive(active);
        }
    }

    private static void SetAnchorColor(RectTransform rt, Color color)
    {
        var img = rt.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    // ── Visual helpers ────────────────────────────────────────────────

    private void HighlightLayer(LayerType layer, Color color)
    {
        if (_images.TryGetValue(layer, out var img))
            img.color = color;
    }

    private void ResetAllLayerColors()
    {
        foreach (var kv in _images)
            kv.Value.color = idleColor;
    }

    // ── Distortion ────────────────────────────────────────────────────

    private void StartSeamDistortion()
    {
        if (_distortionCoroutine != null) StopCoroutine(_distortionCoroutine);
        _distortionCoroutine = StartCoroutine(RampDistortion(GetCurrentPair()));
    }

    private IEnumerator RampDistortion(List<LayerType> pair)
    {
        var upperFx = _distortions.GetValueOrDefault(pair[0]);
        var lowerFx = _distortions.GetValueOrDefault(pair[1]);

        if (upperFx != null) { upperFx.pullDown = true;  upperFx.pullUp   = false; }
        if (lowerFx != null) { lowerFx.pullUp   = true;  lowerFx.pullDown = false; }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * distortionRampSpeed;
            float amount = Mathf.Clamp01(t);
            if (upperFx != null) upperFx.distortionAmount = amount;
            if (lowerFx != null) lowerFx.distortionAmount = amount;
            yield return null;
        }
    }

    private void StopDistortion()
    {
        if (_distortionCoroutine != null)
        {
            StopCoroutine(_distortionCoroutine);
            _distortionCoroutine = null;
        }
    }

    private void ResetAllDistortions()
    {
        foreach (var kv in _distortions) kv.Value?.Reset();
    }

    private List<LayerType> GetCurrentPair()
    {
        bool topDown = _correctOrder[0] == LayerType.Sky;
        return _connectionIndex == 0
            ? (topDown
                ? new List<LayerType> { LayerType.Sky,        LayerType.Earth }
                : new List<LayerType> { LayerType.Underworld, LayerType.Earth })
            : (topDown
                ? new List<LayerType> { LayerType.Earth, LayerType.Underworld }
                : new List<LayerType> { LayerType.Earth, LayerType.Sky });
    }

    // ── Setup ─────────────────────────────────────────────────────────

    private void RandomiseOrder()
    {
        bool topDown = Random.value > 0.5f;
        _correctOrder = topDown
            ? new List<LayerType> { LayerType.Sky,        LayerType.Earth, LayerType.Underworld }
            : new List<LayerType> { LayerType.Underworld, LayerType.Earth, LayerType.Sky };
    }

    private void BuildLookups()
    {
        _images = new()
        {
            { LayerType.Sky,        skyLayer        },
            { LayerType.Earth,      earthLayer      },
            { LayerType.Underworld, underworldLayer }
        };
        _clips = new()
        {
            { LayerType.Sky,        skyClip        },
            { LayerType.Earth,      earthClip      },
            { LayerType.Underworld, underworldClip }
        };
        _distortions = new()
        {
            { LayerType.Sky,        skyDistortion        },
            { LayerType.Earth,      earthDistortion      },
            { LayerType.Underworld, underworldDistortion }
        };
    }

    private void SetupAudio()
    {
        _sfxSource   = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        var sources  = GetComponents<AudioSource>();
        _hoverSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        _hoverSource.loop = false;
    }
}
