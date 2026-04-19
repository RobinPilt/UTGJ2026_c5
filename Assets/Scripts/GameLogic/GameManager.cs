using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum GameState { Navigation, Minigame, Dialogue, RoomReload, EndGame }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int totalTasks = 3;

    [Header("Scenes")]
    [SerializeField] private string outroSceneName = "OutroScene";

    [Header("State Events — wire these in the Inspector")]
    public UnityEvent OnNavigationBegin;
    public UnityEvent OnMinigameBegin;
    public UnityEvent<bool> OnMinigameResolved;
    public UnityEvent OnDialogueBegin;
    public UnityEvent OnRoomReloadBegin;

    // ── Internal state ─────────────────────────────────────────────────
    private GameState _state;
    private int _tasksCompleted;
    private int _tasksSucceeded;

    public GameState State => _state;
    public int TasksCompleted => _tasksCompleted;

    // ── Lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _state = GameState.RoomReload;
        _tasksCompleted = 0;
        _tasksSucceeded = 0;
        StartCoroutine(InitialHintNextFrame());
    }

    private IEnumerator InitialHintNextFrame()
    {
        yield return null; // wait one frame — all Start() methods finish first
        FindAnyObjectByType<RoomManager>()?.PlayInitialHint();
    }

    // ── Core state machine ──────────────────────────────────────────────
    public void TransitionTo(GameState next)
    {
        _state = next;
        switch (next)
        {
            case GameState.Navigation: OnNavigationBegin?.Invoke(); break;
            case GameState.Minigame: OnMinigameBegin?.Invoke(); break;
            case GameState.Dialogue: OnDialogueBegin?.Invoke(); break;
            case GameState.RoomReload: OnRoomReloadBegin?.Invoke(); break;
        }
    }

    // ── Public hooks ────────────────────────────────────────────────────

    /// <summary>Called by MinigameManager when a task finishes.</summary>
    public void ReportTaskResult(bool success)
    {
        _tasksCompleted++;
        if (success) _tasksSucceeded++;

        OnMinigameResolved?.Invoke(success);

        // Any failure → immediate bad ending
        if (!success)
        {
            TriggerEnding(false);
            return;
        }

        // All tasks passed → good ending
        if (_tasksCompleted >= totalTasks)
        {
            TriggerEnding(true);
            return;
        }

        // More tasks remain → continue loop
        TransitionTo(GameState.Dialogue);
    }

    /// <summary>Called by DialogueManager when post-task lines finish.</summary>
    public void OnDialogueFinished()
    {
        TransitionTo(GameState.RoomReload);
    }

    /// <summary>Called by RoomManager when room is ready for the player.</summary>
    public void OnRoomReady() => TransitionTo(GameState.Navigation);

    // ── Ending ──────────────────────────────────────────────────────────
    private void TriggerEnding(bool goodEnding)
    {
        _state = GameState.EndGame;

        // Store result so OutroScene can read it
        if (GameResult.Instance != null)
        {
            GameResult.Instance.Set(goodEnding);
        }
        else
        {
            var go = new GameObject("GameResult");
            var gr = go.AddComponent<GameResult>();
            gr.Set(goodEnding);
        }

        // Fade out then load OutroScene
        ScreenFader.Instance.FadeOut(() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(outroSceneName));
    }

    // ── Debug ───────────────────────────────────────────────────────────
    [ContextMenu("DEBUG Complete Task")]
    public void DebugCompleteTask() => ReportTaskResult(true);

    [ContextMenu("DEBUG Fail Task")]
    public void DebugFailTask() => ReportTaskResult(false);
}