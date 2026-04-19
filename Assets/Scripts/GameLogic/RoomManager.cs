using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoomManager : MonoBehaviour
{
    [Header("Slots — PropSlot objects placed in scene")]
    [SerializeField] private List<PropSlot> slots = new();

    [Header("Props — all moveable objects including objective props")]
    [SerializeField] private List<GameObject> props = new();

    [Header("Objectives — one per task, order does NOT matter here")]
    [Tooltip("Each entry is one objective. Order is randomised at runtime.")]
    [SerializeField] private List<ObjectiveData> objectives = new();

    [Header("Reload Feel")]
    [SerializeField] private float fadeInDelay = 0.2f;
    [SerializeField] private float hintDelay = 0.5f;

    [Header("Events")]
    [Tooltip("Fired after fade-in. Wire to DialogueManager.PlayHint.")]
    public UnityEvent OnHintBegin;

    // ── Internal ──────────────────────────────────────────────────────
    private List<int> _shuffledOrder = new(); // shuffled indices into objectives list
    private int _currentRound = 0;     // 0, 1, 2

    // ── Public accessors ──────────────────────────────────────────────

    /// <summary>
    /// Returns the ObjectiveData for the current round.
    /// DialogueManager calls this to know which hint + audio to play.
    /// </summary>
    public ObjectiveData CurrentObjective =>
        objectives[_shuffledOrder[_currentRound]];

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        // Must run before GameManager.Start() calls PlayInitialHint()
        ShuffleObjectiveOrder();
        DeactivateAllObjectives();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoomReloadBegin.AddListener(BeginReload);

        // Objective already shuffled in Awake — just activate current one
        ActivateCurrentObjective();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoomReloadBegin.RemoveListener(BeginReload);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Called once on first load — skips fade/shuffle, just plays hint.</summary>
    public void PlayInitialHint()
    {
        ActivateCurrentObjective();
        OnHintBegin?.Invoke();
    }

    /// <summary>Called by DialogueManager when hint sequence finishes.</summary>
    public void OnHintComplete()
    {
        GameManager.Instance.OnRoomReady();
    }

    // ── Reload sequence ───────────────────────────────────────────────

    public void BeginReload()
    {
        Debug.Log($"BeginReload called — currentRound={_currentRound}");
        DeactivateObjective(_shuffledOrder[_currentRound]);
        _currentRound++;
        Debug.Log($"BeginReload — incrementing to round {_currentRound}");
        StartCoroutine(ReloadSequence());
    }

    private IEnumerator ReloadSequence()
    {
        Debug.Log("ReloadSequence — fading out");
        bool fadeComplete = false;
        ScreenFader.Instance.FadeOut(() => fadeComplete = true);
        yield return new WaitUntil(() => fadeComplete);

        Debug.Log("ReloadSequence — shuffling props");
        yield return new WaitForSeconds(fadeInDelay);
        ShuffleProps();
        ActivateCurrentObjective();

        Debug.Log("ReloadSequence — fading in");
        bool fadeInDone = false;
        ScreenFader.Instance.FadeIn(() => fadeInDone = true);
        yield return new WaitUntil(() => fadeInDone);

        Debug.Log("ReloadSequence — firing OnHintBegin");
        yield return new WaitForSeconds(hintDelay);
        OnHintBegin?.Invoke();
    }

    // ── Objective management ──────────────────────────────────────────

    private void ShuffleObjectiveOrder()
    {
        _shuffledOrder.Clear();
        for (int i = 0; i < objectives.Count; i++)
            _shuffledOrder.Add(i);

        // Fisher-Yates
        for (int i = _shuffledOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_shuffledOrder[i], _shuffledOrder[j]) = (_shuffledOrder[j], _shuffledOrder[i]);
        }
    }

    private void ActivateCurrentObjective()
    {
        if (_currentRound >= _shuffledOrder.Count) return;

        ObjectiveData obj = objectives[_shuffledOrder[_currentRound]];
        obj.trigger?.PlaceAt(obj.trigger.transform.position);
    }

    private void DeactivateObjective(int index)
    {
        if (index < 0 || index >= objectives.Count) return;
        objectives[index].trigger?.Deactivate();
    }

    private void DeactivateAllObjectives()
    {
        foreach (var obj in objectives)
            obj.trigger?.Deactivate();
    }

    // ── Prop shuffling ────────────────────────────────────────────────

    private void ShuffleProps()
    {
        if (props.Count == 0 || slots.Count == 0) return;

        List<PropSlot> shuffled = new(slots);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int count = Mathf.Min(props.Count, shuffled.Count);
        for (int i = 0; i < count; i++)
        {
            if (props[i] == null) continue;
            props[i].transform.position = shuffled[i].transform.position;
            props[i].transform.rotation = shuffled[i].transform.rotation;
        }
    }
}

/// <summary>
/// Groups everything about one objective in one Inspector entry.
/// </summary>
[System.Serializable]
public class ObjectiveData
{
    public string objectiveName;  // just for your sanity in the Inspector
    public ObjectiveTrigger trigger;       // the prop with ObjectiveTrigger.cs
    public AudioClip hintAudio;     // plays when this objective is hinted
    [TextArea(1, 3)]
    public string hintLine;      // Vanapagan's line for this objective
}