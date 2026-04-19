using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimonSaysMinigame : Minigame
{
    public static SimonSaysMinigame Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private List<SimonSaysButton> buttons = new();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("Timing")]
    [SerializeField] private float previewDelay = 1f;
    [SerializeField] private float betweenFlashes = 0.15f;
    [SerializeField] private float betweenRounds = 1.2f;

    private static readonly int[] RoundLengths = { 2, 3, 4 };

    private List<SignalType> _sequence = new();
    private int _playerIndex = 0;
    private bool _playerTurn = false;
    private int _currentRound = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ── Minigame overrides ────────────────────────────────────────────

    protected override void OnBegin()
    {
        _currentRound = 0;
        SetAllInteractable(false);
        StartCoroutine(RunRound());
    }

    protected override void OnCleanup()
    {
        StopAllCoroutines();
        SetAllInteractable(false);
        _sequence.Clear();
        _playerIndex = 0;
        _playerTurn = false;
        _currentRound = 0;
    }

    // ── Round runner ──────────────────────────────────────────────────

    private IEnumerator RunRound()
    {
        SetStatus("Watch carefully...");
        SetAllInteractable(false);
        GenerateSequence(RoundLengths[_currentRound]);

        yield return new WaitForSeconds(previewDelay);
        yield return PlaySequence();

        SetStatus("Your turn!");
        _playerIndex = 0;
        _playerTurn = true;
        SetAllInteractable(true);
    }

    private IEnumerator PlaySequence()
    {
        foreach (SignalType signal in _sequence)
        {
            SimonSaysButton btn = GetButton(signal);
            if (btn != null) yield return btn.PlayFlash();
            yield return new WaitForSeconds(betweenFlashes);
        }
    }

    // ── Player input ──────────────────────────────────────────────────

    public void OnButtonPressed(SignalType pressed)
    {
        if (!_playerTurn) return;

        if (pressed != _sequence[_playerIndex])
        {
            MinigameCRTController.Instance?.TriggerWrongPressFlash();
            _playerTurn = false;
            SetAllInteractable(false);
            SetStatus("Wrong!");
            StartCoroutine(DelayedComplete(false, 0.8f));
            return;
        }

        _playerIndex++;
        if (_playerIndex < _sequence.Count) return;

        _playerTurn = false;
        SetAllInteractable(false);
        _currentRound++;

        if (_currentRound >= RoundLengths.Length)
        {
            SetStatus("Well done!");
            StartCoroutine(DelayedComplete(true, 0.8f));
        }
        else
        {
            SetStatus("Correct!");
            StartCoroutine(NextRoundAfterDelay());
        }
    }

    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(betweenRounds);
        StartCoroutine(RunRound());
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void GenerateSequence(int length)
    {
        _sequence.Clear();
        SignalType[] all = (SignalType[])System.Enum.GetValues(typeof(SignalType));
        for (int i = 0; i < length; i++)
            _sequence.Add(all[Random.Range(0, all.Length)]);
    }

    private SimonSaysButton GetButton(SignalType type)
        => buttons.Find(b => b.signalType == type);

    private void SetAllInteractable(bool on)
    {
        foreach (var btn in buttons) btn.SetInteractable(on);
    }

    private void SetStatus(string msg)
    {
        if (statusLabel != null) statusLabel.text = msg;
    }

    private IEnumerator DelayedComplete(bool success, float delay)
    {
        yield return new WaitForSeconds(delay);
        Complete(success);
    }
}