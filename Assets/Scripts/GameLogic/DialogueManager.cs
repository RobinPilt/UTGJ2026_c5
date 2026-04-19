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
        if (!_waitingForInput) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("DialogueManager — input received, advancing line");
            AdvanceLine();
        }
    }

    private void OnDisable()
    {
        Debug.LogError("DialogueManager was DISABLED — this is killing the coroutine");
    }

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Called by RoomManager.OnHintBegin.
    /// Plays the hint for the upcoming task, then calls RoomManager.OnHintComplete.
    /// </summary>
    public void PlayHint()
    {
        // Ask RoomManager which objective is active this round
        RoomManager rm = FindAnyObjectByType<RoomManager>();
        if (rm == null)
        {
            FindAnyObjectByType<RoomManager>()?.OnHintComplete();
            return;
        }

        ObjectiveData obj = rm.CurrentObjective;

        // Build a one-line sequence dynamically from the objective's data
        DialogueSequence hint = new DialogueSequence();
        hint.lines = new List<DialogueLine>
    {
        new DialogueLine { speaker = "Vanapagan", text = obj.hintLine }
    };

        // Play audio cue first if assigned, then dialogue
        StartCoroutine(PlayHintSequence(hint, obj.hintAudio, rm));
    }

    private IEnumerator PlayHintSequence(DialogueSequence hint, AudioClip audio, RoomManager rm)
    {
        // Play audio cue
        if (audio != null)
        {
            AudioSource.PlayClipAtPoint(audio, Camera.main.transform.position);
            yield return new WaitForSeconds(audio.length + 0.3f);
        }

        // Then play the dialogue
        StartDialogue(hint, rm.OnHintComplete);
    }

    /// <summary>
    /// Called by GameManager.OnMinigameResolved (wire in Inspector).
    /// Plays Vanapagan's post-task reaction, then calls GameManager.OnDialogueFinished.
    /// </summary>
    public void PlayPostTask(bool taskSucceeded)
    {
        Debug.Log($"PlayPostTask called — taskSucceeded={taskSucceeded}, tasksCompleted={GameManager.Instance.TasksCompleted}");

        int taskIndex = GameManager.Instance.TasksCompleted - 1;
        Debug.Log($"PlayPostTask — taskIndex={taskIndex}, postTaskSequences.Count={postTaskSequences.Count}");

        if (taskIndex < 0 || taskIndex >= postTaskSequences.Count)
        {
            Debug.LogWarning("PlayPostTask — no sequence found, calling OnDialogueFinished directly");
            GameManager.Instance.OnDialogueFinished();
            return;
        }

        if (postTaskSequences[taskIndex].lines == null || postTaskSequences[taskIndex].lines.Count == 0)
        {
            Debug.LogWarning("PlayPostTask — sequence has no lines, calling OnDialogueFinished directly");
            GameManager.Instance.OnDialogueFinished();
            return;
        }

        Debug.Log($"PlayPostTask — starting sequence '{postTaskSequences[taskIndex].sequenceName}'");
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

        Debug.Log($"StartDialogue — setting panel active, lines count={_lines.Count}");
        dialoguePanel.SetActive(true);
        Debug.Log($"StartDialogue — panel active={dialoguePanel.activeSelf}");
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        Debug.Log($"ShowCurrentLine — lineIndex={_lineIndex}");
        if (_lineIndex >= _lines.Count)
        {
            Debug.Log("ShowCurrentLine — no more lines, ending dialogue");
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
        Debug.Log($"ShowCurrentLine — started TypeLine coroutine for: '{line.text}'");
    }

    private IEnumerator TypeLine(string text)
    {
        Debug.Log("TypeLine — coroutine started");
        bodyText.text = "";
        foreach (char c in text)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        Debug.Log("TypeLine — finished typing, waiting for input");
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
            Debug.Log("TypeLine — waitingForInput = true, click or space to advance");
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