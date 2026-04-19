using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Minigames — one per task, in order")]
    [SerializeField] private List<Minigame> minigames = new();

    private Minigame _active;
    private CanvasGroup _activeGroup;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        foreach (var m in minigames)
        {
            if (m == null) continue;
            m.gameObject.SetActive(false);
            if (m.GetComponent<CanvasGroup>() == null)
                m.gameObject.AddComponent<CanvasGroup>();
        }

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
        int index = GameManager.Instance.TasksCompleted;

        if (index >= minigames.Count || minigames[index] == null)
        {
            Debug.LogWarning($"MinigameManager: no minigame for task {index}. Auto-passing.");
            ResolveCurrentMinigame(true);
            return;
        }

        _active = minigames[index];
        _activeGroup = _active.GetComponent<CanvasGroup>();

        // Enable shared CRT pipeline before transition
        MinigameCRTController.Instance?.SetPipelineActive(true);

        MinigameTransitionManager.Instance.TransitionIn(
            panelGroup: _activeGroup,
            onBlack: ActivatePanelHidden,
            onComplete: StartMinigameLogic
        );
    }

    private void ActivatePanelHidden()
    {
        _active.gameObject.SetActive(true);
        _active.OnPipelineEnable();
        if (_activeGroup != null)
        {
            _activeGroup.alpha = 0f;
            _activeGroup.interactable = false;
            _activeGroup.blocksRaycasts = false;
        }
    }

    private void StartMinigameLogic()
    {
        if (_activeGroup != null)
        {
            _activeGroup.interactable = true;
            _activeGroup.blocksRaycasts = true;
        }
        _active.Begin();
    }

    public void ResolveCurrentMinigame(bool success)
    {
        if (_activeGroup != null)
        {
            _activeGroup.interactable = false;
            _activeGroup.blocksRaycasts = false;
        }

        if (_active != null)
        {
            _active.OnPipelineDisable();
            _active.End();
        }

        MinigameTransitionManager.Instance.TransitionOut(
            panelGroup: _activeGroup,
            onBlack: () =>
            {
                // Disable CRT once fully black — before dialogue appears
                MinigameCRTController.Instance?.SetPipelineActive(false);
            },
            onComplete: () =>
            {
                _active = null;
                _activeGroup = null;
                GameManager.Instance.ReportTaskResult(success);
            }
        );
    }
}
