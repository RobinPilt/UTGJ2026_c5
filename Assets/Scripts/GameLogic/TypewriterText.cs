using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Standalone typewriter component. Works independently of DialogueManager.
/// Call Write() with a string and an onComplete callback.
/// </summary>
public class TypewriterText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float typeSpeed = 0.04f;

    private Coroutine _current;

    public void Write(string text, Action onComplete = null)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(TypeRoutine(text, onComplete));
    }

    public void Clear() => label.text = "";

    private IEnumerator TypeRoutine(string text, Action onComplete)
    {
        label.text = "";
        foreach (char c in text)
        {
            label.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        onComplete?.Invoke();
    }
}