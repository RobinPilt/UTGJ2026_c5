using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LightningMinigame : Minigame
{
    [Header("UI — Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackLabel;

    [Header("UI — Lightning Visuals")]
    [SerializeField] private Image lightningBolt;
    [SerializeField] private Image screenFlash;

    [Header("UI — Player Input")]
    [SerializeField] private Button switchButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip thunderSFX;
    [SerializeField] private AudioClip successSFX;
    [SerializeField] private AudioClip failSFX;

    [Header("Tuning")]
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 5f;
    [SerializeField] private float reactionWindow = 1f;
    [SerializeField] private float lightningVisibleDuration = 0.15f;
    [SerializeField] private int totalStages = 3;
    [SerializeField] private int maxMisses = 3;

    // ── Private state ─────────────────────────────────────────────────
    private int _currentStage;
    private int _missCount;
    private bool _strikeActive;
    private bool _lightningPending;
    private bool _finished;          // guard against double Complete()
    private float _strikeTime;
    private Coroutine _gameLoop;

    // ── Minigame overrides ────────────────────────────────────────────

    protected override void OnBegin()
    {
        _currentStage = 0;
        _missCount = 0;
        _strikeActive = false;
        _lightningPending = false;
        _finished = false;

        SetActive(lightningBolt, false);
        SetActive(screenFlash, false);
        SetText(feedbackLabel, "");

        if (switchButton != null)
        {
            switchButton.onClick.RemoveAllListeners();
            switchButton.onClick.AddListener(OnSwitchClicked);
            SetSwitchHighlight(false);
        }

        _gameLoop = StartCoroutine(GameLoop());
    }

    protected override void OnCleanup()
    {
        if (_gameLoop != null)
        {
            StopCoroutine(_gameLoop);
            _gameLoop = null;
        }

        _strikeActive = false;
        _lightningPending = false;
        _finished = false;

        if (switchButton != null) switchButton.onClick.RemoveAllListeners();

        SetActive(lightningBolt, false);
        SetActive(screenFlash, false);
        SetText(feedbackLabel, "");
    }

    // ── Core game loop ────────────────────────────────────────────────

    private IEnumerator GameLoop()
    {
        while (_currentStage < totalStages && !_finished)
        {
            // 1. Wait random interval — no clicks allowed yet
            _lightningPending = true;
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            _lightningPending = false;

            if (_finished) yield break;

            // 2. Lightning strikes
            yield return StartCoroutine(PlayLightningStrike());

            if (_finished) yield break;

            // 3. Open reaction window
            _strikeActive = true;
            _strikeTime = Time.time;
            SetSwitchHighlight(true);

            yield return new WaitForSeconds(reactionWindow);

            // 4. Player didn't click in time
            if (_strikeActive && !_finished)
            {
                _strikeActive = false;
                SetSwitchHighlight(false);
                _missCount++;
                PlaySound(failSFX);

                if (_missCount >= maxMisses)
                {
                    yield return StartCoroutine(ShowFeedback("TOO LATE! TRY AGAIN!", 1.2f));
                    TriggerComplete(false);
                    yield break;
                }

                yield return StartCoroutine(ShowFeedback("TOO LATE!", 0.8f));
            }
        }
    }

    // ── Lightning visual ──────────────────────────────────────────────

    private IEnumerator PlayLightningStrike()
    {
        if (screenFlash != null)
        {
            screenFlash.gameObject.SetActive(true);
            screenFlash.color = new Color(1f, 1f, 1f, 0.9f);
        }

        SetActive(lightningBolt, true);
        PlaySound(thunderSFX);

        yield return new WaitForSeconds(lightningVisibleDuration);

        SetActive(screenFlash, false);
        SetActive(lightningBolt, false);
    }

    // ── Player input ──────────────────────────────────────────────────

    private void OnSwitchClicked()
    {
        if (_finished) return;

        // Clicked before lightning — too early
        if (_lightningPending)
        {
            _missCount++;
            PlaySound(failSFX);

            if (_missCount >= maxMisses)
            {
                if (_gameLoop != null) StopCoroutine(_gameLoop);
                StartCoroutine(EarlyFailSequence());
            }
            else
            {
                StartCoroutine(ShowFeedback("TOO EARLY!", 0.8f));
            }
            return;
        }

        // Clicked during reaction window
        if (!_strikeActive) return;

        _strikeActive = false;
        SetSwitchHighlight(false);
        PlaySound(successSFX);

        _currentStage++;

        if (_currentStage >= totalStages)
        {
            // Stop the loop — we're done
            if (_gameLoop != null) StopCoroutine(_gameLoop);
            StartCoroutine(FinishSuccess());
        }
        else
        {
            StartCoroutine(ShowFeedback("GOOD!", 0.6f));
        }
    }

    // ── Completion helpers ────────────────────────────────────────────

    private IEnumerator EarlyFailSequence()
    {
        _lightningPending = false;
        yield return StartCoroutine(ShowFeedback("TOO EARLY! TRY AGAIN!", 1.2f));
        TriggerComplete(false);
    }

    private IEnumerator FinishSuccess()
    {
        yield return StartCoroutine(ShowFeedback("WELL DONE!", 1.2f));
        TriggerComplete(true);
    }

    /// <summary>Guards against Complete() being called more than once.</summary>
    private void TriggerComplete(bool success)
    {
        if (_finished) return;
        _finished = true;
        Complete(success);
    }

    // ── UI helpers ────────────────────────────────────────────────────

    private IEnumerator ShowFeedback(string message, float duration)
    {
        SetText(feedbackLabel, message);
        yield return new WaitForSeconds(duration);
        SetText(feedbackLabel, "");
    }

    private void SetSwitchHighlight(bool active)
    {
        if (switchButton == null) return;
        ColorBlock cb = switchButton.colors;
        cb.normalColor = active
            ? new Color(1f, 0.85f, 0.1f, 1f)
            : new Color(1f, 1f, 1f, 1f);
        switchButton.colors = cb;
    }

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