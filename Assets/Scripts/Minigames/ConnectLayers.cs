using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StitchSeam
{
    [Tooltip("Stitch anchors on the upper layer, numbered in order")]
    public List<RectTransform> pointsA;
    [Tooltip("Matching anchors on the lower layer, numbered in order")]
    public List<RectTransform> pointsB;
}

public class ConnectLayers : Minigame
{
    public static ConnectLayers Instance { get; private set; }
    public enum LayerType { Sky = 0, Earth = 1, Underworld = 2 }

    private enum GamePhase { Intro, LayerSelection, Stitching }

    [Header("Layer Images")]
    [SerializeField] private Image skyLayer;
    [SerializeField] private Image earthLayer;
    [SerializeField] private Image underworldLayer;

    [Header("Distortion")]
    [SerializeField] private LayerDistortionEffect skyDistortion;
    [SerializeField] private LayerDistortionEffect earthDistortion;
    [SerializeField] private LayerDistortionEffect underworldDistortion;
    [SerializeField] private float distortionRampSpeed = 2f; // how fast distortion builds

    [Header("Seams")]
    [SerializeField] private StitchSeam skyEarthSeam;
    [SerializeField] private StitchSeam earthUnderworldSeam;

    [Header("Thread")]
    [SerializeField] private RectTransform threadContainer;
    [SerializeField] private Sprite threadSprite;

    [Header("Intro Audio — plays once to introduce each layer")]
    [SerializeField] private AudioClip skyIntroClip;
    [SerializeField] private AudioClip earthIntroClip;
    [SerializeField] private AudioClip underworldIntroClip;

    [Header("Selection Audio — plays as cue for each seam's pair")]
    [SerializeField] private AudioClip skySelectClip;
    [SerializeField] private AudioClip earthSelectClip;
    [SerializeField] private AudioClip underworldSelectClip;

    [Header("Canvas (leave null for Overlay)")]
    [SerializeField] private Canvas canvas;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color activeColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField] private Color completedColor = new Color(0.4f, 1f, 0.55f, 1f);
    [SerializeField] private Color successColor = new Color(0f, 1f, 0.3f, 1f);
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.8f, 1f, 1f);

    // ── Internal ──────────────────────────────────────────────────────

    private GamePhase _phase;
    private List<LayerType> _correctOrder = new();
    private List<LayerType> _playerSelection = new();
    private List<LayerType> _currentPair = new();

    private Dictionary<LayerType, Image> _images;
    private Dictionary<LayerType, AudioClip> _introCues;
    private Dictionary<LayerType, AudioClip> _selectCues;
    private Dictionary<LayerType, Outline> _outlines;

    private AudioSource _sfxSource;
    private bool _inputEnabled;
    private bool _finished;

    private int _connectionIndex;
    private int _stitchIndex;
    private bool _isDragging;
    private RectTransform _dragOrigin;
    private Image _activeThread;
    private List<Image> _lockedThreads = new();

    private Coroutine _distortionCoroutine;

    private Dictionary<LayerType, LayerDistortionEffect> _distortions;

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
        SetupOutlines();
        SetupAudio();
        RandomiseOrder();

        _connectionIndex = 0;
        _stitchIndex = 0;
        _isDragging = false;
        _inputEnabled = false;
        _finished = false;
        _playerSelection.Clear();

        ClearThreads();
        SetAllAnchorsActive(false);

        // Allow clicking on non-transparent pixels of layer images
        skyLayer.alphaHitTestMinimumThreshold = 0.1f;
        earthLayer.alphaHitTestMinimumThreshold = 0.1f;
        underworldLayer.alphaHitTestMinimumThreshold = 0.1f;

        StartCoroutine(IntroSequence());
    }

    protected override void OnCleanup()
    {
        StopAllCoroutines();
        _inputEnabled = false;
        _isDragging = false;
        _finished = false;
        _sfxSource.Stop();
        ClearThreads();
        SetAllAnchorsActive(false);
        ResetAllOutlines();
        ResetAllLayerColors();
        StopDistortion();
        ResetAllDistortions();
    }

    // ── Phase 1: Intro — plays all 3 layer sounds to introduce them ──

    private IEnumerator IntroSequence()
    {
        _phase = GamePhase.Intro;
        yield return new WaitForSeconds(0.5f);

        // Play each layer's intro sound so the player learns what each sounds like
        foreach (LayerType layer in new[] { LayerType.Sky, LayerType.Earth, LayerType.Underworld })
        {
            AudioClip clip = _introCues[layer];
            HighlightLayer(layer, activeColor);
            if (clip != null) _sfxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip != null ? clip.length + 0.2f : 0.8f);
            HighlightLayer(layer, idleColor);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.3f);

        // Move into selection phase for the first seam
        StartCoroutine(SelectionCueSequence());
    }

    // ── Phase 2: Layer selection — play cues, player clicks the pair ─

    private IEnumerator SelectionCueSequence()
    {
        _phase = GamePhase.LayerSelection;
        _playerSelection.Clear();
        _currentPair = GetCurrentPair();

        // Play the two cues for this seam's layers so the player knows what to click
        foreach (LayerType layer in _currentPair)
        {
            AudioClip clip = _selectCues[layer];
            HighlightLayer(layer, activeColor);
            if (clip != null) _sfxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip != null ? clip.length + 0.2f : 0.8f);
            HighlightLayer(layer, idleColor);
            yield return new WaitForSeconds(0.15f);
        }

        // Now wait for player to click the two correct layers
        _inputEnabled = true;
    }

    /// <summary>Called by LayerClickHandler on each layer Image.</summary>
    public void OnLayerClicked(LayerType clicked)
    {
        if (_phase != GamePhase.LayerSelection || !_inputEnabled || _finished) return;
        if (_playerSelection.Contains(clicked)) return; // already selected this one

        int index = _playerSelection.Count;

        // Wrong layer clicked
        if (clicked != _currentPair[index])
        {
            StartCoroutine(HandleSelectionFail());
            return;
        }

        // Correct click
        _playerSelection.Add(clicked);
        HighlightLayer(clicked, selectedColor);

        // Both layers selected correctly — move to stitching
        if (_playerSelection.Count >= 2)
        {
            _inputEnabled = false;
            StartCoroutine(TransitionToStitching());
        }
    }

    private IEnumerator TransitionToStitching()
    {
        yield return new WaitForSeconds(0.4f);
        _phase = GamePhase.Stitching;

        // Start distortion on the two active layers for this seam
        StartSeamDistortion();

        ActivateCurrentAnchors();
        _inputEnabled = true;
    }

    private IEnumerator HandleSelectionFail()
    {
        _inputEnabled = false;
        ResetAllOutlines();

        // Flash fail color on all layers
        foreach (LayerType l in new[] { LayerType.Sky, LayerType.Earth, LayerType.Underworld })
            HighlightLayer(l, failColor);

        yield return new WaitForSeconds(1f);
        TriggerComplete(false);
    }

    // ── Phase 3: Stitching ────────────────────────────────────────────

    public void OnStitchPointerDown(RectTransform point)
    {
        if (_phase != GamePhase.Stitching || !_inputEnabled || _finished) return;

        var seam = CurrentSeam;
        if (point != seam.pointsA[_stitchIndex] &&
            point != seam.pointsB[_stitchIndex]) return;

        _isDragging = true;
        _dragOrigin = point;
        _activeThread = CreateThreadLine();
        SetAnchorColor(point, activeColor);
    }

    public void OnStitchPointerUp(Vector2 screenPos)
    {
        if (!_isDragging || _finished) return;
        _isDragging = false;

        var seam = CurrentSeam;
        var target = _dragOrigin == seam.pointsA[_stitchIndex]
            ? seam.pointsB[_stitchIndex]
            : seam.pointsA[_stitchIndex];

        var cam = canvas != null ? canvas.worldCamera : null;
        bool hit = Vector2.Distance(
            screenPos,
            RectTransformUtility.WorldToScreenPoint(cam, target.position)) < 40f;

        if (hit)
        {
            LockThread(_dragOrigin, target);
            SetAnchorColor(_dragOrigin, completedColor);
            SetAnchorColor(target, completedColor);
            _stitchIndex++;

            if (_stitchIndex >= CurrentSeam.pointsA.Count)
                OnSeamComplete();
        }
        else
        {
            // Missed the target — destroy the dangling thread, keep trying
            Destroy(_activeThread.gameObject);
            _activeThread = null;
            SetAnchorColor(_dragOrigin, activeColor);
        }
    }

    public void OnDrag(Vector2 screenPos)
    {
        if (!_isDragging || _activeThread == null || _finished) return;

        var cam = canvas != null ? canvas.worldCamera : null;
        Vector2 from = ToLocal(RectTransformUtility.WorldToScreenPoint(cam, _dragOrigin.position));
        DrawThread(_activeThread, from, ToLocal(screenPos));
    }

    // ── Seam progression ──────────────────────────────────────────────

    private void OnSeamComplete()
    {
        // Snap distortion back on completed seam's layers
        StopDistortion();
        ResetAllDistortions();

        _connectionIndex++;
        _stitchIndex = 0;

        if (_connectionIndex >= 2)
        {
            StartCoroutine(WinSequence());
        }
        else
        {
            _inputEnabled = false;
            ResetLayerColors();
            StartCoroutine(SelectionCueSequence());
        }
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

    /// <summary>Returns the two LayerTypes involved in the current seam, in order.</summary>
    private List<LayerType> GetCurrentPair()
    {
        bool topDown = _correctOrder[0] == LayerType.Sky;
        return _connectionIndex == 0
            ? (topDown
                ? new List<LayerType> { LayerType.Sky, LayerType.Earth }
                : new List<LayerType> { LayerType.Underworld, LayerType.Earth })
            : (topDown
                ? new List<LayerType> { LayerType.Earth, LayerType.Underworld }
                : new List<LayerType> { LayerType.Earth, LayerType.Sky });
    }

    // ── Completion ────────────────────────────────────────────────────

    private IEnumerator WinSequence()
    {
        _inputEnabled = false;
        foreach (var kv in _outlines) ApplyOutline(kv.Key, successColor, 14f);
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
        var go = new GameObject("Thread", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(threadContainer, false);
        var img = go.GetComponent<Image>();
        img.color = completedColor;
        if (threadSprite != null) img.sprite = threadSprite;
        img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return img;
    }

    private void DrawThread(Image line, Vector2 localFrom, Vector2 localTo)
    {
        Vector2 dir = localTo - localFrom;
        float dist = dir.magnitude;
        line.rectTransform.sizeDelta = new Vector2(dist, 4f);
        line.rectTransform.anchoredPosition = (localFrom + localTo) * 0.5f;
        line.rectTransform.rotation = Quaternion.Euler(0, 0,
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void LockThread(RectTransform a, RectTransform b)
    {
        var cam = canvas != null ? canvas.worldCamera : null;
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
        if (_outlines.TryGetValue(layer, out var o))
        {
            o.effectColor = color;
            o.effectDistance = new Vector2(8f, 8f);
        }
    }

    private void ResetAllOutlines()
    {
        foreach (var kv in _outlines) ApplyOutline(kv.Key, idleColor, 3f);
    }

    private void ResetLayerColors()
    {
        foreach (LayerType l in new[] { LayerType.Sky, LayerType.Earth, LayerType.Underworld })
            HighlightLayer(l, idleColor);
    }

    private void ResetAllLayerColors() => ResetLayerColors();

    private void ApplyOutline(LayerType layer, Color color, float size)
    {
        _outlines[layer].effectColor = color;
        _outlines[layer].effectDistance = new Vector2(size, size);
    }

    // ── Distortion ────────────────────────────────────────────────────

    private void StartSeamDistortion()
    {
        List<LayerType> pair = GetCurrentPair();
        if (_distortionCoroutine != null) StopCoroutine(_distortionCoroutine);
        _distortionCoroutine = StartCoroutine(RampDistortion(pair));
    }

    private IEnumerator RampDistortion(List<LayerType> pair)
    {
        // Determine which edges pull based on the pair
        // pair[0] is the upper layer — its bottom edge pulls down
        // pair[1] is the lower layer — its top edge pulls up
        LayerType upper = pair[0];
        LayerType lower = pair[1];

        var upperFx = _distortions.GetValueOrDefault(upper);
        var lowerFx = _distortions.GetValueOrDefault(lower);

        if (upperFx != null) { upperFx.pullDown = true; upperFx.pullUp = false; }
        if (lowerFx != null) { lowerFx.pullUp = true; lowerFx.pullDown = false; }

        // Ramp up distortion over time while stitching
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
        foreach (var kv in _distortions)
            kv.Value?.Reset();
    }


    // ── Setup helpers ─────────────────────────────────────────────────

    private void RandomiseOrder()
    {
        bool topDown = Random.value > 0.5f;
        _correctOrder = topDown
            ? new List<LayerType> { LayerType.Sky, LayerType.Earth, LayerType.Underworld }
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
        _introCues = new()
        {
            { LayerType.Sky,        skyIntroClip        },
            { LayerType.Earth,      earthIntroClip      },
            { LayerType.Underworld, underworldIntroClip }
        };
        _selectCues = new()
        {
            { LayerType.Sky,        skySelectClip        },
            { LayerType.Earth,      earthSelectClip      },
            { LayerType.Underworld, underworldSelectClip }
        };
        _distortions = new()
        {
            { LayerType.Sky,        skyDistortion        },
            { LayerType.Earth,      earthDistortion      },
            { LayerType.Underworld, underworldDistortion }
        };
    }

    private void SetupOutlines()
    {
        _outlines = new();
        foreach (var kv in _images)
        {
            var o = kv.Value.GetComponent<Outline>()
                    ?? kv.Value.gameObject.AddComponent<Outline>();
            o.effectColor = idleColor;
            o.effectDistance = new Vector2(3, 3);
            _outlines[kv.Key] = o;
        }
    }

    private void SetupAudio()
    {
        _sfxSource = GetComponent<AudioSource>()
                     ?? gameObject.AddComponent<AudioSource>();
    }
}