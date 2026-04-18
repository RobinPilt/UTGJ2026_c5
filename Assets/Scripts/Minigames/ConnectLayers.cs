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

    [Header("Layer Images")]
    [SerializeField] private Image skyLayer;
    [SerializeField] private Image earthLayer;
    [SerializeField] private Image underworldLayer;

    [Header("Seams")]
    [SerializeField] private StitchSeam skyEarthSeam;
    [SerializeField] private StitchSeam earthUnderworldSeam;

    [Header("Thread")]
    [SerializeField] private RectTransform threadContainer;
    [SerializeField] private Sprite threadSprite;

    [Header("Intro Audio Cues")]
    [SerializeField] private AudioClip skyIntroClip;
    [SerializeField] private AudioClip earthIntroClip;
    [SerializeField] private AudioClip underworldIntroClip;

    [Header("Hover Ambient Sounds")]
    [SerializeField] private AudioClip skyHoverClip;
    [SerializeField] private AudioClip earthHoverClip;
    [SerializeField] private AudioClip underworldHoverClip;

    [Header("Canvas (leave null for Overlay)")]
    [SerializeField] private Canvas canvas;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color activeColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField] private Color completedColor = new Color(0.4f, 1f, 0.55f, 1f);
    [SerializeField] private Color successColor = new Color(0f, 1f, 0.3f, 1f);
    [SerializeField] private Color failColor = Color.red;

    // ── Internal ──────────────────────────────────────────────────────

    private List<LayerType> _correctOrder = new();
    private Dictionary<LayerType, Image> _images;
    private Dictionary<LayerType, AudioClip> _introCues;
    private Dictionary<LayerType, AudioClip> _hoverCues;
    private Dictionary<LayerType, Outline> _outlines;

    private AudioSource _sfxSource;
    private AudioSource _ambientSource;
    private bool _inputEnabled;

    private int _connectionIndex;
    private int _stitchIndex;
    private bool _isDragging;
    private RectTransform _dragOrigin;
    private Image _activeThread;
    private List<Image> _lockedThreads = new();

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Minigame interface ────────────────────────────────────────────

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

        ClearThreads();
        SetAllAnchorsActive(false);

        skyLayer.alphaHitTestMinimumThreshold = 0.1f;
        earthLayer.alphaHitTestMinimumThreshold = 0.1f;
        underworldLayer.alphaHitTestMinimumThreshold = 0.1f;

        StartCoroutine(PlayIntroThenEnable());
    }

    protected override void OnCleanup()
    {
        StopAllCoroutines();
        _inputEnabled = false;
        _isDragging = false;
        _ambientSource.Stop();
        ClearThreads();
        SetAllAnchorsActive(false);
        foreach (var kv in _outlines) ApplyOutline(kv.Key, idleColor, 3f);
    }

    // ── Stitch interaction (called by StitchAnchor) ───────────────────

    public void OnStitchPointerDown(RectTransform point)
    {
        if (!_inputEnabled) return;
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
        if (!_isDragging) return;
        _isDragging = false;

        var seam = CurrentSeam;
        var target = _dragOrigin == seam.pointsA[_stitchIndex]
            ? seam.pointsB[_stitchIndex]
            : seam.pointsA[_stitchIndex];

        bool hit = Vector2.Distance(screenPos, RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null, target.position)) < 40f;

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
            Destroy(_activeThread.gameObject);
            _activeThread = null;
            SetAnchorColor(_dragOrigin, activeColor);
        }
    }

    public void OnDrag(Vector2 screenPos)
    {
        if (!_isDragging || _activeThread == null) return;
        Vector2 from = ToLocal(RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null, _dragOrigin.position));
        DrawThread(_activeThread, from, ToLocal(screenPos));
    }

    // ── Seam progression ──────────────────────────────────────────────

    private void OnSeamComplete()
    {
        _connectionIndex++;
        _stitchIndex = 0;

        if (_connectionIndex >= 2)
            StartCoroutine(WinSequence());
        else
            ActivateCurrentAnchors();
    }

    private StitchSeam CurrentSeam
    {
        get
        {
            // Connection 0: seam between _correctOrder[0] and Earth
            // Connection 1: seam between Earth and _correctOrder[2]
            // Since order is always Sky-Earth-Underworld or Underworld-Earth-Sky,
            // connection 0 is sky-earth if going top-down, underworld-earth if bottom-up.
            bool topDown = _correctOrder[0] == LayerType.Sky;
            return _connectionIndex == 0
                ? (topDown ? skyEarthSeam : earthUnderworldSeam)
                : (topDown ? earthUnderworldSeam : skyEarthSeam);
        }
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
        ActivateCurrentAnchors();
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

        ClearThreads();
        _connectionIndex = 0;
        _stitchIndex = 0;
        foreach (var kv in _outlines) ApplyOutline(kv.Key, idleColor, 3f);
        RandomiseOrder();
        StartCoroutine(PlayIntroThenEnable());
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

    // ── Helpers ───────────────────────────────────────────────────────

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
}
