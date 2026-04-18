using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private GameObject continuePrompt; // "Click to continue" indicator

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.035f; // seconds per character
    [SerializeField] private float autoAdvanceDelay = 0f;     // 0 = wait for click, >0 = auto

    [Header("Vanapagan Lines")]
    [Tooltip("One DialogueSequence per task, in order. Index 0 = after task 1 resolves.")]
    [SerializeField] private List<DialogueSequence> postTaskSequences = new();

    [Tooltip("One DialogueSequence per task hint, in order. Index 0 = hint before task 1.")]
    [SerializeField] private List<DialogueSequence> hintSequences = new();

    // ── Internal ─────────────────────────────────────────────────────
    private Coroutine _typeCoroutine;
    private bool _lineComplete;
    private bool _waitingForInput;

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Start()
    {
        dialoguePanel.SetActive(false);
        continuePrompt.SetActive(false);
    }

    private void Update()
    {
        // Click or spacebar advances dialogue
        if (!_waitingForInput) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            AdvanceLine();
    }

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Called by RoomManager.OnHintBegin.
    /// Plays the hint for the upcoming task, then calls RoomManager.OnHintComplete.
    /// </summary>
    public void PlayHint()
    {
        int taskIndex = GameManager.Instance.TasksCompleted; // 0-based
        if (taskIndex >= hintSequences.Count)
        {
            // No hint defined — skip straight through
            RoomManager_FinishHint();
            return;
        }
        StartDialogue(hintSequences[taskIndex], RoomManager_FinishHint);
    }

    /// <summary>
    /// Called by GameManager.OnMinigameResolved (wire in Inspector).
    /// Plays Vanapagan's post-task reaction, then calls GameManager.OnDialogueFinished.
    /// </summary>
    public void PlayPostTask(bool taskSucceeded)
    {
        int taskIndex = GameManager.Instance.TasksCompleted - 1; // -1: task already counted
        if (taskIndex < 0 || taskIndex >= postTaskSequences.Count)
        {
            GameManager.Instance.OnDialogueFinished();
            return;
        }
        StartDialogue(postTaskSequences[taskIndex], GameManager.Instance.OnDialogueFinished);
    }

    // ── Core dialogue runner ─────────────────────────────────────────
    private List<DialogueLine> _lines;
    private int _lineIndex;
    private System.Action _onComplete;

    private void StartDialogue(DialogueSequence sequence, System.Action onComplete)
    {
        _lines = sequence.lines;
        _lineIndex = 0;
        _onComplete = onComplete;

        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_lineIndex >= _lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = _lines[_lineIndex];
        speakerLabel.text = line.speaker;

        continuePrompt.SetActive(false);
        _lineComplete = false;
        _waitingForInput = false;

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string text)
    {
        bodyText.text = "";
        foreach (char c in text)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        _lineComplete = true;
        continuePrompt.SetActive(true);

        if (autoAdvanceDelay > 0f)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            AdvanceLine();
        }
        else
        {
            _waitingForInput = true;
        }
    }

    private void AdvanceLine()
    {
        // If still typing, skip to end of current line first
        if (!_lineComplete)
        {
            StopCoroutine(_typeCoroutine);
            bodyText.text = _lines[_lineIndex].text;
            _lineComplete = true;
            continuePrompt.SetActive(true);

            if (autoAdvanceDelay <= 0f)
                _waitingForInput = true;
            return;
        }

        _waitingForInput = false;
        _lineIndex++;
        ShowCurrentLine();
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        continuePrompt.SetActive(false);
        _waitingForInput = false;
        _onComplete?.Invoke();
    }

    // ── Helpers ──────────────────────────────────────────────────────
    private void RoomManager_FinishHint()
    {
        // Find RoomManager without a hard reference
        FindFirstObjectByType<RoomManager>()?.OnHintComplete();
    }
}

// ── Data structures (no MonoBehaviour, just data) ────────────────────
[System.Serializable]
public class DialogueLine
{
    public string speaker = "Vanapagan";
    [TextArea(2, 5)]
    public string text;
}

[System.Serializable]
public class DialogueSequence
{
    public string sequenceName; // just for your sanity in the Inspector
    public List<DialogueLine> lines = new();
}