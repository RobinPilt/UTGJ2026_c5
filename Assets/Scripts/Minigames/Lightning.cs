using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// REAGEERIMINE — Lightning Reaction Minigame (Minigame 2, index 1 in MinigameManager)
///
/// GAMEPLAY:
///   Lightning strikes the powerline at a random interval (1–5 s).
///   The player must click the switch within 1 second of the strike.
///   Three successful reactions complete the minigame.
///   Three total misses (too late OR too early) = failure.
///
/// CO-DEV RULES FOLLOWED:
///   ✔ Inherits from Minigame, not MonoBehaviour.
///   ✔ All startup logic is in OnBegin(); all reset logic is in OnCleanup().
///   ✔ Calls Complete(true) on win, Complete(false) on loss — nothing else.
///   ✔ Never calls GameManager directly.
///   ✔ Never touches gameObject.SetActive() on this panel (Begin/End do that).
///   ✔ Does not care what happens after Complete() — MinigameManager handles it.
///
/// UNITY SETUP:
///   1. Create a UI panel in the Canvas, attach this script to it.
///   2. Drag the panel into MinigameManager → Minigames list at index 1.
///   3. Wire up the [SerializeField] fields in the Inspector.
///
/// CANVAS HIERARCHY SUGGESTION:
///   MinigamePanel  ← this script lives here
///   ├── Background      (dark Image, fills panel)
///   ├── Title           (TMP label — "REAGEERIMINE")
///   ├── StageLabel      (TMP — assign → stageLabel)
///   ├── FeedbackLabel   (TMP — assign → feedbackLabel)
///   ├── LightningBolt   (Image of bolt, starts disabled — assign → lightningBolt)
///   ├── ScreenFlash     (full-panel white Image, starts disabled — assign → screenFlash)
///   └── Switch          (Button on the powerline segment — assign → switchButton)
/// </summary>
class Minigame3 : Minigame
{
    // ── Inspector References ──────────────────────────────────────────

    [Header("UI — Stage & Feedback")]
    [Tooltip("TextMeshPro showing current stage, e.g. 'ETAPP 1 / 3'.")]
    [SerializeField] private TextMeshProUGUI stageLabel;

    [Tooltip("TextMeshPro for timed messages: too early, too late, success, etc.")]
    [SerializeField] private TextMeshProUGUI feedbackLabel;

    [Header("UI — Lightning Visuals")]
    [Tooltip("Image of the lightning bolt. Starts disabled; script enables it on each strike.")]
    [SerializeField] private Image lightningBolt;

    [Tooltip("Full-panel white Image used for the camera-flash effect. Starts disabled.")]
    [SerializeField] private Image screenFlash;

    [Header("UI — Player Input")]
    [Tooltip("The Button the player clicks — represents the powerline switch.")]
    [SerializeField] private Button switchButton;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   thunderSFX;
    [SerializeField] private AudioClip   successSFX;
    [SerializeField] private AudioClip   failSFX;

    [Header("Tuning")]
    [Tooltip("Minimum seconds between lightning strikes.")]
    [SerializeField] private float minInterval = 1f;

    [Tooltip("Maximum seconds between lightning strikes.")]
    [SerializeField] private float maxInterval = 5f;

    [Tooltip("How long the player has to click after a strike (seconds).")]
    [SerializeField] private float reactionWindow = 1f;

    [Tooltip("How long the lightning bolt + screen flash remain visible.")]
    [SerializeField] private float lightningVisibleDuration = 0.15f;

    [Tooltip("Successful reactions needed to finish (one per stage).")]
    [SerializeField] private int totalStages = 3;

    [Tooltip("Total misses (too early OR too late) allowed before failing.")]
    [SerializeField] private int maxMisses = 3;

    // ── Private State ─────────────────────────────────────────────────

    private int      _currentStage;       // successes so far (0 → totalStages)
    private int      _missCount;          // total mistakes so far
    private bool     _strikeActive;       // true = reaction window is open
    private bool     _lightningPending;   // true = we are in the pre-strike wait, no click allowed
    private float    _strikeTime;         // Time.time when the strike fired
    private Coroutine _gameLoop;

    // ── Minigame Overrides ────────────────────────────────────────────

    protected override void OnBegin()
    {
        // Reset all state
        _currentStage    = 0;
        _missCount       = 0;
        _strikeActive    = false;
        _lightningPending = false;

        // Hide dynamic visuals
        SetActive(lightningBolt, false);
        SetActive(screenFlash,   false);
        SetText(feedbackLabel, "");

        // Hook up the switch button
        if (switchButton != null)
        {
            switchButton.onClick.RemoveAllListeners();
            switchButton.onClick.AddListener(OnSwitchClicked);
            SetSwitchHighlight(false);
        }

        UpdateStageLabel();
        _gameLoop = StartCoroutine(GameLoop());
    }

    protected override void OnCleanup()
    {
        // Stop the coroutine and clear listeners so the panel is clean for a replay
        if (_gameLoop != null)
        {
            StopCoroutine(_gameLoop);
            _gameLoop = null;
        }

        _strikeActive    = false;
        _lightningPending = false;

        if (switchButton != null) switchButton.onClick.RemoveAllListeners();

        SetActive(lightningBolt, false);
        SetActive(screenFlash,   false);
    }

    // ── Core Game Loop ────────────────────────────────────────────────

    private IEnumerator GameLoop()
    {
        while (_currentStage < totalStages)
        {
            // ── 1. Wait a random interval before the next strike ─────
            _lightningPending = true;
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);
            _lightningPending = false;

            // ── 2. Play the lightning strike ─────────────────────────
            yield return StartCoroutine(PlayLightningStrike());

            // ── 3. Open the reaction window ───────────────────────────
            _strikeActive = true;
            _strikeTime   = Time.time;
            SetSwitchHighlight(true);   // visual cue: switch is now "live"

            yield return new WaitForSeconds(reactionWindow);

            // ── 4. Check if player reacted in time ────────────────────
            if (_strikeActive)          // still true → no click arrived
            {
                _strikeActive = false;
                SetSwitchHighlight(false);
                RegisterMiss("LIIGA HILJA!");   // "TOO LATE!"

                if (_missCount >= maxMisses)
                {
                    yield return StartCoroutine(ShowFeedback("EBAÕNNESTUS!", 1.2f)); // "FAILED!"
                    Complete(false);
                    yield break;
                }

                yield return StartCoroutine(ShowFeedback("LIIGA HILJA!", 0.8f));
            }
            // If _strikeActive is false here, OnSwitchClicked already handled it.
        }
    }

    // ── Lightning Visual ──────────────────────────────────────────────

    private IEnumerator PlayLightningStrike()
    {
        // Full-panel white flash
        if (screenFlash != null)
        {
            screenFlash.gameObject.SetActive(true);
            screenFlash.color = new Color(1f, 1f, 1f, 0.9f);
        }

        // Show bolt sprite
        SetActive(lightningBolt, true);

        // Play thunder sound
        PlaySound(thunderSFX);

        yield return new WaitForSeconds(lightningVisibleDuration);

        // Clear visuals — reaction window opens straight after this returns
        SetActive(screenFlash,   false);
        SetActive(lightningBolt, false);
    }

    // ── Player Input ──────────────────────────────────────────────────

    private void OnSwitchClicked()
    {
        // ── Early click (before lightning) ────────────────────────────
        if (_lightningPending)
        {
            RegisterMiss("LIIGA VARA!"); // "TOO EARLY!"

            if (_missCount >= maxMisses)
            {
                // Stop the loop and fail immediately
                if (_gameLoop != null) StopCoroutine(_gameLoop);
                StartCoroutine(EarlyFailSequence());
            }
            return;
        }

        // ── Valid reaction window ─────────────────────────────────────
        if (!_strikeActive) return;     // window not open, ignore stray clicks

        float reactionTime = Time.time - _strikeTime;
        _strikeActive = false;          // consume the window so GameLoop doesn't double-penalise
        SetSwitchHighlight(false);

        // Within window → success
        if (reactionTime <= reactionWindow)
        {
            PlaySound(successSFX);
            _currentStage++;
            UpdateStageLabel();

            if (_currentStage >= totalStages)
                StartCoroutine(FinishSuccess());
            else
                StartCoroutine(ShowFeedback($"ETAPP {_currentStage} / {totalStages}!", 0.8f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>Records a miss and plays the fail sound.</summary>
    private void RegisterMiss(string reason)
    {
        _missCount++;
        PlaySound(failSFX);
        Debug.Log($"[Lightning] Miss #{_missCount}: {reason}");
    }

    private IEnumerator EarlyFailSequence()
    {
        yield return StartCoroutine(ShowFeedback("LIIGA VARA! EBAÕNNESTUS!", 1.2f));
        Complete(false);
    }

    private IEnumerator FinishSuccess()
    {
        yield return StartCoroutine(ShowFeedback("VALMIS!", 1.2f));  // "DONE!"
        Complete(true);
    }

    /// <summary>
    /// Displays a message on feedbackLabel for <duration> seconds, then clears it.
    /// Safe if feedbackLabel is unassigned.
    /// </summary>
    private IEnumerator ShowFeedback(string message, float duration)
    {
        SetText(feedbackLabel, message);
        yield return new WaitForSeconds(duration);
        SetText(feedbackLabel, "");
    }

    /// <summary>Updates the stage counter text.</summary>
    private void UpdateStageLabel()
    {
        SetText(stageLabel, $"ETAPP: {Mathf.Min(_currentStage + 1, totalStages)} / {totalStages}");
    }

    /// <summary>Tints the switch button to signal the reaction window is open.</summary>
    private void SetSwitchHighlight(bool active)
    {
        if (switchButton == null) return;

        ColorBlock cb = switchButton.colors;
        cb.normalColor = active
            ? new Color(1f, 0.85f, 0.1f, 1f)   // bright yellow = "click now!"
            : new Color(1f, 1f,    1f,   1f);   // default white
        switchButton.colors = cb;
    }

    // ── Null-safe UI wrappers ─────────────────────────────────────────

    private static void SetActive(Graphic g, bool state)
    {
        if (g != null) g.gameObject.SetActive(state);
    }

    private static void SetText(TextMeshProUGUI label, string text)
    {
        if (label != null) label.text = text;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
