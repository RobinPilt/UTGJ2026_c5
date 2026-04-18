using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Minigames — one per task, in order")]
    [Tooltip("Index 0 = task 1, index 1 = task 2, index 2 = task 3.")]
    [SerializeField] private List<Minigame> minigames = new();

    private Minigame _active;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // All panels start hidden
        foreach (var m in minigames)
            if (m != null) m.gameObject.SetActive(false);

        // Subscribe to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.OnMinigameBegin.AddListener(BeginCurrentMinigame);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMinigameBegin.RemoveListener(BeginCurrentMinigame);
    }

    // ── Core ──────────────────────────────────────────────────────────

    private void BeginCurrentMinigame()
    {
        int index = GameManager.Instance.TasksCompleted; // 0-based, not yet incremented

        if (index >= minigames.Count || minigames[index] == null)
        {
            Debug.LogWarning($"MinigameManager: no minigame assigned for task {index}. Auto-passing.");
            ResolveCurrentMinigame(true);
            return;
        }

        _active = minigames[index];
        _active.Begin();
    }

    /// <summary>
    /// Called by Minigame.Complete(). Do not call this directly from minigame scripts —
    /// use Complete(true/false) inside your Minigame subclass instead.
    /// </summary>
    public void ResolveCurrentMinigame(bool success)
    {
        if (_active != null)
        {
            _active.End();
            _active = null;
        }

        GameManager.Instance.ReportTaskResult(success);
    }
}