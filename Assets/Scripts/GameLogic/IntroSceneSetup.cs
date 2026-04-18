using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the intro cutscene sequence before the game scene loads.
/// Sequence: fade in → line 1 → glitch burst → line 2 → glitch → ... → fade out → load game
/// </summary>
public class IntroSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TypewriterText typewriter;
    [SerializeField] private GlitchEffect glitchImage;
    [SerializeField] private ScreenFader fader;          // ScreenFader on this scene's canvas

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene"; // match your scene name exactly

    [Header("Lines — Vanapagan's intro monologue")]
    [SerializeField]
    private string[] introLines = {
        "Ah. You are awake.",
        "Good. We have much to do.",
        "Pay attention to my signals.",
        "You will learn. Eventually."
    };

    [Header("Timing")]
    [SerializeField] private float pauseAfterLine = 0.8f;  // beat after each line finishes
    [SerializeField] private float glitchDuration = 0.5f;  // how long each glitch burst lasts
    [SerializeField] private float finalPause = 1.2f;  // pause before loading game scene

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(IntroSequence());
    }

    // ── Sequence ─────────────────────────────────────────────────────
    private IEnumerator IntroSequence()
    {
        // Start fully black, fade in
        yield return FadeIn();

        // Play each line with a glitch burst between them
        for (int i = 0; i < introLines.Length; i++)
        {
            yield return TypeLine(introLines[i]);
            yield return new WaitForSeconds(pauseAfterLine);

            // Glitch on every line except the last
            if (i < introLines.Length - 1)
                yield return RunGlitch();
        }

        // Final pause before leaving
        yield return new WaitForSeconds(finalPause);
        yield return FadeOut();

        // Load game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private IEnumerator TypeLine(string line)
    {
        bool done = false;
        typewriter.Write(line, () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator RunGlitch()
    {
        bool done = false;
        glitchImage.Glitch(glitchDuration, () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator FadeIn()
    {
        bool done = false;
        fader.FadeIn(() => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator FadeOut()
    {
        bool done = false;
        fader.FadeOut(() => done = true);
        yield return new WaitUntil(() => done);
    }
}