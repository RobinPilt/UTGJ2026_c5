using UnityEngine;
using UnityEngine.Events;

public enum GameState { Navigation, Minigame, Dialogue, RoomReload, EndGame }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // TEMP — remove when MinigameManager exists
    [ContextMenu("DEBUG Complete Task")]
    public void DebugCompleteTask() => ReportTaskResult(true);

    [ContextMenu("DEBUG Fail Task")]
    public void DebugFailTask() => ReportTaskResult(false);

    [Header("Settings")]
    [SerializeField] private int totalTasks = 3;
    [SerializeField] private int tasksNeededForGoodEnding = 2; // out of 3

    [Header("State Events — wire these in the Inspector")]
    public UnityEvent OnNavigationBegin;
    public UnityEvent OnMinigameBegin;
    public UnityEvent<bool> OnMinigameResolved; // true = success
    public UnityEvent OnDialogueBegin;
    public UnityEvent OnRoomReloadBegin;
    public UnityEvent<bool> OnGameEnd;           // true = good ending

    // ── Internal state ─────────────────────────────────────────────
    private GameState _state;
    private int _tasksCompleted;
    private int _tasksSucceeded;

    public GameState State => _state;
    public int TasksCompleted => _tasksCompleted;

    // ── Lifecycle ───────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => TransitionTo(GameState.Navigation);

    // ── Core state machine ──────────────────────────────────────────
    public void TransitionTo(GameState next)
    {
        _state = next;
        switch (next)
        {
            case GameState.Navigation: OnNavigationBegin?.Invoke(); break;
            case GameState.Minigame: OnMinigameBegin?.Invoke(); break;
            case GameState.Dialogue: OnDialogueBegin?.Invoke(); break;
            case GameState.RoomReload: OnRoomReloadBegin?.Invoke(); break;
            case GameState.EndGame: OnGameEnd?.Invoke(IsGoodEnding()); break;
        }
    }

    // ── Public hooks (called by MinigameManager, DialogueManager, RoomManager)

    /// <summary>MinigameManager calls this when the task finishes.</summary>
    public void ReportTaskResult(bool success)
    {
        _tasksCompleted++;
        if (success) _tasksSucceeded++;
        OnMinigameResolved?.Invoke(success);
        TransitionTo(GameState.Dialogue);
    }

    /// <summary>DialogueManager calls this when all lines are done.</summary>
    public void OnDialogueFinished()
    {
        if (_tasksCompleted >= totalTasks)
            TransitionTo(GameState.EndGame);
        else
            TransitionTo(GameState.RoomReload);
    }

    /// <summary>RoomManager calls this when the room is ready for the player.</summary>
    public void OnRoomReady() => TransitionTo(GameState.Navigation);

    // ── Helpers ─────────────────────────────────────────────────────
    private bool IsGoodEnding() => _tasksSucceeded >= tasksNeededForGoodEnding;
}