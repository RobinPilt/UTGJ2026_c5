using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimonSaysButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    public SignalType signalType;

    [Header("Visuals")]
    [SerializeField] private Image glowOverlay;
    [SerializeField] private float flashDuration = 0.4f;
    [SerializeField] private float dimAlpha = 0f;
    [SerializeField] private float glowAlpha = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip signalSound;

    private SimonSaysGlowController _glowController;

    private bool _interactable = false;

    private void Awake()
    {
        _glowController = GetComponent<SimonSaysGlowController>();
    }

    public IEnumerator PlayFlash()
    {
        SetGlow(true);
        if (signalSound != null) audioSource.PlayOneShot(signalSound);
        yield return new WaitForSeconds(flashDuration);
        SetGlow(false);
        yield return new WaitForSeconds(0.1f);
    }

    public void SetInteractable(bool on) => _interactable = on;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable) return;
        SimonSaysMinigame.Instance?.OnButtonPressed(signalType);
    }

    private void SetGlow(bool on)
    {
        if (_glowController != null)
            _glowController.SetGlow(on);
    }
}

public enum SignalType { Sheep, Hoe, Wolf, Wheat }