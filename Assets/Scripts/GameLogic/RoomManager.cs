using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoomManager : MonoBehaviour
{
    [Header("Slots — place PropSlot objects in the scene")]
    [SerializeField] private List<PropSlot> slots = new();

    [Header("Props — all moveable objects in the room")]
    [SerializeField] private List<GameObject> props = new();

    [Header("Objective props — one per task, in order")]
    [Tooltip("Assign the GameObject that holds ObjectiveTrigger for each task (index 0 = task 1).")]
    [SerializeField] private List<ObjectiveTrigger> objectiveTriggers = new();

    [Header("Reload Feel")]
    [SerializeField] private float fadeInDelay = 0.2f; // pause on black before fade-in
    [SerializeField] private float hintDelay = 0.5f; // pause after fade-in before hint fires

    [Header("Events")]
    [Tooltip("Fired after fade-in. Wire your DialogueManager hint method here.")]
    public UnityEvent OnHintBegin;

    // Temporary — add to RoomManager for testing, remove later
    [ContextMenu("DEBUG Skip Hint")]
    public void DebugSkipHint() => OnHintComplete();

    // ── Internal ─────────────────────────────────────────────────────
    private int _currentTaskIndex = 0; // tracks which objectiveTrigger is active

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Called by GameManager.OnRoomReloadBegin (wire in Inspector).
    /// Runs the full: fade → shuffle → fade-in → hint sequence.
    /// </summary>
    public void BeginReload()
    {
        _currentTaskIndex = GameManager.Instance.TasksCompleted; // 0-based
        StartCoroutine(ReloadSequence());
    }

    /// <summary>
    /// Called by DialogueManager (or whoever runs the hint) when hint is done.
    /// Hands control back to GameManager → unlocks player input.
    /// </summary>
    public void OnHintComplete()
    {
        ActivateCurrentObjective();
        GameManager.Instance.OnRoomReady();
    }

    // ── Sequence ─────────────────────────────────────────────────────
    private IEnumerator ReloadSequence()
    {
        // 1. Fade to black
        bool blackReached = false;
        ScreenFader.Instance.FadeOut(() => blackReached = true);
        yield return new WaitUntil(() => blackReached);

        // 2. Shuffle props while hidden
        yield return new WaitForSeconds(fadeInDelay);
        ShuffleProps();
        DeactivateAllObjectives();

        // 3. Fade back in
        bool fadeInDone = false;
        ScreenFader.Instance.FadeIn(() => fadeInDone = true);
        yield return new WaitUntil(() => fadeInDone);

        // 4. Brief pause then fire hint
        yield return new WaitForSeconds(hintDelay);
        OnHintBegin?.Invoke();

        // OnHintComplete() will be called externally to finish the sequence
    }

    // ── Prop shuffling ────────────────────────────────────────────────
    private void ShuffleProps()
    {
        if (props.Count == 0 || slots.Count == 0)
        {
            Debug.LogWarning("RoomManager: no props or slots assigned.");
            return;
        }

        // Shuffle the slots list (Fisher-Yates)
        List<PropSlot> shuffled = new(slots);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Assign each prop to a slot — more props than slots is fine (extras stay put)
        int count = Mathf.Min(props.Count, shuffled.Count);
        for (int i = 0; i < count; i++)
        {
            if (props[i] == null) continue;
            props[i].transform.position = shuffled[i].transform.position;
            props[i].transform.rotation = shuffled[i].transform.rotation;
        }
    }

    // ── Objective management ──────────────────────────────────────────
    private void DeactivateAllObjectives()
    {
        foreach (var trigger in objectiveTriggers)
            trigger?.Deactivate();
    }

    private void ActivateCurrentObjective()
    {
        if (_currentTaskIndex >= objectiveTriggers.Count)
        {
            Debug.LogWarning($"RoomManager: no ObjectiveTrigger for task {_currentTaskIndex}.");
            return;
        }
        objectiveTriggers[_currentTaskIndex]?.PlaceAt(
            objectiveTriggers[_currentTaskIndex].transform.position
        );
    }

    // ── First load ────────────────────────────────────────────────────
    private void Start()
    {
        // Wire GameManager event automatically
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoomReloadBegin.AddListener(BeginReload);

        // Activate task 0 objective on first load
        ActivateCurrentObjective();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoomReloadBegin.RemoveListener(BeginReload);
    }

    /// <summary>
    /// Called once on first scene load. Skips the fade and shuffle —
    /// just fires the hint, then hands off to Navigation.
    /// </summary>
    public void PlayInitialHint()
    {
        ActivateCurrentObjective();
        OnHintBegin?.Invoke(); // → DialogueManager.PlayHint()
                               // OnHintComplete() → GameManager.OnRoomReady() → Navigation as usual
    }
}